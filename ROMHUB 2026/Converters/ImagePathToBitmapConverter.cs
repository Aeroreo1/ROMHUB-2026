using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ROMHub.Converters
{
    // Converts a file-system path or an empty/null value into a BitmapImage.
    // If the path points to an existing file it will be loaded with CacheOption.OnLoad
    // to avoid file locks; otherwise a packaged default image is returned via pack URI.
    public class ImagePathToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var path = value as string;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(path, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }

                // Try to find any image under a local Images folder next to the executable
                // (e.g., Project/Images copied to output). Use the first supported image found.
                var imagesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                if (Directory.Exists(imagesDir))
                {
                    var supported = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
                    var files = Directory.GetFiles(imagesDir)
                        .Where(f => supported.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()))
                        .ToArray();

                    if (files.Length > 0)
                    {
                        var bmpLocal = new BitmapImage();
                        bmpLocal.BeginInit();
                        bmpLocal.UriSource = new Uri(files[0], UriKind.Absolute);
                        bmpLocal.CacheOption = BitmapCacheOption.OnLoad;
                        bmpLocal.EndInit();
                        bmpLocal.Freeze();
                        return bmpLocal;
                    }
                }

                // If no file is present next to the exe, fall back to a packaged resource named default-cover.png.
                // You can add any image to Project/Images and set 'Copy to Output Directory' or add it as a Resource.
                var packUri = new Uri("pack://application:,,,/Images/default-cover.png", UriKind.Absolute);
                var defaultBmp = new BitmapImage();
                defaultBmp.BeginInit();
                defaultBmp.UriSource = packUri;
                defaultBmp.CacheOption = BitmapCacheOption.OnLoad;
                defaultBmp.EndInit();
                defaultBmp.Freeze();
                return defaultBmp;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
