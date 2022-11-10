using System;
using System.Windows;
using System.Windows.Threading;

namespace LazerArmory
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();

        string textUpload = "Uploading to server...";
        string textDone = "✓ Synchronized";

        public MainWindow()
        {
            InitializeComponent();
        }

        bool ShowCharacterButton = false;
        private void Window_Initialized(object sender, EventArgs e)
        {
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Click += new EventHandler(notifyIcon_Click);
            notifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/cat-64.ico")).Stream);

            PositionMe();
            Opacity = 0;

            DispatcherTimer t = new DispatcherTimer();
            t.Interval = TimeSpan.FromSeconds(5);
            t.Tick += T_Tick1;
            t.Start();

            AddonController.OnUploadStarted += AddonController_OnUploadStarted;
            AddonController.OnUploadEnded += AddonController_OnUploadEnded;
            mainText.Content = textDone;
        }

        private void T_Tick1(object sender, EventArgs e)
        {
            (sender as DispatcherTimer).Stop();
            ShowCharacterButton = true;
        }

        private void AddonController_OnUploadEnded(object sender, object e)
        {
            if (!uploading)
                return;
            uploading = false;
            Dispatcher.Invoke(() => {
                mainText.Content = textDone;
                DispatcherTimer t = new DispatcherTimer();
                t.Interval = TimeSpan.FromMilliseconds(1500);
                t.Tick += T_Tick;
                t.Start();

                if (AddonController.LastSavedCharacter != null && ShowCharacterButton)
                {
                    charButton.Visibility = Visibility.Visible;
                    charButton.Content = "Show character: " + AddonController.LastSavedCharacter.Name;
                }
                else
                {
                    charButton.Visibility = Visibility.Collapsed;
                }
            });
        }

        bool uploading = false;

        public object HttpUtility { get; private set; }

        private void AddonController_OnUploadStarted(object sender, object e)
        {
            if (uploading)
                return;
            uploading = true;
            Dispatcher.Invoke(() => {
                mainText.Content = textUpload;
                ShowMe();
            });
        }

        private void T_Tick(object sender, EventArgs e)
        {
            (sender as DispatcherTimer).Stop();
            Hide();
            AddonController.Ping();
        }

        void PositionMe()
        {
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
        }

        public void ShowMe()
        {
            Show();
            Focus();
            Activate();
            PositionMe();
        }

        void notifyIcon_Click(object sender, EventArgs e)
        {
            ShowMe();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Hide();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            notifyIcon.Visible = true;
            Hide();
            Opacity = 1;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            notifyIcon.Visible = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow s = new SettingsWindow();
            s.ShowDialog();
            if (s.Tag != null && ((bool)s.Tag) == true)
            {
                // Shutdown
                Close();
            }
        }

        private void charButton_Click(object sender, RoutedEventArgs e)
        {
            var p = AddonController.LastSavedCharacter.Name + "-" + AddonController.LastSavedCharacter.Realm;
            System.Diagnostics.Process.Start("http://lazer-kittens.fun/armory/?character=" + Uri.EscapeUriString(p));
        }
    }
}
