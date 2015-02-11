using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Editors.Helpers;
using HelperChat.Models;
using jabber;
using jabber.client;
using log4net;

namespace HelperChat.Controls {
    /// <summary>
    /// Interaction logic for ArchiveUsersList.xaml
    /// </summary>
    public partial class ArchiveUsersList : UserControl {
        private readonly Dictionary<string, ViewChatUser> _allViewChatUsers;
        private ChatWindow _chatWindow;
        private string _filterText;
        private readonly ObservableCollection<ChatUser> _filterUsers;
        private readonly JabberClient _jabberClient;
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ObservableCollection<ChatUser> _users;
        private ObservableCollection<ViewChatUser> _viewChatUsers;

        public string FilterText {
            set {
                _filterUsers.Clear();
                _filterText = value;
                if (string.IsNullOrEmpty(_filterText)) {
                    _users.ForEach(t => _filterUsers.Add(t));
                }
                else {
                    _users.Where( t => t.Name.Contains(_filterText)).ForEach(t => _filterUsers.Add(t));
                }
            }
        }

        public Dictionary<string, ViewChatUser> ViewChatUsers {
            get {
                return _allViewChatUsers;
            }
        }

        public ArchiveUsersList() {
            InitializeComponent();
        }

        public ArchiveUsersList(JabberClient jabberClient) {
            _jabberClient = jabberClient;
            InitializeComponent();
            _users = new ObservableCollection<ChatUser>();
            _filterUsers = new ObservableCollection<ChatUser>();
            usersListView.ItemsSource = _filterUsers;
            _viewChatUsers = new ObservableCollection<ViewChatUser>();
            _allViewChatUsers = new Dictionary<string, ViewChatUser>();
        }

        private ViewChatUser GetViewChatUser(ChatUser chatUser) {
            try {
                ViewChatUser user;
                if (!_allViewChatUsers.ContainsKey(chatUser.Bare)) {
                    user = new ViewChatUser {
                        JabberClient = _jabberClient,
                        ChatUser = chatUser,
                        Messages = new ObservableCollection<ChatMessage>(),
                        LoadHistory = true
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

        public void LoadArchive() {
            try {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "History");
                if (Directory.Exists(path)) {
                    _users.Clear();
                    var enumerable = from t in Directory.GetFiles(path)
                                                              select new FileInfo(t) into t
                                                              orderby t.LastWriteTime descending
                                                              select t;
                    foreach (var info in enumerable) {
                        var jid = new JID(Path.GetFileNameWithoutExtension(info.FullName));
                        var item = new ChatUser {
                            Bare = jid.Bare,
                            Name = jid.User
                        };
                        _users.Add(item);
                    }
                    FilterText = _filterText;
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnListViewItemDoubleClick(object sender, MouseButtonEventArgs e) {
            try {
                var item = (ListViewItem)sender;
                if (item != null) {
                    var chatUser = (ChatUser)item.Content;
                    if (chatUser != null) {
                        if (_chatWindow == null) {
                            _chatWindow = ChatWindow.Instance;
                            _viewChatUsers = _chatWindow.ViewChatUsers;
                        }
                        var viewChatUser = GetViewChatUser(chatUser);
                        if (!(from t in _viewChatUsers select t.ChatUser).Contains<ChatUser>(chatUser)) {
                            _viewChatUsers.Insert(0, viewChatUser);
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


    }
}
