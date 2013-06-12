using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Xml.Serialization;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace AGENTIK
{

    /// <summary>
    /// Interaction logic for BalloonSampleWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Uri _baseAddress = new Uri("http://skylogic.mysecretar.com/mys");
        private Uri _logoutUri = new Uri("http://skylogic.mysecretar.com/mys/logout");
        private Uri _dataUri = new Uri("http://skylogic.mysecretar.com/mys/xml");
        private TimeSpan _interval = new TimeSpan(0, 5, 0);

        private static CookieContainer _cookieContainer;

        private readonly ObservableCollection<ViewTicket> _treeViewSource;

        private ObservableCollection<Ticket> _tickets;

        private bool _isLogin;

        private bool _isClose;

        private TrayIcon trayIcon = new TrayIcon();

        public bool IsLogin
        {
            get { return _isLogin; }
        }

        public MainWindow()
        {
            InitializeComponent();

            _cookieContainer = new CookieContainer();

            _treeViewSource = new ObservableCollection<ViewTicket>();

            _tickets = new ObservableCollection<Ticket>();

            _isLogin = false;

            _treeView.ItemsSource = _treeViewSource;
            _listViewPriority.ItemsSource = _tickets;
            _listViewStatus.ItemsSource = _tickets;
        }

        private async Task<bool> Login(string login, string password)
        {
            try
            {
                using (var handler = new HttpClientHandler {CookieContainer = _cookieContainer, UseCookies = true})
                {
                    using (var client = new HttpClient(handler) {BaseAddress = _baseAddress})
                    {
                        var content = new List<KeyValuePair<string, string>>();
                        content.Add(new KeyValuePair<string, string>("login", login));
                        content.Add(new KeyValuePair<string, string>("password", password));
                        content.Add(new KeyValuePair<string, string>("remember_me", "on"));
                        content.Add(new KeyValuePair<string, string>("loginCaptcha", ""));

                        var requestMessage = new HttpRequestMessage(HttpMethod.Post, _baseAddress);
                        requestMessage.Content = new FormUrlEncodedContent(content);
                        HttpResponseMessage response = await client.SendAsync(requestMessage).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();

                        var responseCookies = _cookieContainer.GetCookies(_baseAddress);

                        return responseCookies.Count == 3;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return false;
            }
        }

        private async Task<bool> Logout()
        {
            try
            {
                using (var handler = new HttpClientHandler {CookieContainer = _cookieContainer, UseCookies = true})
                {
                    using (var client = new HttpClient(handler) {BaseAddress = _baseAddress})
                    {
                        var requestMessage = new HttpRequestMessage();
                        requestMessage.Method = HttpMethod.Get;
                        HttpResponseMessage response = await client.SendAsync(requestMessage).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();

                        var responseCookies = _cookieContainer.GetCookies(_logoutUri);

                        return responseCookies.Count == 3;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return false;
            }
        }


        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_isClose)
            {
                //clean up notifyicon (would otherwise stay open until application finishes)
                MyNotifyIcon.Dispose();

                base.OnClosing(e);
            }
            else
            {
                ShowInTaskbar = false;
                Visibility = Visibility.Hidden;
                e.Cancel = true;
            }
        }

        private void MyNotifyIcon_OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            //_balloon = new FancyBalloon();
            //_balloon.BalloonText = "Мой Секретарь";
            //_balloon.TreeViewSource = _treeViewSource;
            //_balloon.ListViewPrioritySource = _tickets.OrderBy(t => t.PriorityId);
            //_balloon.ListViewStatusSource = _tickets.OrderBy(t => t.StatusId);

            ////show balloon and close it after 4 seconds
            //MyNotifyIcon.ShowCustomBalloon(_balloon, PopupAnimation.Slide, 4000);
            Show();
            ShowInTaskbar = true;
        }

        private void OnLoginClick(object sender, RoutedEventArgs e)
        {
        }

        void OnLoginWindowClosed(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                var loginWindow = (LoginWindow) sender;
                _isLogin = Login(loginWindow.Login, loginWindow.Password).Result;
                if (!_isLogin)
                {
                    Cursor = Cursors.Arrow;
                    if (MessageBox.Show("Неверный логин или пароль!", "Внимание!", MessageBoxButton.OK) == MessageBoxResult.OK)
                    {
                        //loginWindow.ShowDialog();
                    }
                }
                else
                {
                    RefreshData();
                    var dispatcherTimer = new DispatcherTimer();
                    dispatcherTimer.Tick += Callback;
                    dispatcherTimer.Interval = _interval;
                    dispatcherTimer.Start();
                    //MyNotifyIcon.IconSource = new BitmapImage(new Uri("pack://application:,,,/Icons/NetDrives.ico"));
                    Cursor = Cursors.Arrow;
                }
            }
            catch (Exception)
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            _isLogin = !Logout().Result;
            _isClose = true;
            Close();
        }

        private void Callback(object sender, EventArgs e)
        {
            RefreshData();
        }

        private async Task<ObservableCollection<Ticket>> GetTickets()
        {
            try
            {
                using (var handler = new HttpClientHandler {CookieContainer = _cookieContainer, UseCookies = true})
                {
                    using (var client = new HttpClient(handler) {BaseAddress = _baseAddress})
                    {
                        Stream stream = await client.GetStreamAsync(_dataUri).ConfigureAwait(false);
                        XDocument document = XDocument.Load(stream);
                        var serializer = new XmlSerializer(typeof (Ticket));
                        var elements = document.Root.Elements().ToArray();
                        var tickets = elements.Select(element => (Ticket) serializer.Deserialize(element.CreateReader())).ToList();

                        return new ObservableCollection<Ticket>(tickets);
                    }
                }
            }
            catch (Exception e)
            {
                return new ObservableCollection<Ticket>();
            }
        }

        private void RefreshData()
        {
            try
            {
                _tickets.Clear();
                _treeViewSource.Clear();

                foreach (var ticket in GetTickets().Result)
                    _tickets.Add(ticket);

                var contractors = _tickets.Select(t => new { ID = t.Contractor, Name = t.NameContractor }).ToList();
                foreach (var contractor in contractors)
                {
                    var maitTicket = new ViewTicket(null);
                    maitTicket.Title = contractor.Name;

                    foreach (var ticket in _tickets.Where(t => t.Contractor == contractor.ID))
                    {
                        var viewTicket = new ViewTicket(ticket);
                        viewTicket.Title = ticket.Theme;

                        maitTicket.Children.Add(viewTicket);
                    }

                    _treeViewSource.Add(maitTicket);
                }

                RefreshIcon();
                mediaElement.Play();
            }
            catch (Exception)
            {

            }
        }

        private void RefreshIcon()
        {
            try
            {
                trayIcon.ItemCounter = _tickets.Count;
                trayIcon.ItemCounterVisibility = Visibility.Visible;
                var stream = CreateImage();
                MyNotifyIcon.IconStream = stream;
            }
            catch (Exception)
            {
            }
        }

        private Stream CreateImage()
        {
            Matrix m = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice;
            Point dpi = m.Transform(new Point(96, 96));

            bool measureValid = trayIcon.IsMeasureValid;

            if (!measureValid)
            {
                var size = new Size(32, 32);
                trayIcon.Measure(size);
                trayIcon.Arrange(new Rect(size));
            }

            var bmp = new RenderTargetBitmap((int)trayIcon.RenderSize.Width, (int)trayIcon.RenderSize.Height, dpi.X, dpi.Y, PixelFormats.Default);

            // this is waiting for dispatcher to perform measure, arrange and render passes
            trayIcon.Dispatcher.Invoke(((Action)(() => { })), DispatcherPriority.Background);

            bmp.Render(trayIcon);

            return ConvertToBitmap(bmp);
        }

        private Stream ConvertToBitmap(RenderTargetBitmap renderTargetBitmap)
        {
            try
            {

                var bitmapEncoder = new PngBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                var stream = new MemoryStream();
                bitmapEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                return stream;
            }
            catch (Exception)
            {
                
            }
            return null;
        }

        private void LogoutOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _isLogin = !Logout().Result;
            MyNotifyIcon.IconSource = new BitmapImage(new Uri("pack://application:,,,/Icons/Error.ico"));
        }

        private void OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsLogin;
            e.Handled = true;
        }

        private void RefreshOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            RefreshData();
        }

        private void OnLoginCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void LoginOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var loginWindow = new LoginWindow();
                loginWindow.Closed += OnLoginWindowClosed;
                loginWindow.ShowDialog();
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка при Login!", "Ошибка!", MessageBoxButton.OK);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        	RefreshData();
        }

        private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();

                if (settingsWindow.DialogResult != null && (bool) settingsWindow.DialogResult)
                {
                    if(settingsWindow.LoginAddress.Length > 0)
                        _baseAddress = new Uri(settingsWindow.LoginAddress);
                    if(settingsWindow.LogoutAddtess.Length > 0)
                        _logoutUri = new Uri(settingsWindow.LogoutAddtess);
                    if(settingsWindow.DataAddress.Length > 0)
                        _dataUri = new Uri(settingsWindow.DataAddress);
                    _interval = settingsWindow.RefreshTime.TimeOfDay;
                }
            }
            catch (Exception)
            {
                
            }
        }
    }
}
