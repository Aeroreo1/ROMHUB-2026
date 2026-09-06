using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
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
                PlatformComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Game Boy Advance (GBA) [gba; gameboy advance; gameboy]" });
                PlatformComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "DS [ds; nintendo ds]" });
                PlatformComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "PlayStation 1 [ps1; playstation1; playstation]" });
                PlatformComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "PlayStation 2 [ps2; playstation2]" });
                PlatformComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Nintendo GameCube & Wii [gamecube; gc; wii]" });
                PlatformComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Nintendo Switch [switch; ns; nintendo switch]" });
                PlatformComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Nintendo 3DS [3ds; nintendo 3ds]" });
                PlatformComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Nintendo WiiU [wiiu; nintendo wiiu]" });
                PlatformComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Xbox 360 [xbox360; x360]" });
                PlatformComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "PlayStation 3 [ps3; playstation3]" });
                PlatformComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "PlayStation 4 [ps4; playstation4]" });
                PlatformComboBox.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Retro Classics [retro; classic; retro classics]" });
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
            var platformItem = PlatformComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var platform = platformItem?.Content?.ToString() ?? string.Empty;

            // If user did not select a custom cover, attempt to assign a default image for the platform
            if (string.IsNullOrEmpty(_selectedCoverPath))
            {
                var defaultForPlatform = GetDefaultImageForPlatform(platform);
                if (!string.IsNullOrEmpty(defaultForPlatform))
                {
                    _selectedCoverPath = defaultForPlatform;
                }
            }

            Rom = new Rom
            {
                Title = TitleTextBox.Text,
                Platform = platform,
                FilePath = PathTextBox.Text,
                CoverImagePath = _selectedCoverPath ?? string.Empty,
                IsFavourite = FavouriteCheckbox.IsChecked == true
            };

            DialogResult = true;
            Close();
        }

        private string GetDefaultImageForPlatform(string platform)
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

                // Search heuristics: look for keywords in filenames
                string match = null;
                // Accept several platform name variants to improve matching.
                if (lower.Contains("gba") || lower.Contains("gameboy") || lower.Contains("game boy") || lower.Contains("gameboy advance") || lower.Contains("advance"))
                {
                    match = files.FirstOrDefault(f => {
                        var lf = f.ToLowerInvariant();
                        return lf.Contains("gba") || lf.Contains("gameboy") || lf.Contains("game boy") || lf.Contains("gameboy advance") || lf.Contains("advance");
                    });
                }
                else if (lower.Contains("ds"))
                {
                    match = files.FirstOrDefault(f => f.ToLowerInvariant().Contains("ds"));
                }
                else if (lower.Contains("wii"))
                {
                    match = files.FirstOrDefault(f => f.ToLowerInvariant().Contains("wii"));
                }

                // If not found by keyword, fallback to any image
                if (string.IsNullOrEmpty(match))
                {
                    var supported = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
                    match = files.FirstOrDefault(f => supported.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()));
                }

                return match;
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
    }
}
