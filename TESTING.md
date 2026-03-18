# Testing & Verification Guide

## Overview

This guide helps you verify that the AirPods battery parsing is working correctly with real data.

## Quick Start Verification

### 1. Check if Bluetooth is Working

**In Windows Settings:**
1. Settings → Bluetooth & devices → Bluetooth
2. Ensure **Bluetooth is turned ON**
3. Your AirPods should appear in the "Paired devices" or "Other devices" list

**In PowerShell (optional):**
```powershell
Get-PnpDevice | Where-Object {$_.Name -like "*Bluetooth*"}
```

### 2. Run the Application with Debug Output

**From Visual Studio:**
1. Build the solution (Ctrl+Shift+B)
2. Press **F5** to run with debugging
3. Open Debug → Windows → Output
4. Select "Debug" in the dropdown filter

**From Command Line:**
```powershell
cd c:\Users\UC\Desktop\AirPodBat
dotnet run
```

### 3. Bring AirPods Near Your Computer

- Turn on AirPods
- Bring AirPods case within 3-5 meters of your computer
- Watch for messages in Debug Output

### 4. Expected Output (Success)

```
Bluetooth LE scanning started
Advertisement received: 4C 00 02 25 07 42 07 68 07 54
AirPods detected: L:13% R:53% C:26%
AirPods detected: L:13% R:53% C:27%  (slight variation is normal)
```

### 5. Expected Output (No AirPods)

```
Bluetooth LE scanning started
(No AirPods messages = not detected)
```

## Detailed Testing Steps

### Scenario 1: Verify Battery Display

**Setup:**
1. Ensure AirPods are powered on
2. Charge AirPods to known levels (e.g., 100%, 50%, 10%)
3. Check battery in iOS Settings (Settings → Bluetooth → i icon next to AirPods)

**Test:**
1. Run the app
2. Bring AirPods near the computer
3. Compare app display with iOS battery levels

**Success Criteria:**
- ✓ Battery percentages match iOS (within 1-5%)
- ✓ Values update when battery changes
- ✓ No crashes or errors

### Scenario 2: Test Multiple Readings

**Steps:**
1. Run the app with Debug Output visible
2. Each received AirPods advertisement appears as one line
3. Watch the battery values over 10-15 seconds

**Example Output:**
```
AirPods detected: L:85% R:82% C:95%
AirPods detected: L:85% R:82% C:95%  (same)
AirPods detected: L:85% R:83% C:95%  (R changed by 1%)
AirPods detected: L:85% R:82% C:95%  (back to previous)
```

**What this tells you:**
- Some variance is normal (±1-2%)
- Consistent values = parser working correctly
- Large jumps (±10%) = may need investigation

### Scenario 3: Test Left/Right Assignment

This is the **most critical verification** because the parser assumes device sequence order.

**Steps:**
1. Get battery % from iOS for each component:
   - Left earbud: **XX%**
   - Right earbud: **YY%**
   - Case: **ZZ%**

2. Run app and record displayed values (wait 5-10 seconds for stable reading)

3. Compare:

| Component | iOS | App | Match? |
|-----------|-----|-----|--------|
| Left      | 85% | 85% | ✓      |
| Right     | 82% | 82% | ✓      |
| Case      | 95% | 95% | ✓      |

**If values DON'T match:**
- Left and Right might be swapped
- Case might be in wrong position
- Parser decoding might be incorrect for your AirPods model

**Next steps** (see "Debugging Mismatches" below)

### Scenario 4: Test Long-Term Stability

**Run time:** 30+ minutes

**Monitor:**
1. Does app stay Running? (no crashes)
2. Do values update smoothly?
3. Does app consume excessive CPU?

**In Task Manager:**
1. Open Task Manager (Ctrl+Shift+Esc)
2. Find "AirPodBat.exe" process
3. Check "CPU %" (should be <1% most of the time)
4. Check "Memory" (should be <100 MB)

**Success Criteria:**
- ✓ No crashes over 30 minutes
- ✓ Battery updates when actual battery changes
- ✓ Low CPU and memory usage

## Debugging Mismatches

If battery values don't match what you see in iOS:

### Step 1: Enable Detailed Logging

Edit `Services/BluetoothService.cs`:

```csharp
private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, 
    BluetoothLEAdvertisementReceivedEventArgs eventArgs)
{
    try
    {
        foreach (var manufacturerData in eventArgs.Advertisement.ManufacturerData)
        {
            var data = manufacturerData.Data.ToArray();
            
            // ADD THIS: Log raw hex bytes
            string hexData = BitConverter.ToString(data);
            Debug.WriteLine($"[RAW] {hexData}");
            
            var batteryData = AirPodsParser.ParseAirPodsData(data);
            
            if (batteryData != null)
            {
                // ADD THIS: Log parsed values
                Debug.WriteLine($"[PARSED] L:{batteryData.Left}% R:{batteryData.Right}% C:{batteryData.Case}%");
                
                if (!IsSameBatteryData(_lastBatteryData, batteryData))
                {
                    _lastBatteryData = batteryData;
                    OnAirPodsDataReceived(batteryData);
                    Debug.WriteLine($"[UPDATE] New values detected");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error processing advertisement: {ex}");
    }
}
```

### Step 2: Capture Real Data

With detailed logging, you'll see:

```
[RAW] 4C-00-02-25-07-42-07-68-07-54
[PARSED] L:13% R:53% C:26%
[UPDATE] New values detected
```

### Step 3: Manually Decode Bytes

Using the reference in [AIRPODS_PARSING.md](AIRPODS_PARSING.md):

