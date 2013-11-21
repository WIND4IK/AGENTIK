using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using AGENTIK.Models;
using jabber.client;

namespace AGENTIK {
    /// <summary>
    /// Interaction logic for ChatUserWindow.xaml
    /// </summary>
    public partial class ChatUserWindow : Window {

        private readonly JabberClient _jabberClient;

        public ChatUser User { get; set; }

        public ChatUserWindow(JabberClient jabberClient) {
            InitializeComponent();

            _jabberClient = jabberClient;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
            _jabberClient.OnMessage += JabberClientOnMessage;
            sendTextBox.Focus();
        }

        private void sendTextBox_KeyUp(object sender, KeyEventArgs e) {
            if(e.Key == Key.Enter)
                SendMessage();
        }

        private void SendMessage() {
            var reply = new jabber.protocol.client.Message(_jabberClient.Document);
            var textRange = new TextRange(sendTextBox.Document.ContentStart, sendTextBox.Document.ContentEnd);
            if (textRange.Text != @"\n" && textRange.Text != @" ") {
                reply.Body = textRange.Text;
                if (reply.Body != "") {
                    reply.To = User.Bare;
                    _jabberClient.Write(reply);
                    string sentMsg = _jabberClient.User + "\n" + textRange.Text + "\n";
                    AppendConversation(sentMsg);
                    textRange.Text = "";
                }
            }

        }
        public void JabberClientOnMessage(object sender, jabber.protocol.client.Message msg) {
            if (IsActive) {
                if (msg.From.Bare == User.Bare) {
                    if (msg.Body != "") {
                        string receivedMsg = msg.From.User + " Says : " + msg.Body + "\n";
                        AppendConversation(receivedMsg);
                        msg.Body = "";
                    }
                }
            }
        }

        public void AppendConversation(string str) {
            receiveTextBox.AppendText(str);
            receiveTextBox.Focus();
            receiveTextBox.ScrollToEnd();
            receiveTextBox.Focus();
        }

    }
}
