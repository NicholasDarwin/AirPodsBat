# MVVM Architecture Guide

## Overview

The AirPods Battery Overlay uses the **Model-View-ViewModel (MVVM)** pattern to cleanly separate concerns and enable testable, maintainable code.

## MVVM Pattern Components

### 1. Model (Data Layer)

**File**: `Models/AirPodsBattery.cs`

Represents the pure data structure without business logic:

```csharp
public class AirPodsBattery
{
    public int Left { get; set; }      // Left earbud battery %
    public int Right { get; set; }     // Right earbud battery %
    public int Case { get; set; }      // Case battery %
}
```

**Responsibilities**:
- Store battery data
- Validate data (Left, Right, Case in 0-100 or -1 if unavailable)
- Provide helper methods (ToString(), HasValidData)

**Not responsible for**:
- How data is obtained or displayed
- Business logic or calculations
- UI state management

### 2. ViewModel (Business Logic Layer)

**File**: `ViewModels/MainViewModel.cs`

Acts as the bridge between Model and View:

```csharp
public class MainViewModel : INotifyPropertyChanged
{
    private BluetoothService _bluetoothService;
    
    // Properties that UI binds to
    public int LeftBattery { get; set; }
    public string LeftBatteryDisplay { get; }  // Formatted for UI
    public bool IsConnected { get; set; }
    public string Status { get; set; }
}
```

**Responsibilities**:
- Expose observable properties that UI binds to
- Format data for display (e.g., "85%" instead of raw 85)
- Handle application events (Bluetooth data received)
- Manage application state (IsConnected, Status)
- Implement INotifyPropertyChanged for data binding

**Not responsible for**:
- Rendering UI
- Scanning for Bluetooth (delegated to BluetoothService)
- UI layout or styling

### 3. View (Presentation Layer)

**Files**: `Views/MainWindow.xaml` + `MainWindow.xaml.cs`

The WPF UI that displays data to the user:

```xml
<TextBlock Text="{Binding LeftBatteryDisplay}" 
           Foreground="#00ff00" FontSize="16" FontWeight="Bold"/>
```

**Responsibilities**:
- Define UI layout (XAML)
- Display data from ViewModel
- Handle user input (clicks, keyboard)
- Manage window properties (position, style, transparency)

**Not responsible for**:
- Processing data
- Business logic
- Bluetooth operations

## Data Flow Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                         View (UI)                             │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ XAML UI Elements                                        │  │
│  │ - TextBlock: {Binding LeftBatteryDisplay}              │  │
│  │ - TextBlock: {Binding RightBatteryDisplay}             │  │
│  │ - TextBlock: {Binding CaseBatteryDisplay}              │  │
│  └────────────────────────────────────────────────────────┘  │
│                          ⬆️ Data Binding                       │
│                          ⬇️ Interaction                        │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│                    ViewModel (Logic)                          │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ MainViewModel                                           │  │
│  │                                                         │  │
│  │ Properties:                                            │  │
│  │ - LeftBattery (int)                                    │  │
│  │ - LeftBatteryDisplay (string) ← Formats int to "%"    │  │
│  │ - IsConnected (bool)                                   │  │
│  │ - Status (string)                                      │  │
│  │                                                         │  │
│  │ Methods:                                               │  │
│  │ - StartScanning() → calls BluetoothService             │  │
│  │ - OnAirPodsDataReceived() → updates properties         │  │
│  │ - INotifyPropertyChanged ← Notifies View of changes   │  │
│  └────────────────────────────────────────────────────────┘  │
│                          ⬆️ Raw Data                           │
│                          ⬇️ Control Commands                   │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│                     Model + Services                          │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ BluetoothService                                        │  │
│  │ - Scans for BLE advertisements                         │  │
│  │ - Emits AirPodsDataReceived event                      │  │
│  │ - Uses AirPodsParser                                   │  │
│  │                                                         │  │
│  │ AirPodsParser                                          │  │
│  │ - Parses raw BLE manufacturer data                     │  │
│  │ - Extracts battery levels                             │  │
│  │ - Returns AirPodsBattery model                         │  │
│  │                                                         │  │
│  │ AirPodsBattery (Model)                                │  │
│  │ - Left: int                                            │  │
│  │ - Right: int                                           │  │
│  │ - Case: int                                            │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                                │
│ (Continues monitoring Bluetooth hardware)                    │
└──────────────────────────────────────────────────────────────┘
```

## Implementation Details

### INotifyPropertyChanged Pattern

The ViewModel implements `INotifyPropertyChanged` to notify the View when properties change:

```csharp
public class MainViewModel : INotifyPropertyChanged
{
    private int _leftBattery = -1;
    
