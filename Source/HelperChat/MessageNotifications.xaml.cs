using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using HelperChat.Models;

namespace HelperChat {
    /// <summary>
    /// Interaction logic for MessageNotifications.xaml
    /// </summary>
    public partial class MessageNotifications : DXWindow {
        private bool _isActive;
        private const byte MaxNotifications = 1;
        public ObservableCollection<ChatMessage> Notifications = new ObservableCollection<ChatMessage>();

        public MessageNotifications() {
            InitializeComponent();

            NotificationsControl.DataContext = Notifications;
            Loaded += GrowlNotifiactionsLoaded;

        }

        public void AddNotification(ChatMessage notification) {
            if (Notifications.Count >= MaxNotifications) {
                RemoveNotification(Notifications.First());
            }
            Notifications.Insert(0, notification);
            if (!((Notifications.Count <= 0) || _isActive)) {
                Show();
                _isActive = true;
            }
        }

        private void GrowlNotifiactionsLoaded(object sender, RoutedEventArgs e) {
            Left = SystemParameters.WorkArea.Width - ActualWidth;
            Top = SystemParameters.WorkArea.Height - ActualHeight;
        }

        private void NotificationWindowMouseUp(object sender, MouseButtonEventArgs e) {
            var element = sender as Grid;
            if (Notifications.First().From != null && element != null) {
                var notification = Notifications.First(n => n.From.Equals(element.Tag.ToString()));
                ChatWindow.Instance.SelectedUserBare = notification.From;
                RemoveNotification(notification);
            }
        }

        private void NotificationWindowSizeChanged(object sender, SizeChangedEventArgs e) {
            if (e.NewSize.Height == 0)
            {
                var element = sender as Grid;
                if (Notifications.First().From != null && element != null) {
                    RemoveNotification(Notifications.First(n => n.From.Equals(element.Tag.ToString())));
                }
            }
        }

        public void RemoveNotification(ChatMessage notification) {
            if (Notifications.Contains(notification)) {
                Notifications.Remove(notification);
            }
            if (Notifications.Count < 1) {
                Close();
                _isActive = false;
            }
        }

    }
}
