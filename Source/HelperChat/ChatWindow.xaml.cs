using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Docking;
using DevExpress.Xpf.Docking.Base;
using HelperChat.Controls;
using HelperChat.Models;
using HelperChat.Resources;
using log4net;

namespace HelperChat {
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : DXWindow {
        private static ChatWindow _instance;
        private bool _isClose;
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ViewChatUser _selectedViewChatUser;

        private ChatWindow() {
            InitializeComponent();
            ViewChatUsers = new ObservableCollection<ViewChatUser>();
            Activated += OnActivated;
        }

        private void ActivateDockItem() {
            try {
                if (dockManager.DockController != null) {
                    var items = documentContainer.Items;
                    foreach (var baseLayoutItem in items) {
                        var panel = (DocumentPanel)baseLayoutItem;
                        if (panel.Content is ViewChatUser) {
                            var content = panel.Content as ViewChatUser;
                            if (content.ChatUser.Equals(_selectedViewChatUser.ChatUser)) {
                                dockManager.DockController.Activate(panel, true);
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void ActivateDockItemByBare(string bare) {
            try {
                if ((Application.Current.MainWindow is MainWindow) && (Application.Current.MainWindow as MainWindow).ViewChatUsers.Any()) {
                    _selectedViewChatUser = ViewChatUsers.FirstOrDefault(t => t.ChatUser.Bare.Equals(bare));
                    if (_selectedViewChatUser == null) {
                        _selectedViewChatUser = (Application.Current.MainWindow as MainWindow).ViewChatUsers[bare];
                        ViewChatUsers.Insert(0, _selectedViewChatUser);
                        OnSelectedViewChatUserChanged();
                    }
                    Show();
                    ActivateDockItem();
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        public void Dispose() {
            _isClose = true;
            Close();
        }

        private void OnActivated(object sender, EventArgs eventArgs) {
            var control = this.FindVisualAncestorOfType<ChatUserControl>().SingleOrDefault<ChatUserControl>(t => t.User == SelectedViewChatUser.ChatUser);
            if (control != null) {
                control.Refresh();
            }
        }

        protected override void OnClosing(CancelEventArgs e) {
            if (_isClose) {
                OnClosing(e);
            }
            else {
                e.Cancel = true;
                Hide();
            }
        }

        private void OnDockManagerDockItemClosing(object sender, ItemCancelEventArgs e) {
            try {
                var dataContext = e.Item.DataContext;
                if (dataContext is ViewChatUser) {
                    var item = dataContext as ViewChatUser;
                    var itemsSource =
                        documentContainer.ItemsSource as ObservableCollection<ViewChatUser>;
                    if (itemsSource != null) {
                        itemsSource.Remove(item);
                        if (itemsSource.Count == 0) {
                            Hide();
                        }
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnSelectedItemChanged(object sender, SelectedItemChangedEventArgs e) {
            try {
                var itemsSource = documentContainer.ItemsSource as ObservableCollection<ViewChatUser>;
                if (((itemsSource != null) && (e.Item != null)) && (e.Item.DataContext != null)) {
                    var viewChatUser = e.Item.DataContext as ViewChatUser;
                    if (viewChatUser != null) {
                        SelectedViewChatUser = itemsSource.Single(t => t.ChatUser == viewChatUser.ChatUser);
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        public event PropertyChangedEventHandler SelectedViewChatUserChanged;
        protected virtual void OnSelectedViewChatUserChanged([CallerMemberName] string propertyName = null) {
            var selectedViewChatUserChanged = SelectedViewChatUserChanged;
            if (selectedViewChatUserChanged != null) {
                selectedViewChatUserChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public static ChatWindow Instance {
            get { return (_instance ?? (_instance = new ChatWindow())); }
        }

        public string SelectedUserBare {
            set { ActivateDockItemByBare(value); }
        }

        public ViewChatUser SelectedViewChatUser {
            get { return _selectedViewChatUser; }
            set {
                if ((_selectedViewChatUser == null) || (_selectedViewChatUser.ChatUser != value.ChatUser)) {
                    _selectedViewChatUser = value;
                    ActivateDockItem();
                    OnSelectedViewChatUserChanged();
                }
            }
        }

        public ObservableCollection<ViewChatUser> ViewChatUsers {
            get { return (ObservableCollection<ViewChatUser>)documentContainer.ItemsSource; }
            set { documentContainer.ItemsSource = value; }
        }
    }
}
