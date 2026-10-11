using FunctionsV2.Sys_SymPay.Exceptions;
using FunctionsV2.Sys_SymPay.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace FunctionsV2.Sys_SymPay;

public partial class SymPayHelper
{
    private async Task<XDocument> SendSoapAsync(
        string endpoint,
        string operation,
        string body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new SymPayRetryableException($"Missing ASMX endpoint for SymPay {operation}.");

        var envelope = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope
                xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                <soap:Body>
                    {body}
                </soap:Body>
            </soap:Envelope>
            """;

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new StringContent(envelope, Encoding.UTF8, "text/xml");

        // SOAPAction header
        request.Headers.TryAddWithoutValidation("SOAPAction", $"\"{TempUri}{operation}\"");

        // Per-request Basic auth if configured (do not mutate _httpClient.DefaultRequestHeaders)
        var username = GetSetting("SymPayUsername");
        var password = GetSetting("SymPayPassword");

        request.Headers.Authorization = null;
        if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
        {
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basic);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new SymPayRetryableException(
                $"SymPay {operation} returned HTTP {(int)response.StatusCode}.");
        }

        return XDocument.Parse(responseXml);
    }

    private static string Serialize<T>(T model)
    {
        var serializer =
            new XmlSerializer(typeof(T));

        var namespaces =
            new XmlSerializerNamespaces();

        namespaces.Add(string.Empty, TempUri);

        var settings =
            new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Encoding = Encoding.UTF8,
                Indent = false
            };

        using var writer = new StringWriter();
        using var xmlWriter =
            XmlWriter.Create(writer, settings);

        serializer.Serialize(
            xmlWriter,
            model,
            namespaces);

        return writer.ToString();
    }

    private static void ValidateResponse(
        XDocument xml,
        string operation)
    {
        var responseValue =
            GetRequiredInt(xml, "ResponseValue");

        if (responseValue == 0)
            return;

        throw new SymPayBusinessException(
            $"{operation} returned SymPay response {responseValue}.");
    }

    private static int GetRequiredInt(
        XDocument xml,
        string element)
    {
        var value =
            xml.Descendants()
                .FirstOrDefault(x =>
                    x.Name.LocalName == element)
                ?.Value;

        if (!int.TryParse(value, out var result))
        {
            throw new InvalidOperationException(
                $"SymPay response does not contain {element}.");
        }

        return result;
    }

    private static long GetOptionalLong(
        XDocument xml,
        string element)
    {
        var value =
            xml.Descendants()
                .FirstOrDefault(x =>
                    x.Name.LocalName == element)
                ?.Value;

        return long.TryParse(value, out var result)
            ? result
            : 0;
    }

    private static string? GetElementValue(
        XElement parent,
        string element) =>
        parent.Elements()
            .FirstOrDefault(x =>
                x.Name.LocalName == element)
            ?.Value;

    private string GetSetting(string key) =>
        _configuration[key]
        ?? throw new InvalidOperationException(
            $"Missing configuration: {key}");

    private static string Escape(string value) =>
        SecurityElement.Escape(value)
        ?? string.Empty;

    private static List<SymPayCard> ParseCards(XDocument xml)
    {
        var cardsNode =
            xml.Descendants()
                .FirstOrDefault(x =>
                    x.Name.LocalName == "Cards");

        if (cardsNode is null)
            return [];

        return cardsNode
            .Elements()
            .Select(x => new SymPayCard
            {
                CardId =
                    GetElementValue(x, "CardID") ?? string.Empty,

                Active =
                    bool.TryParse(
                        GetElementValue(x, "Active"),
                        out var active)
                        && active,

                Lost =
                    bool.TryParse(
                        GetElementValue(x, "Lost"),
                        out var lost)
                        && lost
            })
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.CardId))
            .ToList();
    }

    private static long GetFirstLongFromValues(XDocument xml)
    {
        var values =
            xml.Descendants()
                .FirstOrDefault(x =>
                    x.Name.LocalName == "Values");

        if (values is null)
            return 0;

        var value =
            values.Elements()
                .Select(x => x.Value)
                .FirstOrDefault();

        return long.TryParse(value, out var accountId)
            ? accountId
            : 0;
    }

    private static string SoapEnvelopeForTResetMacCode(string datasource, string database, string connectorId, string sharedSecret)
    {
        // Build SOAP envelope (escape values for XML)
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <TResetMACCode xmlns=""http://tempuri.org/"">
      <DatabaseConnection>
        <Datasource>{System.Security.SecurityElement.Escape(datasource)}</Datasource>
        <Database>{System.Security.SecurityElement.Escape(database)}</Database>
      </DatabaseConnection>
      <ConnectorID>{connectorId}</ConnectorID>
      <SharedSecret>{System.Security.SecurityElement.Escape(sharedSecret)}</SharedSecret>
    </TResetMACCode>
  </soap:Body>
</soap:Envelope>".Trim();
    }

    // Keep existing implementations/mappings here.
    private static string GeneratePassword(int length)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        string hex = Convert.ToHexString(bytes);

        var rng = RandomNumberGenerator.Create();
        var sb = new StringBuilder(hex);
        for (int i = 0; i < sb.Length; i++)
        {
            if (RandomNumberGenerator.GetInt32(0, 2) == 0)
                sb[i] = char.ToLowerInvariant(sb[i]);
        }
        return sb.ToString();
    }

    private static long? ParseCardId(string? value) =>
        long.TryParse(value, out var result) ? result : null;


    private string BuildNewAccountXml(CampusCard card, string email, long mac)
    {
        var id1 = System.Security.SecurityElement.Escape(GetSetting("SymPayDatasource"));
        var id2 = System.Security.SecurityElement.Escape(GetSetting("SymPayDatabase"));
        var connectorId = int.Parse(GetSetting("SymPayConnectorID"));
        var macCode = mac;
        var lookupMethod = "Email";

        var firstname = System.Security.SecurityElement.Escape(card?.EmosFirstname);
        var surname = System.Security.SecurityElement.Escape(card?.EmosLastname);
        var emailEsc = System.Security.SecurityElement.Escape(email ?? string.Empty);
        var password = System.Security.SecurityElement.Escape(GeneratePassword(8));
        var emosCode = System.Security.SecurityElement.Escape(card?.EmosCode ?? "0");
        var schemeId = int.Parse(GetSetting("SymPaySchemeID"));
        var cardId = ParseCardId(card?.EmosMag) ?? 0;

        var startDate = DateTime.Now.ToString("o");
        var endDate = card?.EmosLeaveDate?.DateTime is DateTime emosDate
            ? emosDate.AddMonths(16).ToString("o")
            : DateTime.UtcNow.AddMonths(16).ToString("o");
        var demographic = GetDemographic(card?.EmosType ?? string.Empty, "No"); //TODO : Determine how to get emosCat, currently hardcoded to "No"

        var body = $@"
   <ImportAccounts xmlns=""http://tempuri.org/"">
      <ImportDetail>
        <ID1>{id1}</ID1>
        <ID2>{id2}</ID2>
        <ConnectorID>{connectorId}</ConnectorID>
        <MACCode>{macCode}</MACCode>
        <LookupMethod>{lookupMethod}</LookupMethod>
        <UpdatePersonalDetails>true</UpdatePersonalDetails>
        <UpdatePurses>true</UpdatePurses>
        <UpdateAccountProperties>true</UpdateAccountProperties>
        <AllowAmendToRegister>true</AllowAmendToRegister>
        <UpdateStartEndDates>true</UpdateStartEndDates>

        <Accounts>
          <ImportAccount>
            <Title></Title>
            <Firstname>{firstname}</Firstname>
            <Surname>{surname}</Surname>
            <Email>{emailEsc}</Email>
            <MobileNumber></MobileNumber>
            <CanEmail>true</CanEmail>
            <CanSMS>false</CanSMS>
            <Password>{password}</Password>
            <AccountEnabled>true</AccountEnabled>
            <AccountValidated>true</AccountValidated>
            <Status>{ImportStatus.NewAccount}</Status>
            <SchemeID>{schemeId}</SchemeID>
            <CardID>{cardId}</CardID><Purses>
            <ImportPurse>
               <PurseID>1</PurseID>
               <Balance>0</Balance>
               <ValueToAdd>0</ValueToAdd>
            </ImportPurse>
           <ImportPurse>
               <PurseID>2</PurseID>
               <Balance>0</Balance>
               <ValueToAdd>0</ValueToAdd>
           </ImportPurse>
           </Purses>

          <PropertyList>
          <ImportProperties>
              <PropertyGroupID>1</PropertyGroupID>
              <PropertyID>{demographic}</PropertyID>
          </ImportProperties>
          <ImportProperties>
              <PropertyGroupID>2</PropertyGroupID>
              <PropertyID>1</PropertyID>
              <Text>{emosCode}</Text>
          </ImportProperties>
         </PropertyList>

            <StartDate>{startDate}</StartDate>
            <EndDate>{endDate}</EndDate>

            <UpdateEmail>true</UpdateEmail>
            <NewEmail>{emailEsc}</NewEmail>

            <UpdateDOB>false</UpdateDOB>
            <UpdateAccountGroupID>false</UpdateAccountGroupID>
       </ImportAccount>
      </Accounts>

   </ImportDetail>
 </ImportAccounts>
";
        return body;
    }

    //Existing Logic from ESB
    private static int GetDemographic(string emosType, string? emosCat)
    {
        if (string.Equals(emosType, EmosType.STUDENT.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return emosCat?.ToUpperInvariant() switch
            {
                "CAT1" => 1,
                "CAT2" => 5,
                "CAT3" => 7,
                _ => 2
            };
        }

        // STAFF
        return emosCat?.ToUpperInvariant() switch
        {
            "CAT1" => 3,
            "CAT2" => 6,
            "CAT3" => 8,
            _ => 2   // Property 4 didnt work so defaulting to 3 for staff
        };
    }

    //Add account id
    private string BuildExistingAccountXml(CampusCard card, string email, long mac, long accountId)
    {
        var id1 = System.Security.SecurityElement.Escape(GetSetting("SymPayDatasource"));
        var id2 = System.Security.SecurityElement.Escape(GetSetting("SymPayDatabase"));
        var connectorId = int.Parse(GetSetting("SymPayConnectorID"));
        var macCode = mac;
        var lookupMethod = "Email";

        var firstname = System.Security.SecurityElement.Escape(card?.EmosFirstname);
        var surname = System.Security.SecurityElement.Escape( card?.EmosLastname);
        var emailEsc = System.Security.SecurityElement.Escape(email);
        var password = System.Security.SecurityElement.Escape(GeneratePassword(8));
        var emosCode = System.Security.SecurityElement.Escape(card?.EmosCode) ?? "0";
        var schemeId = int.Parse(GetSetting("SymPaySchemeID"));
        var cardId = ParseCardId(card?.EmosMag) ?? 0;

        var startDate = DateTime.Now.ToString("o");

        var endDate = card?.EmosLeaveDate?.DateTime is DateTime emosDate
                ? emosDate.AddMonths(16).ToString("o")
                : DateTime.UtcNow.AddMonths(16).ToString("o"); var demographic = GetDemographic(card?.EmosType ?? string.Empty, "No"); 

        var body = $@"
   <ImportAccounts xmlns=""http://tempuri.org/"">
      <ImportDetail>
        <ID1>{id1}</ID1>
        <ID2>{id2}</ID2>
        <ConnectorID>{connectorId}</ConnectorID>
        <MACCode>{macCode}</MACCode>
        <LookupMethod>{lookupMethod}</LookupMethod>
        <UpdatePersonalDetails>true</UpdatePersonalDetails>
        <UpdatePurses>false</UpdatePurses>
        <UpdateAccountProperties>true</UpdateAccountProperties>
        <AllowAmendToRegister>true</AllowAmendToRegister>
        <UpdateStartEndDates>false</UpdateStartEndDates>

        <Accounts>
          <ImportAccount>
            <AccountID>{accountId}</AccountID>
            <Title></Title>
            <Firstname>{firstname}</Firstname>
            <Surname>{surname}</Surname>
            <Email>{emailEsc}</Email>
            <MobileNumber></MobileNumber>
            <CanEmail>true</CanEmail>
            <CanSMS>false</CanSMS>
            <Password>{password}</Password>
            <AccountEnabled>true</AccountEnabled>
            <AccountValidated>true</AccountValidated>
            <Status>{ImportStatus.UpdateAccount}</Status>
            <SchemeID>{schemeId}</SchemeID>
            <CardID>{cardId}</CardID>

          <PropertyList>
          <ImportProperties>
              <PropertyGroupID>1</PropertyGroupID>
              <PropertyID>{demographic}</PropertyID>
          </ImportProperties>
          <ImportProperties>
              <PropertyGroupID>2</PropertyGroupID>
              <PropertyID>1</PropertyID>
              <Text>{emosCode}</Text>
          </ImportProperties>
         </PropertyList>

            <EndDate>{endDate}</EndDate>

            <UpdateEmail>true</UpdateEmail>
            <NewEmail>{emailEsc}</NewEmail>

            <UpdateDOB>false</UpdateDOB>
            <UpdateAccountGroupID>false</UpdateAccountGroupID>
       </ImportAccount>
      </Accounts>

   </ImportDetail>
 </ImportAccounts>
";
        return body;
    }
}


