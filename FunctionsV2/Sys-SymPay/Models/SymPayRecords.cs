using System.Collections.Generic;

namespace FunctionsV2.Sys_SymPay.Models
{
    public sealed record AccountLookupResult(
        long AccountId,
        long NextMacCode);

    public sealed record CardsLookupResult(
        IReadOnlyCollection<SymPayCard> Cards,
        long NextMacCode);

    public sealed class SymPayCard
    {
        public required string CardId { get; init; }
        public bool Active { get; init; }
        public bool Lost { get; init; }
    }
    public sealed record ImportAccountsResult(
    long AccountId);
}
