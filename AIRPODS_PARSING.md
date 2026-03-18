# AirPods Battery Parsing - Technical Reference

## Overview

This document provides detailed information about parsing Apple AirPods battery levels from Bluetooth Low Energy (BLE) advertisements, including the known data format, byte offsets, and decoding strategies.

## Critical Implementation: AirPodsParser Class

The `AirPodsParser` class in `Services/AirPodsParser.cs` handles all battery data extraction.

**Main public function:**
```csharp
public static AirPodsBattery? ParseAirPodsData(byte[] manufacturerData)
```

Returns:
- `AirPodsBattery` object with Left, Right, Case battery percentages
- `null` if data is invalid or not from AirPods

## BLE Manufacturer Data Structure

### Manufacturer Data Frame Layout

```
BYTE    0    1       2        3           4      5      6     7    ...
      │ 0x4C │ 0x00 │ Type   │ Len/Count │ Dev1  Dev1   Dev2  Dev2 ...
      │      │      │        │ [4][4]    │ Type  Info  Type  Info
      └──────┴──────┴────────┴───────────┴──────────────────────────
        Mfg ID    Sub       Devices
        (Apple)   Type
```

### Field Descriptions

| Field | Bytes | Description | Example |
|-------|-------|-------------|---------|
| **Manufacturer ID** | 0-1 | Apple's ID (little-endian) | 0x4C, 0x00 |
| **Type Byte** | 2 | Device class/type | 0x02 (typical for AirPods) |
| **Length/Count** | 3 | Upper nibble: device count; Lower nibble: data length | 0x25 (2 devices, data len 5) |
| **Device Entries** | 4+ | 2 bytes per device: Type/Status + Battery/Charge | Variable |

## Device Entry Structure (2 bytes per device)

```
BYTE    N      N+1
      │ Type  │ Info
      │ [4]   │ [4]
      │ [4]   │ [4]
      └───────┴─────────
       Status  Battery
```

### Byte Breakdown

**Device Type/Status Byte (Byte N):**
```
Bit: 7 6 5 4 | 3 2 1 0
     -------   -------
     Type      Status/Reserved
```

- **Upper 4 bits (7-4)**: Device type (0x07, 0x0E, 0x0F, 0x14)
- **Lower 4 bits (3-0)**: Charging status, low battery warning, etc.

**Battery Info Byte (Byte N+1):**
```
Bit: 7 6 | 5 4 3 2 1 0
     ---   --------
     Flag  Battery Level
```

- **Upper 2 bits (7-6)**: Charging status, low battery flag
- **Lower 6 bits (5-0)**: Battery level (0-63, or may be 4-bit: 0-15)

## Decoding Battery Level

### Standard 4-bit Encoding (Most Common)

Used by AirPods 4, AirPods Pro:

```csharp
// Extract lower 4 bits and scale to 0-100%
int battery = (byte & 0x0F) * 100 / 15;
```

**Examples:**
- `0x0` → 0%
- `0x7` → 46%
- `0xF` → 100%

### 8-bit Encoding (Alternative)

Some devices or firmware versions may use full byte:

```csharp
// Use full byte value scaled to 0-100%
int battery = (byte * 100) / 255;
```

**Examples:**
- `0x00` → 0%
- `0x80` → 50%
- `0xFF` → 100%

## Real-World Example

### Capture 1: AirPods 4 Advertisement

```
Raw bytes:
48 00 | 4C 00 | 02 | 25 | 07 42 | 07 68 | 07 54

Parsed:
└────┘ └────┘ └──┘ └──┘ └────┘ └────┘ └────┘
[ignored]Mfg  Type Cnt  Dev1   Dev2   Dev3
```

**Analysis:**
- Manufacturer: 0x004C (Apple) ✓
- Type: 0x02 (standard)
- Length/Count: 0x25 = 0x2 (2 devices) | 0x5 (data len 5 bytes)
- Device 1: Type=0x07, Battery=0x42
  - Type: `0x07 >> 4 = 0` → Device type 0 (AirPods)
  - Battery: `0x42 & 0x0F = 0x2` → 2 * 100 / 15 = 13%
  - **Charge Rate**: `0x42 >> 6 = 1` → Charging
- Device 2: Type=0x07, Battery=0x68
  - Type: 0x07 >> 4 = 0 → Device type 0
  - Battery: 0x68 & 0x0F = 0x8 → 8 * 100 / 15 = 53%
- Device 3: Type=0x07, Battery=0x54
  - Type: 0x07 >> 4 = 0 → Device type 0
  - Battery: 0x54 & 0x0F = 0x4 → 4 * 100 / 15 = 26%

**Interpretation:**
Device 1 @ 13% (charging), Device 2 @ 53%, Device 3 @ 26%
→ Left earbud, Right earbud, Case (order may vary)

## Multiple Devices (Extended Format)

AirPods may transmit multiple advertisement entries in a single packet:

```
┌─ Manufacturer Data #1 ─────────────┐
│ Mfg ID | Type | Count | Device 1   │
└────────────────────────────────────┘
┌─ Manufacturer Data #2 ─────────────┐
│ Mfg ID | Type | Count | Device 2   │
└────────────────────────────────────┘
```

