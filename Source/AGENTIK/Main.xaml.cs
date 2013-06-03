using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using AGENTIK;
using Clerk;

namespace Agent24
{
    /// <summary>
    /// Interaction logic for BalloonSampleWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Uri _baseAddress = new Uri("http://skylogic.mysecretar.com/mys");

        private static CookieContainer _cookieContainer;

        private FancyBalloon _balloon;

        private readonly List<ViewTicket> _treeViewSource;

        private List<Ticket> _tickets;

        private bool _isLogin;

        private TrayIcon trayIcon = new TrayIcon();

        public bool IsLogin
        {
            get { return _isLogin; }
        }

        public MainWindow()
        {
            InitializeComponent();

            _cookieContainer = new CookieContainer();

            _treeViewSource = new List<ViewTicket>();

            _tickets = new List<Ticket>();

            _isLogin = false;
        }

        private async Task<bool> Login(string login, string password)
        {
            try
            {
                using (var handler = new HttpClientHandler() {CookieContainer = _cookieContainer, UseCookies = true})
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
                using (var handler = new HttpClientHandler() {CookieContainer = _cookieContainer, UseCookies = true})
                {
                    using (var client = new HttpClient(handler) {BaseAddress = _baseAddress})
                    {
                        var baseAddress = new Uri("http://skylogic.mysecretar.com/mys/logout");

                        var requestMessage = new HttpRequestMessage();
                        requestMessage.Method = HttpMethod.Get;
                        HttpResponseMessage response = await client.SendAsync(requestMessage).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();

                        var responseCookies = _cookieContainer.GetCookies(baseAddress);

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
            //clean up notifyicon (would otherwise stay open until application finishes)
            MyNotifyIcon.Dispose();

            base.OnClosing(e);
        }

        private void MyNotifyIcon_OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            _balloon = new FancyBalloon();
            _balloon.BalloonText = "Мой Секретарь";
            _balloon.TreeViewSource = _treeViewSource;
            _balloon.ListViewPrioritySource = _tickets.OrderBy(t => t.PriorityId);
            _balloon.ListViewStatusSource = _tickets.OrderBy(t => t.StatusId);

            //show balloon and close it after 4 seconds
            MyNotifyIcon.ShowCustomBalloon(_balloon, PopupAnimation.Slide, 4000);
        }

        private void OnLoginClick(object sender, RoutedEventArgs e)
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
                    dispatcherTimer.Interval = new TimeSpan(0, 5, 0);
                    dispatcherTimer.Start();
                    MyNotifyIcon.IconSource = new BitmapImage(new Uri("pack://application:,,,/Icons/NetDrives.ico"));
                    Cursor = Cursors.Arrow;
                }
            }
            catch (Exception)
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _isLogin = !Logout().Result;
                MyNotifyIcon.IconSource = new BitmapImage(new Uri("pack://application:,,,/Icons/Error.ico"));
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка при Logout!", "Ошибка!", MessageBoxButton.OK);
            }
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            _isLogin = !Logout().Result;
            Close();
        }

        private void Callback(object sender, EventArgs e)
        {
            RefreshData();
        }

        private async Task<List<Ticket>> GetTickets()
        {
            try
            {
                using (var handler = new HttpClientHandler {CookieContainer = _cookieContainer, UseCookies = true})
                {
                    using (var client = new HttpClient(handler) {BaseAddress = _baseAddress})
                    {

                        string url = String.Format("http://skylogic.mysecretar.com/mys/xml");
                        var address = new Uri(url);

                        Stream stream = await client.GetStreamAsync(address).ConfigureAwait(false);
                        XDocument document = XDocument.Load(stream);
                        var serializer = new XmlSerializer(typeof (Ticket));
                        var elements = document.Root.Elements().ToArray();
                        var tickets = elements.Select(element => (Ticket) serializer.Deserialize(element.CreateReader())).ToList();

                        return tickets;
                    }
                }
            }
            catch (Exception e)
            {
                return new List<Ticket>();
            }
        }

        private void RefreshData()
        {
            try
            {
                _tickets = GetTickets().Result;
                _treeViewSource.Clear();

                var contractors = _tickets.Select(t => new { ID = t.Contractor, Name = t.NameContractor }).ToList();
                foreach (var contractor in contractors)
                {
                    var maitTicket = new ViewTicket(null);
                    maitTicket.Title = String.Format("({1}){0}", contractor.Name, _tickets.Count(t => t.Contractor == contractor.ID));

                    foreach (var ticket in _tickets.Where(t => t.Contractor == contractor.ID))
                    {
                        var viewTicket = new ViewTicket(ticket);
                        viewTicket.Title = ticket.Theme;

                        maitTicket.Children.Add(viewTicket);
                    }

                    _treeViewSource.Add(maitTicket);
                }
                RefreshIcon();
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
                MyNotifyIcon.IconSource = CreateImage();
            }
            catch (Exception)
            {
            }
        }

        private BitmapImage CreateImage()
        {
            Matrix m = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice;
            Point dpi = m.Transform(new Point(32, 32));

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

        private BitmapImage ConvertToBitmap(RenderTargetBitmap renderTargetBitmap)
        {
            try
            {
                var bitmapImage = new BitmapImage();
                var bitmapEncoder = new BmpBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                using (var stream = new MemoryStream())
                {
                    bitmapEncoder.Save(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                }
                return bitmapImage;
            }
            catch (Exception)
            {
                
            }
            return null;
        }

        private void LogoutOnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Logout();
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
    }
}
