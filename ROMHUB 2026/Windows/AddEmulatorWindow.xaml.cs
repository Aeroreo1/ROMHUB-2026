
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ROMHub.Models;

namespace ROMHub.Windows
{
    public partial class AddEmulatorWindow : Window
    {
        public Emulator Emulator { get; private set; }
        private int? _editingId;
        private string _editingTag;

        public AddEmulatorWindow()
        {
            InitializeComponent();
        }

        public AddEmulatorWindow(Emulator existing) : this()
        {
            if (existing == null) return;
            _editingId = existing.Id;
            NameTextBox.Text = existing.Name;
            // Try to set selected platform by Tag or Content
            try
            {
                foreach (var item in PlatformComboBox.Items)
                {
                    if (item is ComboBoxItem cbi)
                    {
                        var tag = cbi.Tag?.ToString();
                        if (!string.IsNullOrEmpty(tag) && !string.IsNullOrEmpty(existing.PlatformTag) && tag.Equals(existing.PlatformTag, StringComparison.OrdinalIgnoreCase))
                        {
                            PlatformComboBox.SelectedItem = cbi;
                            break;
                        }
                        if (cbi.Content?.ToString() == existing.Platform)
                        {
                            PlatformComboBox.SelectedItem = cbi;
                            break;
                        }
                    }
                }
            }
            catch { }

            // Preserve existing tag when editing; tag editing is handled via the emulator overflow menu
            _editingTag = existing.PlatformTag;
            FilePathTextBox.Text = existing.FilePath;
            ImagePathTextBox.Text = existing.ImagePath;
            // Update preview
            ImagePathTextBox_TextChanged(null, null);
        }

        private void PlatformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var item = PlatformComboBox.SelectedItem as ComboBoxItem;
                if (item == null) return;

                // Prefer tag if present
                var tag = item.Tag?.ToString() ?? string.Empty;
                var platformText = !string.IsNullOrEmpty(tag) ? tag : (item.Content?.ToString() ?? string.Empty);

                // If user didn't pick a custom image path, show default preview for selected platform
                if (string.IsNullOrEmpty(ImagePathTextBox.Text))
                {
                    var img = GetDefaultImageForPlatform(platformText, preferStockForEmulator: true);
                    // No preview control in this window; nothing to update here.
                }
            }
            catch { }
        }

        private void BrowseEmulator_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "Executable Files (*.exe)|*.exe";

            if (dialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = dialog.FileName;
            }
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter =
                "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";

            if (dialog.ShowDialog() == true)
            {
                ImagePathTextBox.Text = dialog.FileName;
                // preview will be updated by TextChanged handler
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                PlatformComboBox.SelectedItem == null ||
                string.IsNullOrWhiteSpace(FilePathTextBox.Text))
            {
                MessageBox.Show(
                    "Please enter the emulator name, platform and file location.");

                return;
            }

            var selectedItem = PlatformComboBox.SelectedItem as ComboBoxItem;
            var selectedPlatform = selectedItem?.Content?.ToString() ?? string.Empty;
            var selectedTag = selectedItem?.Tag?.ToString() ?? string.Empty;

            // If no image path provided, try find a default by tag or platform
            var imagePath = ImagePathTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(imagePath))
            {
                var key = !string.IsNullOrEmpty(_editingTag) ? _editingTag : (selectedTag ?? selectedPlatform);
                var found = GetDefaultImageForPlatform(key, preferStockForEmulator: true);
                if (!string.IsNullOrEmpty(found))
                    imagePath = found;
            }

            Emulator = new Emulator
            {
                Name = NameTextBox.Text,
                Platform = selectedPlatform,
                PlatformTag = !string.IsNullOrEmpty(_editingTag) ? _editingTag : (selectedTag ?? string.Empty),
                FilePath = FilePathTextBox.Text,
                ImagePath = imagePath
            };

            // Preserve Id when editing
            if (_editingId.HasValue)
            {
                Emulator.Id = _editingId.Value;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ImagePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Preview removed; do nothing when image path changes
                var _ = ImagePathTextBox.Text;
            }
            catch { }
        }

        private string GetDefaultImageForPlatform(string platform, bool preferStockForEmulator = false)
        {
            try
            {
                var baseImagesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                var appDataImagesDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RomHub",
                    "Images");

                string[] files = Array.Empty<string>();
                if (System.IO.Directory.Exists(baseImagesDir))
                    files = System.IO.Directory.GetFiles(baseImagesDir);
                else if (System.IO.Directory.Exists(appDataImagesDir))
                    files = System.IO.Directory.GetFiles(appDataImagesDir);

                if (files.Length == 0) return null;

                var lower = platform?.ToLowerInvariant() ?? string.Empty;

                if (preferStockForEmulator && (lower.Contains("retro") || lower.Contains("emulator") || lower.Contains("classic")))
                {
                    var stock = files.FirstOrDefault(f => {
                        var nf = System.IO.Path.GetFileName(f).ToLowerInvariant();
                        return nf.Contains("stock") || nf.Contains("emulator") || nf.Contains("retro");
                    });
                    if (!string.IsNullOrEmpty(stock)) return stock;
                }

                // Try direct filename contains match
                var match = files.FirstOrDefault(f => System.IO.Path.GetFileName(f).ToLowerInvariant().Contains(lower));
                if (!string.IsNullOrEmpty(match)) return match;

                // General fallback by keywords (split platform and try parts)
                var parts = lower.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var found = files.FirstOrDefault(f => System.IO.Path.GetFileName(f).ToLowerInvariant().Contains(p));
                    if (!string.IsNullOrEmpty(found)) return found;
                }

                // Final fallback: any supported image
                var supported = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
                return files.FirstOrDefault(f => supported.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()));
            }
            catch
            {
                return null;
            }
        }
    }
}

