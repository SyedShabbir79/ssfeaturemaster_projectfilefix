using System.Collections.Generic;
using System.Xml.Serialization;

namespace FunctionsV2.Sys_SymPay.Models
{
    [XmlRoot("UpdateCards", Namespace = "http://tempuri.org/")]
    public class UpdateCards
    {
        [XmlElement("ImportDetail")]
        public required ImportDetailUpdateCards ImportDetail { get; set; }
    }

    public class ImportDetailUpdateCards : ImportDetailBase
    {
        [XmlArray("UpdateCardList")]
        [XmlArrayItem("UpdateCard")]
        public required List<UpdateCard> UpdateCardList { get; set; }
    }

    public class UpdateCard
    {
        [XmlElement("AccountID")]
        public long? AccountID { get; set; }

        [XmlElement("Email")]
        public required string Email { get; set; }

        [XmlElement("CardID")]
        public long? CardID { get; set; }

        [XmlElement("Action")]
        public CardUpdateAction Action { get; set; }

        [XmlElement("SAMLUser")]
        public string? SAMLUser { get; set; }

        [XmlElement("CosignUser")]
        public string? CosignUser { get; set; }

        [XmlElement("CosignFactor")]
        public string? CosignFactor { get; set; }

        [XmlElement("LookupStudentID")]
        public string? LookupStudentID { get; set; }
    }

    public enum CardUpdateAction
    {
        [XmlEnum("Add")]
        Add,

        [XmlEnum("Delete")]
        Delete,

        [XmlEnum("MarkLost")]
        MarkLost,

        [XmlEnum("MarkNotLost")]
        MarkNotLost,

        [XmlEnum("MarkActive")]
        MarkActive,

        [XmlEnum("MarkNotActive")]
        MarkNotActive,

        [XmlEnum("MarkRegistered")]
        MarkRegistered,

        [XmlEnum("MarkNotRegistered")]
        MarkNotRegistered
    }
}
        