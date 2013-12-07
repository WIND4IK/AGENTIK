using System;

namespace AGENTIK.Models {
    [Serializable]
    public class ChatMessage {
        [NonSerialized]
        private readonly string _fullName;
        public ChatMessage() {
            
        }

        public ChatMessage(string name) {
            Name = name;
            Date = DateTime.Now;
            _fullName = String.Format("{0} ({1})", Name, Date.ToString("dd-MM-yyyy HH:mm:ss"));
        }
        public string Name { get; set; }

        public string FullName { get { return _fullName; } }

        public DateTime Date { get; set; }

        public string From { get; set; }

        public string Text { get; set; }
    }
}
