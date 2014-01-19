using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AGENTIK.Interfaces;
using AGENTIK.Resources;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid.LookUp;

namespace AGENTIK.Controls {
    /// <summary>
    /// Interaction logic for AgentikSettingsControl.xaml
    /// </summary>
    public partial class AgentikSettingsControl : UserControl, ISettings {
        private static AgentikSettingsControl _instance;

        public static AgentikSettingsControl Instance {
            get {
                if(_instance == null)
                    _instance = new AgentikSettingsControl();
                return _instance;
            }
        }

        private AgentikSettingsControl() {
            InitializeComponent();

            Loaded += AgentikSettingsControlLoaded;
        }

        void AgentikSettingsControlLoaded(object sender, RoutedEventArgs e) {
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

        public void Save() {
            RegistryHelper.IsStartUp = IsStartup;
            RegistryHelper.LoginAddress = LoginAddress;
            RegistryHelper.LogoutAddress = LogoutAddress;
            RegistryHelper.DataAddress = DataAddress;
            RegistryHelper.RefreshTime = RefreshTime;

            RegistryHelper.SaveDataFromRegistry();
        }
    }
}
