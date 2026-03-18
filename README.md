# AirPods Battery Overlay - Windows 10 WPF Application

A real-time overlay application for Windows 10 that displays Apple AirPods 4 battery levels using Bluetooth Low Energy (BLE) scanning.

## Features

- **Real-Time Battery Display**: Shows left earbud, right earbud, and case battery percentages
- **Always-On-Top Overlay**: Compact, semi-transparent window positioned in the bottom-right corner
- **No Taskbar Icon**: Minimal system tray footprint
- **Active BLE Scanning**: Uses Windows.Devices.Bluetooth.Advertisement API
- **MVVM Architecture**: Clean separation of concerns with proper data binding
- **Event-Driven Updates**: Only updates UI when battery data changes

## System Requirements

- Windows 10 (build 19041.0 or later)
- .NET 6.0 or later
- Bluetooth 5.0+ compatible adapter
- Apple AirPods (tested with AirPods 4, compatible with AirPods Pro, AirPods Max, etc.)

## Project Structure

```
AirPodBat/
├── Models/
│   └── AirPodsBattery.cs          # Data model for battery levels
├── Services/
│   ├── BluetoothService.cs        # BLE scanning and event handling
│   └── AirPodsParser.cs           # Battery data extraction from BLE advertisements
├── ViewModels/
│   └── MainViewModel.cs           # MVVM view model with data binding
├── Views/
│   ├── MainWindow.xaml            # UI definition (borderless overlay)
│   └── MainWindow.xaml.cs         # Code-behind
├── App.xaml                       # Application definition
├── App.xaml.cs                    # Application code-behind
└── AirPodBat.csproj              # Project file

```

## Architecture

### MVVM Pattern

The application uses the Model-View-ViewModel (MVVM) pattern:

- **Model**: `AirPodsBattery` - Represents battery state
- **View**: `MainWindow.xaml` - WPF UI with data bindings
- **ViewModel**: `MainViewModel` - Exposes observable properties for UI binding

### Bluetooth Scanning Flow

```
BluetoothService
  ↓
  Uses: BluetoothLEAdvertisementWatcher (Active mode)
  ↓
  Receives: Raw BLE advertisements
  ↓
  Processes: Each ManufacturerData payload
  ↓
  Calls: AirPodsParser.ParseAirPodsData()
  ↓
  Emits: AirPodsDataReceived event with AirPodsBattery
  ↓
MainViewModel
  ↓
  Binds to: UI properties (LeftBatteryDisplay, RightBatteryDisplay, CaseBatteryDisplay)
  ↓
MainWindow (View)
  ↓
  Displays: Battery percentages in overlay
```

## Bluetooth LE and AirPods Battery Data Format

### Apple Manufacturer ID

Apple uses manufacturer ID `0x004C` in Bluetooth advertisements (stored as `4C 00` in little-endian format).

### BLE Advertisement Structure

```
Manufacturer Data Layout:
┌─────────────────────────────────────────────┐
│ Byte 0-1: Manufacturer ID (0x4C, 0x00)      │ Apple
├─────────────────────────────────────────────┤
│ Byte 2: Type/Subtype                         │ Varies by device
├─────────────────────────────────────────────┤
│ Byte 3: Length + Device Count                │ Upper nibble = device count
│         [4 bits count][4 bits length]        │ Lower nibble = data length
├─────────────────────────────────────────────┤
│ Byte 4+: Device Data (2 bytes per entry)     │
│  - Byte 0: Device Type & Status              │
│    [upper nibble: type] [lower: status]      │
│  - Byte 1: Battery/Charging Info             │
│    [upper nibble: flags] [lower: battery]    │
└─────────────────────────────────────────────┘
```

### Device Type Codes

| Type | Device |
|------|--------|
| 0x07 | AirPods (1st/2nd Gen) |
| 0x0E | AirPods Pro (Gen 1/2) |
| 0x0F | AirPods Max |
| 0x14 | AirPods 4 |

### Battery Encoding

The parser attempts to decode battery information from the device info byte:

- **4-bit encoding**: Lower 4 bits contain battery level (0-15)
  - Formula: `(byte & 0x0F) * 100 / 15` = percentage
  - Range: 0-100%

- **8-bit encoding** (some models): Full byte represents battery
  - Formula: `byte / 2.55` = percentage
  - Range: 0-100%

### Example Advertisement Data

