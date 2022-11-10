using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LazerArmory
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            version.Content = "version " + AppController.APP_VERSION;
        }

        bool loading = false;
        void LoadSettings()
        {
            loading = true;

            var s = AppController.GetSettings();
            autoStart.IsChecked = s.runAtStartup;
            path.Text = s.wowPath;
            characters.Children.Clear();
            foreach (var c in s.characters)
            {
                CheckBox b = new CheckBox();
                b.Content = c.name + " (" + c.realm + ")";
                b.IsChecked = c.enabled;
                b.Tag = c;
                b.Click += B_Click;
                characters.Children.Add(b);
            }

            loading = false;
        }

        private void B_Click(object sender, RoutedEventArgs e)
        {
            var b = (sender as CheckBox);
            var c = (b.Tag as Character);
            AppController.SetCharacterEnabled(c.name, c.realm, b.IsChecked.Value);
        }

        private void autoStart_Click(object sender, RoutedEventArgs e)
        {
            if (loading)
                return;

            if (autoStart.IsChecked.Value)
                AppController.EnableAutoStartup();
            else
                AppController.DisableAutoStartup();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Tag = false;
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Tag = true;
            Close();
        }

        private void path_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (loading)
                return;

            AppController.SetWowPath(path.Text);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            AppController.RunUpdater(true);
        }
    }
}
