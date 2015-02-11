using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace HelperChat.Resources {
    public static class Extensions {
        public static List<T> FindVisualAncestorOfType<T>(this DependencyObject control) where T : DependencyObject {
            var list = new List<T>();
            for (var obj2 = VisualTreeHelper.GetParent(control); obj2 != null; obj2 = VisualTreeHelper.GetParent(obj2)) {
                var item = obj2 as T;
                if (item != null) {
                    list.Add(item);
                }
            }
            return list;
        }

        public static T GetChildOfType<T>(this DependencyObject depObj) where T : DependencyObject {
            if (depObj != null) {
                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
                    T childOfType;
                    var child = VisualTreeHelper.GetChild(depObj, i);
                    var local1 = child as T;
                    if (local1 != null) {
                        childOfType = local1;
                    }
                    else {
                        childOfType = child.GetChildOfType<T>();
                    }
                    if (childOfType != null) {
                        return childOfType;
                    }
                }
            }
            return default(T);
        }

        public static bool TryGetBoolValue(this XElement xElement) {
            return ((xElement != null) && (xElement.Value == "1"));
        }

        public static DateTime TryGetDateTimeValue(this XElement xElement) {
            return ((xElement != null) ? DateTime.Parse(xElement.Value) : DateTime.MinValue);
        }

        public static int TryGetIntValue(this XElement xElement) {
            return ((xElement != null) ? int.Parse(xElement.Value) : 0);
        }

        public static Uri TryGetValidUri(this string url) {
            if (!(url.StartsWith("http://") || url.StartsWith("https://"))) {
                url = "http://" + url;
            }
            return new Uri(url);
        }

        public static string TryGetValue(this XElement xElement) {
            return ((xElement != null) ? xElement.Value : "");
        }

    }
}
