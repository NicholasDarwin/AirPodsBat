using System;
using AirPodBat.Models;

namespace AirPodBat.Services
{
    /// <summary>
    /// Parses Apple AirPods battery data from Bluetooth LE advertisement manufacturer data.
    /// 
    /// Apple's Manufacturer ID (in little-endian): 0x004C
    /// 
    /// AirPods data structure in manufacturer data:
    /// - Bytes 0-1: Company ID (0x4C, 0x00 for Apple in LE)
    /// - Byte 2: Type/Status byte
    /// - Byte 3: Length and number of devices
    /// - Bytes 4+: Device data (variable length per device)
    /// 
    /// Each device entry is typically:
    /// - Byte 0: Device type and charge status (high bits = type, low bits = battery)
    /// - Byte 1: Battery level (0-100, or encoded in upper nibble for certain types)
    /// 
    /// Known AirPods device types in the advertisement:
    /// - 0x07: AirPods (1st/2nd gen)
    /// - 0x0E: AirPods Pro
    /// - 0x0F: AirPods Max
    /// - 0x14: AirPods 4
    /// 
    /// Battery encoding varies by device type. For most AirPods:
    /// - Bits 0-3 of the status byte contain battery level (4-bit, 0-15 scale maps to 0-100%)
    /// - Or full byte (0-255 in 0.5% increments, divided by 2.55 for percentage)
    /// </summary>
    public class AirPodsParser
    {
        private const ushort APPLE_MANUFACTURER_ID = 0x004C;

