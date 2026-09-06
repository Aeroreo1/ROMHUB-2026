using ROMHub.Data;
using ROMHub.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ROMHub.Pages
{
    public partial class EmulatorsPage : Page
    {
        public EmulatorsPage()
        {
            InitializeComponent();
            LoadEmulators();
        }

        private void LoadEmulators()
        {
            List<Emulator> emulators = EmulatorJsonStore.Load()
                .OrderBy(e => e.Name)
                .ToList();

            EmulatorsListView.ItemsSource = emulators;
        }

        private void AddEmulator_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new Windows.AddEmulatorWindow();

            bool? result = addWindow.ShowDialog();

            if (result == true)
            {
                EmulatorJsonStore.Add(addWindow.Emulator);

                LoadEmulators();
            }
        }

        private void Launch_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;

            var emu = btn.CommandParameter as Emulator ?? btn.DataContext as Emulator;
            if (emu == null) return;

            if (string.IsNullOrWhiteSpace(emu.FilePath) || !System.IO.File.Exists(emu.FilePath))
            {
                System.Windows.MessageBox.Show($"Emulator executable not found: {emu.FilePath}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = emu.FilePath,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(emu.FilePath),
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(psi);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to launch emulator: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
