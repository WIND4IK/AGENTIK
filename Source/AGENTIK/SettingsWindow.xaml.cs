using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using Microsoft.Win32;

namespace AGENTIK
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public const string ApplicationKeyName = "Software\\AGENTIK";

        private RegistryKey _registryKey;

        private const string _loginKey = "LoginUri";
        private const string _logoutKey = "LogoutUri";
        private const string _dataKey = "DataUri";
        private const string _refreshTimeKey = "RefreshTime";
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
            chbStart.IsChecked = IsInStartUp();
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
            get { return timePicker.Value.HasValue ? timePicker.Value.Value : new DateTime(0,0,0,0,5,0); }
            set { timePicker.Value = value; }
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
            catch (Exception)
            {
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

                if(_registryKey.GetValue(_loginKey) != null)
                    LoginAddress = _registryKey.GetValue(_loginKey).ToString();
                if (_registryKey.GetValue(_logoutKey) != null)
                    LogoutAddtess = _registryKey.GetValue(_logoutKey).ToString();
                if (_registryKey.GetValue(_dataKey) != null)
                    DataAddress = _registryKey.GetValue(_dataKey).ToString();
                if (_registryKey.GetValue(_refreshTimeKey) != null)
                    RefreshTime = DateTime.Parse(_registryKey.GetValue(_refreshTimeKey).ToString());
            }
            catch(Exception ex)
            {
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

                _registryKey.SetValue(_loginKey, LoginAddress);
                _registryKey.SetValue(_logoutKey, LogoutAddtess);
                _registryKey.SetValue(_dataKey, DataAddress);
                _registryKey.SetValue(_refreshTimeKey, RefreshTime);

                var isChecked = chbStart.IsChecked.HasValue && chbStart.IsChecked.Value;
                RegisterInStartup(isChecked);
            }
            catch (Exception ex)
            {
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
