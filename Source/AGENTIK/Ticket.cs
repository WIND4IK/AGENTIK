using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace AGENTIK
{
    public class ViewTicket
    {
        private readonly Ticket _ticket;

        public ViewTicket()
        {
            
        }

        public ViewTicket(Ticket ticket)
        {
            _ticket = ticket;
            Children = new ObservableCollection<ViewTicket>();
        }
        public Ticket Ticket
        {
            get { return _ticket; }
        }

        public bool IsNew
        {

            get { return true; }
        }

        public int Count { get { return Children.Count; } }

        public Uri Uri { get; set; }

        public string Title { get; set; }

        public ObservableCollection<ViewTicket> Children { get; set; }
    }

    [XmlRoot("row")]
    public class Ticket : IEquatable<Ticket>, IXmlSerializable
    {
        public Ticket()
        {
            
        }

        public int Id { get; internal set; }

        public int Contractor { get; internal set; }

        public string NameContractor { get; internal set; }

        public string Theme { get; internal set; }

        public Uri Uri { get; internal set; }

        public int PriorityId { get; internal set; }

        public string Priority { get; internal set; }

        public int StatusId { get; internal set; }

        public string Status { get; internal set; }

        public List<Ticket> Children { get; set; }

        public static bool operator ==(Ticket left, Ticket right)
        {
            if (object.ReferenceEquals(left, null))
                return object.ReferenceEquals(right, null);
            else
                return left.Equals(right);
        }

        public static bool operator !=(Ticket left, Ticket right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object other)
        {
            return base.Equals(other as Ticket);
        }

        public bool Equals(Ticket other)
        {
            if (object.ReferenceEquals(other, null))
                return false;
            if (object.ReferenceEquals(this, other))
                return true;
            if (this.GetType() != other.GetType())
                return false;
            return this.Id == other.Id;
        }

        #region IXmlSerializable

        private const string idElementName = "ID";

        private const string contractorElementName = "contractor";

        private const string nameContractorElementName = "name_contractor";

        private const string temaElementName = "tema";

        private const string urlElementName = "url";

        private const string idPriorityElementName = "id_priority";

        private const string priorityElementName = "priority";

        private const string idStatusElementName = "id_status";

        private const string statusElementName = "status";

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader" /> stream from which the object is deserialized.</param>
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            XElement element = XElement.Load(reader);
            Id = int.Parse(element.Element(idElementName).Value);
            Contractor = int.Parse(element.Element(contractorElementName).Value);
            NameContractor = HttpUtility.HtmlDecode(element.Element(nameContractorElementName).Value);
            Theme = HttpUtility.HtmlDecode(element.Element(temaElementName).Value);
            Uri = new Uri(HttpUtility.HtmlDecode(element.Element(urlElementName).Value));
            PriorityId = int.Parse(element.Element(idPriorityElementName).Value);
            Priority = element.Element(priorityElementName).Value;
            StatusId = int.Parse(element.Element(idStatusElementName).Value);
            Status = element.Element(statusElementName).Value;
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter" /> stream to which the object is serialized.</param>
        /// <remarks>Not supported.</remarks>
        /// <exception cref="System.NotSupportedException"></exception>
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            throw new NotSupportedException();
        }

        #endregion IXmlSerializable
    }
}
