using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using HelperChat.Resources;

namespace HelperChat.Models {
    [Serializable, XmlRoot("Message")]
    public class ChatMessage : IXmlSerializable{

        private const string _date = "Date";
        private const string _from = "From";
        private const string _name = "Name";
        private const string _text = "Text";

        public DateTime Date { get; set; }

        public string From { get; set; }

        public string FullName {
            get {
                return string.Format("{0} ({1})", Name, Date.ToString("dd-MM-yyyy HH:mm:ss"));
            }
        }

        public string Name { get; set; }

        public string Text { get; set; }


        public ChatMessage(string name) {
            Name = name;
            Date = DateTime.Now;
            From = string.Empty;
        }

        public override bool Equals(object obj) {
            if (!(obj is ChatMessage)) {
                return false;
            }
            var message = obj as ChatMessage;
            return (message.From.Equals(From) && message.Text.Equals(Text));
        }

        public XmlSchema GetSchema() {
            return null;
        }

        public void ReadXml(XmlReader reader) {
            var element = XElement.Load(reader);
            Name = element.Element("Name").TryGetValue();
            Date = element.Element("Date").TryGetDateTimeValue();
            From = element.Element("From").TryGetValue();
            Text = element.Element("Text").TryGetValue();
        }

        public void WriteXml(XmlWriter writer) {
            writer.WriteElementString("Name", Name);
            writer.WriteElementString("Date", Date.ToString());
            writer.WriteElementString("From", From);
            writer.WriteElementString("Text", Text);
        }
    }
}
