using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AGENTIK
{
    /// <summary>
    /// Interaction logic for TrayIcon.xaml
    /// </summary>
    public partial class TrayIcon : UserControl
    {
        public static readonly DependencyProperty ItemCounterProperty =
            DependencyProperty.Register("ItemCounter", typeof (int), typeof (TrayIcon), new PropertyMetadata(default(int)));

        public int ItemCounter
        {
            get { return (int) GetValue(ItemCounterProperty); }
            set { SetValue(ItemCounterProperty, value); }
        }

        public static readonly DependencyProperty ItemCounterVisibilityProperty =
            DependencyProperty.Register("ItemCounterVisibility", typeof (Visibility), typeof (TrayIcon), new PropertyMetadata(default(Visibility)));

        public Visibility ItemCounterVisibility
        {
            get { return (Visibility) GetValue(ItemCounterVisibilityProperty); }
            set { SetValue(ItemCounterVisibilityProperty, value); }
        }

        public TrayIcon()
        {
            InitializeComponent();
        }
    }
}
