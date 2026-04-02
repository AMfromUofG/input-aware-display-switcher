using InputAwareDisplaySwitcher.Core.Application;
using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Diagnostics;
using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Switching;
using InputAwareDisplaySwitcher.Core.Domain.Zones;
using InputAwareDisplaySwitcher.Infrastructure.Diagnostics;

namespace InputAwareDisplaySwitcher.Tests;

public sealed class AutomaticSwitchingControllerTests
{
    [Fact]
    public async Task Start_WhenMappedDeviceInputArrives_InvokesSwitcherWithResolvedProfile()
    {
        var input = new TestInputActivitySource();
        var switcher = new RecordingDisplaySwitcher();
        var recorder = new RecordingOutcomeRecorder();
        var diagnostics = new DiagnosticsService();
        var controller = CreateController(input, switcher, recorder, diagnostics, new ApplicationRuntimeState(), new SwitchingPolicy { Cooldown = TimeSpan.Zero });

        controller.Start();
        await input.PublishAsync(CreateMappedObservation());

        Assert.Equal(1, switcher.CallCount);
        Assert.Equal("desk-profile", switcher.LastProfile?.DisplayProfileId);
        Assert.Single(recorder.Outcomes);
        Assert.Equal(SwitchDecisionStatus.Allowed, recorder.Outcomes[0].Decision.Status);
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.InputActivityDetected);
    }

    [Fact]
    public async Task HandleObservationAsync_ForUnknownDevice_BlocksAndRecordsReason()
    {
        var input = new TestInputActivitySource();
        var switcher = new RecordingDisplaySwitcher();
        var recorder = new RecordingOutcomeRecorder();
        var diagnostics = new DiagnosticsService();
        var controller = CreateController(input, switcher, recorder, diagnostics);

        var outcome = await controller.HandleObservationAsync(new RuntimeDeviceObservation
        {
            SessionDeviceId = "unknown-session",
            DeviceKind = DeviceKind.Keyboard,
            InstanceId = "unknown-instance",
            ObservedAtUtc = DateTimeOffset.UtcNow
        });

        Assert.Equal(SwitchDecisionStatus.Blocked, outcome.Decision.Status);
        Assert.Equal(SwitchDecisionReason.UnknownDevice, outcome.Decision.Reason);
        Assert.Equal(0, switcher.CallCount);
        Assert.Equal(outcome, recorder.Outcomes.Single());
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.SwitchBlocked);
    }

    [Fact]
    public async Task HandleObservationAsync_WhenCooldownActive_BlocksAndDoesNotSwitch()
    {
        var input = new TestInputActivitySource();
        var switcher = new RecordingDisplaySwitcher();
        var recorder = new RecordingOutcomeRecorder();
        var diagnostics = new DiagnosticsService();
        var runtimeState = new ApplicationRuntimeState
        {
            LastSwitchAtUtc = DateTimeOffset.UtcNow.AddSeconds(-5)
        };

        var controller = CreateController(
            input,
            switcher,
            recorder,
            diagnostics,
            runtimeState,
            new SwitchingPolicy { Cooldown = TimeSpan.FromSeconds(30) });

        var outcome = await controller.HandleObservationAsync(CreateMappedObservation());

        Assert.Equal(SwitchDecisionStatus.Blocked, outcome.Decision.Status);
        Assert.Equal(SwitchDecisionReason.CooldownActive, outcome.Decision.Reason);
        Assert.Equal(0, switcher.CallCount);
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.SwitchBlocked);
    }

    [Fact]
    public async Task HandleObservationAsync_WhenManualLockActive_BlocksAndDoesNotSwitch()
    {
        var input = new TestInputActivitySource();
        var switcher = new RecordingDisplaySwitcher();
        var recorder = new RecordingOutcomeRecorder();
        var diagnostics = new DiagnosticsService();
        var runtimeState = new ApplicationRuntimeState
        {
            IsManualSwitchingLocked = true
        };

        var controller = CreateController(input, switcher, recorder, diagnostics, runtimeState);

        var outcome = await controller.HandleObservationAsync(CreateMappedObservation());

        Assert.Equal(SwitchDecisionStatus.Blocked, outcome.Decision.Status);
        Assert.Equal(SwitchDecisionReason.ManualLockActive, outcome.Decision.Reason);
        Assert.Equal(0, switcher.CallCount);
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.SwitchBlocked);
    }

    [Fact]
    public async Task HandleObservationAsync_WhenSwitchSucceeds_RecordsSuccessfulExecution()
    {
        var input = new TestInputActivitySource();
        var switcher = new RecordingDisplaySwitcher();
        var recorder = new RecordingOutcomeRecorder();
        var diagnostics = new DiagnosticsService();
        var controller = CreateController(input, switcher, recorder, diagnostics, new ApplicationRuntimeState(), new SwitchingPolicy { Cooldown = TimeSpan.Zero });

        var outcome = await controller.HandleObservationAsync(CreateMappedObservation());

        Assert.Equal(SwitchDecisionStatus.Allowed, outcome.Decision.Status);
        Assert.Equal(SwitchExecutionStatus.Succeeded, outcome.ExecutionResult.Status);
        Assert.Equal("desk-profile", outcome.ExecutionResult.DisplayProfileId);
        Assert.Equal(outcome, recorder.Outcomes.Single());
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.SwitchAttempted);
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.SwitchSucceeded);
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.RuntimeStateUpdated);
    }

    [Fact]
    public async Task HandleObservationAsync_WhenSwitcherFails_RecordsFailure()
    {
        var input = new TestInputActivitySource();
        var switcher = new FailingDisplaySwitcher();
        var recorder = new RecordingOutcomeRecorder();
        var diagnostics = new DiagnosticsService();
        var controller = CreateController(input, switcher, recorder, diagnostics, new ApplicationRuntimeState(), new SwitchingPolicy { Cooldown = TimeSpan.Zero });

        var outcome = await controller.HandleObservationAsync(CreateMappedObservation());

        Assert.Equal(SwitchDecisionStatus.Allowed, outcome.Decision.Status);
        Assert.Equal(SwitchExecutionStatus.Failed, outcome.ExecutionResult.Status);
        Assert.Equal("Injected switch failure.", outcome.ExecutionResult.ErrorMessage);
        Assert.Equal(1, switcher.CallCount);
        Assert.Equal(outcome, recorder.Outcomes.Single());
        Assert.Contains(diagnostics.Records, record => record.EventType == DiagnosticEventTypes.SwitchFailed);
    }

    private static AutomaticSwitchingController CreateController(
        TestInputActivitySource input,
        InputAwareDisplaySwitcher.Core.Abstractions.IDisplaySwitcher switcher,
        RecordingOutcomeRecorder recorder,
        DiagnosticsService diagnostics,
        ApplicationRuntimeState? runtimeState = null,
        SwitchingPolicy? policy = null)
    {
        var orchestrator = new SwitchingOrchestrator(
            new DeviceRegistryService(new InMemoryDeviceRegistryStore(CreateMappedSnapshot())),
            new DecisionEngineV1(),
            switcher,
            diagnostics);

        return new AutomaticSwitchingController(
            input,
            orchestrator,
            new InMemoryRuntimeStateStore(runtimeState),
            recorder,
            policy ?? new SwitchingPolicy(),
            diagnostics);
    }

    private static DeviceRegistrySnapshot CreateMappedSnapshot()
    {
        return new DeviceRegistrySnapshot
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
    }

    private static RuntimeDeviceObservation CreateMappedObservation()
    {
        return new RuntimeDeviceObservation
        {
            SessionDeviceId = "desk-session",
            DeviceKind = DeviceKind.Keyboard,
            InstanceId = "desk-keyboard",
            FriendlyName = "Desk Keyboard",
            ObservedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
