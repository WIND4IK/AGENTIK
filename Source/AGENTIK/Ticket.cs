using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace AGENTIK
{
    public class Notifications : ObservableCollection<Ticket> { }

    [Serializable]
    public class ViewTicket
    {
        private readonly Ticket _ticket;

        public ViewTicket(){ }

        public ViewTicket(Ticket ticket)
        {
            _ticket = ticket;
            Children = new List<ViewTicket>();
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

        public List<TimerButton> TimerButtons {get { return _ticket != null ? _ticket.Buttons : new List<TimerButton>(); }} 

        public string Title { get; set; }

        public List<ViewTicket> Children { get; set; }

        public object Clone() {
            return this.MemberwiseClone();
        }
        public ViewTicket DeepClone() {
            using (var ms = new MemoryStream()) {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, this);
                ms.Position = 0;
                return (ViewTicket)formatter.Deserialize(ms);
            }
        }
    }

    [Serializable]
    [XmlRoot("row")]
    public class Ticket : IEquatable<Ticket>, IXmlSerializable
    {
        public Ticket()
        {
            Buttons = new List<TimerButton>();
        }

        public RowProperty RowProperty { get; internal set; }

        public RowType RowType { get; internal set; }

        public List<TimerButton> Buttons { get; internal set; }

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

        private const string ButtonGroup = "button";
        private const string ButtonsGroup = "buttons";

        private static readonly XmlSerializer RowTypeSerializer = new XmlSerializer(typeof(RowType));
        private static readonly XmlSerializer RowPropertySerializer = new XmlSerializer(typeof(RowProperty));
        private static readonly XmlSerializer ButtonSerializer = new XmlSerializer(typeof(TimerButton));

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
            XElement buttonsElement = element.Element(ButtonsGroup);
            if (buttonsElement != null) {
                foreach (var buttonElement in buttonsElement.Elements(ButtonGroup)) {
                    Buttons.Add((TimerButton)ButtonSerializer.Deserialize(buttonElement.CreateReader()));
                }
            }
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

    [Serializable]
    [XmlRoot("row_type")]
    public class RowType : IXmlSerializable
    {

        #region IXmlSerializable
        private const string TemaElementName = "tema";
        private const string UrlElementName = "url";
        private const string IconElementName = "ico";
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
            Icon = new Uri(HttpUtility.HtmlDecode(element.Element(IconElementName).TryGetValue()));
            Name = HttpUtility.HtmlDecode(element.Element(TypeNameElementName).TryGetValue());
            TypeRow = element.Element(TypeElementName).TryGetValue();
        }

        public string TypeRow { get; internal set; }

        public string Name { get; internal set; }

        public Uri Uri { get; internal set; }

        public Uri Icon { get; internal set; }

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

    [Serializable]
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
            var statusGroup = element.Element(StatusGroup);
            if (statusGroup != null)
                Status = (Status)StatusSerializer.Deserialize(statusGroup.CreateReader());
            var contractorGroup = element.Element(ContractorGroup);
            if (contractorGroup != null)
                Contractor = (Contractor)ContractorSerializer.Deserialize(contractorGroup.CreateReader());
            var priorityGroup = element.Element(PriorityGroup);
            if (priorityGroup != null)
                Priority = (Priority)PrioritySerializer.Deserialize(priorityGroup.CreateReader());
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

    [Serializable]
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

    [Serializable]
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

    [Serializable]
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

    [Serializable]
    [XmlRoot("button")]
    public class TimerButton : IXmlSerializable {

        #region IXmlSerializable
        private const string ActionElementName = "action";
        private const string IconElementName = "ico";
        private const string MethodElementName = "method";

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() {
            return null;
        }
        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader" /> stream from which the object is deserialized.</param>
        void IXmlSerializable.ReadXml(XmlReader reader) {
            XElement element = XElement.Load(reader);
            Action = new Uri(HttpUtility.HtmlDecode(element.Element(ActionElementName).TryGetValue()));
            Icon = new Uri(HttpUtility.HtmlDecode(element.Element(IconElementName).TryGetValue()));
            Method = element.Element(MethodElementName).TryGetValue();
        }

        public Uri Action { get; internal set; }

        public Uri Icon { get; internal set; }

        public string Method { get; internal set; }

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
