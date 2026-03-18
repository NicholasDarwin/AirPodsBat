using System;
using System.Windows;
using AirPodBat.ViewModels;

namespace AirPodBat.Views
{
    /// <summary>
    /// MainWindow.xaml code-behind
    /// Manages the overlay window positioning and Bluetooth scanning lifecycle.
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Create and set the view model
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;

            // Set window position to bottom-right of primary screen
            PositionBottomRight();

            // Handle window loaded event
            this.Loaded += WithdrawButtonWindow_Loaded;
            this.Closed += WithdrawButtonWindow_Closed;
        }

        private void WithdrawButtonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Start Bluetooth scanning when window is loaded
            _viewModel.StartScanning();
        }

        private void WithdrawButtonWindow_Closed(object sender, EventArgs e)
        {
            // Stop Bluetooth scanning when window is closed
            _viewModel.StopScanning();
        }

        /// <summary>
        /// Positions the window in the bottom-right corner of the primary screen.
        /// </summary>
        private void PositionBottomRight()
        {
            try
            {
                // Position at bottom-right with some margin from the edges
                this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 10;
                this.Top = SystemParameters.PrimaryScreenHeight - this.Height - 10;
            }
            catch
            {
                // Fallback position if screen detection fails
                this.Left = 1720;
                this.Top = 1400;
            }
        }
    }
}