The parser loops through `BluetoothLEAdvertisement.ManufacturerData` collection.

## Device Type Identification

```csharp
// Extract device type from upper nibble
byte deviceType = (byte)((statusByte >> 4) & 0x0F);

// Map to AirPods model
switch (deviceType)
{
    case 0x07: // AirPods (1st/2nd Gen), AirPods 3
    case 0x0E: // AirPods Pro (Gen 1/2)
    case 0x0F: // AirPods Max
    case 0x14: // AirPods 4
        // Valid AirPods
        break;
    default:
        // Not AirPods or unknown model
        break;
}
```

## Left/Right/Case Identification

**Challenge**: The BLE advertisement doesn't explicitly label which device is left, right, or case.

**Strategies (in order of reliability)**:

1. **Device Sequence**: Assume specific order (common: Left, Right, Case)
   - First entry → Left
   - Second entry → Right
   - Third entry → Case

2. **Device ID/Address Matching**: Pair individual AirPods addresses with known orientations
   - Requires one-time setup/pairing
   - Store in persistent config

3. **Battery Threshold Heuristics**:
   - Case battery usually differs from earbuds
   - Earbuds might be closer in battery %
   - (Unreliable, not recommended)

4. **Charging Status Flags**:
   - Upper bits of battery byte indicate if device is charging
   - May help infer role in some cases
   - (Limited reliability)

**Current Implementation**: Assumes sequence (first=left, second=right, third=case)

This may need adjustment based on real-world testing.

## Byte Offset Reference Table

| Component | Offset | Bytes | Purpose |
|-----------|--------|-------|---------|
| Manufacturer ID | 0-1 | 2 | Apple ID (0x4C, 0x00) |
| Type Byte | 2 | 1 | Device class |
| Length/Count | 3 | 1 | Device count & data length |
| Device 1 Type/Status | 4 | 1 | Type (upper nibble), status (lower) |
| Device 1 Battery | 5 | 1 | Battery level & charge status |
| Device 2 Type/Status | 6 | 1 | Second device type |
| Device 2 Battery | 7 | 1 | Second device battery |
| Device 3 Type/Status | 8 | 1 | Third device (e.g., case) |
| Device 3 Battery | 9 | 1 | Case battery level |

## Implementation Status

- ✅ Apple manufacturer ID detection (0x004C)
- ✅ Device count extraction
- ✅ Device type identification (0x07, 0x0E, 0x0F, 0x14)
- ✅ Basic 4-bit battery decoding
- ⚠️ Left/Right/Case assignment (assumes sequence, needs verification)
- ⚠️ Charging status extraction (parsed but not exposed in UI)
- 🔲 Advanced encoding schemes (8-bit, variable formats)
- 🔲 Extended advertisement data with CRC
- 🔲 Device address matching for orientation

## Testing & Verification

To verify parser correctness:

1. **Capture Real Advertisements**:
   - Use [nRF Connect](https://www.nordicsemi.com/Products/Development-tools/nrf-connect-for-mobile) (Android)
   - Or Bluetooth LE sniffing tools (Windows)

2. **Log Raw Bytes**:
   - Add Debug.WriteLine() in `OnAdvertisementReceived()`
   - Print: device MAC, timestamp, raw manufacturer data bytes

3. **Compare with Known Values**:
   - Check AirPods battery in iOS Settings
   - Compare against parsed values from this parser

4. **Validate Decoding**:
   - Manually calculate battery % from raw bytes
   - Verify matches parser output

## Improvements for Production Use

### 1. Robust Device Mapping
```csharp
private Dictionary<string, string> _deviceAddressMap = new()
{
    ["AA:BB:CC:DD:EE:FF"] = "Left",
    ["11:22:33:44:55:66"] = "Right",
    ["99:88:77:66:55:44"] = "Case"
};
```

### 2. Validate Against Multiple Captures
```csharp
// Average last N readings to smooth out noise
private Queue<AirPodsBattery> _lastReadings = new(5);

public AirPodsBattery? GetAveragedBattery()
{
    if (_lastReadings.Count == 0) return null;
    
    var avg = new AirPodsBattery
    {
        Left = (int)_lastReadings.Average(b => b.Left),
        Right = (int)_lastReadings.Average(b => b.Right),
        Case = (int)_lastReadings.Average(b => b.Case)
    };
    return avg;
}
```

### 3. Add Logging & Telemetry
```csharp
private void LogAdvertisement(byte[] data, AirPodsBattery? result)
{
    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Raw: {BitConverter.ToString(data)} → {result}");
}
```

## References

- **Bluetooth Specification**: https://www.bluetooth.com/specifications/specs/
- **Apple Reverse Engineering**: Search GitHub for "apple-airpods-battery"
- **Windows.Devices.Bluetooth.Advertisement**: Official Microsoft documentation
- **BlueZ (Linux)**: Reference implementation - https://github.com/torvalds/linux/tree/master/drivers/bluetooth

