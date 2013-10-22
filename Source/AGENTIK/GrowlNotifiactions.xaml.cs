using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AGENTIK
{
    public partial class GrowlNotifiactions
    {
        private const byte MAX_NOTIFICATIONS = 4;
        private int count;
        private bool _isActive;
        public Notifications Notifications = new Notifications();
        private readonly Notifications buffer = new Notifications();

        public GrowlNotifiactions()
        {
            InitializeComponent();
            NotificationsControl.DataContext = Notifications;
            Loaded += GrowlNotifiactionsLoaded;
        }

        void GrowlNotifiactionsLoaded(object sender, RoutedEventArgs e) {
            Left = SystemParameters.WorkArea.Width - ActualWidth;
            Top = SystemParameters.WorkArea.Height - ActualHeight;
        }

        public void AddNotification(Ticket notification)
        {
            if (Notifications.Count + 1 > MAX_NOTIFICATIONS)
                buffer.Add(notification);
            else 
                Notifications.Insert(0, notification);

            //Show window if there're notifications
            if (Notifications.Count > 0 && !_isActive) {
                Show();
                _isActive = true;
            }
        }

        public void RemoveNotification(Ticket notification)
        {
            if (Notifications.Contains(notification))
                Notifications.Remove(notification);
            
            if (buffer.Count > 0)
            {
                Notifications.Insert(0, buffer[0]);
                buffer.RemoveAt(0);
            }
            
            //Close window if there's nothing to show
            if (Notifications.Count < 1) {
                Hide();
                _isActive = false;
            }
        }

        private void NotificationWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Height != 0.0)
                return;
            var element = sender as Grid;
            RemoveNotification(Notifications.First(n => n.RowProperty.Number == Int32.Parse(element.Tag.ToString())));
        }

        private void NotificationWindowMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var element = sender as Grid;
            var ticket = Notifications.First(t => t.RowProperty.Number == Int32.Parse(element.Tag.ToString()));
            Process.Start(ticket.RowType.Uri.AbsoluteUri);
            RemoveNotification(ticket);
        }
    }
}
