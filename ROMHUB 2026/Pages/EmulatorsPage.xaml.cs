using ROMHub.Data;
using ROMHub.Models;
using ROMHUB_2026.Data;
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
    }
}
