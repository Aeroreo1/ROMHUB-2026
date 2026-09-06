using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ROMHub.Data;
using ROMHub.Models;

namespace ROMHub.Pages
{
    public partial class LibraryPage : Page
    {
        public LibraryPage()
        {
            InitializeComponent();
            LoadRoms();
        }

        private void LoadRoms()
        {
            List<Rom> roms = ROMHub.Data.RomJsonStore.Load()
                .OrderBy(r => r.Title)
                .ToList();

            var lv = this.FindName("RomsListView") as System.Windows.Controls.ListView;
            if (lv != null)
            {
                lv.ItemsSource = roms;
            }
        }

        private void AddRom_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new Windows.AddRomWindow();
            bool? result = addWindow.ShowDialog();
            if (result == true)
            {
                // Save new rom to database
                var newRom = addWindow.Rom;
                // Persist to JSON store instead of EF context so items persist across app runs
                ROMHub.Data.RomJsonStore.Add(newRom);

                LoadRoms();
            }
        }


    }
}
