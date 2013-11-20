using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace AGENTIK.Resources {
    public static class Extensions {
        public static string TryGetValue(this XElement xElement) {
            return xElement != null ? xElement.Value : "";
        }
        public static int TryGetIntValue(this XElement xElement) {
            return xElement != null ? int.Parse(xElement.Value) : 0;
        }
        public static bool TryGetBoolValue(this XElement xElement) {
            return xElement != null && xElement.Value == "1";
        }
        public static DateTime TryGetDateTimeValue(this XElement xElement) {
            return xElement != null ? DateTime.Parse(xElement.Value) : DateTime.MinValue;
        }

        public static Uri TryGetValidUri(this string url) {
            if (!(url.StartsWith(@"http://") || url.StartsWith(@"http://"))) {
                url = @"http://" + url;
            }
            return new Uri(url);
        }

        public static List<ViewTicket> SelectByType(this List<ViewTicket> source, string type) {
            var list = new List<ViewTicket>();
            foreach (var viewTicket in source.ToList()) {
                if (viewTicket.Children.Any(t => t.Ticket.RowType.TypeRow.Equals(type))) {
                    var clone = viewTicket.DeepClone();
                    clone.Children = viewTicket.Children.Where(t => t.Ticket.RowType.TypeRow.Equals(type)).ToList();
                    list.Add(clone);
                }
            }
            return list;
        }
    }
}
