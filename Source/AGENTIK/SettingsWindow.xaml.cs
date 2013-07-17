using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid.LookUp;
using DevExpress.Xpf.Ribbon;
using Microsoft.Win32;
using log4net;

namespace AGENTIK
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : DXRibbonWindow
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public const string ApplicationKeyName = "Software\\AGENTIK";

        private RegistryKey _registryKey;

        private const string LoginKey = "LoginUri";
        private const string LogoutKey = "LogoutUri";
        private const string DataKey = "DataUri";
        private const string RefreshTimeKey = "RefreshTime";
        private readonly string _applicationName = "AGENTIK";

        public SettingsWindow()
        {
            InitializeComponent();
            _applicationName = Assembly.GetExecutingAssembly().GetName().Name;
            Loaded += SettingsWindowLoaded;
        }

        void SettingsWindowLoaded(object sender, RoutedEventArgs e)
        {
            LoadDataFromRegistry();

            leTheme.ItemsSource = Theme.Themes.ToList().Where(t => !t.Name.Equals(Theme.TouchlineDark.Name)).ToList();
            leTheme.EditValue = ThemeManager.ApplicationThemeName ?? Theme.DeepBlue.Name;
            leTheme.EditValueChanged += OnLeThemeEditValueChanged;
            chbStart.IsChecked = IsInStartUp();
        }

        void OnLeThemeEditValueChanged(object sender, RoutedEventArgs e)
        {
            var control = (LookUpEdit) sender;
            if(control == null || control.EditValue == null || control.EditValue.ToString().Length == 0)
                return;

            ThemeManager.ApplicationThemeName = control.EditValue.ToString();
        }

        public string LoginAddress
        {
            get { return txtMain.Text; }
            set { txtMain.Text = value; }
        }

        public string LogoutAddtess
        {
            get { return txtLogout.Text; }
            set { txtLogout.Text = value; }
        }

        public string DataAddress
        {
            get { return txtData.Text; }
            set { txtData.Text = value; }
        }

        public DateTime RefreshTime
        {
            get { return timePicker.DateTime; }
            set { timePicker.DateTime = value; }
        }

        private bool IsInStartUp()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            return registryKey.GetValue(_applicationName) != null;
        }

        private void RegisterInStartup(bool isChecked)
        {
            try
            {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (isChecked)
                {
                    var address = Assembly.GetExecutingAssembly().Location;
                    registryKey.SetValue(_applicationName, address);
                }
                else
                {
                    var exist = registryKey.GetValue(_applicationName);
                    if(exist != null)
                        registryKey.DeleteValue(_applicationName);
                }

            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                MessageBox.Show("Ошибка!", "Внимание", MessageBoxButton.OK);
            }
        }

        protected void LoadDataFromRegistry()
        {
            try
            {
                RegistryKey hklm = Registry.CurrentUser;
                _registryKey = hklm.CreateSubKey(ApplicationKeyName);

                if (_registryKey == null)
                    return;

                if(_registryKey.GetValue(LoginKey) != null)
                    LoginAddress = _registryKey.GetValue(LoginKey).ToString();
                if (_registryKey.GetValue(LogoutKey) != null)
                    LogoutAddtess = _registryKey.GetValue(LogoutKey).ToString();
                if (_registryKey.GetValue(DataKey) != null)
                    DataAddress = _registryKey.GetValue(DataKey).ToString();
                if (_registryKey.GetValue(RefreshTimeKey) != null)
                    RefreshTime = DateTime.Parse(_registryKey.GetValue(RefreshTimeKey).ToString());
            }
            catch(Exception ex)
            {
                _log.Error(ex.Message);
                MessageBox.Show("Ошибка при загрузке данных из реестра", "Внимание", MessageBoxButton.OK);
            }
        }

        protected void SaveDataFromRegistry()
        {
            try
            {
                RegistryKey hklm = Registry.CurrentUser;
                _registryKey = hklm.CreateSubKey(ApplicationKeyName);

                if (_registryKey == null)
                    return;

                _registryKey.SetValue(LoginKey, LoginAddress);
                _registryKey.SetValue(LogoutKey, LogoutAddtess);
                _registryKey.SetValue(DataKey, DataAddress);
                _registryKey.SetValue(RefreshTimeKey, RefreshTime);

                var isChecked = chbStart.IsChecked.HasValue && chbStart.IsChecked.Value;
                RegisterInStartup(isChecked);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                MessageBox.Show("Ошибка при сохранении настроек", "Внимание", MessageBoxButton.OK);
            }
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            SaveDataFromRegistry();
            Close();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
