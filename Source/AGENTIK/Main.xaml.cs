using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using DevExpress.Data.PLinq.Helpers;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.NavBar;
using log4net;
using Updater;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace AGENTIK {

    /// <summary>
    /// Interaction logic for BalloonSampleWindow.xaml
    /// </summary>
    public partial class MainWindow : DXWindow {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Uri _baseAddress = new Uri("http://skylogic.mysecretar.com/mys");
        private Uri _logoutUri = new Uri("http://skylogic.mysecretar.com/mys/logout");
        private Uri _dataUri = new Uri("http://skylogic.mysecretar.com/mys/xml");
        private TimeSpan _interval = new TimeSpan(0, 5, 0);

        private static CookieContainer _cookieContainer;

        private readonly List<ViewTicket> _treeViewSource;

        private readonly List<Ticket> _tickets;

        private bool _isClose;

        private readonly TrayIcon _trayIcon = new TrayIcon();

        public MainWindow() {}

        public MainWindow(CookieContainer cookieContainer) {
            InitializeComponent();

            _cookieContainer = cookieContainer;

            _treeViewSource = new List<ViewTicket>();

            _tickets = new List<Ticket>();

            Loaded += OnMainWindowLoaded;
        }

        void OnMainWindowLoaded(object sender, RoutedEventArgs e) {
            Left = SystemParameters.WorkArea.Width - ActualWidth;
            Top = SystemParameters.WorkArea.Height - ActualHeight;
            //SizeToContent = SizeToContent.Height;

            LoadData();
        }

        private void LoadData() {
            try {
                RefreshData();
                var dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Tick += Callback;
                dispatcherTimer.Interval = _interval;
                dispatcherTimer.Start();
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }


        private async void Logout() {
            try {
                using (var handler = new HttpClientHandler { CookieContainer = _cookieContainer, UseCookies = true }) {
                    using (var client = new HttpClient(handler) { BaseAddress = _baseAddress }) {
                        var requestMessage = new HttpRequestMessage { Method = HttpMethod.Get };
                        HttpResponseMessage response = await client.SendAsync(requestMessage).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                    }
                }
            }
            catch (HttpRequestException ex) {
                _log.Error(ex.Message);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e) {
            if (_isClose) {
                //clean up notifyicon (would otherwise stay open until application finishes)
                MyNotifyIcon.Dispose();

                base.OnClosing(e);
            }
            else {
                ShowInTaskbar = false;
                Visibility = Visibility.Hidden;
                e.Cancel = true;
            }
        }

        private void MyNotifyIconOnTrayMouseDoubleClick(object sender, RoutedEventArgs e) {
            //_balloon = new FancyBalloon();
            //_balloon.BalloonText = "Мой Секретарь";
            //_balloon.TreeViewSource = _treeViewSource;
            //_balloon.ListViewPrioritySource = _tickets.OrderBy(t => t.PriorityId);
            //_balloon.ListViewStatusSource = _tickets.OrderBy(t => t.StatusId);

            ////show balloon and close it after 4 seconds
            //MyNotifyIcon.ShowCustomBalloon(_balloon, PopupAnimation.Slide, 4000);
            if (Visibility == Visibility.Hidden) {
                Show();
                ShowInTaskbar = true;
            }
            else if (Visibility == Visibility.Visible) {
                Activate();
            }

            RefreshIcon(0, Visibility.Hidden);
        }

        private void OnExitClick(object sender, RoutedEventArgs e) {
            Logout();
            _isClose = true;
            Close();
        }

        private void Callback(object sender, EventArgs e) {
            RefreshData();
        }

        private async Task<ObservableCollection<Ticket>> GetTickets() {
            try {
                using (var handler = new HttpClientHandler { CookieContainer = _cookieContainer, UseCookies = true }) {
                    using (var client = new HttpClient(handler) { BaseAddress = _baseAddress }) {
                        Stream stream = await client.GetStreamAsync(_dataUri).ConfigureAwait(false);
                        XDocument document = XDocument.Load(stream);
                        var serializer = new XmlSerializer(typeof(Ticket));
                        var elements = document.Root.Elements().ToArray();
                        var tickets = elements.Select(element => (Ticket)serializer.Deserialize(element.CreateReader())).ToList();

                        return new ObservableCollection<Ticket>(tickets);
                    }
                }
            }
            catch (Exception ex) {
                _log.Error(ex.Message);
                return new ObservableCollection<Ticket>();
            }
        }

        private void RefreshData() {
            try {
                _tickets.Clear();
                _treeViewSource.Clear();

                foreach (var ticket in GetTickets().Result)
                    _tickets.Add(ticket);

                var contractors = _tickets.Select(t => new {t.RowProperty.Contractor.ID, t.RowProperty.Contractor.Name }).Distinct().ToList();
                foreach (var contractor in contractors) {
                    var maitTicket = new ViewTicket(null) {Title = contractor.Name};

                    foreach (var ticket in _tickets.Where(t => t.RowProperty.Contractor.ID == contractor.ID)) {
                        var viewTicket = new ViewTicket(ticket) {Title = ticket.RowType.Theme};

                        maitTicket.Children.Add(viewTicket);
                    }

                    _treeViewSource.Add(maitTicket);
                }

                navBar.Groups.Clear();
                var types = _tickets.Where(t => !String.IsNullOrEmpty(t.RowType.TypeRow)).Select(t => t.RowType.TypeRow).Distinct().ToList();
                foreach (var type in types) {
                    var typeTickets = _treeViewSource.SelectByType(type);
                    AddNavBarGroup(typeTickets);
                }
                AddTimerButtons();

                ShowNotification();
            }
            catch (Exception ex) {
                _log.Error(ex.Message);
            }
        }

        private void AddNavBarGroup(IEnumerable<ViewTicket> viewTickets) {
            try {
                var navBarContentTemplate = FindResource("NavBarGroupContentTemplate") as DataTemplate;
                if (navBarContentTemplate != null) {
                    RowType rowType = viewTickets.First().Children.First().Ticket.RowType;
                    var navBarGroup = new NavBarGroup { DisplaySource = DisplaySource.Content, Header = rowType.Name, ImageSource = GetImageFromUrl(rowType.Icon), ContentTemplate = navBarContentTemplate, Content = viewTickets };
                    navBar.Groups.Add(navBarGroup);
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private static BitmapImage GetImageFromUrl(Uri uri) {
            string file = Path.GetTempFileName();
            var webClient = new WebClient();
            webClient.DownloadFile(uri.AbsoluteUri, file);
            var filepath = Path.GetFullPath(file);
            var pictureUri = new Uri(filepath, UriKind.Absolute);
            return new BitmapImage(pictureUri);
        }

        private void AddTimerButtons() {
            try {
                var navBarContentTemplate = FindResource("NavBarGroupContentTemplate") as DataTemplate;
                if (navBarContentTemplate != null) {
                    var navBarControl = GetVisualChild<NavBarControl>(navBar);
                    if (navBarControl != null && navBarControl.View != null) {
                        var view = navBarControl.View as ExplorerBarView;
                        //var itemTemplate = view.ItemTemplate;

                        foreach (NavBarGroup navBarGroup in navBar.Groups) {
                            var viewTickets = navBarGroup.Content as List<ViewTicket>;
                            foreach (ViewTicket viewTicket in viewTickets) {
                                var grid = (Grid)navBarGroup.FindName("grid");
                                //var tt = view.ItemContainerGenerator.ContainerFromItem(navBarItem) as TreeViewItem;
                                //ContentPresenter myContentPresenter = GetVisualChild<ContentPresenter>(navBarItem);
                                //DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
                                //Grid grid = (Grid)myDataTemplate.FindName("grid", myContentPresenter);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private void MediaElementOnMediaEnded(object sender, RoutedEventArgs routedEventArgs) {
            mediaElement.Source = null;
        }

        private void ShowNotification() {
            try {
                var source = _tickets.Where(t => t.RowProperty.New).ToList();

                RefreshIcon(source.Count, source.Count == 0 ? Visibility.Hidden : Visibility.Visible);

                if (!source.Any())
                    return;

                var growlNotifications = new GrowlNotifiactions();
                foreach (Ticket ticket in source) {
                    growlNotifications.AddNotification(ticket);                    
                }

                var directory = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
                var path = Path.Combine(directory.FullName, "Sounds\\sound.mp3");
                mediaElement.MediaEnded += MediaElementOnMediaEnded;
                mediaElement.Source = new Uri(path);
                mediaElement.Play();

            }
            catch (Exception ex) {
                _log.Error(ex.Message);
            }
        }

        private void RefreshIcon(int count, Visibility visibility) {
            try {
                _trayIcon.ItemCounter = count;
                _trayIcon.ItemCounterVisibility = visibility;
                var stream = CreateImage();
                MyNotifyIcon.IconStream = stream;
            }
            catch (Exception ex) {
                _log.Error(ex.Message);
            }
        }

        private Stream CreateImage() {
            try {
                Visual visual = Application.Current.MainWindow ?? (Visual)VisualTreeHelper.GetParent((DependencyObject)Content);
                var presentationSource = PresentationSource.FromVisual(visual);
                if (presentationSource != null && presentationSource.CompositionTarget != null) {
                    Matrix m = presentationSource.CompositionTarget.TransformToDevice;
                    Point dpi = m.Transform(new Point(96, 96));

                    bool measureValid = _trayIcon.IsMeasureValid;

                    if (!measureValid) {
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
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
            return null;
        }

        private Stream ConvertToBitmap(RenderTargetBitmap renderTargetBitmap) {
            try {

                var bitmapEncoder = new PngBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                var stream = new MemoryStream();
                bitmapEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                return stream;
            }
            catch (Exception ex) {
                _log.Error(ex.Message);
            }
            return null;
        }

        private void LogoutOnExecuted(object sender, ExecutedRoutedEventArgs e) {
            Logout();

            _tickets.Clear();
            _treeViewSource.Clear();

            var loginWindow = new LoginWindow { DisableAutoLogin = true };
            loginWindow.Show();

            _isClose = true;
            Close();
        }

        private void OnCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void RefreshOnExecuted(object sender, ExecutedRoutedEventArgs e) {
            RefreshData();
        }

        private void ButtonClick(object sender, RoutedEventArgs e) {
            RefreshData();
        }

        private void OnSettingsButtonClick(object sender, RoutedEventArgs e) {
            try {
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();

                if (settingsWindow.DialogResult != null && (bool)settingsWindow.DialogResult) {
                    if (settingsWindow.LoginAddress.Length > 0)
                        _baseAddress = new Uri(settingsWindow.LoginAddress);
                    if (settingsWindow.LogoutAddtess.Length > 0)
                        _logoutUri = new Uri(settingsWindow.LogoutAddtess);
                    if (settingsWindow.DataAddress.Length > 0)
                        _dataUri = new Uri(settingsWindow.DataAddress);
                    _interval = settingsWindow.RefreshTime.TimeOfDay;
                }
            }
            catch (Exception ex) {
                _log.Error(ex.Message);
            }
        }

        private T GetVisualChild<T>(DependencyObject parent) where T : Visual {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++) {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null) {
                    child = GetVisualChild<T>(v);
                }
                if (child != null) {
                    break;
                }
            }
            return child;
        }
    }
}
