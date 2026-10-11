using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace FunctionsV2.Sys_SymPay.Models;
// Common base for the <ImportDetail> wrapper used by multiple SOAP operations
[ExcludeFromCodeCoverage]
public class ImportDetailBase
{
    [XmlElement("ID1")]
    public required string ID1 { get; set; }

    [XmlElement("ID2")]
    public required string ID2 { get; set; }

    [XmlElement("ConnectorID")]
    public int? ConnectorID { get; set; }

    [XmlElement("MACCode")]
    public long? MACCode { get; set; }

    [XmlElement("LookupMethod")]
    public LookupMethod LookupMethod { get; set; }
}

[ExcludeFromCodeCoverage]
// Root payload for the ImportAccounts SOAP method
[XmlRoot("ImportAccounts", Namespace = "http://tempuri.org/")]
public class ImportAccounts
{
    [XmlElement("ImportDetail")]
    public required ImportDetailAccounts ImportDetail { get; set; }
}

[ExcludeFromCodeCoverage]
// ImportDetail specialization for ImportAccounts
public class ImportDetailAccounts : ImportDetailBase
{
    [XmlElement("UpdatePersonalDetails")]
    public bool? UpdatePersonalDetails { get; set; }

    [XmlElement("UpdatePurses")]
    public bool? UpdatePurses { get; set; }

    [XmlElement("UpdateAccountProperties")]
    public bool? UpdateAccountProperties { get; set; }

    [XmlElement("AllowAmendToRegister")]
    public bool? AllowAmendToRegister { get; set; }

    [XmlElement("UpdateStartEndDates")]
    public bool? UpdateStartEndDates { get; set; }

    [XmlArray("Accounts")]
    [XmlArrayItem("ImportAccount")]
    public required List<ImportAccount> Accounts { get; set; }
}

[ExcludeFromCodeCoverage]
// Models an account item inside Accounts -> ImportAccount
[Serializable]
[XmlType("ImportAccount")]
public class ImportAccount
{
    [XmlElement("AccountID")]
    public long? AccountID { get; set; }

    [XmlElement("Title")]
    public string? Title { get; set; }

    [XmlElement("Firstname")]
    public string? Firstname { get; set; }

    [XmlElement("Surname")]
    public string? Surname { get; set; }

    [XmlElement("Email")]
    public required string Email { get; set; }

    [XmlElement("MobileNumber")]
    public string? MobileNumber { get; set; }

    [XmlElement("CanEmail", IsNullable = true)]
    public bool? CanEmail { get; set; }

    [XmlElement("CanSMS", IsNullable = true)]
    public bool? CanSMS { get; set; }

    [XmlElement("Password")]
    public string? Password { get; set; }

    [XmlElement("AccountEnabled", IsNullable = true)]
    public bool? AccountEnabled { get; set; }

    [XmlElement("AccountValidated", IsNullable = true)]
    public bool? AccountValidated { get; set; }

    // Allow missing/nullable status values from the SOAP payload
    [XmlElement("Status", IsNullable = true)]
    public ImportStatus? Status { get; set; }

    [XmlElement("SchemeID", IsNullable = true)]
    public int? SchemeID { get; set; }

    [XmlElement("CardID", IsNullable = true)]
    public long? CardID { get; set; }

    // These arrays may be xsi:nil="true" in the SOAP payload; allow null via IsNullable = true
    [XmlArray("Purses", IsNullable = true)]
    [XmlArrayItem("Purse")]
    public List<ImportPurse>? Purses { get; set; }

    [XmlArray("PropertyList", IsNullable = true)]
    [XmlArrayItem("Property")]
    public List<ImportProperties>? PropertyList { get; set; }

    [XmlElement("CosignFactor")]
    public string? CosignFactor { get; set; }

    [XmlElement("CosignUser")]
    public string? CosignUser { get; set; }

    [XmlElement("SAMLName")]
    public string? SAMLName { get; set; }

    [XmlElement("StartDate", IsNullable = true)]
    public DateTime? StartDate { get; set; }

    [XmlElement("EndDate", IsNullable = true)]
    public DateTime? EndDate { get; set; }

    [XmlElement("UpdateEmail", IsNullable = true)]
    public bool? UpdateEmail { get; set; }

    [XmlElement("NewEmail")]
    public string? NewEmail { get; set; }

    [XmlElement("LookupStudentID")]
    public string? LookupStudentID { get; set; }

    [XmlElement("UpdateDOB", IsNullable = true)]
    public bool? UpdateDOB { get; set; }

    [XmlElement("DOB", IsNullable = true)]
    public DateTime? DOB { get; set; }

    [XmlElement("UpdateAccountGroupID", IsNullable = true)]
    public bool? UpdateAccountGroupID { get; set; }

    [XmlElement("AccountGroupID", IsNullable = true)]
    public int? AccountGroupID { get; set; }
}

// Reusable enums for LookupMethod, Status
public enum LookupMethod
{
    [XmlEnum("CardID")]
    CardID,

    [XmlEnum("Email")]
    Email,

    [XmlEnum("AccountID")]
    AccountID
}

public enum ImportStatus
{
    [XmlEnum("NewAccount")]
    NewAccount,

    [XmlEnum("UpdateAccount")]
    UpdateAccount,

    [XmlEnum("Unknown")]
    Unknown
}

public class ImportProperties
{
    public int PropertyGroupID { get; set; }
    public int PropertyID { get; set; }
    public string? Text { get; set; }
}

public class ImportPurse
{
    public int PurseID { get; set; }
    public decimal Balance { get; set; }
    public decimal ValueToAdd { get; set; }
}


