using System.Windows.Media;
using jabber.protocol.client;

namespace HelperChat.Models {
    public class JabberStatus {
        public ImageSource Image { get; set; }

        public string Name { get; set; }

        public PresenceType PresenceType { get; set; }

    }
}
