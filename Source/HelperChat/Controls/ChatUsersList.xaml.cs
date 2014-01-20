using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Xpf.Editors.Helpers;
using HelperChat.Models;
using jabber;
using jabber.client;
using jabber.protocol.client;
using jabber.protocol.iq;
using log4net;

namespace HelperChat.Controls {
    /// <summary>
    /// Interaction logic for ChatUsersList.xaml
    /// </summary>
    public partial class ChatUsersList : UserControl {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private JabberClient _jabberClient;

        private readonly ObservableCollection<ChatUser> _users;

        private readonly ObservableCollection<ChatUser> _filterUsers;

        private ObservableCollection<ViewChatUser> _viewChatUsers;

        private readonly Dictionary<string, ViewChatUser> _allViewChatUsers;

        private JID _jid;

        private ChatWindow _chatWindow;

        private DispatcherTimer _dispatcherTimer;

        public JabberClient JabberClient {
            get { return _jabberClient; }
        }

        private static ChatUsersList _instance;

        public static ChatUsersList Instance {
            get { return _instance ?? (_instance = new ChatUsersList()); }
        }

        public Dictionary<string, ViewChatUser> ViewChatUsers {
            get { return _allViewChatUsers; }
        }

        public string FilterText {
            set {
                _filterUsers.Clear();
                if (String.IsNullOrEmpty(value))
                    _users.ForEach(t => _filterUsers.Add(t));
                else {
                    var filterUsers = from t in _users where t.Name.StartsWith(value) select t;
                    filterUsers.ForEach(t => _filterUsers.Add(t));
                }
            }
        }

