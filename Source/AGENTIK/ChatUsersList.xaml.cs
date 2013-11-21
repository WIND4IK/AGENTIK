using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AGENTIK.Models;
using jabber;
using jabber.client;
using jabber.protocol.client;
using jabber.protocol.iq;

namespace AGENTIK {
    /// <summary>
    /// Interaction logic for ChatUsersList.xaml
    /// </summary>
    public partial class ChatUsersList : Window {

        private JabberClient _jabberClient;

        private List<ChatUser> _users;

        public ChatUsersList() {
            InitializeComponent();

            InitializeJabberClient();

            _users = new List<ChatUser>();

            Closing += OnClosing;
        }

        private void OnClosing(object sender, CancelEventArgs cancelEventArgs) {
            _jabberClient.Close(true);
        }

        private void InitializeJabberClient() {
            _jabberClient = new JabberClient();

            _jabberClient.OnMessage += JabberClientOnMessage;
            _jabberClient.OnDisconnect += JabberClientOnDisconnect;
            _jabberClient.OnError += JabberClientOnError;
            _jabberClient.OnAuthError += JabberClientOnAuthError;
            _jabberClient.OnPresence += OnPresence;

            var user = "wind4ik@jabber.ru";
            var pwd = "alexandrita";

            var jid = new JID(user);

            _jabberClient.User = jid.User;
            _jabberClient.Server = jid.Server;
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

        void JabberClientOnAuthError(object sender, System.Xml.XmlElement rp) {
            if (rp.Name == "failure") {
                MessageBox.Show(@"Invalid User Name or Password", @"Error!!!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void JabberClientOnError(object sender, Exception ex) {
            MessageBox.Show(ex.Message, @"Error!!!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        void JabberClientOnDisconnect(object sender) {
        }

        private void JabberClientOnMessage(object sender, Message msg) {
            if (!_users.Select(t => t.Bare).Contains(msg.From.Bare)) {
                _users.Add(new ChatUser { Bare = msg.From.Bare, Name = msg.From.User });
            }
        }

        void OnPresence(object sender, Presence pres) {
            if (!_users.Select(t => t.Bare).Contains(pres.From.Bare)) {
                _users.Add(new ChatUser{Bare = pres.From.Bare, Name = pres.From.User});
            }
        }

        void RosterManagerOnRosterItem(object sender, Item ri) {
            try {
                //_chatIndex.Add(ri.JID.Bare, ++_chatCount);
                //InitializeFrmChat(ri.JID.Bare, ri.Nickname);
            }
            catch (Exception) {
                Console.Write(@"Error");
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
            Title = _jabberClient.User;
        }

        private void OnListViewItemClick(object sender, MouseButtonEventArgs e) {
            var listViewItem = (ListViewItem) sender;
            if (listViewItem != null) {
                var chatUser = (ChatUser) listViewItem.Content;
                if (chatUser != null) {
                    var chatUserWindow = new ChatUserWindow(_jabberClient) {User = chatUser};
                    chatUserWindow.Show();
                }
            }
        }
    }
}
