using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;
using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Switching;
using InputAwareDisplaySwitcher.Core.Domain.Zones;
using InputAwareDisplaySwitcher.Infrastructure.Diagnostics;

namespace InputAwareDisplaySwitcher.Tests;

public sealed class SwitchingOrchestratorTests
{
    [Fact]
    public async Task ProcessAsync_DoesNotCallDisplaySwitcherForBlockedDecision()
    {
        var snapshot = new DeviceRegistrySnapshot();
        var switcher = new RecordingDisplaySwitcher();
        var diagnostics = new DiagnosticsService();
        var orchestrator = new SwitchingOrchestrator(
            new DeviceRegistryService(new InMemoryDeviceRegistryStore(snapshot)),
            new DecisionEngineV1(),
            switcher,
            diagnostics);

        var outcome = await orchestrator.ProcessAsync(
            new RuntimeDeviceObservation
            {
                SessionDeviceId = "unknown-session",
                DeviceKind = DeviceKind.Keyboard,
                InstanceId = "unknown",
                ObservedAtUtc = DateTimeOffset.UtcNow
            },
            new ApplicationRuntimeState(),
            new SwitchingPolicy());

        Assert.Equal(SwitchDecisionStatus.Blocked, outcome.Decision.Status);
        Assert.Equal(0, switcher.CallCount);
        Assert.Equal(SwitchExecutionStatus.NotAttempted, outcome.ExecutionResult.Status);
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.DeviceResolutionCompleted);
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.SwitchBlocked);
    }

    [Fact]
    public async Task ProcessAsync_CallsDisplaySwitcherWhenDecisionIsAllowed()
    {
        var snapshot = new DeviceRegistrySnapshot
        {
            Devices =
            [
                new PersistedDeviceIdentity
                {
                    DeviceId = "keyboard-1",
                    FriendlyName = "Desk Keyboard",
                    DeviceKind = DeviceKind.Keyboard,
                    PreferredPersistenceKey = "instance:desk-keyboard",
                    AssignedZoneId = "desk"
                }
            ],
            Zones =
            [
                new ZoneDefinition
                {
                    ZoneId = "desk",
                    Name = "Desk",
                    PreferredDisplayProfileId = "desk-profile"
                }
            ],
            DisplayProfiles =
            [
                new DisplayProfile
                {
                    DisplayProfileId = "desk-profile",
                    Name = "Desk Only",
                    IntentKind = DisplayProfileIntentKind.ExternalOnly
                }
            ]
        };

        var switcher = new RecordingDisplaySwitcher();
        var diagnostics = new DiagnosticsService();
        var orchestrator = new SwitchingOrchestrator(
            new DeviceRegistryService(new InMemoryDeviceRegistryStore(snapshot)),
            new DecisionEngineV1(),
            switcher,
            diagnostics);

        var outcome = await orchestrator.ProcessAsync(
            new RuntimeDeviceObservation
            {
                SessionDeviceId = "desk-session",
                DeviceKind = DeviceKind.Keyboard,
                InstanceId = "desk-keyboard",
                FriendlyName = "Desk Keyboard",
                ObservedAtUtc = DateTimeOffset.UtcNow
            },
            new ApplicationRuntimeState(),
            new SwitchingPolicy
            {
                Cooldown = TimeSpan.Zero
            });

        Assert.Equal(SwitchDecisionStatus.Allowed, outcome.Decision.Status);
        Assert.Equal(1, switcher.CallCount);
        Assert.Equal("desk-profile", switcher.LastProfile?.DisplayProfileId);
        Assert.Equal(SwitchExecutionStatus.Succeeded, outcome.ExecutionResult.Status);
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.SwitchDecisionEvaluated);
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.SwitchSucceeded);
    }
}
