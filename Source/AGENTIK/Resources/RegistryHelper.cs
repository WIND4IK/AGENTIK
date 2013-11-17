using System;
using System.Reflection;
using System.Windows;
using log4net;
using Microsoft.Win32;

namespace AGENTIK.Resources {
    public static class RegistryHelper {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public const string ApplicationKeyName = "Software\\AGENTIK";
        private const string LoginKey = "LoginUri";
        private const string LogoutKey = "LogoutUri";
        private const string DataKey = "DataUri";
        private const string RefreshTimeKey = "RefreshTime";
        private static RegistryKey _registryKey;
        public static readonly string ApplicationName = Assembly.GetExecutingAssembly().GetName().Name ?? "AGENTIK";

        private static bool _isStatrtUp;

        static RegistryHelper() {
            LoadDataFromRegistry();
        }

        public static string LoginAddress { get; set; }
        public static DateTime RefreshTime { get; set; }

        public static string DataAddress { get; set; }

        public static string LogoutAddress { get; set; }

        public static bool IsStartUp {
            get {
                _isStatrtUp = IsInStartUp();
                return _isStatrtUp;
            }
            set { _isStatrtUp = value; }
        }

        private static bool IsInStartUp() {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            if (registryKey == null) {
                Log.Error("Registry key is null");
                return false;
            }

            return registryKey.GetValue(ApplicationName) != null;
        }
        private static void RegisterInStartup(bool isChecked) {
            try {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (registryKey == null) {
                    Log.Error("Registry key is null");
                    return;
                }

                if (isChecked) {
                    var address = Assembly.GetExecutingAssembly().Location;
                    registryKey.SetValue(ApplicationName, address);
                }
                else {
                    var exist = registryKey.GetValue(ApplicationName);
                    if (exist != null)
                        registryKey.DeleteValue(ApplicationName);
                }

            }
            catch (Exception ex) {
                Log.Error(ex.Message);
                MessageBox.Show("Ошибка!", "Внимание", MessageBoxButton.OK);
            }
        }

        public static void LoadDataFromRegistry() {
            try {
                RegistryKey hklm = Registry.CurrentUser;
                _registryKey = hklm.CreateSubKey(ApplicationKeyName);

                if (_registryKey == null)
                    return;

                if (_registryKey.GetValue(LoginKey) != null && !String.IsNullOrEmpty(_registryKey.GetValue(LoginKey).ToString()))
                    LoginAddress = _registryKey.GetValue(LoginKey).ToString();
                if (_registryKey.GetValue(LogoutKey) != null && !String.IsNullOrEmpty(_registryKey.GetValue(LogoutKey).ToString()))
                    LogoutAddress = _registryKey.GetValue(LogoutKey).ToString();
                if (_registryKey.GetValue(DataKey) != null && !String.IsNullOrEmpty(_registryKey.GetValue(LogoutKey).ToString()))
                    DataAddress = _registryKey.GetValue(DataKey).ToString();
                if (_registryKey.GetValue(RefreshTimeKey) != null)
                    RefreshTime = DateTime.Parse(_registryKey.GetValue(RefreshTimeKey).ToString());
            }
            catch (Exception ex) {
                Log.Error(ex.Message);
                MessageBox.Show("Ошибка при загрузке данных из реестра", "Внимание", MessageBoxButton.OK);
            }
        }

        public static void SaveDataFromRegistry() {
            try {
                RegistryKey hklm = Registry.CurrentUser;
                _registryKey = hklm.CreateSubKey(ApplicationKeyName);

                if (_registryKey == null)
                    return;

                _registryKey.SetValue(LoginKey, LoginAddress);
                _registryKey.SetValue(LogoutKey, LogoutAddress);
                _registryKey.SetValue(DataKey, DataAddress);
                _registryKey.SetValue(RefreshTimeKey, RefreshTime);

                RegisterInStartup(IsStartUp);
            }
            catch (Exception ex) {
                Log.Error(ex.Message);
                MessageBox.Show("Ошибка при сохранении настроек", "Внимание", MessageBoxButton.OK);
            }
        }
    }
}