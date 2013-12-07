using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AGENTIK.Models;
using DevExpress.Xpf.Core;
using jabber;
using jabber.client;
using jabber.protocol.client;
using jabber.protocol.iq;
using log4net;

namespace AGENTIK.Controls {
    /// <summary>
    /// Interaction logic for ChatUsersList.xaml
    /// </summary>
    public partial class ChatUsersList : UserControl {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private JabberClient _jabberClient;

        private readonly ObservableCollection<ChatUser> _users;

        private ObservableCollection<ViewChatUser> _viewChatUsers;

        private Dictionary<string, ViewChatUser> _allViewChatUsers;

        private MessageNotifiactions _notificationsWindow; 

        private JID _jid;

        private ChatWindow _chatWindow;

        private static ChatUsersList _instance;

        public static ChatUsersList Instance {
            get {
                if(_instance == null)
                    _instance = new ChatUsersList();
                return _instance;
            }
        }

        public Dictionary<string, ViewChatUser> ViewChatUsers {
            get { return _allViewChatUsers; }
        }

        private ChatUsersList() {
            InitializeComponent();

            InitializeJabberClient();

            _users = new ObservableCollection<ChatUser>();
            usersListView.ItemsSource = _users;

            _viewChatUsers = new ObservableCollection<ViewChatUser>();
            _allViewChatUsers = new Dictionary<string, ViewChatUser>();

            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs) {
            try {
                _jabberClient.Close(true);
            }
            catch (Exception ex) {
                _log.Error(ex);
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

                var user = "wind4ik@jabber.ru";
                var pwd = "alexandrita";

                _jid = new JID(user);

                _jabberClient.User = _jid.User;
                _jabberClient.Server = _jid.Server;
                _jabberClient.Password = pwd;
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

                _jabberClient.Connect();
                _jabberClient.OnAuthenticate += JabberClientOnAuthenticate;
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        void JabberClientOnAuthError(object sender, System.Xml.XmlElement rp) {
            if (rp.Name == "failure") {
                Application.Current.Dispatcher.Invoke(() => {
                    _log.ErrorFormat("Invalid User Name or Password");
                    DXMessageBox.Show("Invalid User Name or Password", "Error!!!", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        void JabberClientOnError(object sender, Exception ex) {
            Application.Current.Dispatcher.Invoke(() => {
                _log.Error(ex);
                //DXMessageBox.Show(ex.Message, "Error!!!", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        void JabberClientOnDisconnect(object sender) {
        }

        private void JabberClientOnMessage(object sender, Message msg) {
            try {
                ChatUser user;
                Application.Current.Dispatcher.Invoke(() => {
                    if (!_users.Select(t => t.Bare).Contains(msg.From.Bare)) {
                        user = new ChatUser {Bare = msg.From.Bare, Name = msg.From.User};
                        _users.Add(user);
                    }
                    else
                        user = _users.First(t => t.Bare.Equals(msg.From.Bare));

                    var viewChatUser = GetViewChatUser(user);
                    if (msg.Body != "") {
                        string receivedMsg = msg.Body;

                        var message = new ChatMessage(viewChatUser.ChatUser.Name) { Text = receivedMsg, From = msg.From.Bare };
                        viewChatUser.Messages.Add(message);

                        ShowMessageNotification(message);

                        msg.Body = "";
                    }
                });
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        void OnPresence(object sender, Presence pres) {
            try {
                Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (Action) (() => {
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
                    Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                        (Action)(() => _users.Add(new ChatUser {Bare = ri.JID.Bare, Name = ri.JID.User})));
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
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
                //Application.Current.Dispatcher.Invoke(() => nameTextBlock.Text = _jabberClient.User);
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private void ShowMessageNotification(ChatMessage message) {
            try {
                if (_notificationsWindow == null)
                    _notificationsWindow = new MessageNotifiactions();
                _notificationsWindow.AddNotification(message);
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
    }
}