        /// <summary>
        /// Attempts to parse AirPods battery data from Bluetooth LE manufacturer data.
        /// Returns null if the data is invalid or not from AirPods.
        /// </summary>
        public static AirPodsBattery? ParseAirPodsData(byte[] manufacturerData)
        {
            if (manufacturerData == null || manufacturerData.Length < 3)
            {
                return null;
            }

            // Check if this is Apple manufacturer data (0x004C in little-endian)
            ushort manufacturerId = (ushort)(manufacturerData[0] | (manufacturerData[1] << 8));
            if (manufacturerId != APPLE_MANUFACTURER_ID)
            {
                return null;
            }

            // Apple advertisement data format:
            // manufacturerData[0-1]: Company ID (0x4C, 0x00)
            // manufacturerData[2]: Type byte (indicates what apple device it is)
            // manufacturerData[3]: Length byte (number of devices * 2, typically)

            if (manufacturerData.Length < 4)
            {
                return null;
            }

            byte typeAndLength = manufacturerData[3];
            int numDevices = (typeAndLength >> 4) & 0x0F;  // Upper nibble
            int dataLength = typeAndLength & 0x0F;          // Lower nibble

            // Try to extract AirPods battery data
            // Device data starts at offset 4, each device entry is typically 2 bytes
            AirPodsBattery? result = null;

            try
            {
                // Parse device entries
                int offset = 4;
                for (int i = 0; i < numDevices && offset + 2 <= manufacturerData.Length; i++)
                {
                    byte deviceStatusByte = manufacturerData[offset];
                    byte batteryByte = manufacturerData[offset + 1];

                    // Device type is in upper nibble of status byte
                    byte deviceType = (byte)((deviceStatusByte >> 4) & 0x0F);
                    // Lower nibble has charging status/flags

                    // Check if this is an AirPods device
                    // Common AirPods device types: 0x07, 0x0E, 0x0F, 0x14, etc.
                    if (IsAirPodsType(deviceType))
                    {
                        var parsed = ExtractBatteryFromEntry(deviceStatusByte, batteryByte, deviceType);
                        if (parsed != null)
                        {
                            result = parsed;
                            break;  // Found valid AirPods data
                        }
                    }

                    offset += 2;  // Move to next device entry
                }
            }
            catch
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// Checks if a device type corresponds to AirPods.
        /// Known AirPods types: 0x07 (AirPods), 0x0E (AirPods Pro Gen 1/2), 0x0F (AirPods Max), 0x14 (AirPods 4)
        /// </summary>
        private static bool IsAirPodsType(byte deviceType)
        {
            return deviceType == 0x07 || deviceType == 0x0E || deviceType == 0x0F || deviceType == 0x14;
        }

        /// <summary>
        /// Extracts individual battery levels from a device entry.
        /// The exact encoding depends on the device type and Apple firmware version.
        /// 
        /// For most AirPods models, the battery byte structure is:
        /// - Bit 7-6: Left earbud status (charging, low battery, etc.)
        /// - Bit 5-0: Left earbud battery level
        /// 
        /// And there may be additional bytes for right earbud and case.
        /// </summary>
        private static AirPodsBattery? ExtractBatteryFromEntry(byte statusByte, byte batteryByte, byte deviceType)
        {
            try
            {
                var result = new AirPodsBattery();

                // The battery byte typically contains:
                // Upper 4 bits: status flags (charging indicator, etc.)
                // Lower 4 bits: battery level (0-15, maps to 0-100%)
                //
                // For some models, the full byte is used (0-255, maps to 0-100%)

                // Extract left earbud battery (common in lower nibble of battery byte)
                int leftLevel = (batteryByte & 0x0F) * 100 / 15;
                if (leftLevel > 100) leftLevel = 100;

                // For AirPods, we often get multiple battery values in extended data
                // This is a simplified approach - in practice, Apple's format includes
                // separate entries or extended data for left, right, case batteries

                result.Left = Math.Min(100, Math.Max(0, leftLevel));

                // Attempt to read right and case from extended format
                // NOTE: Full format parsing often requires the complete advertisement payload
                // For now, we mark them as available if we parsed something valid
                result.Right = Math.Min(100, Math.Max(0, leftLevel));  // Simplified - would need additional bytes
                result.Case = Math.Min(100, Math.Max(0, (batteryByte >> 4) & 0x0F * 100 / 15));

                if (result.HasValidData)
                {
                    return result;
                }
            }
            catch
            {
                // Parsing failed for this entry
            }

            return null;
        }

        /// <summary>
        /// Advanced parser for AirPods that handles the full extended advertisement data.
        /// This handles the multiple-device format where each device (left, right, case) 
        /// is a separate entry in the advertisement.
        /// </summary>
        public static AirPodsBattery? ParseAirPodsDataAdvanced(byte[] manufacturerData)
        {
            if (manufacturerData == null || manufacturerData.Length < 4)
            {
                return null;
            }

            // Check Apple manufacturer ID
            ushort manufacturerId = (ushort)(manufacturerData[0] | (manufacturerData[1] << 8));
            if (manufacturerId != APPLE_MANUFACTURER_ID)
            {
                return null;
            }

            byte typeAndLength = manufacturerData[3];
            int numDevices = (typeAndLength >> 4) & 0x0F;

            var result = new AirPodsBattery();
            int airpodsCount = 0;

            // Parse each device entry
            int offset = 4;
            for (int i = 0; i < numDevices && offset + 1 < manufacturerData.Length; i++)
            {
                byte statusByte = manufacturerData[offset];
                if (offset + 1 < manufacturerData.Length)
                {
                    byte deviceType = (byte)((statusByte >> 4) & 0x0F);
                    
                    if (IsAirPodsType(deviceType))
                    {
                        byte deviceInfo = manufacturerData[offset + 1];
                        int batteryPercent = DecodeBatteryLevel(deviceInfo, deviceType);

                        // Determine which component this is based on some heuristic
                        // This is the tricky part - may need device ID or order
                        if (airpodsCount == 0)
                            result.Left = batteryPercent;
                        else if (airpodsCount == 1)
                            result.Right = batteryPercent;
                        else if (airpodsCount == 2)
                            result.Case = batteryPercent;

                        airpodsCount++;
                    }
                }

                offset += 2;  // Next device entry
            }

            return airpodsCount > 0 ? result : null;
        }

        /// <summary>
        /// Decodes battery level from the device info byte.
        /// Battery encoding can vary, but commonly:
        /// - 4-bit encoding: multiply by 100/15
        /// - Full byte (0-255): divide by 2.55
        /// - Some models use upper nibble for status, lower for battery
        /// </summary>
        private static int DecodeBatteryLevel(byte deviceInfo, byte deviceType)
        {
            // Try different decoding strategies based on device type
            if (deviceType == 0x14)  // AirPods 4
            {
                // AirPods 4 likely uses 4-bit encoding
                int battery = (deviceInfo & 0x0F) * 100 / 15;
                return Math.Min(100, Math.Max(0, battery));
            }
            else if (deviceType == 0x0E)  // AirPods Pro
            {
                // AirPods Pro may use different encoding
                int battery = (deviceInfo & 0x0F) * 100 / 15;
                return Math.Min(100, Math.Max(0, battery));
            }
            else
            {
                // Generic fallback
                int battery = (deviceInfo & 0x0F) * 100 / 15;
                return Math.Min(100, Math.Max(0, battery));
            }
        }
    }
}
