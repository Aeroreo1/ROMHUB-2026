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

            // Build 'Open With' submenu containing available emulators
            try
            {
                var openWith = new System.Windows.Controls.MenuItem { Header = "Open With" };
                var emus = ROMHub.Data.EmulatorJsonStore.Load().OrderBy(x => x.Name).ToList();
                foreach (var emu in emus)
                {
                    var capturedEmu = emu; // avoid closure over loop variable
                    var mi = new System.Windows.Controls.MenuItem { Header = capturedEmu.Name, CommandParameter = capturedEmu };
                    mi.Click += (s, args) =>
                    {
                        // Remember user's choice for this ROM and persist
                        try
                        {
                            rom.PreferredEmulatorId = capturedEmu.Id;
                            ROMHub.Data.RomJsonStore.Update(rom);
                        }
                        catch
                        {
                            // ignore persistence errors
                        }

                        // Launch emulator with rom path as argument
                        LaunchEmulatorWithRom(capturedEmu, rom.FilePath);
                    };
                    openWith.Items.Add(mi);
                }

                if (openWith.Items.Count > 0)
                {
                    cm.Items.Add(new System.Windows.Controls.Separator());
                    cm.Items.Add(openWith);
                }
            }
            catch
            {
                // ignore if emulator list cannot be read
            }

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

        private void LaunchRomWithDefaultEmulator_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;

            var rom = btn.CommandParameter as Rom ?? btn.DataContext as Rom;
            if (rom == null) return;

            // Find first emulator whose Platform matches the rom.Platform (case-insensitive, contains)
            try
            {
                var emus = ROMHub.Data.EmulatorJsonStore.Load();

                // 1) If ROM has a preferred emulator saved, try that first
                ROMHub.Models.Emulator emulator = null;
                if (rom.PreferredEmulatorId.HasValue)
                {
                    emulator = emus.FirstOrDefault(x => x.Id == rom.PreferredEmulatorId.Value);
                }

                // 2) Otherwise fall back to platform matching
                if (emulator == null)
                {
                    var romTag = (rom.PlatformTag ?? string.Empty).ToLowerInvariant();
                    var plat = (rom.Platform ?? string.Empty).ToLowerInvariant();

                    // Prefer matching by canonical tag when present on both ROM and emulator
                    if (!string.IsNullOrWhiteSpace(romTag))
                    {
                        emulator = emus.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.PlatformTag) && x.PlatformTag.ToLowerInvariant() == romTag);
                    }

                    // Fallbacks: match emulator tag contained in rom platform, or platform strings as before
                    if (emulator == null && !string.IsNullOrWhiteSpace(romTag))
                    {
                        emulator = emus.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.PlatformTag) && plat.Contains(x.PlatformTag.ToLowerInvariant()));
                    }

                    if (emulator == null)
                    {
                        emulator = emus.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Platform) && plat.Contains(x.Platform.ToLowerInvariant()));
                    }

                    if (emulator == null)
                    {
                        // Try a looser match: emulator.Platform contains rom.Platform
                        emulator = emus.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Platform) && x.Platform.ToLowerInvariant().Contains(plat));
                    }
                }

                if (emulator == null)
                {
                    MessageBox.Show($"No emulator registered for platform '{rom.Platform}'. Use 'Open With' to pick an emulator.", "No emulator", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                LaunchEmulatorWithRom(emulator, rom.FilePath);
            }
            catch
            {
                MessageBox.Show("Failed to locate or launch an emulator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LaunchEmulatorWithRom(ROMHub.Models.Emulator emulator, string romPath)
        {
            if (emulator == null)
                return;

            if (string.IsNullOrWhiteSpace(emulator.FilePath) || !System.IO.File.Exists(emulator.FilePath))
            {
                MessageBox.Show($"Emulator executable not found: {emulator.FilePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(romPath) || !System.IO.File.Exists(romPath))
            {
                MessageBox.Show($"ROM file not found: {romPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Replace {rom} placeholder in emulator.Arguments with quoted rom path
                var args = emulator.Arguments ?? string.Empty;
                if (args.Contains("{rom}"))
                {
                    args = args.Replace("{rom}", '"' + romPath + '"');
                }
                else
                {
                    // If no placeholder provided, append ROM path as last argument
                    if (!string.IsNullOrWhiteSpace(args))
                        args = args + " " + '"' + romPath + '"';
                    else
                        args = '"' + romPath + '"';
                }

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = emulator.FilePath,
                    Arguments = args,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(emulator.FilePath),
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(psi);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to launch emulator: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
