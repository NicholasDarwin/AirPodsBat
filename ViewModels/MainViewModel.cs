using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AirPodBat.Models;
using AirPodBat.Services;

namespace AirPodBat.ViewModels
{
    /// <summary>
    /// Main ViewModel for the application.
    /// Handles binding between Bluetooth service and UI.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly BluetoothService _bluetoothService;
        private int _leftBattery = -1;
        private int _rightBattery = -1;
        private int _caseBattery = -1;
        private string _status = "Initializing...";
        private bool _isConnected = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int LeftBattery
        {
            get => _leftBattery;
            set => SetProperty(ref _leftBattery, value);
        }

        public int RightBattery
        {
            get => _rightBattery;
            set => SetProperty(ref _rightBattery, value);
        }

        public int CaseBattery
        {
            get => _caseBattery;
            set => SetProperty(ref _caseBattery, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public string LeftBatteryDisplay => _leftBattery >= 0 ? $"{_leftBattery}%" : "–";
        public string RightBatteryDisplay => _rightBattery >= 0 ? $"{_rightBattery}%" : "–";
        public string CaseBatteryDisplay => _caseBattery >= 0 ? $"{_caseBattery}%" : "–";

        public MainViewModel()
        {
            _bluetoothService = new BluetoothService();
            _bluetoothService.AirPodsDataReceived += OnAirPodsDataReceived;
            _bluetoothService.StatusChanged += OnStatusChanged;
        }

        /// <summary>
        /// Starts Bluetooth scanning.
        /// </summary>
        public void StartScanning()
        {
            _bluetoothService.StartScanning();
        }

        /// <summary>
        /// Stops Bluetooth scanning.
        /// </summary>
        public void StopScanning()
        {
            _bluetoothService.StopScanning();
        }

        /// <summary>
        /// Called when AirPods battery data is received and updated.
        /// </summary>
        private void OnAirPodsDataReceived(object? sender, AirPodsBattery batteryData)
        {
            LeftBattery = batteryData.Left;
            RightBattery = batteryData.Right;
            CaseBattery = batteryData.Case;
            IsConnected = true;
            Status = "Connected";

            // Force UI to refresh display properties
            OnPropertyChanged(nameof(LeftBatteryDisplay));
            OnPropertyChanged(nameof(RightBatteryDisplay));
            OnPropertyChanged(nameof(CaseBatteryDisplay));
        }

        private void OnStatusChanged(object? sender, string status)
        {
            Status = status;
        }

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
