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
            get { return _ticket != null && _ticket.RowProperty.New; }
        }

        public int Count { get { return Children.Count; } }

        public Uri Uri { get { return _ticket != null ? _ticket.RowType.Uri : null; } }

        public string Title { get; set; }

        public ObservableCollection<ViewTicket> Children { get; set; }
    }

    [XmlRoot("row")]
    public class Ticket : IEquatable<Ticket>, IXmlSerializable
    {
        public Ticket()
        {
            
        }

        public RowProperty RowProperty { get; internal set; }

        public RowType RowType { get; internal set; }

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
            return RowProperty.Number.GetHashCode();
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
            return this.RowProperty != null && other.RowProperty != null && this.RowProperty.Number == other.RowProperty.Number;
        }

        #region IXmlSerializable

        private const string RowTypeGroup = "row_type";
        private const string RowPropertyGroup = "row_properties";

        private static readonly XmlSerializer RowTypeSerializer = new XmlSerializer(typeof(RowType));
        private static readonly XmlSerializer RowPropertySerializer = new XmlSerializer(typeof(RowProperty));

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
            if(element.Element(RowTypeGroup) != null) RowType = (RowType)RowTypeSerializer.Deserialize(element.Element(RowTypeGroup).CreateReader());
            if (element.Element(RowPropertyGroup) != null) RowProperty = (RowProperty)RowPropertySerializer.Deserialize(element.Element(RowPropertyGroup).CreateReader());
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

    [XmlRoot("row_type")]
    public class RowType : IXmlSerializable
    {

        #region IXmlSerializable
        private const string TemaElementName = "tema";
        private const string UrlElementName = "url";
        private const string TypeNameElementName = "typename";
        private const string TypeElementName = "type";

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() {
            return null;
        }
        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader" /> stream from which the object is deserialized.</param>
        void IXmlSerializable.ReadXml(XmlReader reader) {
            XElement element = XElement.Load(reader);
            Theme = HttpUtility.HtmlDecode(element.Element(TemaElementName).TryGetValue());
            Uri = new Uri(HttpUtility.HtmlDecode(element.Element(UrlElementName).TryGetValue()));
            Name = HttpUtility.HtmlDecode(element.Element(TypeNameElementName).TryGetValue());
            TypeRow = element.Element(TypeElementName).TryGetValue();
        }

        public string TypeRow { get; internal set; }

        public string Name { get; internal set; }

        public Uri Uri { get; internal set; }

        public string Theme { get; internal set; }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter" /> stream to which the object is serialized.</param>
        /// <remarks>Not supported.</remarks>
        /// <exception cref="System.NotSupportedException"></exception>
        void IXmlSerializable.WriteXml(XmlWriter writer) {
            throw new NotSupportedException();
        }
        #endregion
    }

    [XmlRoot("row_properties")]
    public class RowProperty : IXmlSerializable {
        public Priority Priority { get; internal set; }

        public Contractor Contractor { get; internal set; }

        public Status Status { get; internal set; }

        public DateTime Date { get; internal set; }

        public bool New { get; internal set; }

        public int Number { get; internal set; }

        #region IXmlSerializable
        private const string NumberElementName = "number";
        private const string NewElementName = "new";
        private const string DateElementName = "date";

        private const string StatusGroup = "status";
        private const string ContractorGroup = "contractor";
        private const string PriorityGroup = "priority";

        private static readonly XmlSerializer StatusSerializer = new XmlSerializer(typeof(Status));
        private static readonly XmlSerializer ContractorSerializer = new XmlSerializer(typeof(Contractor));
        private static readonly XmlSerializer PrioritySerializer = new XmlSerializer(typeof(Priority));

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() {
            return null;
        }
        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader" /> stream from which the object is deserialized.</param>
        void IXmlSerializable.ReadXml(XmlReader reader) {
            XElement element = XElement.Load(reader);
            Number = element.Element(NumberElementName).TryGetIntValue();
            New = element.Element(NewElementName).TryGetBoolValue();
            Date = element.Element(DateElementName).TryGetDateTimeValue();
            if (element.Element(StatusGroup) != null) Status = (Status)StatusSerializer.Deserialize(element.Element(StatusGroup).CreateReader());
            if (element.Element(ContractorGroup) != null) Contractor = (Contractor)ContractorSerializer.Deserialize(element.Element(ContractorGroup).CreateReader());
            if (element.Element(PriorityGroup) != null) Priority = (Priority)PrioritySerializer.Deserialize(element.Element(PriorityGroup).CreateReader());
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter" /> stream to which the object is serialized.</param>
        /// <remarks>Not supported.</remarks>
        /// <exception cref="System.NotSupportedException"></exception>
        void IXmlSerializable.WriteXml(XmlWriter writer) {
            throw new NotSupportedException();
        }
        #endregion
    }

    [XmlRoot("status")]
    public class Status : IXmlSerializable {

        #region IXmlSerializable
        private const string StatusIDElementName = "status_id";
        private const string StatusNameElementName = "status_name";

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() {
            return null;
        }
        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader" /> stream from which the object is deserialized.</param>
        void IXmlSerializable.ReadXml(XmlReader reader) {
            XElement element = XElement.Load(reader);
            ID = element.Element(StatusIDElementName).TryGetIntValue();
            Name = HttpUtility.HtmlDecode(element.Element(StatusNameElementName).TryGetValue());
        }

        public int ID { get; internal set; }

        public string Name { get; internal set; }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter" /> stream to which the object is serialized.</param>
        /// <remarks>Not supported.</remarks>
        /// <exception cref="System.NotSupportedException"></exception>
        void IXmlSerializable.WriteXml(XmlWriter writer) {
            throw new NotSupportedException();
        }
        #endregion
    }

    [XmlRoot("contractor")]
    public class Contractor : IXmlSerializable {

        #region IXmlSerializable
        private const string ContractorIDElementName = "contractor_id";
        private const string ContractorNameElementName = "contractor_name";

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() {
            return null;
        }
        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader" /> stream from which the object is deserialized.</param>
        void IXmlSerializable.ReadXml(XmlReader reader) {
            XElement element = XElement.Load(reader);
            ID = element.Element(ContractorIDElementName).TryGetIntValue();
            Name = HttpUtility.HtmlDecode(element.Element(ContractorNameElementName).TryGetValue());
        }

        public int ID { get; internal set; }

        public string Name { get; internal set; }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter" /> stream to which the object is serialized.</param>
        /// <remarks>Not supported.</remarks>
        /// <exception cref="System.NotSupportedException"></exception>
        void IXmlSerializable.WriteXml(XmlWriter writer) {
            throw new NotSupportedException();
        }
        #endregion
    }

    [XmlRoot("priority")]
    public class Priority : IXmlSerializable {

        #region IXmlSerializable
        private const string PriorityIDElementName = "priority_id";
        private const string PriorityNameElementName = "priority_name";

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() {
            return null;
        }
        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader" /> stream from which the object is deserialized.</param>
        void IXmlSerializable.ReadXml(XmlReader reader) {
            XElement element = XElement.Load(reader);
            ID = element.Element(PriorityIDElementName).TryGetIntValue();
            Name = HttpUtility.HtmlDecode(element.Element(PriorityNameElementName).TryGetValue());
        }

        public int ID { get; internal set; }

        public string Name { get; internal set; }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter" /> stream to which the object is serialized.</param>
        /// <remarks>Not supported.</remarks>
        /// <exception cref="System.NotSupportedException"></exception>
        void IXmlSerializable.WriteXml(XmlWriter writer) {
            throw new NotSupportedException();
        }
        #endregion
    }
}
