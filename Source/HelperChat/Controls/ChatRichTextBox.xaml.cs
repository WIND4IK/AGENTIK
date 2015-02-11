using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using HelperChat.Models;
using log4net;

namespace HelperChat.Controls {
    /// <summary>
    /// Interaction logic for ChatRichTextBox.xaml
    /// </summary>
    public partial class ChatRichTextBox : UserControl {
        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly DependencyProperty AcceptsReturnProperty = DependencyProperty.Register("AcceptsReturn", typeof(bool), typeof(ChatRichTextBox), new UIPropertyMetadata(false, OnAcceptsReturnChanged));
        private const string DefaultFontFamily = "Verdana";
        private const int DefaultHeaderTextSize = 12;
        public static readonly DependencyProperty HeaderForegroundProperty = DependencyProperty.Register("HeaderForeground", typeof(Brush), typeof(ChatRichTextBox), new PropertyMetadata(Brushes.DodgerBlue));
        public static readonly DependencyProperty HeaderTextSizeProperty = DependencyProperty.Register("HeaderTextSize", typeof(int), typeof(ChatRichTextBox), new PropertyMetadata(DefaultHeaderTextSize));
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<ChatMessage>), typeof(ChatRichTextBox), new PropertyMetadata(null));
        public static readonly DependencyProperty TextFontFamilyProperty = DependencyProperty.Register("TextFontFamily", typeof(string), typeof(ChatRichTextBox), new PropertyMetadata(DefaultFontFamily));

        public ChatRichTextBox() {
            InitializeComponent();
            richTextBox.AcceptsReturn = false;
            richTextBox.SizeChanged += OnRichTextBoxSizeChanged;
            ClearText();
        }

        private void AppendMessage(ChatMessage message) {
            try {
                var inline = new Run(message.FullName) {
                    Foreground = HeaderForeground,
                    FontSize = HeaderTextSize,
                    FontFamily = new FontFamily(TextFontFamily)
                };
                richTextBox.Document.Blocks.Add(new Paragraph(inline));
                var run2 = new Run(message.Text) {
                    FontFamily = new FontFamily(TextFontFamily)
                };
                richTextBox.Document.Blocks.Add(new Paragraph(run2));
                richTextBox.ScrollToEnd();
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        public void Clear() {
            richTextBox.Document = new FlowDocument();
        }

        private void ClearText()
        {
            new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd) { Text = "" };
        }

        public string GetPlainText() {
            //return EmoticonsHelper.GetPlainText(*/this.richTextBox.Document);
            return richTextBox.Document.ToString();
        }

        private void ItemsSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            try {
                foreach (var item in e.NewItems) {
                    AppendMessage(item as ChatMessage);
                }
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private static void OnAcceptsReturnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var box = (ChatRichTextBox)d;
            box.richTextBox.AcceptsReturn = (bool)e.NewValue;
        }

        private void OnRichTextBoxSizeChanged(object sender, SizeChangedEventArgs e) {
            try {
                richTextBox.ScrollToEnd();
            }
            catch (Exception exception) {
                _log.Error(exception);
            }
        }

        private void OnRichTextboxTextChanged(object sender, TextChangedEventArgs e) {
            richTextBox.ScrollToEnd();
        }


        public bool AcceptsReturn {
            get {
                return (bool)GetValue(AcceptsReturnProperty);
            }
            set {
                SetValue(AcceptsReturnProperty, value);
            }
        }

        public Brush HeaderForeground {
            get {
                return (Brush)GetValue(HeaderForegroundProperty);
            }
            set {
                SetValue(HeaderForegroundProperty, value);
            }
        }

        public int HeaderTextSize {
            get {
                return (int)GetValue(HeaderTextSizeProperty);
            }
            set {
                SetValue(HeaderTextSizeProperty, value);
            }
        }

        public ObservableCollection<ChatMessage> ItemsSource {
            get {
                return (ObservableCollection<ChatMessage>)GetValue(ItemsSourceProperty);
            }
            set {
                SetValue(ItemsSourceProperty, value);
                if (value != null) {
                    ClearText();
                    foreach (var message in value) {
                        AppendMessage(message);
                    }
                    value.CollectionChanged += ItemsSource_CollectionChanged;
                }
            }
        }

        public string TextFontFamily {
            get {
                return (string)GetValue(TextFontFamilyProperty);
            }
            set {
                SetValue(TextFontFamilyProperty, value);
            }
        }
    }
}
