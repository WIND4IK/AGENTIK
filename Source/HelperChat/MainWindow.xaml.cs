using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Helpers;
using HelperChat.Controls;
using HelperChat.Models;
using HelperChat.Resources;
using jabber;
using jabber.client;
using jabber.protocol.client;
using jabber.protocol.iq;
using log4net;
using Image = System.Drawing.Image;
using Path = System.IO.Path;

namespace HelperChat {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DXWindow {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Dictionary<string, ViewChatUser> _allViewChatUsers;
        private AnimatedImage _animatedImage;
        private ChatWindow _chatWindow;
        private ChatUser _currentUser;
        private DispatcherTimer _dispatcherTimer;
        private readonly BitmapImage _errorImage;
        private string _filterText;
        private readonly ObservableCollection<ChatUser> _filterUsers;
        private readonly Uri _helperChatUri;
        private readonly BitmapImage _inactiveImage;
        private JabberClient _jabberClient;
        private JID _jid;
        private readonly BitmapImage _messageImage;
        private readonly Uri _messageUri;
        private readonly List<JabberStatus> _statuses;
        private readonly ObservableCollection<ChatUser> _users;
        private ObservableCollection<ViewChatUser> _viewChatUsers;
        private const string MessageSound = "Sounds/sound.mp3";

        public MainWindow() {
            InitializeComponent();

            _filterText = string.Empty;
            _inactiveImage = new BitmapImage(new Uri("pack://application:,,,/Images/Inactive.ico"));
            _errorImage = new BitmapImage(new Uri("pack://application:,,,/Images/Error.ico"));
            _messageImage = new BitmapImage(new Uri("pack://application:,,,/Images/message.gif"));
            _messageUri = new Uri("pack://application:,,,/Images/message.gif");
            _helperChatUri = new Uri("pack://application:,,,/Images/mainicon.ico");

            _statuses = new List<JabberStatus>();
            TryToConnect();
            _users = new ObservableCollection<ChatUser>();

            _users.CollectionChanged += (sender, args) => FilterText = _filterText;
            _filterUsers = new ObservableCollection<ChatUser>();
            usersListView.ItemsSource = _filterUsers;
            _viewChatUsers = new ObservableCollection<ViewChatUser>();
            _allViewChatUsers = new Dictionary<string, ViewChatUser>();
            InitAnimatedImage();
            Unloaded += OnUnloaded;
            Loaded += OnMainWindowLoaded;
            Closed += OnMainWindowClosed;
        }

        public string CurrentUser {
            get {
                return ((JabberClient != null) ? JabberClient.User : "");
            }
        }

        public string FilterText {
            set {
                _filterUsers.Clear();
                _filterText = value;
                if (string.IsNullOrEmpty(_filterText)) {
                    _users.ForEach(t => _filterUsers.Add(t));
                }
                else {
                    _users.Where(t => t.Name.Contains(_filterText)).ForEach(t => _filterUsers.Add(t));
                }
            }
        }

        public bool IsClose { get; set; }
        public JabberClient JabberClient { get { return _jabberClient; } }
        public Dictionary<string, ViewChatUser> ViewChatUsers { get { return _allViewChatUsers; } }


        private void Callback(object sender, EventArgs e) {
            _jabberClient.Connect();
        }

        private void CreateChatWindow() {
            if (_chatWindow == null) {
                _chatWindow = ChatWindow.Instance;

                _chatWindow.SelectedViewChatUserChanged += (sender, args) => {
                    _chatWindow.SelectedViewChatUser.ChatUser.Image = _inactiveImage;
                    ShowGifForMessage(false);
                };

                _viewChatUsers = _chatWindow.ViewChatUsers;
                _chatWindow.Activated += (sender, args) => {
                    if (_chatWindow.SelectedViewChatUser.ChatUser.Equals(_currentUser)) {
                        ShowGifForMessage(false);
                        _chatWindow.SelectedViewChatUser.ChatUser.Image = _inactiveImage;
                    }
                };
            }
        }

