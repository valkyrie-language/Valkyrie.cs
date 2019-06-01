using System.Text.Json;

namespace VOA.ToolChain.Tests;

public sealed class VoaEffectIntegrationTests
{
    [Fact]
    public void EffectConfig_Default_Values()
    {
        var config = EffectConfig.Default;

        Assert.Equal(3, config.RetryCount);
        Assert.Equal(1000, config.RetryDelayMs);
        Assert.Equal(30000, config.TimeoutMs);
        Assert.Equal(0, config.CacheTtlMs);
        Assert.True(config.Dedupe);
    }

    [Fact]
    public void EffectConfig_Custom_ValuesSnapped()
    {
        var config = new EffectConfig
        {
            RetryCount = 5,
            RetryDelayMs = 200,
            TimeoutMs = 5000,
            CacheTtlMs = 60000
        };

        Assert.Equal(5, config.RetryCount);
        Assert.Equal(200, config.RetryDelayMs);
        Assert.Equal(5000, config.TimeoutMs);
        Assert.Equal(60000, config.CacheTtlMs);
    }

    [Fact]
    public void EffectResult_Pending_HasCorrectStatus()
    {
        var result = EffectFacade.CreatePendingResult("ef-001");

        Assert.Equal(EffectStatus.Pending, result.Status);
        Assert.Equal("ef-001", result.EntryId);
        Assert.Null(result.Data);
        Assert.Null(result.Error);
    }

    [Fact]
    public void EffectResult_Resolved_HasData()
    {
        var data = JsonSerializer.SerializeToElement(@"{""name"": ""Alice""}");
        var result = EffectFacade.CreateResolvedResult("ef-002", data);

        Assert.Equal(EffectStatus.Resolved, result.Status);
        Assert.Equal("ef-002", result.EntryId);
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
    }

