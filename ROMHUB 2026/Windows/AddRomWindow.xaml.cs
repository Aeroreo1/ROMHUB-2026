using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ROMHub.Models;

namespace ROMHub.Windows
{
    public partial class AddRomWindow : Window
    {
        public Rom Rom { get; private set; }

        public AddRomWindow()
        {
            InitializeComponent();
            // Ensure platform list includes aliases and the requested order at runtime
            try
            {
                PlatformComboBox.Items.Clear();
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "Nintendo Entertainment System (NES) [nes; nintendo entertainment system]" });
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "Game Boy Advance (GBA) [gba; gameboy advance; gameboy]", Tag = "gba" });
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "DS [ds; nintendo ds]", Tag = "ds" });
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "PlayStation 1 [ps1; playstation1; playstation]", Tag = "ps1" });
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "PlayStation 2 [ps2; playstation2]", Tag = "ps2" });
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "Nintendo GameCube & Wii [gamecube; gc; wii]", Tag = "gamecube" });
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "Nintendo Switch [switch; ns; nintendo switch]", Tag = "switch" });
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "Nintendo 3DS [3ds; nintendo 3ds]", Tag = "3ds" });
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "Nintendo WiiU [wiiu; nintendo wiiu]", Tag = "wiiu" });
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "Xbox 360 [xbox360; x360]", Tag = "xbox360" });
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "PlayStation 3 [ps3; playstation3]", Tag = "ps3" });
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "PlayStation 4 [ps4; playstation4]", Tag = "ps4" });
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "Retro Classics [retro; classic; retro classics]", Tag = "retro" });
                // Add custom option
                PlatformComboBox.Items.Add(new ComboBoxItem { Content = "Custom (enter name)...", Tag = "custom" });
                PlatformComboBox.SelectedIndex = 0;
            }
            catch
            {
                // ignore if PlatformComboBox isn't available at design time
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "ROM files|*.bin;*.iso;*.nes;*.sfc;*.gba;*.zip|All files|*.*";
            if (dlg.ShowDialog() == true)
            {
                PathTextBox.Text = dlg.FileName;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Determine selected platform from ComboBox
            var platformItem = PlatformComboBox.SelectedItem as ComboBoxItem;
            var platform = platformItem?.Content?.ToString() ?? string.Empty;

            // If custom selected, use value from CustomPlatformTextBox
            if (!string.IsNullOrEmpty(platform) && platform.StartsWith("Custom", StringComparison.OrdinalIgnoreCase))
            {
                var custom = CustomPlatformTextBox?.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(custom))
                {
                    MessageBox.Show("Please enter a custom platform name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                platform = custom;
            }

            // If user did not select a custom cover, attempt to assign a default image for the platform
            if (string.IsNullOrEmpty(_selectedCoverPath))
            {
                // Prefer stock/emulator images for retro/emulator selections
                var defaultForPlatform = GetDefaultImageForPlatform(platform, preferStockForEmulator: true);
                if (!string.IsNullOrEmpty(defaultForPlatform))
                {
                    _selectedCoverPath = defaultForPlatform;
                }
            }

            Rom = new Rom
            {
                Title = TitleTextBox.Text,
                Platform = platform,
                PlatformTag = (platformItem?.Tag?.ToString() ?? string.Empty),
                FilePath = PathTextBox.Text,
                CoverImagePath = _selectedCoverPath ?? string.Empty,
                IsFavourite = FavouriteCheckbox.IsChecked == true
            };

            DialogResult = true;
            Close();
        }

        private string GetDefaultImageForPlatform(string platform, bool preferStockForEmulator = false)
        {
            try
            {
                // Search in several locations for default cover images:
                // 1) App base directory / Images (project images copied to output)
                // 2) ApplicationData RomHub/Images (user-copied images)
                // If neither exists, return null.

                var baseImagesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                var appDataImagesDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RomHub",
                    "Images");

                string[] files = Array.Empty<string>();
                if (System.IO.Directory.Exists(baseImagesDir))
                {
                    files = System.IO.Directory.GetFiles(baseImagesDir);
                }
                else if (System.IO.Directory.Exists(appDataImagesDir))
                {
                    files = System.IO.Directory.GetFiles(appDataImagesDir);
                }

                if (files.Length == 0)
                    return null;
                var lower = platform?.ToLowerInvariant() ?? string.Empty;

                // If platform looks like an emulator/retro collection and caller prefers stock images,
                // look for files named with "stock" or "emulator" first.
                if (preferStockForEmulator && (lower.Contains("retro") || lower.Contains("emulator") || lower.Contains("classic")))
                {
                    var stock = files.FirstOrDefault(f => {
                        var nf = System.IO.Path.GetFileName(f).ToLowerInvariant();
                        return nf.Contains("stock") || nf.Contains("emulator") || nf.Contains("retro");
                    });
                    if (!string.IsNullOrEmpty(stock))
                        return stock;
                }

                // Map some platform keywords to preferred filename fragments for more deterministic matching.
                var platformKeywords = new[] {
                    new { Key = "gba", Keywords = new[] { "gba", "gameboy", "game boy", "gameboy advance", "advance" } },
                    new { Key = "ds", Keywords = new[] { "ds", "nintendo ds" } },
                    new { Key = "3ds", Keywords = new[] { "3ds", "nintendo 3ds" } },
                    new { Key = "wiiu", Keywords = new[] { "wiiu", "nintendo wiiu" } },
                    new { Key = "wii", Keywords = new[] { "wii" } },
                    new { Key = "gamecube", Keywords = new[] { "gamecube", "gc" } },
                    new { Key = "switch", Keywords = new[] { "switch", "ns", "nintendo switch" } },
                    new { Key = "nes", Keywords = new[] { "nes", "nintendo entertainment system" } },
                    new { Key = "ps1", Keywords = new[] { "ps1", "playstation 1", "playstation1", "playstation" } },
                    new { Key = "ps2", Keywords = new[] { "ps2", "playstation 2" } },
                    new { Key = "ps3", Keywords = new[] { "ps3", "playstation 3" } },
                    new { Key = "ps4", Keywords = new[] { "ps4", "playstation 4" } },
                    new { Key = "xbox360", Keywords = new[] { "xbox360", "x360", "xbox 360" } },
                    new { Key = "retro", Keywords = new[] { "retro", "classic", "retro classics" } }
                };

                // Try matching files by those keywords in filename first
                foreach (var map in platformKeywords)
                {
                    if (map.Key == null) continue;
                    if (map.Key == "retro")
                    {
                        if (!lower.Contains("retro") && !lower.Contains("classic"))
                            continue;
                    }
                    else
                    {
                        if (!map.Key.Equals("retro", StringComparison.OrdinalIgnoreCase) && !map.Key.Equals("nes", StringComparison.OrdinalIgnoreCase) && !map.Key.Equals("gba", StringComparison.OrdinalIgnoreCase))
                        {
                            // check any of the keyword variants
                            if (!map.Key.Equals("retro") && !map.Key.Equals("nes") && !map.Key.Equals("gba") && !map.Key.Any()) { }
                        }
                    }

                    foreach (var kw in map.Keywords)
                    {
                        if (!lower.Contains(kw))
                            continue;

                        var found = files.FirstOrDefault(f => System.IO.Path.GetFileName(f).ToLowerInvariant().Contains(kw));
                        if (!string.IsNullOrEmpty(found))
                            return found;
                    }
                }

                // Fallback: attempt general keyword search across filenames
                var supported = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
                var match = files.FirstOrDefault(f => supported.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()) && System.IO.Path.GetFileName(f).ToLowerInvariant().Contains(lower.Split(' ').FirstOrDefault() ?? string.Empty));

                if (!string.IsNullOrEmpty(match))
                    return match;

                // Final fallback: any supported image
                return files.FirstOrDefault(f => supported.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()));
            }
            catch
            {
                return null;
            }
        }

        private string _selectedCoverPath;

        private void SelectCover_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*";
            if (dlg.ShowDialog() == true)
            {
                // Copy selected image into AppData/RomHub/Images to manage it locally
                var source = dlg.FileName;
                var imagesDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RomHub",
                    "Images");

                if (!System.IO.Directory.Exists(imagesDir))
                    System.IO.Directory.CreateDirectory(imagesDir);

                var destFileName = System.IO.Path.Combine(imagesDir, System.IO.Path.GetFileName(source));

                // Ensure unique file name
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
                _selectedCoverPath = dest;

                // Update UI if you have an Image control named CoverPreview
                var preview = this.FindName("CoverPreview") as System.Windows.Controls.Image;
                if (preview != null)
                {
                    try
                    {
                        var bmp = new System.Windows.Media.Imaging.BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new System.Uri(dest);
                        bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        preview.Source = bmp;
                    }
                    catch
                    {
                        // ignore preview errors
                    }
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Show/hide custom platform textbox when user selects the custom option
        private void PlatformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = PlatformComboBox.SelectedItem as ComboBoxItem;
            var isCustom = item?.Content?.ToString()?.StartsWith("Custom", StringComparison.OrdinalIgnoreCase) == true;
            var panel = this.FindName("CustomPlatformPanel") as StackPanel;
            if (panel != null)
            {
                panel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
                if (isCustom)
                {
                    CustomPlatformTextBox?.Focus();
                }
            }

            // Update preview to show default cover for the selected platform unless the user already selected a custom cover
            try
            {
                // Prefer Tag (canonical) if present and not the custom marker
                var platformText = string.Empty;
                if (item != null)
                {
                    var tag = item.Tag?.ToString();
                    if (!string.IsNullOrEmpty(tag) && !tag.Equals("custom", StringComparison.OrdinalIgnoreCase))
                        platformText = tag;
                    else
                        platformText = (item.Content?.ToString() ?? string.Empty).Trim();
                }

                if (!isCustom && !string.IsNullOrEmpty(platformText))
                {
                    // For emulator/retro selections prefer a stock/emulator image
                    var defaultImage = GetDefaultImageForPlatform(platformText, preferStockForEmulator: true);
                    if (!string.IsNullOrEmpty(defaultImage))
                    {
                        var preview = this.FindName("CoverPreview") as System.Windows.Controls.Image;
                        if (preview != null)
                        {
                            try
                            {
                                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                                bmp.BeginInit();
                                bmp.UriSource = new System.Uri(defaultImage);
                                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                bmp.EndInit();
                                preview.Source = bmp;
                            }
                            catch
                            {
                                // ignore preview errors
                            }
                        }
                    }
                }
                else if (isCustom)
                {
                    // For custom, if the user has typed a name, attempt to find a matching image
                    var customName = CustomPlatformTextBox?.Text?.Trim() ?? string.Empty;
                    if (!string.IsNullOrEmpty(customName) && string.IsNullOrEmpty(_selectedCoverPath))
                    {
                        var img = GetDefaultImageForPlatform(customName);
                        if (!string.IsNullOrEmpty(img))
                        {
                            var preview = this.FindName("CoverPreview") as System.Windows.Controls.Image;
                            if (preview != null)
                            {
                                try
                                {
                                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                                    bmp.BeginInit();
                                    bmp.UriSource = new System.Uri(img);
                                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                    bmp.EndInit();
                                    preview.Source = bmp;
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}