# Quick Run Guide - Simply Download & Run

## The Simplest Way

### All You Need To Do:

1. **Download this project** from GitHub
2. **Double-click `RUN.bat`** in the folder
3. **Wait 10-30 seconds** for first build (subsequent runs are faster)
4. **Done!** Battery overlay appears in bottom-right corner

That's literally everything. You don't need to know anything about programming.

### What It Does Automatically

When you double-click `RUN.bat`, it:
1. ✅ Checks if .NET 6.0 SDK is installed
2. ✅ Builds the project (just downloads and compiles once)
3. ✅ Launches the overlay window
4. ✅ Closes automatically when done

### Expected Result

Within 10-30 seconds (depending on first build):
- A small overlay window appears in the **bottom-right corner** of your screen
- Shows: `L: – R: – C: –` (waiting for AirPods)
- When you bring AirPods nearby: `L: 85% R: 82% C: 95%`

### What If Something Goes Wrong?

#### "ERROR: .NET 6.0 SDK is not installed"

**Solution**: Install .NET 6.0 SDK
1. Go to: https://dotnet.microsoft.com/download/dotnet/6.0
2. Click "Windows x64 Installer" (or x86 if 32-bit)
3. Run the installer and restart your computer
4. Double-click `RUN.bat` again

#### The window closes immediately

**Possible causes**:
- Read the error message that appeared before closing
- Try right-clicking `RUN.bat` → Edit → Look for error details
- Or open PowerShell manually and run: `RUN.ps1` (shows full output)

#### "The app runs but battery shows '–'"

This is normal! You need:
- AirPods powered on
- AirPods within 3-5 meters of your computer
- Some models take 10-20 seconds to appear

See [TESTING.md](TESTING.md) for debugging battery value issues.

---

## How It Works

### The RUN.bat Script

```batch
@echo off
REM Check .NET is installed
dotnet --version

REM Build the project
dotnet build -c Release

REM Run the application
start bin\Release\net6.0-windows10.0.19041.0\AirPodBat.exe
```

**Translation**:
- Confirms .NET 6.0 SDK exists
- Compiles the project (first time takes 10-20 seconds)
- Launches the executable
- Subsequent runs are faster (only rebuilds changed files)

### First Run vs. Subsequent Runs

**First Run**: 10-30 seconds
- Nuget packages download
- Project compiles
- Executable starts

**Subsequent Runs**: 2-5 seconds
- Only changed files rebuild
- Executable starts faster

---

## Alternative Methods

### Method 1: Command Prompt (Recommended)
```cmd
RUN.bat
```

### Method 2: PowerShell
```powershell
RUN.ps1
```

### Method 3: Manual Commands
```powershell
cd c:\Users\UC\Desktop\AirPodBat
dotnet run --configuration Release
```

### Method 4: Visual Studio
1. Open `AirPodBat.csproj` in Visual Studio
2. Press **F5** (Debug) or **Ctrl+F5** (Release)

---

## Next Steps

Once the app is running:

1. **Test with AirPods**
   - Turn on your AirPods
   - Bring them within 5 meters of your computer
   - Wait 10-20 seconds for battery to appear
   - See [TESTING.md](TESTING.md) if values don't match iOS

2. **Understand the Code**
   - Read [README.md](README.md) (10 min)
   - Read [MVVM_GUIDE.md](MVVM_GUIDE.md) (15 min)
   - Browse source code in the project

3. **Customize It**
   - Change UI colors in `Views/MainWindow.xaml`
   - Adjust window position in code-behind
   - Add new features using MVVM pattern

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| File not found: `RUN.bat` | Make sure you're in the project directory |
| BuildException in output | Usually just slow - wait for first build to complete |
| .NET SDK not found | Install from https://dotnet.microsoft.com/download/dotnet/6.0 |
| App closes immediately | Try `RUN.ps1` to see error messages before it closes |
| Battery shows "–" | See [TESTING.md](TESTING.md) - needs AirPods nearby |

---

## Getting Help

- **Build Issues**: See [SETUP.md](SETUP.md) → Troubleshooting
- **Battery Values Wrong**: See [TESTING.md](TESTING.md) → Debugging
- **Understanding Code**: See [MVVM_GUIDE.md](MVVM_GUIDE.md)
- **General Questions**: See [README.md](README.md)
- **File Navigation**: See [INDEX.md](INDEX.md)

---

**That's it!** You now have a working AirPods battery overlay on your Windows 10 PC. 🎉

Need to modify or understand it? Check [README.md](README.md) next.

