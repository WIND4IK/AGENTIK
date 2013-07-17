using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Ribbon;

namespace AGENTIK
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : DXRibbonWindow
    {
        private readonly IsolatedStorageFile _isolatedStorageFile;

        private readonly Dictionary<string, string> _dictionary;

        private const string FileName = "LoginData";

        public string Login
        {
            get { return cmbBoxUserName.Text; }
        }

        public string Password
        {
            get { return passwordBox.Password; }
            set { passwordBox.Password = value; }
        }

        public PasswordBoxEdit PasswordBox
        {
            get { return passwordBox; } 
        }

        public bool Remember
        {
            get { return chboxRemember.IsChecked.HasValue && chboxRemember.IsChecked.Value; }
            set { chboxRemember.IsChecked = value; }
        }

        public LoginWindow()
        {
            InitializeComponent();

            _dictionary = new Dictionary<string, string>();

            _isolatedStorageFile = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain, null, null);

            Prepare();
        }

        private void Prepare()
        {
            try
            {
                Load();
                cmbBoxUserName.ItemsSource = _dictionary.Keys.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Runtime Error:" + ex.Message);
            }
        }

        public void Save()
        {
            try
            {
                using (var isolatedStorageFileStream = new IsolatedStorageFileStream(FileName, FileMode.OpenOrCreate, _isolatedStorageFile))
                {
                    if (!_dictionary.ContainsKey(cmbBoxUserName.Text))
                    {
                        using (var writer = new StreamWriter(isolatedStorageFileStream))
                        {
                            writer.WriteLine("{0}:{1}", cmbBoxUserName.Text, passwordBox.Password);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Runtime Error:" + ex.Message);
            }
        }

        public void Load()
        {
            try
            {
                using (var isolatedStorageFileStream = new IsolatedStorageFileStream(FileName, FileMode.OpenOrCreate, _isolatedStorageFile))
                {
                    using (var reader = new StreamReader(isolatedStorageFileStream))
                    {
                        while (true)
                        {
                            var line = reader.ReadLine();
                            if (String.IsNullOrEmpty(line))
                                break;

                            var array = line.Split(':');
                            _dictionary.Add(array[0], array[1]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Runtime Error:" + ex.Message);
            }
        }

        private void OnPasswordBoxGotFocus(object sender, RoutedEventArgs e)
        {
            SetPassword();
        }

        private void SetPassword()
        {
            try
            {
                if (_dictionary.ContainsKey(cmbBoxUserName.Text))
                    passwordBox.EditValue = _dictionary[cmbBoxUserName.Text];
            }
            catch (Exception ex)
            {
                MessageBox.Show("Runtime Error:" + ex.Message);
            }
        }
    }
}