```
Raw bytes: [4C 00 02 25 07 42 07 68 07 54]
             └─┬─┘ └─┬─┘ └─────────────────┘
             Mfg ID  Type  Device entries (3 devices)

Breakdown:
- Bytes 0-1: 4C 00 = Apple manufacturer ID
- Byte 2: 02 = Type byte
- Byte 3: 25 = 0x25 
  - Upper nibble (2): 2 devices
  - Lower nibble (5): data length
- Byte 4-5: 07 42 = Device 1 (type 0, battery 0x42)
- Byte 6-7: 07 68 = Device 2 (type 0, battery 0x68)
- Byte 8-9: 07 54 = Device 3 (type 0, battery 0x54)
```

## AirPodsParser Implementation Notes

### Critical Function

```csharp
public static AirPodsBattery? ParseAirPodsData(byte[] manufacturerData)
```

**Input**: Raw manufacturer data bytes from BLE advertisement
**Output**: `AirPodsBattery` object with Left, Right, Case battery levels, or `null` if invalid

**Key Steps**:
1. Validate minimum length (≥ 3 bytes)
2. Check Apple manufacturer ID (0x004C)
3. Extract device count from byte 3
4. Iterate through device entries
5. Identify AirPods device types
6. Decode battery levels
7. Return populated AirPodsBattery or null

### Known Limitations & TODO

The current parser is a **stub implementation** with documented byte offsets. Real AirPods data may require:

- [ ] Verify actual AirPods 4 advertisement format
- [ ] Distinguish between left/right/case from device ID or sequence
- [ ] Handle different encoding schemes per firmware version
- [ ] Support extended advertisement data with additional payloads
- [ ] Add checksum/CRC validation if needed
- [ ] Implement complete reverse-engineering of Apple's format

**To improve the parser**:

1. **Capture real AirPods advertisements** using BLE sniffer tools (e.g., nRF Connect app)
2. **Analyze byte patterns** to identify which values correspond to left/right/case
3. **Test with different AirPods models** (Pro, Max, etc.)
4. **Cross-reference** against:
   - Apple's official documentation (if available)
   - Open-source iOS reverse engineering projects
   - Linux bluez project AirPods implementations

## Building and Running

### Prerequisites

1. Install [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
2. Ensure Windows 10 (build 19041.0 or later)
3. Visual Studio 2019/2022 or VS Code with C# extension

### Build

```bash
cd c:\Users\UC\Desktop\AirPodBat
dotnet build
```

### Run

```bash
dotnet run
```

Or build and run from Visual Studio:
1. Open `AirPodBat.csproj` in Visual Studio
2. Press F5 or Ctrl+F5

## Troubleshooting

### "No AirPods detected"

- Ensure AirPods are powered on and in pairing range
- Check Windows Bluetooth settings → Devices
- Verify your system has a Bluetooth adapter
- Try stopping and restarting the app

### Window appears but shows "–" for battery levels

- Real AirPods advertisement format may differ from parser expectations
- Capture and analyze actual BLE advertisements
- Review byte offsets in `AirPodsParser.cs`
- Enable Debug output to see parsed data

### Bluetooth scanning not working

- Verify Bluetooth is enabled in Windows Settings
- Check if another app is blocking BLE scanning
- Try restarting Bluetooth adapter
- Look for error messages in Output/Debug console

### Window positioning incorrect

- The app auto-detects screen resolution
- Manual positioning available in `MainWindow.xaml` (Left/Top attributes)
- For multi-monitor setups, calculate appropriate coordinates

## Development Notes

### Adding Support for Other Apple Devices

The `IsAirPodsType()` method in `AirPodsParser` can be extended:

```csharp
private static bool IsAirPodsType(byte deviceType)
{
    return deviceType == 0x07 ||   // AirPods
           deviceType == 0x0E ||   // AirPods Pro
           deviceType == 0x0F ||   // AirPods Max
           deviceType == 0x14;     // AirPods 4
    // Add other types as discovered
}
```

### Implementing Accurate Battery Decoding

Once real advertisement data is captured, update `ExtractBatteryFromEntry()` and `DecodeBatteryLevel()` with verified byte offsets.

### Performance Considerations

- **Active Scanning**: Increases power consumption but provides more complete advertisement data
- **Event-Driven Updates**: Only processes changed data, reducing CPU usage
- **No Polling Loop**: Avoids constant thread activity

## License

This project is provided as-is for educational and personal use.

## References

- [Windows.Devices.Bluetooth.Advertisement API](https://learn.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.advertisement)
- [Bluetooth LE Specification](https://www.bluetooth.com/specifications/specs/)
- [Apple Bluetooth Manufacturer Data Format (reverse-engineered)](https://github.com/topics/airpods-battery-osx)

