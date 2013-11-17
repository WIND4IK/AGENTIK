using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using AGENTIK.Resources;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid.LookUp;
using DevExpress.Xpf.Ribbon;
using Microsoft.Win32;
using log4net;

namespace AGENTIK {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : DXRibbonWindow {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SettingsWindow() {
            InitializeComponent();
            Loaded += SettingsWindowLoaded;
        }

        void SettingsWindowLoaded(object sender, RoutedEventArgs e) {
            leTheme.ItemsSource = Theme.Themes.ToList().Where(t => !t.Name.Equals(Theme.TouchlineDark.Name)).ToList();
            leTheme.EditValue = ThemeManager.ApplicationThemeName ?? Theme.Office2007Blue.Name;
            leTheme.EditValueChanged += OnLeThemeEditValueChanged;
            
            IsStartup = RegistryHelper.IsStartUp;
            LoginAddress = RegistryHelper.LoginAddress;
            LogoutAddress = RegistryHelper.LogoutAddress;
            DataAddress = RegistryHelper.DataAddress;
            RefreshTime = RegistryHelper.RefreshTime;
        }

        void OnLeThemeEditValueChanged(object sender, RoutedEventArgs e) {
            var control = (LookUpEdit)sender;
            if (control == null || control.EditValue == null || control.EditValue.ToString().Length == 0)
                return;

            ThemeManager.ApplicationThemeName = control.EditValue.ToString();
        }

        public string LoginAddress {
            get { return txtMain.Text; }
            set { txtMain.Text = value; }
        }

        public string LogoutAddress {
            get { return txtLogout.Text; }
            set { txtLogout.Text = value; }
        }

        public string DataAddress {
            get { return txtData.Text; }
            set { txtData.Text = value; }
        }

        public DateTime RefreshTime {
            get { return timePicker.DateTime; }
            set { timePicker.DateTime = value; }
        }

        public bool IsStartup {
            get { return chbStart.IsChecked.HasValue && chbStart.IsChecked.Value; }
            set { chbStart.IsChecked = value; }
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e) {
            DialogResult = true;

            RegistryHelper.IsStartUp = IsStartup;
            RegistryHelper.LoginAddress = LoginAddress;
            RegistryHelper.LogoutAddress = LogoutAddress;
            RegistryHelper.DataAddress = DataAddress;
            RegistryHelper.RefreshTime = RefreshTime;

            RegistryHelper.SaveDataFromRegistry();
            Close();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
