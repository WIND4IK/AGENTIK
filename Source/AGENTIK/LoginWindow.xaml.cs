using System.Windows;

namespace AGENTIK
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {

        public string Login
        {
            get { return txtBoxUserName.Text; }
        }

        public string Password
        {
            get { return passwordBox.Password; }
        }

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void OnBtnLoginClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
