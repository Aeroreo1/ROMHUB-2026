using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using ROMHub.Data;
using ROMHub.Models;

namespace ROMHub.Pages
{
    public partial class FavouritesPage : Page
    {
        public FavouritesPage()
        {
            InitializeComponent();
            LoadFavourites();
        }

        private void LoadFavourites()
        {
            List<Rom> roms = ROMHub.Data.RomJsonStore.Load()
                .Where(r => r.IsFavourite)
                .OrderBy(r => r.Title)
                .ToList();

            var lv = this.FindName("FavsListView") as System.Windows.Controls.ListView;
            if (lv != null)
            {
                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(roms);
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription("Platform"));

                lv.ItemsSource = view;
            }
        }

        private void LaunchRomWithDefaultEmulator_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;

            var rom = btn.CommandParameter as Rom ?? btn.DataContext as Rom;
            if (rom == null) return;

            try
            {
                var emus = ROMHub.Data.EmulatorJsonStore.Load().OrderBy(x => x.Name).ToList();

                if (rom.PreferredEmulatorId.HasValue)
                {
                    var pid = rom.PreferredEmulatorId.Value;
                    if (pid == -1)
                    {
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

        private void OverflowButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;
            var rom = btn.CommandParameter as Rom ?? btn.DataContext as Rom;
            if (rom == null) return;

            var cm = new System.Windows.Controls.ContextMenu();

            var removeFav = new System.Windows.Controls.MenuItem { Header = rom.IsFavourite ? "Remove from favourites" : "Add to favourites" };
            removeFav.Click += (s, args) =>
            {
                rom.IsFavourite = !rom.IsFavourite;
                ROMHub.Data.RomJsonStore.Update(rom);
                LoadFavourites();
            };

            var delete = new System.Windows.Controls.MenuItem { Header = "Delete ROM" };
            delete.Click += (s, args) =>
            {
                var msg = $"Delete '{rom.Title}'? This cannot be undone.";
                var res = System.Windows.MessageBox.Show(msg, "Confirm delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (res == System.Windows.MessageBoxResult.Yes)
                {
                    ROMHub.Data.RomJsonStore.Delete(rom.Id);
                    LoadFavourites();
                }
            };

            cm.Items.Add(removeFav);
            cm.Items.Add(new System.Windows.Controls.Separator());
            cm.Items.Add(delete);

            // Open With submenu
            try
            {
                var openWith = new System.Windows.Controls.MenuItem { Header = "Open With" };
                var emus = ROMHub.Data.EmulatorJsonStore.Load().OrderBy(x => x.Name).ToList();
                foreach (var emu in emus)
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
                    openWith.Items.Add(mi);
                }

                if (openWith.Items.Count > 0)
                    cm.Items.Add(openWith);
            }
            catch { }

            btn.ContextMenu = cm;
            cm.PlacementTarget = btn;
            cm.IsOpen = true;
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
                var providedTemplate = emulator.Arguments ?? string.Empty;
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
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch emulator: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
