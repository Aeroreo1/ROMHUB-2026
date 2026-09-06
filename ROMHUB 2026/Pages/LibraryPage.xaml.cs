using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ROMHub.Data;
using ROMHub.Models;

namespace ROMHub.Pages
{
    // LibraryPage code-behind:
    // - Loads ROM entries from the JSON store and binds them to the ListView.
    // - Handles adding new ROMs via AddRomWindow.
    // - Provides UI actions for changing/removing the ROM cover image via the overflow menu.
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
                // Create a grouped view by Platform so items are listed under console categories
                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(roms);
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription("Platform"));

                lv.ItemsSource = view;
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

        private void OverflowButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;
            // Get the Rom from the button's DataContext/CommandParameter
            var rom = btn.CommandParameter as Rom ?? (btn.DataContext as Rom);
            if (rom == null) return;

            // Create a ContextMenu on the fly so the user sees options before an action
            var cm = new System.Windows.Controls.ContextMenu();

            var change = new System.Windows.Controls.MenuItem { Header = "Change Cover..." };
            change.Click += (s, args) => ChangeCoverForRom(rom);

            var remove = new System.Windows.Controls.MenuItem { Header = "Remove Cover" };
            remove.Click += (s, args) =>
            {
                var msg = $"Remove the cover image for '{rom.Title}'?";
                var res = System.Windows.MessageBox.Show(msg, "Confirm remove", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (res == System.Windows.MessageBoxResult.Yes)
                {
                    rom.CoverImagePath = string.Empty;
                    ROMHub.Data.RomJsonStore.Update(rom);
                    LoadRoms();
                }
            };

            var delete = new System.Windows.Controls.MenuItem { Header = "Delete ROM" };
            delete.Click += (s, args) =>
            {
                var msg = $"Delete '{rom.Title}'? This cannot be undone.";
                var res = System.Windows.MessageBox.Show(msg, "Confirm delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (res == System.Windows.MessageBoxResult.Yes)
                {
                    // Attempt to delete any copied cover file if it lives under the AppData RomHub/images path
                    try
                    {
                        if (!string.IsNullOrEmpty(rom.CoverImagePath) && System.IO.File.Exists(rom.CoverImagePath))
                        {
                            // Only delete files located under the application's AppData RomHub Images folder to avoid deleting user files elsewhere
                            var appDataImages = System.IO.Path.Combine(
                                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                                "RomHub",
                                "Images");
                            var full = System.IO.Path.GetFullPath(rom.CoverImagePath);
                            if (full.StartsWith(appDataImages, System.StringComparison.OrdinalIgnoreCase))
                            {
                                System.IO.File.Delete(full);
                            }
                        }
                    }
                    catch
                    {
                        // ignore any file delete errors
                    }

                    ROMHub.Data.RomJsonStore.Delete(rom.Id);
                    LoadRoms();
                }
            };

            cm.Items.Add(change);
            cm.Items.Add(remove);
            cm.Items.Add(new System.Windows.Controls.Separator());
            cm.Items.Add(delete);

            // Attach to button and open
            btn.ContextMenu = cm;
            cm.PlacementTarget = btn;
            cm.IsOpen = true;
        }

        private void ChangeCoverMenu_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var rom = menuItem?.CommandParameter as Rom ?? menuItem?.DataContext as Rom;
            if (rom == null) return;

            ChangeCoverForRom(rom);
        }

        private void RemoveCoverMenu_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            var rom = menuItem?.CommandParameter as Rom ?? menuItem?.DataContext as Rom;
            if (rom == null) return;

            var msg = $"Remove the cover image for '{rom.Title}'?";
            var res = System.Windows.MessageBox.Show(msg, "Confirm remove", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (res == System.Windows.MessageBoxResult.Yes)
            {
                rom.CoverImagePath = string.Empty;
                ROMHub.Data.RomJsonStore.Update(rom);
                LoadRoms();
            }
        }

        private void ChangeCoverForRom(Rom rom)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*";
            if (dlg.ShowDialog() != true) return;

            var source = dlg.FileName;
            var imagesDir = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "RomHub",
                "Images");

            if (!System.IO.Directory.Exists(imagesDir))
                System.IO.Directory.CreateDirectory(imagesDir);

            var destFileName = System.IO.Path.Combine(imagesDir, System.IO.Path.GetFileName(source));
            var dest = destFileName;
            var i = 1;
            while (System.IO.File.Exists(dest))
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(destFileName);
                var ext = System.IO.Path.GetExtension(destFileName);
                dest = System.IO.Path.Combine(imagesDir, $"{name}_{i}{ext}");
                i++;
            }

            System.IO.File.Copy(source, dest);

            rom.CoverImagePath = dest;
            ROMHub.Data.RomJsonStore.Update(rom);
            LoadRoms();
        }


    }
}
