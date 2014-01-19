using System;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using jabber.client;
using jabber.protocol.client;

namespace HelperChat.Models {
    public class ChatUser {
        public string Name { get; set; }

        public string Bare { get; set; }

        public PresenceType Status { get; set; }

        public BitmapImage Image { get { return new BitmapImage(new Uri(Status == PresenceType.available ? "pack://application:,,,/Images/Inactive.ico" : "pack://application:,,,/Images/Error.ico")); } }

        public override bool Equals(object obj) {
            var chatUser = obj as ChatUser;
            if (chatUser != null) {
                return Bare.Equals(chatUser.Bare);
            }
            return false;
        }

        public override int GetHashCode() {
            return Bare.GetHashCode();
        }
    }

    public class ViewChatUser {
        public JabberClient JabberClient { get; set; }

        public ChatUser ChatUser { get; set; }

        public ObservableCollection<ChatMessage> Messages { get; set; }
    }
}
