// Adapted from the pinned upstream SteamOperationStateTests.cs; GPL-3.0-only.
using Game.Core;
using Xunit;

namespace Game.Godot.Session.Tests;

public sealed class SteamOperationStateTests
{
    [Fact]
    public void UnavailablePlatformDoesNotReserveRequestsAndRetryCanRecover()
    {
        var operations = new SteamOperationState();
        Assert.False(operations.BeginCreate(1, 0, false));
        Assert.False(operations.Offer(42, 0, 1, false));
        Assert.False(operations.BeginJoin(42, 1, 0, false));
        Assert.True(operations.BeginCreate(1, 1, true));
        Assert.True(operations.CompleteCreate(1, true));
    }

    [Fact]
    public void CanceledCreateMustDrainBeforeAnyNewLobbyOperation()
    {
        var operations = new SteamOperationState();
        Assert.True(operations.BeginCreate(1, 0, true));
        operations.Cancel();
        Assert.False(operations.BeginCreate(2, 1, true));
        Assert.False(operations.Offer(42, 0, 2, true));
        Assert.False(operations.BeginJoin(42, 2, 1, true));
        Assert.False(operations.CompleteCreate(2, true));
        Assert.True(operations.BeginJoin(42, 2, 2, true));
        Assert.Equal(SteamJoinCompletion.Accept, operations.CompleteJoin(42, 0, 2, true));
    }

    [Fact]
    public void LateJoinCannotReplaceNewerJoinAndSameLobbyCannotRetryBeforeDrain()
    {
        var operations = new SteamOperationState();
        Assert.True(operations.BeginJoin(42, 1, 0, true));
        operations.Cancel();
        Assert.False(operations.BeginJoin(42, 2, 1, true));
        Assert.True(operations.BeginJoin(99, 2, 1, true));
        Assert.Equal(SteamJoinCompletion.Leave, operations.CompleteJoin(42, 0, 2, true));
        Assert.Equal(99UL, operations.Target);
        Assert.Equal(SteamJoinCompletion.Accept, operations.CompleteJoin(99, 0, 2, true));
        Assert.Equal(SteamJoinCompletion.Ignore, operations.CompleteJoin(99, 99, 2, true));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void CallbackBeforeNextProcessCannotAdoptCanceledScreenOrSession(long generation, bool acceptingScreen)
    {
        var creation = new SteamOperationState();
        Assert.True(creation.BeginCreate(1, 0, true));
        Assert.False(creation.CompleteCreate(generation, acceptingScreen));
        var joining = new SteamOperationState();
        Assert.True(joining.BeginJoin(42, 1, 0, true));
        Assert.Equal(SteamJoinCompletion.Leave, joining.CompleteJoin(42, 0, generation, acceptingScreen));
    }

    [Fact]
    public void AutomaticHostEntryDoesNotBecomeGuestAndAbandonedJoinDoesNotHideBehindCreate()
    {
        var operations = new SteamOperationState();
        Assert.True(operations.BeginJoin(42, 1, 0, true));
        operations.Cancel();
        Assert.True(operations.BeginCreate(2, 1, true));
        Assert.Equal(SteamJoinCompletion.Leave, operations.CompleteJoin(42, 0, 2, true));
        Assert.Equal(SteamJoinCompletion.Ignore, operations.CompleteJoin(99, 0, 2, true));
        Assert.True(operations.CompleteCreate(2, true));
        Assert.Equal(SteamJoinCompletion.Ignore, operations.CompleteJoin(99, 99, 2, true));
    }

    [Fact]
    public void DuplicateInvitationDoesNotReplaceConsentAndDeclineKeepsCurrentSession()
    {
        var operations = new SteamOperationState();
        Assert.False(operations.Offer(42, 42, 7, true));
        Assert.True(operations.Offer(99, 42, 7, true));
        Assert.False(operations.Offer(99, 42, 7, true));
        Assert.False(operations.Offer(100, 42, 7, true));
        // Opening and closing settings changes neither the session generation nor consent.
        Assert.Equal(SteamOperationKind.None, operations.Kind);
        Assert.Equal(99UL, operations.Confirmation);
        Assert.False(operations.ExpireConfirmation(7));
        operations.DeclineConfirmation(99, 7);
        Assert.True(operations.Offer(99, 42, 7, true));
        Assert.True(operations.AcceptConfirmation(99, 7, 7));
        Assert.False(operations.AcceptConfirmation(99, 7, 7));
        Assert.True(operations.BeginJoin(99, 8, 1, true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SessionChangeDiscardsConsentBeforePollingOrAcceptAndFutureInvitationsRemainUsable(bool acceptBeforePolling)
    {
        var operations = new SteamOperationState();
        Assert.True(operations.Offer(99, 42, 7, true));
        Assert.False(operations.ExpireConfirmation(7));
        Assert.Equal(99UL, operations.Confirmation);
        if (acceptBeforePolling) Assert.False(operations.AcceptConfirmation(99, 8, 7));
        else Assert.True(operations.ExpireConfirmation(8));
        Assert.Equal(0UL, operations.Confirmation);
        Assert.Equal(SteamOperationKind.None, operations.Kind);
        Assert.Equal(0UL, operations.Target);
        Assert.False(operations.AcceptConfirmation(99, 8, 7));
        Assert.True(operations.Offer(100, 42, 8, true));
        Assert.False(operations.AcceptConfirmation(99, 7, 7));
        Assert.Equal(100UL, operations.Confirmation);
        Assert.True(operations.AcceptConfirmation(100, 8, 8));
        Assert.True(operations.BeginJoin(100, 9, 1, true));
    }

    [Fact]
    public void StaleConsentCallbacksCannotAcceptOrDeclineSameLobbyOfferedInReplacementSession()
    {
        var operations = new SteamOperationState();
        Assert.True(operations.Offer(99, 42, 7, true));
        Assert.True(operations.ExpireConfirmation(8));
        Assert.True(operations.Offer(99, 42, 8, true));
        Assert.False(operations.AcceptConfirmation(99, 8, 7));
        operations.DeclineConfirmation(99, 7);
        Assert.Equal(99UL, operations.Confirmation);
        Assert.True(operations.AcceptConfirmation(99, 8, 8));
        Assert.True(operations.BeginJoin(99, 9, 1, true));
    }

    [Fact]
    public void TimeoutRetainsOutstandingIdentityAndSynchronousFailureAllowsRetry()
    {
        var operations = new SteamOperationState();
        Assert.True(operations.BeginJoin(42, 1, 100, true));
        Assert.False(operations.CancelIfInvalid(1, true, 15100, 15000, out _));
        Assert.True(operations.CancelIfInvalid(1, true, 15101, 15000, out ulong abandoned));
        Assert.Equal(42UL, abandoned);
        Assert.False(operations.BeginJoin(42, 1, 15102, true));
        Assert.Equal(SteamJoinCompletion.Leave, operations.CompleteJoin(42, 0, 1, true));
        Assert.True(operations.BeginJoin(42, 1, 15103, true));
        operations.JoinFailed(42);
        Assert.True(operations.BeginCreate(1, 15104, true));
        operations.CreateFailed();
        Assert.True(operations.BeginCreate(1, 15105, true));
    }
}
