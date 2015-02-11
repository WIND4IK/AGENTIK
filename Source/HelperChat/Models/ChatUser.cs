using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using jabber.protocol.client;

namespace HelperChat.Models {
    public class ChatUser : INotifyPropertyChanged {
        private BitmapImage _image;

        public string Bare { get; set; }

        public BitmapImage Image {
            get {
                return _image;
            }
            set {
                _image = value;
                OnPropertyChanged();
            }
        }

        public string Name { get; set; }

        public PresenceType Status { get; set; }

        public bool Equals(ChatUser other) {
            if (ReferenceEquals(other, null)) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }
            if (GetType() != other.GetType()) {
                return false;
            }
            return (Bare == other.Bare);
        }

        public override bool Equals(object other) {
            return Equals(other as ChatUser);
        }

        public override int GetHashCode() {
            return Bare.GetHashCode();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null) {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public static bool operator ==(ChatUser left, ChatUser right) {
            if (ReferenceEquals(left, null)) {
                return ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }

        public static bool operator !=(ChatUser left, ChatUser right) {
            return !(left == right);
        }
    }
}
