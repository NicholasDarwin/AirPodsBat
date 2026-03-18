using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using AirPodBat.Models;

namespace AirPodBat.Services
{
    /// <summary>
    /// Handles Bluetooth LE scanning for AirPods devices.
    /// Uses Windows.Devices.Bluetooth.Advertisement API with Active scanning mode.
    /// </summary>
    public class BluetoothService
    {
        private BluetoothLEAdvertisementWatcher? _watcher;
        private readonly AirPodsParser _parser = new();
        private AirPodsBattery? _lastBatteryData;

        public event EventHandler<AirPodsBattery>? AirPodsDataReceived;
        public event EventHandler<string>? StatusChanged;

        public BluetoothService()
        {
        }

        /// <summary>
        /// Starts scanning for Bluetooth LE advertisements.
        /// </summary>
        public void StartScanning()
        {
            try
            {
                if (_watcher != null)
                {
                    StopScanning();
                }

                // Create a new watcher
                _watcher = new BluetoothLEAdvertisementWatcher();

                // Set scanning mode to Active to receive more advertisement data
                _watcher.ScanningMode = BluetoothLEScanningMode.Active;

                // Subscribe to events
                _watcher.Received += OnAdvertisementReceived;
                _watcher.Stopped += OnWatcherStopped;

                // Start watching
                _watcher.Start();

                OnStatusChanged("Bluetooth scanning started");
                Debug.WriteLine("Bluetooth LE scanning started");
            }
            catch (Exception ex)
            {
                OnStatusChanged($"Error starting Bluetooth: {ex.Message}");
                Debug.WriteLine($"Error starting Bluetooth: {ex}");
            }
        }

        /// <summary>
        /// Stops scanning for Bluetooth LE advertisements.
        /// </summary>
        public void StopScanning()
        {
            try
            {
                if (_watcher != null)
                {
                    _watcher.Stop();
                    _watcher.Received -= OnAdvertisementReceived;
                    _watcher.Stopped -= OnWatcherStopped;
                    _watcher = null;
                }

                OnStatusChanged("Bluetooth scanning stopped");
                Debug.WriteLine("Bluetooth LE scanning stopped");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping Bluetooth: {ex}");
            }
        }

        /// <summary>
        /// Handles advertisement packets received by the watcher.
        /// </summary>
        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, 
            BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {
            try
            {
                // Check manufacturer data
                foreach (var manufacturerData in eventArgs.Advertisement.ManufacturerData)
                {
                    // ManufacturerData is a KeyValuePair<ushort, IBuffer>
                    var data = manufacturerData.Data.ToArray();
                    
                    // Try to parse as AirPods
                    var batteryData = AirPodsParser.ParseAirPodsData(data);
                    
                    if (batteryData != null)
                    {
                        // Only notify if data changed
                        if (!IsSameBatteryData(_lastBatteryData, batteryData))
                        {
                            _lastBatteryData = batteryData;
                            OnAirPodsDataReceived(batteryData);
                            Debug.WriteLine($"AirPods detected: {batteryData}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing advertisement: {ex}");
            }
        }

        /// <summary>
        /// Handles when the watcher stops.
        /// </summary>
        private void OnWatcherStopped(BluetoothLEAdvertisementWatcher watcher, 
            BluetoothLEAdvertisementWatcherStoppedEventArgs eventArgs)
        {
            Debug.WriteLine($"Watcher stopped: {eventArgs.Error}");
        }

        private void OnAirPodsDataReceived(AirPodsBattery batteryData)
        {
            AirPodsDataReceived?.Invoke(this, batteryData);
        }

        private void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        /// <summary>
        /// Compares two battery data objects to detect if values have changed.
        /// Used to avoid redundant UI updates.
        /// </summary>
        private static bool IsSameBatteryData(AirPodsBattery? prev, AirPodsBattery? curr)
        {
            if (prev == null || curr == null)
                return false;

            return prev.Left == curr.Left && 
                   prev.Right == curr.Right && 
                   prev.Case == curr.Case;
        }
    }
}
