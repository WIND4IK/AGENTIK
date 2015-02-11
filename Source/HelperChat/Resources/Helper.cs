using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;
using HelperChat.Models;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace HelperChat.Resources {
    class Helper {
        private static string _company;
        private static string _productName;

        public static Icon ConvertToBitmap(BitmapSource bitmapSource) {
            var pixelWidth = bitmapSource.PixelWidth;
            var pixelHeight = bitmapSource.PixelHeight;
            var stride = pixelWidth * ((bitmapSource.Format.BitsPerPixel + 7) / 8);
            var buffer = Marshal.AllocHGlobal(pixelHeight * stride);
            bitmapSource.CopyPixels(new Int32Rect(0, 0, pixelWidth, pixelHeight), buffer, pixelHeight * stride, stride);
            var bitmap = new Bitmap(pixelWidth, pixelHeight, stride, PixelFormat.Format32bppPArgb, buffer);
            return Icon.FromHandle(bitmap.GetHicon());
        }

        public static object DeSerializeObject(string xmlOfObject, Type objectType) {
            object obj2;
            var input = new StringReader(xmlOfObject);
            var serializer = new XmlSerializer(objectType);
            var xmlReader = new XmlTextReader(input);
            try {
                obj2 = serializer.Deserialize(xmlReader);
            }
            finally {
                xmlReader.Close();
                input.Close();
            }
            return obj2;
        }

        public static Window GetParentWindow(DependencyObject child) {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null) {
                return null;
            }
            var window = parent as Window;
            if (window != null) {
                return window;
            }
            return GetParentWindow(parent);
        }

        public static ImageSource ImageSourceFromPath(string path) {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri("pack://application:,,,/HelperChat;component/" + path);
            image.EndInit();
            return image;
        }

        public static void SaveToFile(ChatMessage message) {
            if (string.IsNullOrEmpty(_productName)) {
                _productName = Assembly.GetExecutingAssembly().GetName().Name;
            }
            var customAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), true);
            if ((customAttributes.Length > 0) && string.IsNullOrEmpty(_company)) {
                _company = ((AssemblyCompanyAttribute)customAttributes[0]).Company;
            }
            var path = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _company, _productName), "History");
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            var str3 = Path.Combine(path, string.Format("{0}.csv", message.From));
            if (!File.Exists(str3)) {
                File.AppendAllLines(str3, new List<string> { message.Name });
            }
            var contents = string.Format("{0};{1};{2};{3}{4}", new object[] { message.Name, message.Date, message.From, message.Text.Replace("\r\n", "☺n☺").Replace("\r", "☺n☺").Replace("\n", "☺n☺"), Environment.NewLine });
            File.AppendAllText(str3, contents);
        }

        public static string SerializeObject(object obj) {
            var serializer = new XmlSerializer(obj.GetType());
            var writer = new StringWriter();
            serializer.Serialize(writer, obj);
            return writer.ToString();
        }

        public static T FindVisualAncestorOfType<T>(DependencyObject d) where T : DependencyObject {
            for (var parent = VisualTreeHelper.GetParent(d); parent != null; parent = VisualTreeHelper.GetParent(parent)) {
                var result = parent as T;
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