        private string GetUserName(string bare) {
            try {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "History");
                if (!Directory.Exists(path)) {
                    return null;
                }
                var str3 = Path.Combine(path, string.Format("{0}.csv", bare));
                if (!File.Exists(str3)) {
                    return null;
                }
                using (var reader = new StreamReader(File.Open(str3, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))) {
                    while (!reader.EndOfStream) {
                        return reader.ReadLine();
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
            return null;
        }

        private ViewChatUser GetViewChatUser(ChatUser chatUser) {
            try {
                ViewChatUser user;
                if (!_allViewChatUsers.ContainsKey(chatUser.Bare)) {
                    user = new ViewChatUser {
                        JabberClient = _jabberClient,
                        ChatUser = chatUser,
                        Messages = new ObservableCollection<ChatMessage>()
                    };
                    _allViewChatUsers.Add(chatUser.Bare, user);
                }
                else {
                    user = _allViewChatUsers[chatUser.Bare];
                }
                return user;
            }
            catch (Exception exception) {
                _log.Error(exception);
                return null;
            }
        }

        private void InitAnimatedImage() {
            try {
                _animatedImage = new AnimatedImage();
                _animatedImage.PropertyChanged += OnAnimatedImagePropertyChanged;
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void InitControls() {
            try {
                ThemeManager.ApplicationThemeName = Theme.Office2007Blue.Name;
                var list = new ArchiveUsersList(JabberClient);
                list.LoadArchive();
                archiveTab.Content = list;
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void InitializeJabberClient() {
            try {
                _jabberClient = new JabberClient();
                _jabberClient.OnMessage += JabberClientOnMessage;
                _jabberClient.OnDisconnect += JabberClientOnDisconnect;
                _jabberClient.OnError += JabberClientOnError;
                _jabberClient.OnAuthError += JabberClientOnAuthError;
                _jabberClient.OnPresence += OnPresence;
                _jabberClient.AutoStartCompression = true;
                _jabberClient.AutoStartTLS = false;
                _jabberClient.AutoStartCompression = true;
                _jabberClient.PlaintextAuth = true;
                _jabberClient.RequiresSASL = true;
                _jabberClient.LocalCertificate = null;
                _jabberClient.AutoPresence = true;
                _jabberClient.AutoRoster = true;
                _jabberClient.AutoReconnect = -1f;
                var manager = new RosterManager {
                    Stream = _jabberClient,
                    AutoSubscribe = true,
                    AutoAllow = AutoSubscriptionHanding.AllowAll
                };
                manager.OnRosterBegin += RosterManagerOnRosterBegin;
                manager.OnRosterEnd += RosterManagerOnRosterEnd;
                manager.OnRosterItem += RosterManagerOnRosterItem;
                _jabberClient.OnAuthenticate += JabberClientOnAuthenticate;
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void JabberClientOnAuthenticate(object sender) {
            try {
                Application.Current.Dispatcher.Invoke(() => {
                    if (_dispatcherTimer != null) {
                        _dispatcherTimer.Stop();
                    }
                    JabberClient.Presence(PresenceType.available, PresenceType.available.ToString(), "", 1);
                    statusListView.SelectedIndex = 0;
                    userTextBlock.GetBindingExpression(TextBlock.TextProperty).UpdateTarget();
                });
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void JabberClientOnAuthError(object sender, XmlElement rp) {
            try {
                if (rp.Name == "failure") {
                    Application.Current.Dispatcher.Invoke(() => {
                        _log.ErrorFormat("Неправильный логин или пароль!", new object[0]);
                        var itemsSource = usersListView.ItemsSource as ObservableCollection<ChatUser>;
                        if (itemsSource != null) {
                            itemsSource.Clear();
                        }
                        statusListView.SelectedIndex = 1;
                        var message = new ChatMessage(ChatSettingsControl.Instance.Login) {
                            Text = "Неправильный логин или пароль!"
                        };
                        ShowMessageNotification(message);
                    });
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void JabberClientOnDisconnect(object sender) {
            try {
                Application.Current.Dispatcher.Invoke(() => statusListView.SelectedIndex = 1);
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void JabberClientOnError(object sender, Exception ex) {
            Application.Current.Dispatcher.Invoke(() => {
                try {
                    _log.Error(ex);
                    _dispatcherTimer.Start();
                    statusListView.SelectedIndex = 1;
                    var message = new ChatMessage(ChatSettingsControl.Instance.Login) {
                        Text = @"Связь прервана. \тПроверьте настройки подключения!"
                    };
                    ShowMessageNotification(message);
                }
                catch (Exception exception) {
                    _log.Error(exception);
                }
            });
        }

        private void JabberClientOnMessage(object sender, Message msg) {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!(from t in _users select t.Bare).Contains<string>(msg.From.Bare))
                    {
                        var user = new ChatUser
                        {
                            Bare = msg.From.Bare,
                            Name = msg.From.User
                        };
                        _currentUser = user;
                        _users.Add(_currentUser);
                    }
                    else
                    {
                        _currentUser = _users.First(t => t.Bare.Equals(msg.From.Bare));
                    }
                    notifyIcon.Tag = _currentUser;
                    var viewChatUser = GetViewChatUser(_currentUser);
                    CreateChatWindow();
                    var item = new ChatMessage(_currentUser.Name)
                    {
                        Text = msg.Body,
                        From = _currentUser.Bare
                    };
                    viewChatUser.Messages.Add(item);
                    if (((_chatWindow == null) || !_chatWindow.IsActive) ||
                        (_chatWindow.IsActive && !_chatWindow.SelectedViewChatUser.Equals(viewChatUser)))
                    {
                        _currentUser.Image = _messageImage;
                        ShowGifForMessage(true);
                        ShowMessageNotification(item);
                        PlaySound();
                    }
                    Helper.SaveToFile(item);
                });
            }
            catch (Exception exception)
            {
                _log.Error(exception);
            }
        }

        private void LoadStatus() {
            try {
                var status = new JabberStatus {
                    Name = "В сети",
                    PresenceType = PresenceType.available,
                    Image = Helper.ImageSourceFromPath("Images/Online.png")
                };
                _statuses.Add(status);
                status = new JabberStatus {
                    Name = "Не в сети",
                    PresenceType = PresenceType.unavailable,
                    Image = Helper.ImageSourceFromPath("Images/Offline.png")
                };
                _statuses.Add(status);
                statusListView.ItemsSource = _statuses;
                statusListView.SelectedIndex = 1;
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void MediaElementOnMediaEnded(object sender, RoutedEventArgs routedEventArgs) {
            mediaElement.Source = null;
        }

        private void OnAnimatedImagePropertyChanged(object sender, PropertyChangedEventArgs e) {
            try {
                var image = (AnimatedImage)sender;
                notifyIcon.Icon = Helper.ConvertToBitmap((BitmapSource)image.Source);
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnBtnStatusClick(object sender, RoutedEventArgs e) {
            try {
                popup.IsOpen = true;
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        protected override void OnClosing(CancelEventArgs e) {
            try {
                if (IsClose) {
                    if (_chatWindow != null) {
                        _chatWindow.Close();
                    }
                    base.OnClosing(e);
                    Application.Current.Shutdown();
                }
                else {
                    ShowInTaskbar = false;
                    Visibility = Visibility.Hidden;
                    e.Cancel = true;
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnDXTabControlSelectionChanged(object sender, TabControlSelectionChangedEventArgs e) {
            try {
                if (tabControl.SelectedIndex == 1) {
                    var selectedItem = tabControl.SelectedItem as DXTabItem;
                    if (selectedItem != null) {
                        var content = selectedItem.Content as ArchiveUsersList;
                        if (content != null) {
                            content.LoadArchive();
                            content.FilterText = searchTextBox.Text;
                        }
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnExitClick(object sender, RoutedEventArgs e) {
            if (_chatWindow != null) {
                _chatWindow.Close();
            }
            IsClose = true;
            Close();
        }

        private void OnListViewItemDoubleClick(object sender, MouseButtonEventArgs e) {
            try {
                var item = (ListViewItem)sender;
                if (item != null) {
                    var chatUser = (ChatUser)item.Content;
                    if (chatUser != null) {
                        CreateChatWindow();
                        var viewChatUser = GetViewChatUser(chatUser);
                        if (!(from t in _viewChatUsers select t.ChatUser).Contains<ChatUser>(chatUser)) {
                            _viewChatUsers.Insert(0, viewChatUser);
                        }
                        chatUser.Image = _inactiveImage;
                        if (chatUser.Equals(_currentUser)) {
                            ShowGifForMessage(false);
                        }

                        _chatWindow.SelectedViewChatUser = _viewChatUsers.First(t => t.ChatUser.Equals(chatUser));
                        _chatWindow.Show();
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnListViewSelectedItemClick(object sender, MouseButtonEventArgs e) {
            try {
                var item = (ListViewItem)sender;
                if (item != null) {
                    var content = (JabberStatus)item.Content;
                    if (content != null) {
                        if (!((JabberClient != null) && JabberClient.IsAuthenticated)) {
                            TryToConnect();
                        }
                        if ((JabberClient != null) && JabberClient.IsAuthenticated) {
                            JabberClient.Presence(content.PresenceType, content.Name, "HelperChat", 0);
                        }
                        popup.IsOpen = false;
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnMainWindowClosed(object sender, EventArgs e) {
            try {
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e) {
            try {
                LoadStatus();
                InitControls();
                _dispatcherTimer = new DispatcherTimer();
                _dispatcherTimer.Tick += Callback;
                _dispatcherTimer.Interval = new TimeSpan(0, 0, 30);
                ShowGifForMessage(false);
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnNotifyIconMouseDoubleClick(object sender, RoutedEventArgs e) {
            try {
                if (notifyIcon.Tag == null) {
                    if (!IsVisible) {
                        Show();
                    }
                    if (!IsActive) {
                        Activate();
                    }
                }
                else {
                    CreateChatWindow();
                    var viewChatUser = GetViewChatUser(_currentUser);
                    if (!(from t in _viewChatUsers select t.ChatUser).Contains<ChatUser>(_currentUser)) {
                        _viewChatUsers.Insert(0, viewChatUser);
                    }
                    if (!_chatWindow.IsVisible) {
                        _chatWindow.Show();
                    }
                    if (!_chatWindow.IsActive) {
                        _chatWindow.Activate();
                    }
                    _chatWindow.SelectedViewChatUser = _viewChatUsers.Single(t => t.ChatUser == _currentUser);
                    notifyIcon.Tag = null;
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnPresence(object sender, Presence pres) {
            try {
                Application.Current.Dispatcher.Invoke(() => {
                        if ((from t in _users select t.Bare).Contains<string>(pres.From.Bare)) {
                            var user = _users.First(t => t.Bare.Equals(pres.From.Bare));
                            user.Status = pres.Type;
                            user.Image = _inactiveImage;
                        }
                    }, DispatcherPriority.Normal);
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnRenameButtonClick(object sender, RoutedEventArgs e) {
            try {
                var box = (TextBox)((Grid)((Button)sender).Parent).Children[2];
                var block = (TextBlock)((Grid)((Button)sender).Parent).Children[1];
                box.Visibility = Visibility.Visible;
                block.Visibility = Visibility.Collapsed;
                box.Focus();
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnSearchTextBoxKeyUp(object sender, KeyEventArgs e) {
            try {
                var edit = (TextEdit)sender;
                if (edit != null) {
                    if (tabControl.SelectedIndex == 0) {
                        FilterText = edit.Text;
                    }
                    else {
                        var selectedItem = tabControl.SelectedItem as DXTabItem;
                        if (selectedItem != null) {
                            var content = selectedItem.Content as ArchiveUsersList;
                            if (content != null) {
                                content.FilterText = edit.Text;
                            }
                        }
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnSettingsButtonClick(object sender, RoutedEventArgs e) {
            try {
                new SettingsWindow { Owner = this }.ShowDialog();
            }
            catch (Exception exception) {
                _log.Error(exception.Message);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs) {
        }

        private void OnUserNameTextBoxGotFocus(object sender, RoutedEventArgs e) {
            try {
                var box = (TextBox)sender;
                box.CaretIndex = box.Text.Length;
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnUserNameTextBoxKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Return) {
                usersListView.Focus();
            }
        }

        private void OnUserNameTextBoxLostFocus(object sender, RoutedEventArgs e) {
            try {
                var block = (TextBlock)((Grid)((TextBox)sender).Parent).Children[1];
                block.Text = ((TextBox)sender).Text;
                block.Visibility = Visibility.Visible;
                ((TextBox)sender).Visibility = Visibility.Collapsed;
                var selectedItem = usersListView.SelectedItem as ChatUser;
                if (selectedItem == null) {
                    _log.ErrorFormat("Unable to determine user!", new object[0]);
                }
                else {
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "History");
                    if (!Directory.Exists(path)) {
                        Directory.CreateDirectory(path);
                    }
                    var str3 = Path.Combine(path, string.Format("{0}.csv", selectedItem.Bare));
                    if (!File.Exists(str3)) {
                        File.AppendAllLines(str3, new [] { block.Text });
                    }
                    else {
                        var source = File.ReadAllLines(str3);
                        source[0] = block.Text;
                        File.WriteAllLines(str3, source.ToArray());
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void PlaySound() {
            var uriString = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, MessageSound);
            mediaElement.MediaEnded += MediaElementOnMediaEnded;
            mediaElement.Source = new Uri(uriString);
            mediaElement.Play();
        }

        private void RosterManagerOnRosterBegin(object sender) {
        }

        private void RosterManagerOnRosterEnd(object sender) {
        }

        private void RosterManagerOnRosterItem(object sender, Item ri) {
            try {
                if ((ri.JID.Bare != _jid.Bare) && !(from t in _users select t.Bare).Contains<string>(ri.JID.Bare)) {
                    Application.Current.Dispatcher.Invoke(() => {
                            var userName = GetUserName(ri.JID.Bare);
                            var item = new ChatUser {
                                Bare = ri.JID.Bare,
                                Name = userName ?? ri.JID.User,
                                Image = _errorImage
                            };
                            _users.Add(item);
                            FilterText = string.Empty;
                            usersListView.Items.Refresh();
                        }, DispatcherPriority.Normal);
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void ShowGifForMessage(bool isMessage) {
            try {
                if (isMessage) {
                    var image = Image.FromStream(Application.GetResourceStream(_messageUri).Stream);
                    _animatedImage.AnimatedBitmap = (Bitmap)image;
                }
                else {
                    _animatedImage.StopAnimate();
                    notifyIcon.Icon = System.Drawing.Icon.FromHandle(new Bitmap(Application.GetResourceStream(_helperChatUri).Stream).GetHicon());
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void ShowMessageNotification(ChatMessage message) {
            try {
                new MessageNotifications().AddNotification(message);
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        public void TryToConnect() {
            try {
                if (string.IsNullOrEmpty(ChatSettingsControl.Instance.Login)) {
                    var message = new ChatMessage("Онлайн-консультант") {
                        Text = "Логин не может быть пустым!"
                    };
                    ShowMessageNotification(message);
                }
                else {
                    if (_jabberClient == null) {
                        InitializeJabberClient();
                    }
                    if (!((_jid == null) || _jid.User.Equals(ChatSettingsControl.Instance.Login))) {
                        _users.Clear();
                    }
                    _jid = new JID(ChatSettingsControl.Instance.Login.Trim());
                    _jabberClient.User = _jid.User;
                    _jabberClient.Server = _jid.Server;
                    _jabberClient.Password = ChatSettingsControl.Instance.Password;
                    _jabberClient.Connect();
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private Bitmap Uri2Bitmap(Uri uri) {
            using (var stream = new MemoryStream()) {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(uri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad));
                encoder.Save(stream);
                return new Bitmap(new Bitmap(stream));
            }
        }
    }
}
