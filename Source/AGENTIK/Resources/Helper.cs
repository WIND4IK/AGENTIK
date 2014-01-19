using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;
using log4net;

namespace AGENTIK.Resources {
    public static class Helper {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static Stream ConvertToBitmap(RenderTargetBitmap renderTargetBitmap) {
            try {

                var bitmapEncoder = new PngBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                var stream = new MemoryStream();
                bitmapEncoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                return stream;
            }
            catch (Exception ex) {
                Log.Error(ex.Message);
            }
            return null;
        }

        public static BitmapImage GetImageFromUrl(Uri uri) {
            string file = Path.GetTempFileName();
            var webClient = new WebClient();
            webClient.DownloadFile(uri.AbsoluteUri, file);
            var filepath = Path.GetFullPath(file);
            var pictureUri = new Uri(filepath, UriKind.Absolute);
            return new BitmapImage(pictureUri);
        }

        public static T GetVisualChild<T>(DependencyObject parent) where T : Visual {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++) {
                Visual v = (Visual) VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null) {
                    child = GetVisualChild<T>(v);
                }
                if (child != null) {
                    break;
                }
            }
            return child;
        }

        public static ImageSource ImageSourceFromPath(string path) {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri("pack://application:,,,/AGENTIK;component/" + path);
            bitmapImage.EndInit();

            return bitmapImage;
        }

    }
}
