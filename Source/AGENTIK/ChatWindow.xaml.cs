using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using AGENTIK.Models;
using DevExpress.Data.PLinq.Helpers;
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
        public ObservableCollection<ViewChatUser> ChatUserControls {
            get { return (ObservableCollection<ViewChatUser>)documentContainer.ItemsSource; }
            set {
                documentContainer.ItemsSource = value;
            }
        }

        public ViewChatUser SelectedViewChatUser {
            set { ActivateDockItem(value); }
        }

        private void ActivateDockItem(ViewChatUser viewChatUser) {
            try {
                var baseDockItems = documentContainer.Items;
                foreach (BaseLayoutItem baseLayoutItem in baseDockItems) {
                    if (baseLayoutItem.DataContext.Equals(viewChatUser)) {
                        dockManager.DockController.Activate(baseLayoutItem, true);
                        break;
                    }
                }
            }
            catch (Exception ex) {
                _log.Error(ex);
            }
        }

        public ChatWindow() {
            InitializeComponent();
            
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
                var viewChatUser = e.Item.DataContext as ViewChatUser;
                if (viewChatUser != null) {
                    var itemSource = documentContainer.ItemsSource as ObservableCollection<ViewChatUser>;
                    if (itemSource != null) {
                        itemSource.Remove(viewChatUser);
                        if(itemSource.Count == 0)
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
