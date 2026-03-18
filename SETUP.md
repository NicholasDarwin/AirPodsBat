# AirPods Battery Overlay - Setup & Build Guide

## ⚡ FASTEST WAY - One-Click Run

**Just downloaded?** Run immediately with:

### Windows
**Option 1: Double-click RUN.bat**
- Automatically checks for .NET 6.0 SDK
- Builds the project in Release mode
- Launches the overlay window
- That's it!

**Option 2: PowerShell - Double-click RUN.ps1**
- Same as above, using PowerShell
- Colored output and better error messages

The scripts will guide you through everything. No manual steps needed!

---

## Manual Prerequisites Installation

### 1. Install .NET 6.0 SDK

1. **Download**: Visit https://dotnet.microsoft.com/download/dotnet/6.0
2. **Choose**: "Windows x64 Installer" (or x86 if 32-bit)
3. **Run** the installer and follow the instructions
4. **Verify** in PowerShell:
   ```powershell
   dotnet --version
   ```

### 2. Install Visual Studio 2022 (Recommended)

For a better development experience:

1. **Download**: https://visualstudio.microsoft.com/downloads/
2. **Choose**: Visual Studio 2022 Community (free)
3. **Workloads to Select**:
   - ✅ Desktop & Mobile
   - ✅ .NET desktop development
   - ✅ Windows app development (includes WPF)
4. **Install** and restart your computer

Alternatively, use **Visual Studio Code** with C# extension.

## Building the Project

### Option 1: Using Visual Studio (Recommended)

1. Open Visual Studio
2. File → Open → Project/Solution
3. Navigate to: `c:\Users\UC\Desktop\AirPodBat\AirPodBat.csproj`
4. **Build**: 
   - Go to Build → Build Solution (Ctrl+Shift+B)
   - Or Build → Rebuild Solution if issues occur
5. **Run**: 
   - Press F5 to start with debugging
   - Or Ctrl+F5 to run without debugging

### Option 2: Using Command Line

After installing .NET 6.0 SDK:

```powershell
cd c:\Users\UC\Desktop\AirPodBat
dotnet build
```

**Build Output** should appear in `bin\Debug\net6.0-windows10.0.19041.0`

### Option 3: Using Visual Studio Code

1. **Install Extensions**:
   - C# Extension Pack (by Microsoft)
   - WPF Application Debugger (optional)

2. **Build**:
   ```powershell
   dotnet build
   ```

3. **Run**:
   ```powershell
   dotnet run
   ```

## Running the Application

### From Visual Studio

1. In Solution Explorer, right-click `AirPodBat` project
2. Select "Set as Startup Project"
3. Press **F5** to run

### From Command Line

```powershell
cd c:\Users\UC\Desktop\AirPodBat
dotnet run
```

### Run Built Executable Directly

After building:

```powershell
c:\Users\UC\Desktop\AirPodBat\bin\Debug\net6.0-windows10.0.19041.0\AirPodBat.exe
```

Or in Release mode:

```powershell
c:\Users\UC\Desktop\AirPodBat\bin\Release\net6.0-windows10.0.19041.0\AirPodBat.exe
```

## Expected Application Behavior

### On Launch

1. **Window appears** in bottom-right corner of screen
2. **Size**: 200×120 pixels
3. **Style**: Dark semi-transparent overlay
4. **Content**:
   ```
   AirPods Battery
   L:  –    R:  –    C:  –
   Initializing...
   ```

### When AirPods Are Nearby

1. Status changes to: `Connected`
2. Battery percentages display:
   ```
   AirPods Battery
   L: 85%   R: 82%   C: 95%
   Connected
   ```
3. Updates automatically when battery levels change

### When No AirPods Detected

- Continues scanning
- Displays "–" for all values
- Status: `Bluetooth scanning started`

## NuGet Packages Dependencies

The project requires:

- **Windows.Win32**: For Windows API access
  - Version: 1.0.0-preview.1 (latest may vary)
  - Provides Bluetooth support

The .csproj file automatically downloads these when you build.

## Troubleshooting Build Issues

### Error: "no suitable SDK"

**Solution**: Ensure .NET 6.0 is installed correctly
```powershell
dotnet --list-sdks
```

Should show: `6.0.xxx [path]`

### Error: "Windows.Devices.Bluetooth" not found

**Verify**: Project targets Windows-specific SDK
- File: `AirPodBat.csproj`
- Should contain: `<TargetFramework>net6.0-windows10.0.19041.0</TargetFramework>`

### Error: WPF-related compilation failures

**Solutions**:
1. Delete `bin` and `obj` folders:
   ```powershell
   Remove-Item -Recurse bin, obj
   ```
2. Clean and rebuild:
   ```powershell
   dotnet clean
   dotnet build
   ```

### Warning: "Nullable reference types"

This is normal with nullable enabled. The app compiles successfully with these warnings.

## Project file Details

### AirPodBat.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows10.0.19041.0</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Windows.Win32" Version="1.0.0-preview.1" />
  </ItemGroup>
</Project>
```

**Key Points**:
- `WindowsDesktop` SDK: Required for WPF applications
- `TargetFramework`: Windows 10 build 19041.0 minimum
- `UseWPF`: Enables WPF project setup
- `Windows.Win32`: Provides Bluetooth APIs

## Running in Release Mode

For better performance (slightly faster, smaller output):

```powershell
dotnet run --configuration Release
```

Or build and run executable:

```powershell
dotnet publish -c Release
```

Output: `bin\Release\net6.0-windows10.0.19041.0\publish\AirPodBat.exe`

## Creating a Standalone Executable

To create a self-contained .exe that doesn't require .NET to be installed:

```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

This creates a larger executable but can run on any Windows 10+ system.

## Debugging

### Visual Studio Debugger

1. Set breakpoints by clicking left margin
2. Press F5 to run (stops at breakpoints)
3. Step through code with F10 (step over) or F11 (step into)
4. View variables in Locals window

### Debug Output

The application logs to Visual Studio Debug Output:
- Look for: Debug → Windows → Output
- Filter to "Debug" channel to see application messages

### Example Debug Messages

```
Bluetooth LE scanning started
Advertisement received: [raw hex bytes]
AirPods detected: L:85% R:82% C:95%
Watcher stopped: Success
```

## Next Steps

1. **Build** the project using above steps
2. **Run** the application with AirPods nearby
3. **Test** battery display accuracy
4. **Debug** if battery values seem incorrect using AIRPODS_PARSING.md
5. **Customize** UI colors, position, or polling interval as needed

## File Structure Verification

After setup, verify you have:

```
c:\Users\UC\Desktop\AirPodBat\
├── AirPodBat.csproj               ✓
├── README.md                       ✓
├── AIRPODS_PARSING.md             ✓
├── Models/
│   └── AirPodsBattery.cs          ✓
├── Services/
│   ├── BluetoothService.cs        ✓
│   └── AirPodsParser.cs           ✓
├── ViewModels/
│   └── MainViewModel.cs           ✓
├── Views/
│   ├── MainWindow.xaml            ✓
│   └── MainWindow.xaml.cs         ✓
├── App.xaml                       ✓
├── App.xaml.cs                    ✓
└── bin/                           (Created after build)
```

## Support & Debugging

If you encounter issues:

1. **Check Output Window**: Build output shows errors clearly
2. **Read AIRPODS_PARSING.md**: For battery parsing questions
3. **Verify Bluetooth**: Windows Settings → Devices → Bluetooth & other devices
4. **Review Code Comments**: Extensive comments in source files explain logic

