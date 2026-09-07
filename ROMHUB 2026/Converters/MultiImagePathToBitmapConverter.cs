using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ROMHub.Converters
{
    // Multi-value converter that takes [0]=imagePath (string), [1]=platform or platformTag (string)
    // If imagePath exists on disk it is used. Otherwise searches the Images folders for a file
    // whose filename contains the platform/tag. Falls back to any local image or packaged default.
    public class MultiImagePathToBitmapConverter : IMultiValueConverter
    {
        private static bool _webpWarningShown;
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var path = values?.Length > 0 ? values[0] as string : null;
                var platform = values?.Length > 1 ? values[1] as string : null;

                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(path, UriKind.Absolute);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }
                    catch
                    {
                        var ext = Path.GetExtension(path).ToLowerInvariant();
                        if (ext == ".webp" && !_webpWarningShown)
                        {
                            _webpWarningShown = true;
                            MessageBox.Show("Failed to render WebP image. Your system may not have WebP imaging support installed. Falling back to default image.", "Image Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }

                // Look for images in App Images folder or AppData RomHub/Images
                var baseImagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                var appDataImagesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RomHub", "Images");

                string[] files = Array.Empty<string>();
                if (Directory.Exists(baseImagesDir)) files = Directory.GetFiles(baseImagesDir);
                else if (Directory.Exists(appDataImagesDir)) files = Directory.GetFiles(appDataImagesDir);

                if (files.Length == 0)
                {
                    // fallback to packaged default
                    return PackDefault();
                }

                var lower = (platform ?? string.Empty).ToLowerInvariant();

                // prefer exact tag match first
                if (!string.IsNullOrEmpty(lower))
                {
                    var byTag = files.FirstOrDefault(f => Path.GetFileName(f).ToLowerInvariant().Contains(lower));
                    if (!string.IsNullOrEmpty(byTag))
                    {
                        try { return ToBitmap(byTag); }
                        catch
                        {
                            var ext = Path.GetExtension(byTag).ToLowerInvariant();
                            if (ext == ".webp" && !_webpWarningShown)
                            {
                                _webpWarningShown = true;
                                MessageBox.Show("Failed to render WebP image from Images folder. Install WebP support or provide a PNG/JPG. Falling back to default.", "Image Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                }

                // try a set of common keywords fallbacks
                var keywords = new[] { "nes", "gba", "ds", "3ds", "wii", "wiiu", "switch", "gamecube", "ps1", "ps2", "ps3", "ps4", "xbox360", "retro", "emulator", "stock" };
                foreach (var kw in keywords)
                {
                    var found = files.FirstOrDefault(f => Path.GetFileName(f).ToLowerInvariant().Contains(kw));
                    if (!string.IsNullOrEmpty(found))
                    {
                        try { return ToBitmap(found); }
                        catch
                        {
                            var ext = Path.GetExtension(found).ToLowerInvariant();
                            if (ext == ".webp" && !_webpWarningShown)
                            {
                                _webpWarningShown = true;
                                MessageBox.Show("Failed to render WebP image from Images folder. Install WebP support or provide a PNG/JPG. Falling back to default.", "Image Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                }

                // final fallback: any supported image
                var supported = new[] { ".png", ".jpg", ".jpeg", ".jfif", ".webp", ".bmp", ".gif" };
                var any = files.FirstOrDefault(f => supported.Contains(Path.GetExtension(f).ToLowerInvariant()));
                if (!string.IsNullOrEmpty(any))
                {
                    try { return ToBitmap(any); }
                    catch
                    {
                        var ext = Path.GetExtension(any).ToLowerInvariant();
                        if (ext == ".webp" && !_webpWarningShown)
                        {
                            _webpWarningShown = true;
                            MessageBox.Show("Failed to render WebP image. Falling back to packaged default.", "Image Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }

                return PackDefault();
            }
            catch
            {
                return null;
            }
        }

        private BitmapImage ToBitmap(string file)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(file, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private BitmapImage PackDefault()
        {
            try
            {
                var packUri = new Uri("pack://application:,,,/Images/default-cover.png", UriKind.Absolute);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = packUri;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