    [Fact]
    public void EffectResult_Rejected_HasError()
    {
        var error = "Fetch failed: timeout";
        var result = EffectFacade.CreateRejectedResult("ef-003", error);

        Assert.Equal(EffectStatus.Rejected, result.Status);
        Assert.Equal("ef-003", result.EntryId);
        Assert.Null(result.Data);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void EffectResult_IsPending_ReturnsTrue()
    {
        var result = EffectFacade.CreatePendingResult("ef-004");

        Assert.True(EffectFacade.IsPending(result));
        Assert.False(EffectFacade.IsResolved(result));
        Assert.False(EffectFacade.IsRejected(result));
    }

    [Fact]
    public void EffectResult_IsResolved_ReturnsTrue()
    {
        var data = JsonSerializer.SerializeToElement("{}");
        var result = EffectFacade.CreateResolvedResult("ef-005", data);

        Assert.True(EffectFacade.IsResolved(result));
        Assert.False(EffectFacade.IsPending(result));
        Assert.False(EffectFacade.IsRejected(result));
    }

    [Fact]
    public void EffectResult_IsRejected_ReturnsTrue()
    {
        var result = EffectFacade.CreateRejectedResult("ef-006", "fail");

        Assert.True(EffectFacade.IsRejected(result));
        Assert.False(EffectFacade.IsPending(result));
        Assert.False(EffectFacade.IsResolved(result));
    }

    [Fact]
    public void EffectEntry_Initial_StatusPending()
    {
        var entry = new EffectEntry
        {
            Id = "entry-001",
            FunctionName = "fetchUser",
            Args = new[] { "123" },
            Status = EffectStatus.Pending,
            CreatedAt = 1000L,
            UpdatedAt = 1000L
        };

        Assert.Equal(EffectStatus.Pending, entry.Status);
        Assert.Equal("fetchUser", entry.FunctionName);
        Assert.Empty(entry.Error);
        Assert.Null(entry.Result);
    }

    [Fact]
    public void EffectEntry_TransitionPendingToResolved()
    {
        var entry = new EffectEntry
        {
            Id = "entry-002",
            FunctionName = "loadData",
            Args = [],
            Status = EffectStatus.Pending,
            CreatedAt = 1000L,
            UpdatedAt = 1000L
        };

        Assert.Equal(EffectStatus.Pending, entry.Status);

        var resolved = entry with
        {
            Status = EffectStatus.Resolved,
            Result = JsonSerializer.SerializeToElement(@"{""ok"": true}"),
            UpdatedAt = 2000L
        };

        Assert.Equal(EffectStatus.Resolved, resolved.Status);
        Assert.NotEqual(entry.UpdatedAt, resolved.UpdatedAt);
        Assert.NotNull(resolved.Result);
    }

    [Fact]
    public void EffectEntry_TransitionPendingToRejected()
    {
        var entry = new EffectEntry
        {
            Id = "entry-003",
            FunctionName = "loadData",
            Args = [],
            Status = EffectStatus.Pending,
            CreatedAt = 1000L,
            UpdatedAt = 1000L
        };

        var rejected = entry with
        {
            Status = EffectStatus.Rejected,
            Error = "Network error",
            UpdatedAt = 2000L
        };

        Assert.Equal(EffectStatus.Rejected, rejected.Status);
        Assert.Equal("Network error", rejected.Error);
    }

    [Fact]
    public void EffectStore_RegisterAndResolve()
    {
        var store = new EffectStore();

        var entryId = store.Register("fetchUser", ["42"], EffectConfig.Default);
        Assert.NotNull(entryId);

        var data = JsonSerializer.SerializeToElement(@"{""name"": ""Bob""}");
        store.Resolve(entryId, data);
        var result = store.GetResult(entryId);

        Assert.Equal(EffectStatus.Resolved, result!.Status);
        Assert.NotNull(result.Data);
        Assert.Null(result.Error);
    }

    [Fact]
    public void EffectStore_RegisterAndReject()
    {
        var store = new EffectStore();

        var entryId = store.Register("fetchUser", ["99"], EffectConfig.Default);
        store.Reject(entryId, "Not found");
        var result = store.GetResult(entryId);

        Assert.Equal(EffectStatus.Rejected, result!.Status);
        Assert.Null(result.Data);
        Assert.Equal("Not found", result.Error);
    }

    [Fact]
    public void EffectStore_Duplicate_NullIdReturned()
    {
        var store = new EffectStore();

        var id1 = store.Register("fetch", ["1"], EffectConfig.Default);
        Assert.NotNull(id1);

        var result = store.GetResult(id1);
        Assert.Equal(EffectStatus.Pending, result!.Status);

        store.Resolve(id1, JsonSerializer.SerializeToElement("{}"));
        Assert.Equal(0, store.PendingCount);
    }

    [Fact]
    public void EffectStore_PendingCount_DecrementsOnResolve()
    {
        var store = new EffectStore();

        var id1 = store.Register("fn1", [], new EffectConfig { TimeoutMs = 500 });
        var id2 = store.Register("fn2", [], new EffectConfig { TimeoutMs = 500 });

        Assert.Equal(2, store.PendingCount);

        store.Resolve(id1!, JsonSerializer.SerializeToElement("{}"));
        Assert.Equal(1, store.PendingCount);

        store.Reject(id2!, "fail");
        Assert.Equal(0, store.PendingCount);
    }

    [Fact]
    public void EffectStore_ExpireEntries_RemovesOld()
    {
        var store = new EffectStore();

        var id = store.Register("fn", [], new EffectConfig { TimeoutMs = 0 });
        Assert.NotNull(id);

        var resultBeforeTimeout = store.GetResult(id!)!;

        Assert.Equal(EffectStatus.Pending, resultBeforeTimeout.Status);

        store.Expire(now: 100L);

        var resultAfterTimeout = store.GetResult(id!);

        Assert.Equal(EffectStatus.Rejected, resultAfterTimeout!.Status);
        Assert.Equal("timeout", resultAfterTimeout.Error);
    }

    [Fact]
    public void EffectStore_FullLifecycle()
    {
        var store = new EffectStore();

        var id = store.Register("fetchUser", ["42"], new EffectConfig
        {
            RetryCount = 3,
            RetryDelayMs = 100
        });

        Assert.NotNull(id);
        Assert.Equal(1, store.PendingCount);

        var result = store.GetResult(id!);
        Assert.Equal(EffectStatus.Pending, result!.Status);

        var data = JsonSerializer.SerializeToElement(@"{""id"": 42, ""name"": ""Alice""}");
        store.Resolve(id!, data);

        var resolved = store.GetResult(id!);
        Assert.Equal(EffectStatus.Resolved, resolved!.Status);
        Assert.NotNull(resolved.Data);
    }

    public const double DefaultRetryDelayMs = 1000;
}