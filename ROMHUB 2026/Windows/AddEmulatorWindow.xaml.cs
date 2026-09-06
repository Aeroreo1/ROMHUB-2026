
using System.Windows;
using Microsoft.Win32;
using ROMHub.Models;

namespace ROMHub.Windows
{
    public partial class AddEmulatorWindow : Window
    {
        public Emulator Emulator { get; private set; }

        public AddEmulatorWindow()
        {
            InitializeComponent();
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

            var selectedPlatform =
                ((System.Windows.Controls.ComboBoxItem)
                PlatformComboBox.SelectedItem).Content.ToString();

            Emulator = new Emulator
            {
                Name = NameTextBox.Text,
                Platform = selectedPlatform,
                FilePath = FilePathTextBox.Text,
                ImagePath = ImagePathTextBox.Text
            };

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

