using System.Collections.ObjectModel;
using jabber.client;

namespace HelperChat.Models {
    public class ViewChatUser {
        public ChatUser ChatUser { get; set; }

        public JabberClient JabberClient { get; set; }

        public bool LoadHistory { get; set; }

        public ObservableCollection<ChatMessage> Messages { get; set; }

    }
}
