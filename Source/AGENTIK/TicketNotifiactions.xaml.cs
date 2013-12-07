using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AGENTIK.Models;

namespace AGENTIK
{
    public partial class TicketNotifiactions
    {
        private const byte MAX_NOTIFICATIONS = 4;
        private bool _isActive;
        public ObservableCollection<Ticket> TicketNotifications = new ObservableCollection<Ticket>();
        private readonly ObservableCollection<Ticket> buffer = new ObservableCollection<Ticket>();

        public TicketNotifiactions()
        {
            InitializeComponent();
            NotificationsControl.DataContext = TicketNotifications;
            Loaded += GrowlNotifiactionsLoaded;
        }

        void GrowlNotifiactionsLoaded(object sender, RoutedEventArgs e) {
            Left = SystemParameters.WorkArea.Width - ActualWidth;
            Top = SystemParameters.WorkArea.Height - ActualHeight;
        }

        public void AddNotification(Ticket notification)
        {
            if (TicketNotifications.Count + 1 > MAX_NOTIFICATIONS)
                buffer.Add(notification);
            else 
                TicketNotifications.Insert(0, notification);

            //Show window if there're notifications
            if (TicketNotifications.Count > 0 && !_isActive) {
                Show();
                _isActive = true;
            }
        }

        public void RemoveNotification(Ticket notification)
        {
            if (TicketNotifications.Contains(notification))
                TicketNotifications.Remove(notification);
            
            if (buffer.Count > 0)
            {
                TicketNotifications.Insert(0, buffer[0]);
                buffer.RemoveAt(0);
            }
            
            //Close window if there's nothing to show
            if (TicketNotifications.Count < 1) {
                Close();
                _isActive = false;
            }
        }

        private void NotificationWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Height != 0.0)
                return;
            var element = sender as Grid;
            RemoveNotification(TicketNotifications.First(n => n.RowProperty.Number == Int32.Parse(element.Tag.ToString())));
        }

        private void NotificationWindowMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var element = sender as Grid;
            var ticket = TicketNotifications.First(t => t.RowProperty.Number == Int32.Parse(element.Tag.ToString()));
            Process.Start(ticket.RowType.Uri.AbsoluteUri);
            RemoveNotification(ticket);
        }
    }
}
