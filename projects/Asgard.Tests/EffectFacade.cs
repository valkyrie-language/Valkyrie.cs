using System.Text.Json;

namespace VOA.ToolChain.Tests;

public sealed class EffectFacade
{
    public static EffectResult CreatePendingResult(string entryId) =>
        new()
        {
            Status = EffectStatus.Pending,
            EntryId = entryId
        };

    public static EffectResult CreateResolvedResult(string entryId, JsonElement data) =>
        new()
        {
            Status = EffectStatus.Resolved,
            EntryId = entryId,
            Data = data
        };

    public static EffectResult CreateRejectedResult(string entryId, string error) =>
        new()
        {
            Status = EffectStatus.Rejected,
            EntryId = entryId,
            Error = error
        };

    public static bool IsPending(EffectResult result) => result.Status == EffectStatus.Pending;

    public static bool IsResolved(EffectResult result) => result.Status == EffectStatus.Resolved;

    public static bool IsRejected(EffectResult result) => result.Status == EffectStatus.Rejected;
}