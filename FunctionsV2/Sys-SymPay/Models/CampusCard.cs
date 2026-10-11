using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FunctionsV2.Sys_SymPay.Models
{
    [ExcludeFromCodeCoverage]
    public class CampusCard
    {
        [JsonPropertyName("consolidated_trackingid")]
        public string? ConsolidatedTrackingId { get; set; }

        [JsonPropertyName("emos_bump")]
        public int? EmosBump { get; set; }

        [JsonPropertyName("emos_campus_location")]
        public string? EmosCampusLocation { get; set; }

        [JsonPropertyName("emos_code")]
        public string? EmosCode { get; set; }

        [JsonPropertyName("emos_csn")]
        public string? EmosCsn { get; set; }

        [JsonPropertyName("emos_firstname")]
        public string? EmosFirstname { get; set; }

        [JsonPropertyName("emos_lastname")]
        public string? EmosLastname { get; set; }

        [JsonPropertyName("emos_leave_date")]
        public DateTimeOffset? EmosLeaveDate { get; set; }

        [JsonPropertyName("emos_mag")]
        public string? EmosMag { get; set; }

        [JsonPropertyName("emos_pin")]
        public string? EmosPin { get; set; }

        [JsonPropertyName("emos_type")]
        public string? EmosType { get; set; }

        [JsonPropertyName("emos_upn")]
        public string? EmosUpn { get; set; }

        [JsonPropertyName("enriched_data")]
        public EnrichedData? EnrichedData { get; set; }

        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("ItemInternalId")]
        public string? ItemInternalId { get; set; }

        [JsonPropertyName("rv")]
        public string? Rv { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class EnrichedData
    {
        [JsonPropertyName("email_address")]
        public string? EmailAddress { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("upn")]
        public string? Upn { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }

    public enum EmosType
    {
        ASSOCIATE,
        LIBRARY,
        STUDENT,
        STAFF,
        GREENLANDS,
        GUEST
    }
}
