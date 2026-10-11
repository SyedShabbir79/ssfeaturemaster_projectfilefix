using FunctionsV2.Sys_SymPay.Exceptions;
using FunctionsV2.Sys_SymPay.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_SymPay;

public interface ISymPayHelper
{
    Task<AccountLookupResult> GetAccountIdAsync(
        string email,
        long macCode,
        CancellationToken cancellationToken);

    Task<CardsLookupResult> GetCardsForAccountAsync(
        long accountId,
        long macCode,
        CancellationToken cancellationToken);

    Task<ImportAccountsResult> ImportNewAccountAsync(
      CampusCard card,
      string email,
      long mac,
      CancellationToken cancellationToken);

    Task<ImportAccountsResult> ImportExistingAccountAsync(
    CampusCard card,
    string email,
    long mac,
    long accountId,
    CancellationToken cancellationToken);

    Task UpdateCardsAsync(
        Models.UpdateCards request,
        CancellationToken cancellationToken);

    Task<long> ResetCurrentMacAsync();
}

public partial class SymPayHelper : ISymPayHelper
{
    private const string TempUri = "http://tempuri.org/";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SymPayHelper(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClient =
            httpClientFactory.CreateClient("SymPay");

        _configuration = configuration;
    }

    public async Task<AccountLookupResult> GetAccountIdAsync(
        string email,
        long macCode,
        CancellationToken cancellationToken)
    {
        long nextMac = macCode;
        var body = $"""
            <TGetAccountID xmlns="{TempUri}">
                <DatabaseConnection>
                    <Datasource>{Escape(GetSetting("SymPayDatasource"))}</Datasource>
                    <Database>{Escape(GetSetting("SymPayDatabase"))}</Database>
                </DatabaseConnection>
                <ConnectorID>{GetSetting("SymPayConnectorID")}</ConnectorID>
                <SchemeID>{GetSetting("SymPaySchemeID")}</SchemeID>
                <MACCode>{macCode}</MACCode>
                <EmailAddress>{Escape(email)}</EmailAddress>
            </TGetAccountID>
            """;

        var xml = await SendSoapAsync(
            GetSetting("SymPayRulesHost"),
            "TGetAccountID",
            body,
            cancellationToken);

        var responseValue =
            GetRequiredInt(xml, "ResponseValue");

        //Next Mac is only returned when the response value is 0 (success)
        if (responseValue == 0)
        {
            nextMac =
               GetRequiredInt(xml, "NextMacCode");
        }

        var accountId =
            GetFirstLongFromValues(xml);

        return new AccountLookupResult(
            accountId,
            nextMac);
    }

    public async Task<CardsLookupResult> GetCardsForAccountAsync(
        long accountId,
        long macCode,
        CancellationToken cancellationToken)
    {
        var body = $"""
            <TGetCardsForAccount xmlns="{TempUri}">
                <DatabaseConnection>
                    <Datasource>{Escape(GetSetting("SymPayDatasource"))}</Datasource>
                    <Database>{Escape(GetSetting("SymPayDatabase"))}</Database>
                </DatabaseConnection>
                <ConnectorID>{GetSetting("SymPayConnectorID")}</ConnectorID>
                <MACCode>{macCode}</MACCode>
                <AccountID>{accountId}</AccountID>
            </TGetCardsForAccount>
            """;

        var xml = await SendSoapAsync(
            GetSetting("SymPayRulesHost"),
            "TGetCardsForAccount",
            body,
            cancellationToken);

        ValidateResponse(xml, "TGetCardsForAccount");

        var cards = ParseCards(xml);

        return new CardsLookupResult(
            cards,
            GetRequiredInt(xml, "NextMacCode"));
    }

    public async Task<ImportAccountsResult> ImportNewAccountAsync(
     CampusCard card,
     string email,
     long mac,
     CancellationToken cancellationToken)
    {
        return await ImportAccountService(BuildNewAccountXml(card, email, mac), cancellationToken);
    }

    public async Task<ImportAccountsResult> ImportExistingAccountAsync(
        CampusCard card,
        string email,
        long mac,
        long accountId,
        CancellationToken cancellationToken)
    {
        return await ImportAccountService(BuildExistingAccountXml(card, email, mac, accountId), cancellationToken);
    }

    private async Task<ImportAccountsResult> ImportAccountService(string body, CancellationToken cancellationToken)
    {
        var xml = await SendSoapAsync(
            GetSetting("SymPayCashlessHost"),
            "ImportAccounts",
            body,
            cancellationToken);

        ValidateResponse(xml, "ImportAccounts");

        var accountId =
            GetOptionalLong(xml, "AccountID");

        return new ImportAccountsResult(
            accountId);
    }

    public async Task<long> ResetCurrentMacAsync()
    {
        // Read configuration using existing GetSetting method with sensible defaults
        var endpoint = GetSetting("SymPayRulesHost");
        var datasource = GetSetting("SymPayDatasource");
        var database = GetSetting("SymPayDatabase");
        var connectorId = GetSetting("SymPayConnectorID");
        var sharedSecret = GetSetting("SymPaySharedSecret");
        var username = GetSetting("SymPayUsername");
        var password = GetSetting("SymPayPassword");
        string soapEnvelope = SoapEnvelopeForTResetMacCode(datasource, database, connectorId, sharedSecret);

        using var httpClient = new System.Net.Http.HttpClient();
        using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, endpoint)
        {
            Content = new System.Net.Http.StringContent(soapEnvelope, System.Text.Encoding.UTF8, "text/xml")
        };

        // Ensure Authorization header is cleared, then set Basic auth when credentials are present
        request.Headers.Authorization = null;
        if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
        {
            var basic = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basic);
        }

        // SOAPAction header required by the service
        request.Headers.Add("SOAPAction", "\"http://tempuri.org/TResetMACCode\"");

        using var response = await httpClient.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // Parse response XML and extract the TResetMACCodeResult element
        var xmlDoc = new System.Xml.XmlDocument();
        xmlDoc.LoadXml(responseContent);
        var ns = new System.Xml.XmlNamespaceManager(xmlDoc.NameTable);
        ns.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
        ns.AddNamespace("t", "http://tempuri.org/");

        var resultNode = xmlDoc.SelectSingleNode("//t:TResetMACCodeResult", ns);
        if (resultNode == null)
        {
            throw new SymPayBusinessException("TResetMACCodeResult not found in SOAP response.");
        }

        if (!long.TryParse(resultNode.InnerText, out var result))
        {
            throw new SymPayBusinessException("TResetMACCodeResult is not a valid long value.");
        }

        return result;
    }

    public async Task UpdateCardsAsync(
        UpdateCards request,
        CancellationToken cancellationToken)
    {
        var body = Serialize(request);

        var xml = await SendSoapAsync(
            GetSetting("SymPayCashlessHost"),
            "UpdateCards",
            body,
            cancellationToken);

        ValidateResponse(xml, "UpdateCards");
    }

}
