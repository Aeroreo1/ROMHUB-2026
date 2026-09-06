using Microsoft.Win32;
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
            Rom = new Rom
            {
                Title = TitleTextBox.Text,
                Platform = PlatformTextBox.Text,
                FilePath = PathTextBox.Text,
                IsFavourite = FavouriteCheckbox.IsChecked == true
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
