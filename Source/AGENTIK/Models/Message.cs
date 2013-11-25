using System;

namespace AGENTIK.Models {
    class ChatMessage {
        public ChatMessage(string name) {
            Name = String.Format("{0} ({1})", name, DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
        }
        public string Name { get; set; }

        public string Text { get; set; }
    }
}