```
Raw:  4C-00 02 25 | 07-42 | 07-68 | 07-54
      Mfg  Type   | Dev1  | Dev2  | Dev3
           
           Len/Count = 0x25 → 2 devices, 5 bytes
           
Device 1: Status=0x07, Battery=0x42
  - Type: 0x07 >> 4 = 0 → AirPods
  - Battery: 0x42 & 0x0F = 0x2 → 2 * 100 / 15 = 13%

Device 2: Status=0x07, Battery=0x68
  - Battery: 0x68 & 0x0F = 0x8 → 8 * 100 / 15 = 53%

Device 3: Status=0x07, Battery=0x54
  - Battery: 0x54 & 0x0F = 0x4 → 4 * 100 / 15 = 26%
```

### Step 4: Compare with iOS

| Device # | Decoded % | iOS L | iOS R | iOS C | Matches? |
|----------|-----------|-------|-------|-------|----------|
| 1        | 13%       | 85%   | 82%   | 95%   | NO       |
| 2        | 53%       | 85%   | 82%   | 95%   | NO       |
| 3        | 26%       | 85%   | 82%   | 95%   | NO       |

**Analysis:** Values don't match any iOS reading → **Parser bug**

**Solution:** 
1. Check byte offset documentation in parser
2. Try alternative decoding formula (8-bit instead of 4-bit)
3. Verify Apple manufacturer ID (should be 0x004C)

### Step 5: Alternative Decoding

If 4-bit decoding doesn't work, try 8-bit:

Edit `Services/AirPodsParser.cs`, method `DecodeBatteryLevel()`:

```csharp
private static int DecodeBatteryLevel(byte deviceInfo, byte deviceType)
{
    // Try 8-bit decoding instead of 4-bit
    int battery = (deviceInfo * 100) / 255;  // Full byte scaling
    return Math.Min(100, Math.Max(0, battery));
}
```

Then re-run and check if values match better.

### Step 6: Document Findings

If you discover the correct decoding, create an issue with:
- Actual iOS battery levels
- Raw hex advertisement data
- Your AirPods model (Gen 1/2, Pro, 4, Max)
- Decoded values
- What decoding formula worked

This helps improve the parser for everyone!

## Testing with Multiple AirPods Models

Each AirPods generation may encode data differently.

### AirPods 4
- Expected Type Code: 0x14
- Likely Encoding: 4-bit battery (0-15 scale)
- Reference: Current parser implementation

### AirPods Pro
- Expected Type Code: 0x0E
- Likely Encoding: 4-bit battery (0-15 scale)
- Note: May have different byte structure

### AirPods Max
- Expected Type Code: 0x0F
- Likely Encoding: Unknown (needs testing)
- Note: More complex data, potentially different format

### AirPods (1st/2nd Gen)
- Expected Type Code: 0x07
- Likely Encoding: Unknown
- Note: Older format, may differ significantly

**If you test with other models**, please document:
1. Model name and generation
2. Raw advertisement bytes
3. Expected vs. actual values
4. What encoding worked

## Automated Testing (Future Enhancement)

```csharp
[TestClass]
public class AirPodsParserTests
{
    [TestMethod]
    public void ParseAirPods4_85_82_95Percent()
    {
        // Real captured data from AirPods 4 with 85/82/95%
        byte[] data = new byte[] { 0x4C, 0x00, 0x02, 0x25, 
                                   0x07, 0xD5, 0x07, 0xD2, 0x07, 0xDF };
        
        var result = AirPodsParser.ParseAirPodsData(data);
        
        Assert.IsNotNull(result);
        Assert.AreEqual(85, result.Left);
        Assert.AreEqual(82, result.Right);
        Assert.AreEqual(95, result.Case);
    }
    
    [TestMethod]
    public void ParseInvalidData_ReturnsNull()
    {
        byte[] data = new byte[] { 0xFF, 0xFF };  // Not Apple
        var result = AirPodsParser.ParseAirPodsData(data);
        Assert.IsNull(result);
    }
}
```

## Performance Testing

### CPU Usage
Monitor in Task Manager while:
- Running 30+ minutes
- Scrolling other windows (while app in background)
- Playing audio from different apps

**Acceptable range:** <5% CPU average, <1% idle

### Memory Usage
Monitor in Task Manager:

**Acceptable range:**
- Initial: ~50-80 MB
- After 30 min: same (no leak)
- Peak: <200 MB

If memory grows continuously → memory leak in Bluetooth service

### Battery Drain
Leave app running 1+ hour:

**Measure:** Check laptop battery drain rate with app vs. without

**Note:** Active BLE scanning does consume power. If concerned, can switch to passive scanning mode (see line in BluetoothService.cs).

## Reporting Issues

If you find bugs, provide:

1. **AirPods Model**: AirPods 4 / Pro / Max / Gen 2, etc.
2. **Raw Data**: Hex bytes from Debug Output
3. **Expected Values**: iOS battery levels
4. **Actual Values**: App display
5. **Steps to Reproduce**: How to make it happen again
6. **Environment**: Windows 10 build, .NET version

Example:
```
Model: AirPods 4
Raw:   4C-00-02-25-07-42-07-68-07-54
iOS:   Left 85%, Right 82%, Case 95%
App:   Left 13%, Right 53%, Case 26%
Step:  Turn on AirPods near computer, run app, wait 10 seconds
```

## Verification Checklist

- [ ] Bluetooth enabled in Windows
- [ ] AirPods powered on and in range
- [ ] App starts without errors
- [ ] Debug Output shows "Bluetooth scanning started"
- [ ] After 10-15 seconds, AirPods detected message appears
- [ ] Battery percentages display in overlay window
- [ ] Percentages roughly match iOS Settings
- [ ] values update when battery changes
- [ ] No excessive CPU/memory usage
- [ ] App runs stable for 30+ minutes