    public int LeftBattery
    {
        get => _leftBattery;
        set => SetProperty(ref _leftBattery, value);  // Triggers PropertyChanged
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

**How it works**:
1. Property value changes
2. `SetProperty()` invokes `OnPropertyChanged()`
3. PropertyChanged event fires
4. WPF binding layer receives notification
5. UI re-renders with new value

### Binding in XAML

```xml
<TextBlock Text="{Binding LeftBatteryDisplay}"/>
```

**Binding Direction**:
- **OneWay** (default): ViewModel → View (data flows to UI)
- **TwoWay**: Both directions (for user input)
- **OneTime**: Set once at load

For read-only display, OneWay is used (the default).

### Event-Driven Updates

Instead of polling, the architecture uses events:

```csharp
// BluetoothService emits this event
public event EventHandler<AirPodsBattery>? AirPodsDataReceived;

// ViewModel subscribes
_bluetoothService.AirPodsDataReceived += OnAirPodsDataReceived;

// When new data arrives, ViewModel updates properties
private void OnAirPodsDataReceived(object? sender, AirPodsBattery batteryData)
{
    LeftBattery = batteryData.Left;    // Triggers PropertyChanged
    RightBattery = batteryData.Right;  // Triggers PropertyChanged
    CaseBattery = batteryData.Case;    // Triggers PropertyChanged
}
```

**Benefits**:
- No continuous polling thread
- Only updates when data changes
- Lower CPU usage
- Better battery efficiency

## Layering Benefits

### Separation of Concerns
- **View**: Only cares about rendering
- **ViewModel**: Only cares about logic
- **Model/Services**: Only care about data

### Testability
```csharp
// Can test ViewModel without UI
[Test]
public void TestBatteryUpdate()
{
    var vm = new MainViewModel();
    
    // Simulate incoming data
    vm.OnAirPodsDataReceived(null, new AirPodsBattery 
    { 
        Left = 85, Right = 90, Case = 95 
    });
    
    // Verify state
    Assert.Equal("85%", vm.LeftBatteryDisplay);
}
```

### Reusability
- Can use same ViewModel with different Views (WPF, WinUI, Console, etc.)
- Can swap BluetoothService with mock for testing

### Maintainability
- Changes to UI don't affect business logic
- Changes to parsing logic don't affect UI
- Clear structure for new developers

## Component Interactions

### Initialization Flow

```
App.xaml.cs
├─ Creates MainWindow
│
MainWindow.xaml.cs (code-behind)
├─ Creates MainViewModel
├─ Sets DataContext = ViewModel
├─ Window Loaded event
│  └─ Calls ViewModel.StartScanning()
│
ViewModel.StartScanning()
├─ Creates BluetoothService
├─ Subscribes to BluetoothService.AirPodsDataReceived event
├─ Subscribes to BluetoothService.StatusChanged event
│
BluetoothService.StartScanning()
├─ Creates BluetoothLEAdvertisementWatcher
├─ Subscribes to Watcher.Received event
└─ Calls Start()
   └─ (Watcher begins monitoring Bluetooth advertisements)
```

### Runtime Flow (When AirPods Data Arrives)

```
1. Bluetooth Hardware:
   Detects AirPods BLE advertisement
   └─ Sends to OS Bluetooth stack

2. BluetoothLEAdvertisementWatcher:
   Receives advertisement via OS
   └─ Invokes Watcher.Received event

3. BluetoothService.OnAdvertisementReceived():
   Extracts manufacturer data from advertisement
   │
   ├─ Calls AirPodsParser.ParseAirPodsData(rawBytes)
   │  └─ Returns AirPodsBattery { Left: 85, Right: 82, Case: 95 }
   │
   └─ Emits AirPodsDataReceived event with AirPodsBattery data

4. MainViewModel.OnAirPodsDataReceived():
   Receives AirPodsBattery data
   │
   ├─ Sets LeftBattery = 85
   │  └─ Triggers PropertyChanged event
   │
   ├─ Sets RightBattery = 82
   │  └─ Triggers PropertyChanged event
   │
   └─ Sets CaseBattery = 95
      └─ Triggers PropertyChanged event

5. WPF Data Binding:
   Receives PropertyChanged notifications
   │
   ├─ Updates LeftBatteryDisplay binding
   │  └─ Calls ToString() / string formatting
   │
   ├─ Updates RightBatteryDisplay binding
   │
   └─ Updates CaseBatteryDisplay binding

6. MainWindow.xaml (View):
   Renders updated values
   │
   └─ User sees:
      L: 85%
      R: 82%
      C: 95%
```

## Extending the Application

### Adding a Feature: Show Charging Status

1. **Model Update**:
```csharp
public class AirPodsBattery
{
    public int Left { get; set; }
    public int Right { get; set; }
    public int Case { get; set; }
    public bool LeftCharging { get; set; }  // NEW
    public bool RightCharging { get; set; } // NEW
    public bool CaseCharging { get; set; }  // NEW
}
```

2. **ViewModel Update**:
```csharp
public class MainViewModel : INotifyPropertyChanged
{
    // ... existing code ...
    
    private bool _leftCharging = false;
    public bool LeftCharging
    {
        get => _leftCharging;
        set => SetProperty(ref _leftCharging, value);
    }
    
    // ... similar for right and case ...
    
    public string LeftBatteryDisplay
    {
        get => _leftBattery >= 0 
            ? $"{_leftBattery}% {(_leftCharging ? "⚡" : "")}" 
            : "–";
    }
}
```

3. **Parser Update**:
```csharp
// In AirPodsParser.ExtractBatteryFromEntry()
parsed.LeftCharging = (deviceInfo & 0x80) != 0;  // Extract bit
```

4. **No View Changes Needed** (if using LeftBatteryDisplay)

This demonstrates MVVM: changes flow from Model → ViewModel → View automatically.

## Best Practices

### ✅ Do's
- Keep Views simple (XAML only)
- Put all logic in ViewModels
- Use data binding instead of code-behind
- Implement INotifyPropertyChanged for observable properties
- Use events for ViewModel → ViewModel communication
- Inject services into ViewModels (testability)

### ❌ Don'ts
- Don't put business logic in Views (code-behind)
- Don't have Views directly access Services
- Don't bind to Model objects directly (violates pattern)
- Don't use polling loops in ViewModel
- Don't create dependencies between Views
- Don't ignore INotifyPropertyChanged

## Performance Considerations

The current implementation is efficient because:

1. **Event-Driven**: Only processes data when something changes
2. **Selective Binding**: Only updates display when actual values change
3. **No Polling**: No background threads constantly checking
4. **Automatic Cleanup**: Services disposed when window closes

For future optimization:
```csharp
// Could add: Only update if values actually changed
private void OnAirPodsDataReceived(object? sender, AirPodsBattery batteryData)
{
    if (LeftBattery != batteryData.Left)  // Check before updating
        LeftBattery = batteryData.Left;
    
    // Reduces redundant PropertyChanged events
}
```

