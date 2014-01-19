using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using AGENTIK.Controls;
using AGENTIK.Resources;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Ribbon;
using log4net;
using Updater;

namespace AGENTIK {
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : DXRibbonWindow {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IsolatedStorageFile _isolatedStorageFile;

        private readonly Dictionary<string, string> _dictionary;

        private const string UsersFileName = "LoginData";

        private const string LoginFileName = "AutoLoginData";

        private Uri _baseAddress = new Uri("http://skylogic.mysecretar.com/mys");

        private static CookieContainer _cookieContainer;

        private bool _isLogin;

        private string _currentUser;

        private bool _isRemember;

        public bool DisableAutoLogin { get; set; }

        public LoginWindow() {
            InitializeComponent();

            _cookieContainer = new CookieContainer();

            _dictionary = new Dictionary<string, string>();

            _isolatedStorageFile = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain, null, null);

            Prepare();

            Loaded += OnLoginWindowLoaded;

            Closing += OnLoginWindowClosing;
        }

        void OnLoginWindowClosing(object sender, EventArgs e) {
            try {
                if (_isLogin) {
                    Hide();
                    ShowInTaskbar = false;

                    _isLogin = false;

                    var mainWindow = new MainWindow(_cookieContainer);
                    mainWindow.Show();

                    CheckAndUpdate();
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        void OnLoginWindowLoaded(object sender, RoutedEventArgs e) {
            try {
                ThemeManager.ApplicationThemeName = Theme.Office2007Blue.Name;

                if (!String.IsNullOrEmpty(_currentUser)) {
                    cmbBoxUserName.Text = _currentUser;
                    if (!DisableAutoLogin && chboxRemember.IsChecked.HasValue && chboxRemember.IsChecked.Value) {
                        passwordBox.Password = _dictionary[_currentUser];
                        TryLogin();
                    }
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private void Prepare() {
            try {
                Load();
                _baseAddress = RegistryHelper.LoginAddress.TryGetValidUri();
                cmbBoxUserName.ItemsSource = _dictionary.Keys.ToList();
            }
            catch (Exception ex) {
                _log.Error(ex);
                DXMessageBox.Show("Runtime Error:" + ex.Message);
            }
        }

        public void Save() {
            try {
                using (var isolatedStorageFileStream = new IsolatedStorageFileStream(UsersFileName, FileMode.Create, _isolatedStorageFile)) {
                    using (var writer = new StreamWriter(isolatedStorageFileStream)) {
                        if (cmbBoxUserName.Text.Length > 0) {
                            var password = (chboxRemember.IsChecked.HasValue && chboxRemember.IsChecked.Value) ? passwordBox.Password : "";
                            if (!_dictionary.ContainsKey(cmbBoxUserName.Text)) {
                                _dictionary.Add(cmbBoxUserName.Text, password);
                            }
                            else
                                _dictionary[cmbBoxUserName.Text] = password;
                        }

                        foreach (var keyValuePair in _dictionary) {
                            writer.WriteLine("{0}:{1}", keyValuePair.Key, keyValuePair.Value);
                        }
                    }
                }

                //Save Last Login UserName
                using (var isolatedStorageFileStream = new IsolatedStorageFileStream(LoginFileName, FileMode.Create, _isolatedStorageFile)) {
                    using (var writer = new StreamWriter(isolatedStorageFileStream)) {
                        writer.WriteLine("{0}:{1}", cmbBoxUserName.Text, chboxRemember.IsChecked.HasValue && chboxRemember.IsChecked.Value ? "1" : "0");
                    }
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
                DXMessageBox.Show("Runtime Error:" + ex.Message);
            }
        }

        public void Load() {
            try {
                using (var isolatedStorageFileStream = new IsolatedStorageFileStream(UsersFileName, FileMode.OpenOrCreate, _isolatedStorageFile)) {
                    using (var reader = new StreamReader(isolatedStorageFileStream)) {
                        while (true) {
                            var line = reader.ReadLine();
                            if (String.IsNullOrEmpty(line))
                                break;

                            var array = line.Split(':');
                            if (!_dictionary.ContainsKey(array[0]))
                                _dictionary.Add(array[0], array[1]);
                        }
                    }
                }

                //Get Last Login UserName
                using (var isolatedStorageFileStream = new IsolatedStorageFileStream(LoginFileName, FileMode.OpenOrCreate, _isolatedStorageFile)) {
                    using (var reader = new StreamReader(isolatedStorageFileStream)) {
                        var line = reader.ReadLine();
                        if(String.IsNullOrEmpty(line))
                            return;

                        var array = line.Split(':');
                        _currentUser = array[0];
                        chboxRemember.IsChecked = _isRemember = array[1].Equals("1");
                    }
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
                DXMessageBox.Show("Runtime Error:" + ex.Message);
            }
        }

        private void CheckAndUpdate() {
            try {
                var urlAddress = @"http://agentik.com/update/Agentik.exe";
                Version currentVersion = null, newVersion = null;
                string execPath = Assembly.GetExecutingAssembly().Location;
                if (!String.IsNullOrEmpty(execPath)) {
                    currentVersion = new Version(FileVersionInfo.GetVersionInfo(execPath).FileVersion);
                }

                String versionUrl = "http://agentik.com/update/Version.xml";
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(versionUrl);
                var elements = xmlDoc.GetElementsByTagName("version");
                newVersion = new Version(elements[0].InnerText);

                if (newVersion > currentVersion) {
                    if (DXMessageBox.Show("Доступна новая версия. Скачать?", "AGENTIK", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {
                        var localPath = Path.GetTempPath() + "Agentik.exe";
                        var updater = new UpdaterWindow(urlAddress, localPath);
                        updater.ShowDialog();
                        Close();
                    }
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private void OnPasswordBoxGotFocus(object sender, RoutedEventArgs e) {
            SetPassword();
        }

        private void SetPassword() {
            try {
                if (_dictionary.ContainsKey(cmbBoxUserName.Text) && !DisableAutoLogin && _isRemember)
                    passwordBox.EditValue = _dictionary[cmbBoxUserName.Text];
            }
            catch (Exception ex) {
                _log.Error(ex);
                DXMessageBox.Show("Runtime Error:" + ex.Message);
            }
        }

        private async Task<bool> Login(string login, string password) {
            try {
                using (var handler = new HttpClientHandler { CookieContainer = _cookieContainer, UseCookies = true }) {
                    using (var client = new HttpClient(handler) { BaseAddress = _baseAddress }) {
                        var content = new List<KeyValuePair<string, string>>
                            {
                                new KeyValuePair<string, string>("login", login), 
                                new KeyValuePair<string, string>("password", password), 
                                new KeyValuePair<string, string>("remember_me", "on"), 
                                new KeyValuePair<string, string>("loginCaptcha", "")
                            };

                        var requestMessage = new HttpRequestMessage(HttpMethod.Post, _baseAddress) { Content = new FormUrlEncodedContent(content) };
                        HttpResponseMessage response = await client.SendAsync(requestMessage).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();

                        var responseCookies = _cookieContainer.GetCookies(_baseAddress);

                        return responseCookies.Count == 3;
                    }
                }
            }
            catch (HttpRequestException ex) {
                _log.Error(ex.Message);
                return false;
            }
        }

        private void TryLogin() {
            try {
                _isLogin = !DisableAutoLogin ? Login(_currentUser, _dictionary[_currentUser]).Result : Login(cmbBoxUserName.Text, passwordBox.Password).Result;

                if (!_isLogin) {
                    DXMessageBox.Show("Неверный логин или пароль!", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Error);
                    passwordBox.Password = String.Empty;
                }
                else {
                    Save();
                    Close();
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private void OnPasswordBoxKeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                DoLogin();
            }
        }

        void OnLoginButtonClick(object sender, RoutedEventArgs e) {
            DoLogin();
        }

        private void DoLogin() {
            if (passwordBox.Password.Length == 0) {
                DXMessageBox.Show("Пароль не может быть пустым!", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DisableAutoLogin = true;
            TryLogin();
        }

        private void OnSettingsButtonClick(object sender, RoutedEventArgs e) {
            try {
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();

                if (settingsWindow.DialogResult != null && (bool)settingsWindow.DialogResult) {
                    if (AgentikSettingsControl.Instance.LoginAddress.Length > 0)
                        _baseAddress = AgentikSettingsControl.Instance.LoginAddress.TryGetValidUri();
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

    }
}
