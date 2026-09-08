using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using System;
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
                .Where(r => !r.HideFromLibrary)
                .OrderBy(r => r.Title)
                .ToList();

            var lv = this.FindName("RomsListView") as System.Windows.Controls.ListView;
            if (lv != null)
            {
                // Create a grouped view by Platform (friendly name) so items are listed under console categories
                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(roms);
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription("Platform"));

                lv.ItemsSource = view;
            }
        }

        private void ExpandGroupByName(string groupName)
        {
            try
            {
                var lv = this.FindName("RomsListView") as ListView;
                if (lv == null) return;

                var groupItems = new List<GroupItem>();
                GetVisualChildrenRecursiveFor<GroupItem>(lv, groupItems);
                foreach (var gi in groupItems)
                {
                    var grp = gi.DataContext as System.Windows.Data.CollectionViewGroup;
                    if (grp == null) continue;
                    var name = grp.Name?.ToString() ?? string.Empty;
                    if (string.Equals(name, groupName, StringComparison.OrdinalIgnoreCase) || name.IndexOf(groupName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // find Expander inside groupItem
                        var exps = new List<Expander>();
                        GetVisualChildrenRecursiveFor<Expander>(gi, exps);
                        var exp = exps.FirstOrDefault();
                        if (exp != null)
                        {
                            exp.IsExpanded = true;
                        }

                        // scroll first item into view
                        if (grp.ItemCount > 0)
                        {
                            var first = grp.Items[0];
                            lv.ScrollIntoView(first);
                        }
                    }
                }
            }
            catch { }
        }

        private void GetVisualChildrenRecursiveFor<T>(DependencyObject parent, List<T> results) where T : DependencyObject
        {
            if (parent == null) return;
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) results.Add(t);
                GetVisualChildrenRecursiveFor<T>(child, results);
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

                var platform = newRom?.Platform ?? string.Empty;
                if (!string.IsNullOrEmpty(platform))
                {
                    // Delay action to allow visual containers to be generated
                    this.Dispatcher.BeginInvoke(new System.Action(async () =>
                    {
                        await System.Threading.Tasks.Task.Delay(120);
                        ExpandGroupByName(platform);
                    }));
                }
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

            try
            {
                var emus = ROMHub.Data.EmulatorJsonStore.Load().OrderBy(x => x.Name).ToList();

                // If a preferred emulator was saved, honour it
                if (rom.PreferredEmulatorId.HasValue)
                {
                    var pid = rom.PreferredEmulatorId.Value;
                    if (pid == -1)
                    {
                        // -1 is a marker for 'Open With' (default application)
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(rom.FilePath) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to open ROM: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }

                    var preferred = emus.FirstOrDefault(x => x.Id == pid);
                    if (preferred != null)
                    {
                        LaunchEmulatorWithRom(preferred, rom.FilePath);
                        return;
                    }
                }

                // No saved preference - prompt user to choose a matching emulator or 'Open With'
                var candidates = new List<ROMHub.Models.Emulator>();
                var romTag = (rom.PlatformTag ?? string.Empty).ToLowerInvariant();
                var plat = (rom.Platform ?? string.Empty).ToLowerInvariant();

                if (!string.IsNullOrWhiteSpace(romTag))
                    candidates.AddRange(emus.Where(x => !string.IsNullOrWhiteSpace(x.PlatformTag) && x.PlatformTag.ToLowerInvariant() == romTag));

                if (!candidates.Any() && !string.IsNullOrWhiteSpace(romTag))
                    candidates.AddRange(emus.Where(x => !string.IsNullOrWhiteSpace(x.PlatformTag) && plat.Contains(x.PlatformTag.ToLowerInvariant())));

                if (!candidates.Any())
                    candidates.AddRange(emus.Where(x => !string.IsNullOrWhiteSpace(x.Platform) && plat.Contains(x.Platform.ToLowerInvariant())));

                if (!candidates.Any())
                    candidates.AddRange(emus);

                var cm = new System.Windows.Controls.ContextMenu();
                foreach (var emu in candidates.Distinct())
                {
                    var captured = emu;
                    var mi = new System.Windows.Controls.MenuItem { Header = captured.Name };
                    mi.Click += (s, args) =>
                    {
                        try
                        {
                            rom.PreferredEmulatorId = captured.Id;
                            ROMHub.Data.RomJsonStore.Update(rom);
                        }
                        catch { }

                        LaunchEmulatorWithRom(captured, rom.FilePath);
                    };
                    cm.Items.Add(mi);
                }

                cm.Items.Add(new System.Windows.Controls.Separator());
                var openWith = new System.Windows.Controls.MenuItem { Header = "Open With (default application)" };
                openWith.Click += (s, args) =>
                {
                    try
                    {
                        rom.PreferredEmulatorId = -1;
                        ROMHub.Data.RomJsonStore.Update(rom);
                    }
                    catch { }

                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(rom.FilePath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to open ROM: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                cm.Items.Add(openWith);

                cm.PlacementTarget = btn;
                cm.IsOpen = true;
            }
            catch
            {
                MessageBox.Show("Failed to locate or launch an emulator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FavouriteToggle_Click(object sender, RoutedEventArgs e)
        {
            var toggle = sender as System.Windows.Controls.Primitives.ToggleButton;
            var rom = toggle?.CommandParameter as Rom ?? toggle?.DataContext as Rom;
            if (rom == null) return;

            var isChecked = toggle.IsChecked == true;

            try
            {
                var all = ROMHub.Data.RomJsonStore.Load();

                if (isChecked)
                {
                    // Create a favourite copy (do not remove original) if one doesn't already exist
                    var exists = all.Any(r => r.IsFavourite && r.HideFromLibrary && string.Equals(r.FilePath, rom.FilePath, StringComparison.OrdinalIgnoreCase));
                    if (!exists)
                    {
                        var copy = new Rom
                        {
                            Title = rom.Title,
                            Platform = rom.Platform,
                            PlatformTag = rom.PlatformTag,
                            FilePath = rom.FilePath,
                            CoverImagePath = rom.CoverImagePath,
                            IsFavourite = true,
                            HideFromLibrary = true,
                            PreferredEmulatorId = rom.PreferredEmulatorId
                        };
                        ROMHub.Data.RomJsonStore.Add(copy);
                    }
                }
                else
                {
                    // Remove any favourite copy matching this ROM
                    var copies = all.Where(r => r.IsFavourite && r.HideFromLibrary && string.Equals(r.FilePath, rom.FilePath, StringComparison.OrdinalIgnoreCase)).ToList();
                    foreach (var c in copies)
                    {
                        try { ROMHub.Data.RomJsonStore.Delete(c.Id); } catch { }
                    }
                }
            }
            catch { }

            LoadRoms();
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
                // Start with emulator-provided template (may be empty)
                var providedTemplate = emulator.Arguments ?? string.Empty;

                // Handle Windows shortcuts (.lnk) by resolving their target and collecting any shortcut arguments
                var fileName = emulator.FilePath;
                var workingDir = System.IO.Path.GetDirectoryName(emulator.FilePath);
                var shortcutArgs = string.Empty;
                try
                {
                    if (!string.IsNullOrWhiteSpace(fileName) && fileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                    {
                        var shellType = Type.GetTypeFromProgID("WScript.Shell");
                        if (shellType != null)
                        {
                            dynamic shell = Activator.CreateInstance(shellType);
                            try
                            {
                                dynamic shortcut = shell.CreateShortcut(fileName);
                                string target = (shortcut.TargetPath as string) ?? string.Empty;
                                string lnkArgs = (shortcut.Arguments as string) ?? string.Empty;
                                string lnkWd = (shortcut.WorkingDirectory as string) ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(target))
                                {
                                    fileName = target;
                                    shortcutArgs = lnkArgs;
                                    if (!string.IsNullOrWhiteSpace(lnkWd))
                                        workingDir = lnkWd;
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                // If the emulator has no explicit argument template containing {rom}, apply a small default mapping
                var template = providedTemplate;
                if (string.IsNullOrWhiteSpace(template) || !template.Contains("{rom}"))
                {
                    try
                    {
                        var exeName = System.IO.Path.GetFileName(fileName)?.ToLowerInvariant() ?? string.Empty;
                        var defaults = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "retroarch.exe", "{rom}" },
                            { "retroarch64.exe", "{rom}" },
                            { "dolphin.exe", "{rom}" },
                            { "pcsx2.exe", "{rom}" },
                            { "snes9x.exe", "{rom}" },
                            { "snes9x-gtk.exe", "{rom}" },
                            { "mame.exe", "{rom}" },
                            { "mednafen.exe", "{rom}" },
                            { "genesisplusgx.exe", "{rom}" },
                            { "ppsspp.exe", "{rom}" }
                        };

                        if (defaults.TryGetValue(exeName, out var def))
                        {
                            template = def;
                        }
                    }
                    catch { }
                }

                // Build final arguments by substituting {rom} when present, or appending the rom path
                string args;
                if (!string.IsNullOrWhiteSpace(template) && template.Contains("{rom}"))
                {
                    args = template.Replace("{rom}", '"' + romPath + '"');
                }
                else if (!string.IsNullOrWhiteSpace(template))
                {
                    args = template + " " + '"' + romPath + '"';
                }
                else
                {
                    args = '"' + romPath + '"';
                }

                // Prepend any shortcut arguments
                if (!string.IsNullOrWhiteSpace(shortcutArgs))
                {
                    args = shortcutArgs + (string.IsNullOrWhiteSpace(args) ? string.Empty : " " + args);
                }

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false
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
