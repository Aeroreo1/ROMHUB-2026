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

        private void EmulatorOverflowButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;
            var emu = btn.CommandParameter as Emulator ?? btn.DataContext as Emulator;
            if (emu == null) return;

            var cm = new System.Windows.Controls.ContextMenu();

            var edit = new System.Windows.Controls.MenuItem { Header = "Edit..." };
            edit.Click += (s, args) =>
            {
                var win = new Windows.AddEmulatorWindow(emu);
                var res = win.ShowDialog();
                if (res == true)
                {
                    EmulatorJsonStore.Update(win.Emulator);
                    LoadEmulators();
                }
            };

            var change = new System.Windows.Controls.MenuItem { Header = "Change Image..." };
            change.Click += (s, args) =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*";
                if (dlg.ShowDialog() == true)
                {
                    var source = dlg.FileName;
                    var imagesDir = System.IO.Path.Combine(
                        System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                        "RomHub",
                        "Images");
                    if (!System.IO.Directory.Exists(imagesDir)) System.IO.Directory.CreateDirectory(imagesDir);
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
                    emu.ImagePath = dest;
                    EmulatorJsonStore.Update(emu);
                    LoadEmulators();
                }
            };

            var remove = new System.Windows.Controls.MenuItem { Header = "Remove Image" };
            remove.Click += (s, args) =>
            {
                var msg = $"Remove the image for '{emu.Name}'?";
                var res = System.Windows.MessageBox.Show(msg, "Confirm remove", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (res == System.Windows.MessageBoxResult.Yes)
                {
                    emu.ImagePath = string.Empty;
                    EmulatorJsonStore.Update(emu);
                    LoadEmulators();
                }
            };

            var del = new System.Windows.Controls.MenuItem { Header = "Delete Emulator" };
            del.Click += (s, args) =>
            {
                var msg = $"Delete '{emu.Name}'? This cannot be undone.";
                var res = System.Windows.MessageBox.Show(msg, "Confirm delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (res == System.Windows.MessageBoxResult.Yes)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(emu.ImagePath) && System.IO.File.Exists(emu.ImagePath))
                        {
                            var appDataImages = System.IO.Path.Combine(
                                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                                "RomHub",
                                "Images");
                            var full = System.IO.Path.GetFullPath(emu.ImagePath);
                            if (full.StartsWith(appDataImages, System.StringComparison.OrdinalIgnoreCase))
                            {
                                System.IO.File.Delete(full);
                            }
                        }
                    }
                    catch { }

                    EmulatorJsonStore.Delete(emu.Id);
                    LoadEmulators();
                }
            };

            cm.Items.Add(edit);
            var setTag = new System.Windows.Controls.MenuItem { Header = "Set Tag..." };
            setTag.Click += (s, args) =>
            {
                var input = ShowInputDialog("Set Platform Tag", "Enter canonical platform tag (e.g. nes, gba, ps1):", emu.PlatformTag ?? string.Empty);
                if (input != null)
                {
                    emu.PlatformTag = input.Trim();
                    EmulatorJsonStore.Update(emu);
                    LoadEmulators();
                }
            };
            cm.Items.Add(setTag);
            cm.Items.Add(change);
            cm.Items.Add(remove);
            cm.Items.Add(new System.Windows.Controls.Separator());
            cm.Items.Add(del);

            cm.IsOpen = true;
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

        private string ShowInputDialog(string title, string prompt, string defaultValue)
        {
            var w = new Window()
            {
                Title = title,
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Owner = Application.Current.MainWindow
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };
            var txt = new System.Windows.Controls.TextBlock { Text = prompt };
            var tb = new System.Windows.Controls.TextBox { Text = defaultValue ?? string.Empty, Margin = new Thickness(0, 6, 0, 6) };

            var buttons = new System.Windows.Controls.StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new System.Windows.Controls.Button { Content = "OK", Width = 80, Margin = new Thickness(6, 0, 0, 0) };
            var cancel = new System.Windows.Controls.Button { Content = "Cancel", Width = 80, Margin = new Thickness(6, 0, 0, 0) };

            ok.Click += (s, e) => { w.DialogResult = true; w.Close(); };
            cancel.Click += (s, e) => { w.DialogResult = false; w.Close(); };

            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);

            panel.Children.Add(txt);
            panel.Children.Add(tb);
            panel.Children.Add(buttons);

            w.Content = panel;

            var res = w.ShowDialog();
            if (res == true)
                return tb.Text;
            return null;
        }
    }
}
