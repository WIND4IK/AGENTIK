using System.Reflection;
using System.Windows;
using HelperChat.Interfaces;
using HelperChat.Models;
using log4net;

namespace HelperChat {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SettingsWindow() {
            InitializeComponent();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e) {
            DialogResult = true;
            foreach (var item in settingsUserControl.ItemsSource) {
                var control = item.Control as ISettings;
                if (control != null) {
                    control.Save();
                }
            }
            Close();
        }

    }
}
