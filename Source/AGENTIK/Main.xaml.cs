using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
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
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Ribbon;
using log4net;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace AGENTIK
{

    /// <summary>
    /// Interaction logic for BalloonSampleWindow.xaml
    /// </summary>
    public partial class MainWindow : DXWindow
    {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Uri _baseAddress = new Uri("http://skylogic.mysecretar.com/mys");
        private Uri _logoutUri = new Uri("http://skylogic.mysecretar.com/mys/logout");
        private Uri _dataUri = new Uri("http://skylogic.mysecretar.com/mys/xml");
        private TimeSpan _interval = new TimeSpan(0, 5, 0);

        private static CookieContainer _cookieContainer;

        private readonly ObservableCollection<ViewTicket> _treeViewSource;

        private readonly ObservableCollection<Ticket> _tickets;

        private bool _isLogin;

        private bool _isClose;

        private readonly TrayIcon _trayIcon = new TrayIcon();

        private LoginWindow _loginWindow;

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

            taskNavControl.ItemsSource = _treeViewSource;
            //_treeView.ItemsSource = _treeViewSource;
            //_listViewPriority.ItemsSource = _tickets;
            //_listViewStatus.ItemsSource = _tickets;

            Loaded += OnMainWindowLoaded;
        }

        void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            Left = SystemParameters.WorkArea.Width - ActualWidth;
            Top = SystemParameters.WorkArea.Height - ActualHeight;
            //SizeToContent = SizeToContent.Height;
            
            ThemeManager.ApplicationThemeName = Theme.Office2007Blue.Name;
        }

        private async Task<bool> Login(string login, string password)
        {
            try
            {
                using (var handler = new HttpClientHandler {CookieContainer = _cookieContainer, UseCookies = true})
                {
                    using (var client = new HttpClient(handler) {BaseAddress = _baseAddress})
                    {
                        var content = new List<KeyValuePair<string, string>>
                            {
                                new KeyValuePair<string, string>("login", login), 
                                new KeyValuePair<string, string>("password", password), 
                                new KeyValuePair<string, string>("remember_me", "on"), 
                                new KeyValuePair<string, string>("loginCaptcha", "")
                            };

                        var requestMessage = new HttpRequestMessage(HttpMethod.Post, _baseAddress) {Content = new FormUrlEncodedContent(content)};
                        HttpResponseMessage response = await client.SendAsync(requestMessage).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();

                        var responseCookies = _cookieContainer.GetCookies(_baseAddress);

                        return responseCookies.Count == 3;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _log.Error(ex.Message);
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
                        var requestMessage = new HttpRequestMessage {Method = HttpMethod.Get};
                        HttpResponseMessage response = await client.SendAsync(requestMessage).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();

                        var responseCookies = _cookieContainer.GetCookies(_logoutUri);

                        return responseCookies.Count == 3;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _log.Error(ex.Message);
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

        private void MyNotifyIconOnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            //_balloon = new FancyBalloon();
            //_balloon.BalloonText = "Мой Секретарь";
            //_balloon.TreeViewSource = _treeViewSource;
            //_balloon.ListViewPrioritySource = _tickets.OrderBy(t => t.PriorityId);
            //_balloon.ListViewStatusSource = _tickets.OrderBy(t => t.StatusId);

            ////show balloon and close it after 4 seconds
            //MyNotifyIcon.ShowCustomBalloon(_balloon, PopupAnimation.Slide, 4000);
            if (Visibility == Visibility.Hidden)
            {
                Show();
                ShowInTaskbar = true;
            }
            else if (Visibility == Visibility.Visible)
            {
                Activate();
            }

            if(_isLogin)
                RefreshIcon(0, Visibility.Hidden);
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
            catch (Exception ex)
            {
                _log.Error(ex.Message);
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

                ShowNotification();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        private void MediaElementOnMediaEnded(object sender, RoutedEventArgs routedEventArgs)
        {
            mediaElement.Source = null;
        }

        private void ShowNotification()
        {
            try
            {
                var source = _treeViewSource.Where(t => t.IsNew).ToList();

                if(!source.Any())
                    return;

                var balloon = new FancyBalloon();
                balloon.BalloonText = "Мой Секретарь";
                balloon.TreeViewSource = source;
                balloon.MouseLeftButtonUp += OnBalloonMouseLeftButtonUp;
                balloon.MouseRightButtonUp += OnBalloonMouseRightButtonUp;

                //show balloon and close it after 4 seconds
                MyNotifyIcon.ShowCustomBalloon(balloon, PopupAnimation.Slide, 4000);

                RefreshIcon(_tickets.Count, Visibility.Visible);
                var directory = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
                var path = Path.Combine(directory.FullName, "Sounds\\sound.mp3");
                mediaElement.MediaEnded += MediaElementOnMediaEnded;
                mediaElement.Source = new Uri(path);
                mediaElement.Play();

            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        void OnBalloonMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                MyNotifyIcon.CloseBalloon();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        void OnBalloonMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Visibility == Visibility.Hidden)
                    Visibility = Visibility.Visible;
                else
                    Activate();
                MyNotifyIcon.CloseBalloon();
                RefreshIcon(0, Visibility.Hidden);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        private void RefreshIcon(int count, Visibility visibility)
        {
            try
            {
                _trayIcon.ItemCounter = count;
                _trayIcon.ItemCounterVisibility = visibility;
                var stream = CreateImage();
                MyNotifyIcon.IconStream = stream;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        private Stream CreateImage()
        {
            Matrix m = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice;
            Point dpi = m.Transform(new Point(96, 96));

            bool measureValid = _trayIcon.IsMeasureValid;

            if (!measureValid)
            {
                var size = new Size(32, 32);
                _trayIcon.Measure(size);
                _trayIcon.Arrange(new Rect(size));
            }

            var bmp = new RenderTargetBitmap((int)_trayIcon.RenderSize.Width, (int)_trayIcon.RenderSize.Height, dpi.X, dpi.Y, PixelFormats.Default);

            // this is waiting for dispatcher to perform measure, arrange and render passes
            _trayIcon.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);

            bmp.Render(_trayIcon);

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
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            return null;
        }

        private void LogoutOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _isLogin = !Logout().Result;

            _tickets.Clear();
            _treeViewSource.Clear();

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
                _loginWindow = new LoginWindow();
                _loginWindow.btnLogin.Click += OnLoginButtonClick;
                _loginWindow.PasswordBox.KeyUp += OnPasswordBoxKeyUp;
                _loginWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
                MessageBox.Show("Ошибка при Login!", "Ошибка!", MessageBoxButton.OK);
            }
        }

        private void OnPasswordBoxKeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                TryLogin();
        }

        void OnLoginButtonClick(object sender, RoutedEventArgs e)
        {
            _loginWindow.btnLogin.Focus();
            TryLogin();
        }

        private void TryLogin()
        {
            try
            {
                _isLogin = Login(_loginWindow.Login, _loginWindow.Password).Result;
                if (!_isLogin)
                {
                    MessageBox.Show("Неверный логин или пароль!", "Внимание!", MessageBoxButton.OK);
                    _loginWindow.Password = String.Empty;
                }
                else
                {
                    RefreshData();
                    var dispatcherTimer = new DispatcherTimer();
                    dispatcherTimer.Tick += Callback;
                    dispatcherTimer.Interval = _interval;
                    dispatcherTimer.Start();

                    if(_loginWindow.Remember)
                        _loginWindow.Save();
                    _loginWindow.Close();
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }

        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            if(_isLogin)
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
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }
    }
}
