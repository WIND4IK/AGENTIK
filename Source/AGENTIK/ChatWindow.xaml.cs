using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using AGENTIK.Controls;
using AGENTIK.Models;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Docking;
using log4net;

namespace AGENTIK {
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : DXWindow {
        private ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool _isClose;

        private static ChatWindow _instance;

        public static ChatWindow Instance {
            get {
                if (_instance == null)
                    _instance = new ChatWindow();
                return _instance;
            }
        }

        public ObservableCollection<ViewChatUser> ViewChatUsers {
            get { return (ObservableCollection<ViewChatUser>)documentContainer.ItemsSource; }
            set {
                documentContainer.ItemsSource = value;
            }
        }

        public ViewChatUser SelectedViewChatUser {
            set { ActivateDockItem(value); }
        }

        public string SelectedUserBare {
            set { ActivateDockItemByBare(value); }
        }

        private void ActivateDockItemByBare(string bare) {
            try {
                var viewChatUser = ViewChatUsers.FirstOrDefault(t => t.ChatUser.Bare.Equals(bare));
                if (viewChatUser == null) {
                    viewChatUser = ChatUsersList.Instance.ViewChatUsers[bare];
                    ViewChatUsers.Insert(0, viewChatUser);
                }

                Show();
                ActivateDockItem(viewChatUser);
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private void ActivateDockItem(ViewChatUser viewChatUser) {
            try {
                var baseDockItems = documentContainer.Items;
                foreach (DocumentPanel baseLayoutItem in baseDockItems) {
                    if (baseLayoutItem.Content is ViewChatUser) {
                        var content = baseLayoutItem.Content as ViewChatUser;
                        if (content.ChatUser.Equals(viewChatUser.ChatUser)) {
                            dockManager.DockController.Activate(baseLayoutItem, true);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        private ChatWindow() {
            InitializeComponent();

            ViewChatUsers = new ObservableCollection<ViewChatUser>();
            
            Closing += OnClosing;
        }

        private void OnClosing(object sender, CancelEventArgs e) {
            if (!_isClose) {
                Hide();
                e.Cancel = true;
            }
        }

        private void OnDockManagerDockItemClosing(object sender, DevExpress.Xpf.Docking.Base.ItemCancelEventArgs e) {
            try {
                var dataContext = e.Item.DataContext;
                if (dataContext is ViewChatUser) {
                    var viewChatUser = dataContext as ViewChatUser;
                    var itemSource = documentContainer.ItemsSource as ObservableCollection<ViewChatUser>;
                    if (itemSource != null) {
                        itemSource.Remove(viewChatUser);
                        if (itemSource.Count == 0)
                            Close();
                    }
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        public void Dispose() {
            _isClose = true;
            Close();
        }
    }
}
