using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Switching;
using InputAwareDisplaySwitcher.Core.Domain.Zones;

namespace InputAwareDisplaySwitcher.Tests;

public sealed class DecisionEngineV1Tests
{
    private readonly DecisionEngineV1 _engine = new();

    [Fact]
    public void Evaluate_BlocksUnknownDevice()
    {
        var request = CreateRequest(new DeviceRegistryResolution
        {
            Status = DeviceRegistryResolutionStatus.UnknownDevice,
            Observation = CreateObservation()
        });

        var decision = _engine.Evaluate(request);

        Assert.Equal(SwitchDecisionStatus.Blocked, decision.Status);
        Assert.Equal(SwitchDecisionReason.UnknownDevice, decision.Reason);
    }

    [Fact]
    public void Evaluate_BlocksUnmappedDevice()
    {
        var request = CreateRequest(new DeviceRegistryResolution
        {
            Status = DeviceRegistryResolutionStatus.UnmappedZone,
            Observation = CreateObservation(),
            MatchedDevice = CreateDevice()
        });

        var decision = _engine.Evaluate(request);

        Assert.Equal(SwitchDecisionStatus.Blocked, decision.Status);
        Assert.Equal(SwitchDecisionReason.UnmappedDevice, decision.Reason);
    }

    [Fact]
    public void Evaluate_BlocksWhenManualLockIsActive()
    {
        var request = CreateRequest(CreateResolvedResolution(), new ApplicationRuntimeState
        {
            IsManualSwitchingLocked = true
        });

        var decision = _engine.Evaluate(request);

        Assert.Equal(SwitchDecisionStatus.Blocked, decision.Status);
        Assert.Equal(SwitchDecisionReason.ManualLockActive, decision.Reason);
    }

    [Fact]
    public void Evaluate_BlocksWhenCooldownIsActive()
    {
        var now = DateTimeOffset.UtcNow;
        var request = CreateRequest(
            CreateResolvedResolution(),
            new ApplicationRuntimeState
            {
                LastSwitchAtUtc = now.AddSeconds(-5)
            },
            new SwitchingPolicy
            {
                Cooldown = TimeSpan.FromSeconds(30)
            },
            now);

        var decision = _engine.Evaluate(request);

        Assert.Equal(SwitchDecisionStatus.Blocked, decision.Status);
        Assert.Equal(SwitchDecisionReason.CooldownActive, decision.Reason);
        Assert.NotNull(decision.CooldownEndsAtUtc);
    }

    [Fact]
    public void Evaluate_ReturnsNoActionWhenZoneIsAlreadyActive()
    {
        var request = CreateRequest(
            CreateResolvedResolution(),
            new ApplicationRuntimeState
            {
                CurrentZoneId = "desk",
                CurrentDisplayProfileId = "desk-profile"
            });

        var decision = _engine.Evaluate(request);

        Assert.Equal(SwitchDecisionStatus.NoAction, decision.Status);
        Assert.Equal(SwitchDecisionReason.AlreadyActive, decision.Reason);
    }

    [Fact]
    public void Evaluate_AllowsSwitchForValidMappedDevice()
    {
        var request = CreateRequest(CreateResolvedResolution());

        var decision = _engine.Evaluate(request);

        Assert.Equal(SwitchDecisionStatus.Allowed, decision.Status);
        Assert.Equal(SwitchDecisionReason.Allowed, decision.Reason);
        Assert.True(decision.ShouldSwitch);
        Assert.Equal("desk", decision.TargetZoneId);
        Assert.Equal("desk-profile", decision.TargetDisplayProfileId);
    }

    private static DecisionRequest CreateRequest(
        DeviceRegistryResolution resolution,
        ApplicationRuntimeState? runtimeState = null,
        SwitchingPolicy? policy = null,
        DateTimeOffset? evaluatedAtUtc = null)
    {
        return new DecisionRequest
        {
            Observation = resolution.Observation,
            Resolution = resolution,
            RuntimeState = runtimeState ?? new ApplicationRuntimeState(),
            Policy = policy ?? new SwitchingPolicy(),
            EvaluatedAtUtc = evaluatedAtUtc ?? DateTimeOffset.UtcNow
        };
    }

    private static DeviceRegistryResolution CreateResolvedResolution()
    {
        var device = CreateDevice();
        var zone = new ZoneDefinition
        {
            ZoneId = "desk",
            Name = "Desk",
            PreferredDisplayProfileId = "desk-profile"
        };
        var profile = new DisplayProfile
        {
            DisplayProfileId = "desk-profile",
            Name = "Desk Only",
            IntentKind = DisplayProfileIntentKind.ExternalOnly
        };

        return new DeviceRegistryResolution
        {
            Status = DeviceRegistryResolutionStatus.Resolved,
            Observation = CreateObservation(),
            MatchedDevice = device,
            Zone = zone,
            TargetProfile = profile
        };
    }

    private static PersistedDeviceIdentity CreateDevice()
    {
        return new PersistedDeviceIdentity
        {
            DeviceId = "keyboard-1",
            FriendlyName = "Desk Keyboard",
            DeviceKind = DeviceKind.Keyboard,
            PreferredPersistenceKey = "instance:desk-keyboard",
            AssignedZoneId = "desk"
        };
    }

    private static RuntimeDeviceObservation CreateObservation()
    {
        return new RuntimeDeviceObservation
        {
            SessionDeviceId = "session-1",
            DeviceKind = DeviceKind.Keyboard,
            InstanceId = "desk-keyboard",
            FriendlyName = "Desk Keyboard",
            ObservedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
