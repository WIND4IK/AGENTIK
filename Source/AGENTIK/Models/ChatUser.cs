using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using jabber.client;
using jabber.protocol.client;

namespace AGENTIK.Models {
    public class ChatUser {
        public string Name { get; set; }

        public string Bare { get; set; }

        public PresenceType Status { get; set; }

        public BitmapImage Image { get { return new BitmapImage(new Uri(Status == PresenceType.available ? "pack://application:,,,/Icons/Inactive.ico" : "pack://application:,,,/Icons/Error.ico")); } }
    }

    public class ViewChatUser {
        public JabberClient JabberClient { get; set; }

        public ChatUser ChatUser { get; set; }
    }
}
