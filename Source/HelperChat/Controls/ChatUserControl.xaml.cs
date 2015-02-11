using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using HelperChat.Models;
using HelperChat.Resources;
using jabber.client;
using jabber.protocol.client;
using log4net;

namespace HelperChat.Controls {
    /// <summary>
    /// Interaction logic for ChatUserControl.xaml
    /// </summary>
    public partial class ChatUserControl : UserControl {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly DependencyProperty JabberClientProperty = DependencyProperty.Register("JabberClient", typeof(JabberClient), typeof(ChatUserControl), new PropertyMetadata(null));
        public static readonly DependencyProperty LoadHistoryProperty = DependencyProperty.Register("LoadHistory", typeof(bool), typeof(ChatUserControl), new PropertyMetadata(false));
        public static readonly DependencyProperty MessagesProperty = DependencyProperty.Register("Messages", typeof(ObservableCollection<ChatMessage>), typeof(ChatUserControl), new PropertyMetadata(null));
        public static readonly DependencyProperty UserProperty = DependencyProperty.Register("User", typeof(ChatUser), typeof(ChatUserControl), new PropertyMetadata(null));

        public JabberClient JabberClient {
            get {
                return (JabberClient)GetValue(JabberClientProperty);
            }
            set {
                SetValue(JabberClientProperty, value);
            }
        }

        public bool LoadHistory {
            get {
                return (bool)GetValue(LoadHistoryProperty);
            }
            set {
                SetValue(LoadHistoryProperty, value);
            }
        }

        public ObservableCollection<ChatMessage> Messages {
            get {
                return (ObservableCollection<ChatMessage>)GetValue(MessagesProperty);
            }
            set {
                SetValue(MessagesProperty, value);
            }
        }

        public ChatUser User {
            get {
                return (ChatUser)GetValue(UserProperty);
            }
            set {
                SetValue(UserProperty, value);
            }
        }

        public ChatUserControl() {
            InitializeComponent();
            Loaded += OnLoaded;
        }


        public void AppendConversation(string user, string text, string bare) {
            try {
                var item = new ChatMessage(user) {
                    Text = text,
                    From = bare
                };
                Messages.Add(item);
                Helper.SaveToFile(item);
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        public void JabberClientOnMessage(object sender, Message msg) {
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
            try {
                sendTextBox.Focus();
                if (JabberClient != null) {
                    JabberClient.OnMessage += JabberClientOnMessage;
                }
                if (LoadHistory) {
                    ReadFromFile();
                }
                receiveRichTextBox.ItemsSource = Messages;
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnSendTextBoxKeyUp(object sender, KeyEventArgs e) {
            var flag = e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) | e.KeyboardDevice.IsKeyDown(Key.RightCtrl);
            if (!(flag || (e.Key != Key.Return))) {
                e.Handled = true;
                SendMessage();
            }
            if ((e.Key == Key.Return) && flag) {
                e.Handled = true;
                var box = (RichTextBox)sender;
                if (box != null) {
                    var offsetToPosition = box.CaretPosition.GetOffsetToPosition(box.Selection.Start);
                    var num2 = box.CaretPosition.GetOffsetToPosition(box.CaretPosition.DocumentEnd);
                    box.CaretPosition.InsertTextInRun("\r");
                    if ((offsetToPosition + 2) == num2) {
                        box.CaretPosition = box.CaretPosition.DocumentEnd;
                    }
                }
            }
        }

        private void OnSendTextBoxPreviewKeyDown(object sender, KeyEventArgs e) {
            try {
                if (!(e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) | e.KeyboardDevice.IsKeyDown(Key.RightCtrl)) && (e.Key == Key.Return)) {
                    e.Handled = true;
                    var box = (RichTextBox)sender;
                    if (box != null) {
                        box.CaretPosition = box.CaretPosition.DocumentEnd;
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void ReadFromFile() {
            try {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "History");
                if (Directory.Exists(path)) {
                    var str3 = Path.Combine(path, string.Format("{0}.csv", User.Bare));
                    if (File.Exists(str3)) {
                        var list = new List<ChatMessage>();
                        var num = 0;
                        using (var reader = new StreamReader(File.Open(str3, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))) {
                            while (!reader.EndOfStream) {
                                var str4 = reader.ReadLine();
                                if ((str4 != null) && (num != 0)) {
                                    var strArray = str4.Split(new[] { ';' });
                                    var item = new ChatMessage(strArray[0]) {
                                        Date = DateTime.Parse(strArray[1]),
                                        From = strArray[2],
                                        Text = strArray[3].Replace("☺n☺", "\r")
                                    };
                                    list.Add(item);
                                }
                                num++;
                            }
                        }
                        list.Reverse();
                        foreach (var message3 in list) {
                            Messages.Insert(0, message3);
                        }
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        public void Refresh() {
            if (LoadHistory) {
                ReadFromFile();
            }
        }

        private void SendMessage() {
            try {
                if ((JabberClient != null) && JabberClient.IsAuthenticated) {
                    var elem = new Message(JabberClient.Document);
                    var range = new TextRange(sendTextBox.Document.ContentStart, sendTextBox.Document.ContentEnd);
                    if ((range.Text != @"\n") && (range.Text != " ")) {
                        elem.Body = range.Text.TrimEnd(new[] { '\n', '\r' });
                        if (elem.Body != "") {
                            elem.To = User.Bare;
                            JabberClient.Write(elem);
                            var text = range.Text.TrimEnd(new[] { '\n', '\r' });
                            AppendConversation(JabberClient.User, text, JabberClient.JID.Bare);
                            range.Text = "";
                        }
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

    }
}
