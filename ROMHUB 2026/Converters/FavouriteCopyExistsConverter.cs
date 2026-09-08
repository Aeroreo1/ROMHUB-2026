using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ROMHub.Data;
using ROMHub.Models;

namespace ROMHub.Converters
{
    public class FavouriteCopyExistsConverter : IValueConverter
    {
        // Expects the binding value to be a Rom instance. Returns true if a HideFromLibrary favourite copy exists for the same FilePath.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var rom = value as Rom;
            if (rom == null) return false;

            try
            {
                var copies = RomJsonStore.Load();
                return copies.Any(r => r.IsFavourite && r.HideFromLibrary && string.Equals(r.FilePath, rom.FilePath, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