        private ChatUsersList() {
            InitializeComponent();

            InitializeJabberClient();

            _users = new ObservableCollection<ChatUser>();
            _filterUsers = new ObservableCollection<ChatUser>();
            usersListView.ItemsSource = _filterUsers;

            _viewChatUsers = new ObservableCollection<ViewChatUser>();
            _allViewChatUsers = new Dictionary<string, ViewChatUser>();

            Loaded += OnChatUsersListLoaded;
            Unloaded += OnUnloaded;
        }

        
        void OnChatUsersListLoaded(object sender, RoutedEventArgs e) {
            try {
                _dispatcherTimer = new DispatcherTimer();
                _dispatcherTimer.Tick += Callback;
                _dispatcherTimer.Interval = new TimeSpan(0, 0, 30);
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private void Callback(object sender, EventArgs e) {
            _jabberClient.Connect();
        }

        private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs) {
            try {
                if(_chatWindow != null)
                    _chatWindow.Close();
                //if(_jabberClient != null)
                //    _jabberClient.Close(true);
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        public void InitializeJabberClient() {
            try {
                if (String.IsNullOrEmpty(ChatSettingsControl.Instance.Login)) {
                    ShowMessageNotification(new ChatMessage("Онлайн-консультант"){Text = "Логин не может быть пустым!"});
                    return;
                }

                _jabberClient = new JabberClient();

                _jabberClient.OnMessage += JabberClientOnMessage;
                _jabberClient.OnDisconnect += JabberClientOnDisconnect;
                _jabberClient.OnError += JabberClientOnError;
                _jabberClient.OnAuthError += JabberClientOnAuthError;
                _jabberClient.OnPresence += OnPresence;

                _jid = new JID(ChatSettingsControl.Instance.Login);

                _jabberClient.User = _jid.User;
                _jabberClient.Server = _jid.Server;
                _jabberClient.Password = ChatSettingsControl.Instance.Password;
                _jabberClient.AutoStartCompression = true;
                _jabberClient.AutoStartTLS = false;
                _jabberClient.AutoStartCompression = true;
                _jabberClient.PlaintextAuth = true;
                _jabberClient.RequiresSASL = true;
                _jabberClient.LocalCertificate = null;
                // don't do extra stuff, please.
                _jabberClient.AutoPresence = true;
                _jabberClient.AutoRoster = true;
                _jabberClient.AutoReconnect = -1;

                var rosterManager = new RosterManager();
                rosterManager.Stream = _jabberClient;
                rosterManager.AutoSubscribe = true;
                rosterManager.AutoAllow = AutoSubscriptionHanding.AllowAll;
                rosterManager.OnRosterBegin += RosterManagerOnRosterBegin;
                rosterManager.OnRosterEnd += RosterManagerOnRosterEnd;
                rosterManager.OnRosterItem += RosterManagerOnRosterItem;

                var presenceManager = new PresenceManager();
                presenceManager.Stream = _jabberClient;

                _jabberClient.OnAuthenticate += JabberClientOnAuthenticate;
                _jabberClient.Connect();
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        void JabberClientOnAuthError(object sender, System.Xml.XmlElement rp) {
            if (rp.Name == "failure") {
                Application.Current.Dispatcher.Invoke(() => {
                    _log.ErrorFormat("Неправильный логин или пароль!");
                    var userList = usersListView.ItemsSource as ObservableCollection<ChatUser>;
                    if(userList != null)
                        userList.Clear();
                    ShowMessageNotification(new ChatMessage(ChatSettingsControl.Instance.Login) { Text = "Неправильный логин или пароль!"});
                });
            }
        }

        void JabberClientOnError(object sender, Exception ex) {
            Application.Current.Dispatcher.Invoke(() => {
                _log.Error(ex);
                _dispatcherTimer.Start();
                JabberClient.Presence(PresenceType.unavailable, PresenceType.unavailable.ToString(), "", 1);
                ShowMessageNotification(new ChatMessage(ChatSettingsControl.Instance.Login) { Text = @"Связь прервана. \тПроверьте настройки подключения!"});
            });
        }

        void JabberClientOnDisconnect(object sender) {

        }

        private void JabberClientOnMessage(object sender, Message msg) {
            try {
                Application.Current.Dispatcher.Invoke(() => {
                    ChatUser user;
                    if (!_users.Select(t => t.Bare).Contains(msg.From.Bare)) {
                        user = new ChatUser {Bare = msg.From.Bare, Name = msg.From.User};
                        _users.Add(user);
                    }
                    else
                        user = _users.First(t => t.Bare.Equals(msg.From.Bare));
                    ShowMessageNotification(new ChatMessage(user.Name) { Text = msg.Body, From = user.Bare });
                });
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        void OnPresence(object sender, Presence pres) {
            try {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (Action) (() => {
                    if (_users.Select(t => t.Bare).Contains(pres.From.Bare)) {
                        _users.First(t => t.Bare.Equals(pres.From.Bare)).Status = pres.Type;
                        usersListView.Items.Refresh();
                    }
                }));
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        void RosterManagerOnRosterItem(object sender, Item ri) {
            try {
                if (ri.JID.Bare != _jid.Bare && !_users.Select(t => t.Bare).Contains(ri.JID.Bare)) {
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal,
                        (Action) (() => {
                            var userName = GetUserName(ri.JID.Bare);
                            _users.Add(new ChatUser {Bare = ri.JID.Bare, Name = userName ?? ri.JID.User});
                            FilterText = String.Empty;
                        }));
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private string GetUserName(string bare) {
            try {
                var currentPath = Directory.GetCurrentDirectory();
                var directory = Path.Combine(currentPath, "History");

                if (!Directory.Exists(directory))
                    return null;

                var path = Path.Combine(directory, String.Format("{0}.csv", bare));
                if (!File.Exists(path))
                    return null;

                using (var reader = new StreamReader(File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))) {
                    while (!reader.EndOfStream) {
                        return reader.ReadLine();
                    }
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
            return null;
        }

        void RosterManagerOnRosterBegin(object sender) {
            //_frmChat = new FrmChat[500];
            //_chatIndex = new Hashtable();
            //rosterTree1.BeginUpdate();
        }

        void RosterManagerOnRosterEnd(object sender) {
            //Done.Set();
            //rosterTree1.EndUpdate();
            //_jabberClient.Presence(PresenceType.available, tbStatus.Text, null, 0);
            //lblPresence.Text = @"Available";
            //rosterTree1.ExpandAll();
        }

        void JabberClientOnAuthenticate(object sender) {
            try {
                Application.Current.Dispatcher.Invoke(() => {
                    if(_dispatcherTimer != null)
                        _dispatcherTimer.Stop();
                    JabberClient.Presence(PresenceType.available, PresenceType.available.ToString(), "", 1);
                });
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private void ShowMessageNotification(ChatMessage message) {
            try {
                var notificationsWindow = new MessageNotifiactions();
                notificationsWindow.AddNotification(message);
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private void OnListViewItemDoubleClick(object sender, MouseButtonEventArgs e) {
            try {
                var listViewItem = (ListViewItem) sender;
                if (listViewItem != null) {
                    var chatUser = (ChatUser) listViewItem.Content;
                    if (chatUser != null) {
                        if (_chatWindow == null) {
                            _chatWindow = ChatWindow.Instance;
                            _viewChatUsers = _chatWindow.ViewChatUsers;
                        }
                        var viewChatUser = GetViewChatUser(chatUser);

                        if (!_viewChatUsers.Select(t => t.ChatUser).Contains(chatUser)) {
                            _viewChatUsers.Insert(0, viewChatUser);
                        }
                        _chatWindow.SelectedViewChatUser = _viewChatUsers.First(t => t.ChatUser.Equals(chatUser));
                        _chatWindow.Show();
                    }
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private ViewChatUser GetViewChatUser(ChatUser chatUser) {
            try {
                ViewChatUser viewChatUser;
                if (!_allViewChatUsers.ContainsKey(chatUser.Bare)) {
                    viewChatUser = new ViewChatUser { JabberClient = _jabberClient, ChatUser = chatUser, Messages = new ObservableCollection<ChatMessage>() };
                    _allViewChatUsers.Add(chatUser.Bare, viewChatUser);
                }
                else
                    viewChatUser = _allViewChatUsers[chatUser.Bare];

                return viewChatUser;
            }
            catch (Exception ex) {
                _log.Error(ex);
                return null;
            }
        }

        private void OnRenameButtonClick(object sender, RoutedEventArgs e) {
            try {
                var txt = (TextBox)((Grid)((Button)sender).Parent).Children[2];
                var txtBlock = (TextBlock)((Grid)((Button)sender).Parent).Children[1];
                txt.Visibility = Visibility.Visible;
                txtBlock.Visibility = Visibility.Collapsed;
                txt.Focus();
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private void OnUserNameTextBoxLostFocus(object sender, RoutedEventArgs e) {
            try {
                var tb = (TextBlock)((Grid)((TextBox)sender).Parent).Children[1];
                tb.Text = ((TextBox)sender).Text;
                tb.Visibility = Visibility.Visible;
                ((TextBox)sender).Visibility = Visibility.Collapsed;

                var user = usersListView.SelectedItem as ChatUser;
                if (user == null) {
                    _log.ErrorFormat("Unable to determine user!");
                    return;
                }
                var currentPath = Directory.GetCurrentDirectory();
                var directory = Path.Combine(currentPath, "History");

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var path = Path.Combine(directory, String.Format("{0}.csv", user.Bare));
                if (!File.Exists(path))
                    File.AppendAllLines(path, new[]{tb.Text});
                else {
                    var lines = File.ReadAllLines(path);
                    lines[0] = tb.Text;
                    File.WriteAllLines(path, lines.ToArray());
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private void OnUserNameTextBoxGotFocus(object sender, RoutedEventArgs e) {
            try {
                var txt = (TextBox) sender;
                txt.CaretIndex = txt.Text.Length;
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private void OnUserNameTextBoxKeyDown(object sender, KeyEventArgs e) {
            if(e.Key == Key.Enter)
                usersListView.Focus();
        }
    }
}
