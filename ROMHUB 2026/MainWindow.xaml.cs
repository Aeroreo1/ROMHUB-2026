using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ROMHub
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Navigate to the default home page on startup
            MainFrame.Navigate(new Pages.HomePage());
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.HomePage());
        }

        private void Library_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.LibraryPage());
        }

        private void Favourites_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.FavouritesPage());
        }

        private void Emulators_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.EmulatorsPage());
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.SettingsPage());
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}