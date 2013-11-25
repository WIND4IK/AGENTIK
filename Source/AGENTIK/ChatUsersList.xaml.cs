using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AGENTIK.Models;
using DevExpress.Xpf.Core;
using jabber;
using jabber.client;
using jabber.protocol.client;
using jabber.protocol.iq;
using log4net;

namespace AGENTIK {
    /// <summary>
    /// Interaction logic for ChatUsersList.xaml
    /// </summary>
    public partial class ChatUsersList : UserControl {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private JabberClient _jabberClient;

        private readonly ObservableCollection<ChatUser> _users;

        private ObservableCollection<ViewChatUser> _chatUserControls;

        private JID _jid;

        private ChatWindow _chatWindow; 

        public ChatUsersList() {
            InitializeComponent();

            InitializeJabberClient();

            _users = new ObservableCollection<ChatUser>();
            usersListView.ItemsSource = _users;

            _chatUserControls = new ObservableCollection<ViewChatUser>();

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
                if (!_users.Select(t => t.Bare).Contains(msg.From.Bare)) {
                    Application.Current.Dispatcher.Invoke(() => _users.Add(new ChatUser {Bare = msg.From.Bare, Name = msg.From.User}));
                }
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

        private void OnListViewItemDoubleClick(object sender, MouseButtonEventArgs e) {
            try {
                var listViewItem = (ListViewItem) sender;
                if (listViewItem != null) {
                    var chatUser = (ChatUser) listViewItem.Content;
                    if (chatUser != null) {
                        if (_chatWindow == null) {
                            _chatWindow = new ChatWindow();
                            _chatWindow.ChatUserControls = _chatUserControls;
                        }

                        if (!_chatUserControls.Select(t => t.ChatUser).Contains(chatUser)) {
                            var viewChatUser = new ViewChatUser();
                            viewChatUser.JabberClient = _jabberClient;
                            viewChatUser.ChatUser = chatUser;
                            _chatUserControls.Insert(0, viewChatUser);
                        }
                        _chatWindow.SelectedViewChatUser = _chatUserControls.First(t => t.ChatUser == chatUser);
                        _chatWindow.Show();
                    }
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }
    }
}
