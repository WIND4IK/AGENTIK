using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AGENTIK.Controls.AutoCompleteTextBox;

namespace AGENTIK
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly IsolatedStorageFile _isolatedStorageFile = null;

        private readonly Dictionary<string, string> _dictionary;

        private const string FileName = "LoginData";

        public string Login
        {
            get { return txtBoxUserName.Text; }
        }

        public string Password
        {
            get { return passwordBox.Password; }
            set { passwordBox.Password = value; }
        }

        public PasswordBox PasswordBox
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
                foreach (var pair in _dictionary)
                    txtBoxUserName.AddItem(new AutoCompleteEntry(pair.Key, pair.Key));
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
                    if (!_dictionary.ContainsKey(txtBoxUserName.Text))
                    {
                        using (var writer = new StreamWriter(isolatedStorageFileStream))
                        {
                            writer.WriteLine("{0}:{1}", txtBoxUserName.Text, passwordBox.Password);
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
                if (_dictionary.ContainsKey(txtBoxUserName.Text))
                    passwordBox.Password = _dictionary[txtBoxUserName.Text];
                SetSelection(passwordBox, passwordBox.Password.Length, 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Runtime Error:" + ex.Message);
            }
        }

        private void SetSelection(PasswordBox passwordBox, int start, int length)
        {
            passwordBox.GetType()
                       .GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic)
                       .Invoke(passwordBox, new object[] { start, length });
        } 
    }
}
