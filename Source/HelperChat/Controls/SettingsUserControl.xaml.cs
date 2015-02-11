using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using HelperChat.Models;
using log4net;

namespace HelperChat.Controls {
    /// <summary>
    /// Interaction logic for SettingsUserControl.xaml
    /// </summary>
    public partial class SettingsUserControl : UserControl {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SettingsUserControl() {
            InitializeComponent();
            LoadData();
        }

        public List<SettingItem> ItemsSource {
            get {
                return (settingsListView.ItemsSource as List<SettingItem>);
            }
        }

        private void LoadData() {
            try {
                var list = new List<SettingItem>();
                var item = new SettingItem {
                    Name = "Консультант",
                    Control = ChatSettingsControl.Instance
                };
                list.Add(item);
                settingsListView.ItemsSource = list;
                settingsListView.SelectedIndex = 0;
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

    }
}
