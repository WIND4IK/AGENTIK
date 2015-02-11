using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using HelperChat.Interfaces;
using log4net;

namespace HelperChat.Controls {
    /// <summary>
    /// Interaction logic for ChatSettingsControl.xaml
    /// </summary>
    public partial class ChatSettingsControl : UserControl, ISettings {
        private static ChatSettingsControl _instance;
        private readonly IsolatedStorageFile _isolatedStorageFile;
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string ChatLoginFileName = "ChatLoginData";

        public static ChatSettingsControl Instance {
            get {
                return (_instance ?? (_instance = new ChatSettingsControl()));
            }
        }

        public string Login {
            get {
                return tblogin.Text;
            }
            set {
                tblogin.Text = value;
            }
        }

        public string Password {
            get {
                return password.Password;
            }
            set {
                password.Password = value;
            }
        }

        private ChatSettingsControl() {
            InitializeComponent();
            _isolatedStorageFile = IsolatedStorageFile.GetStore(IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain | IsolatedStorageScope.User, null, null);
            Load();
        }

        public void Load() {
            try {
                using (var stream = new IsolatedStorageFileStream(ChatLoginFileName, FileMode.OpenOrCreate, _isolatedStorageFile)) {
                    using (var reader = new StreamReader(stream)) {
                        var str = reader.ReadLine();
                        if (!string.IsNullOrEmpty(str)) {
                            var strArray = str.Split(new [] { ':' });
                            Login = strArray[0];
                            Password = strArray[1];
                        }
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        public void Save() {
            try
            {
                using (var stream = new IsolatedStorageFileStream(ChatLoginFileName, FileMode.Create, _isolatedStorageFile)) {
                    using (var writer = new StreamWriter(stream)) {
                        writer.WriteLine("{0}:{1}", tblogin.Text, password.Password);
                    }
                }
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null) mainWindow.TryToConnect();
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

    }
}
