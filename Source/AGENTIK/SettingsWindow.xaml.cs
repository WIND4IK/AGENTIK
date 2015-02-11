using System.Reflection;
using System.Windows;
using AGENTIK.Interfaces;
using DevExpress.Xpf.Ribbon;
using log4net;

namespace AGENTIK {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : DXRibbonWindow {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SettingsWindow() {
            InitializeComponent();
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e) {
            DialogResult = true;
            //foreach (var settingItem in settingsUserControl.ItemsSource) {
            //    var settings = settingItem.Control as ISettings;
            //    if (settings != null)
            //        settings.Save();
            //}
            Close();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
