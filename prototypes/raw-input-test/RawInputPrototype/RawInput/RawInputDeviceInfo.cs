namespace RawInputPrototype.RawInput;

internal sealed class RawInputDeviceInfo
{
    public required nint DeviceHandle { get; init; }

    public required RawInputDeviceType DeviceType { get; init; }

    public required string DeviceName { get; init; }

    public required string VendorId { get; init; }

    public required string ProductId { get; init; }

    public required string Identifier { get; init; }

    public required string Details { get; init; }

    public required DevicePathAnalysis DevicePathAnalysis { get; init; }

    public required SetupApiDeviceMetadata SetupApiMetadata { get; init; }

    public required DeviceIdentityAnalysis IdentityAnalysis { get; init; }

    public string DeviceHandleText => RawInputInterop.FormatHandle(DeviceHandle);

    public string DeviceTypeText => DeviceType switch
    {
        RawInputDeviceType.Keyboard => "Keyboard",
        RawInputDeviceType.Mouse => "Mouse",
        RawInputDeviceType.Hid => "HID",
        _ => "Unknown"
    };

    public string FriendlyNameText => !string.IsNullOrWhiteSpace(SetupApiMetadata.DisplayName)
        ? SetupApiMetadata.DisplayName
        : "(no friendly name resolved)";

    public string DeviceInstanceIdText => !string.IsNullOrWhiteSpace(SetupApiMetadata.DeviceInstanceId)
        ? SetupApiMetadata.DeviceInstanceId
        : "(not resolved)";

    public string CandidatePersistenceKey => IdentityAnalysis.CandidatePersistenceKey;

    public string CandidateFingerprint => IdentityAnalysis.CandidateFingerprint;

    public string SessionScopedCandidates => IdentityAnalysis.SessionScopedCandidates;

    public string PotentiallyStableCandidates => IdentityAnalysis.PotentiallyStableCandidates;

    public string ReconciliationMetadata => IdentityAnalysis.ReconciliationMetadata;

    public string LookupStatus => SetupApiMetadata.LookupStatus;

    public string DisplayLabel
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SetupApiMetadata.DisplayName))
            {
                return SetupApiMetadata.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(Identifier))
            {
                return Identifier;
            }

            return $"{DeviceTypeText}: {DeviceHandleText}";
        }
    }

    public string AnalysisReport
    {
        get
        {
            var lines = new List<string>
            {
                $"Display label: {DisplayLabel}",
                $"Device type: {DeviceTypeText}",
                $"Raw handle: {DeviceHandleText}",
                $"Raw Input path: {ValueOrPlaceholder(DeviceName)}",
                $"Normalized path: {ValueOrPlaceholder(DevicePathAnalysis.NormalizedDeviceInterfacePath)}",
                $"Path kind: {ValueOrPlaceholder(DevicePathAnalysis.PathKind)}",
                $"Transport segment: {ValueOrPlaceholder(DevicePathAnalysis.TransportSegment)}",
                $"Instance path segment: {ValueOrPlaceholder(DevicePathAnalysis.InstanceSegment)}",
                $"Collection suffix: {ValueOrPlaceholder(DevicePathAnalysis.CollectionSuffix)}",
                $"Interface class GUID: {ValueOrPlaceholder(DevicePathAnalysis.InterfaceClassGuid)}",
                string.Empty,
                $"Friendly name: {ValueOrPlaceholder(SetupApiMetadata.FriendlyName)}",
                $"Device description: {ValueOrPlaceholder(SetupApiMetadata.DeviceDescription)}",
                $"Device class: {ValueOrPlaceholder(SetupApiMetadata.DeviceClass)}",
                $"Manufacturer: {ValueOrPlaceholder(SetupApiMetadata.Manufacturer)}",
                $"Enumerator: {ValueOrPlaceholder(SetupApiMetadata.EnumeratorName)}",
                $"Location info: {ValueOrPlaceholder(SetupApiMetadata.LocationInformation)}",
                $"Device instance ID: {ValueOrPlaceholder(SetupApiMetadata.DeviceInstanceId)}",
                $"Hardware IDs: {ValueOrPlaceholder(SetupApiMetadata.HardwareIdsText)}",
                $"Lookup status: {ValueOrPlaceholder(SetupApiMetadata.LookupStatus)}",
                string.Empty,
                $"VID/PID: {ValueOrPlaceholder(BuildVidPidText())}",
                $"RID details: {ValueOrPlaceholder(Details)}",
                string.Empty,
                $"Candidate persistence key: {CandidatePersistenceKey}",
                $"Candidate fingerprint: {CandidateFingerprint}",
                $"Session-scoped fields: {SessionScopedCandidates}",
                $"Potentially stable fields: {PotentiallyStableCandidates}",
                $"Reconciliation metadata: {ReconciliationMetadata}",
                $"Recommendation: {IdentityAnalysis.RecommendationSummary}"
            };

            return string.Join(Environment.NewLine, lines);
        }
    }

    public static RawInputDeviceInfo CreateFallback(nint deviceHandle, RawInputDeviceType deviceType, string reason)
    {
        var devicePathAnalysis = DevicePathAnalysis.Empty;
        var setupApiMetadata = SetupApiDeviceMetadata.CreateUnavailable(string.Empty, reason);
        var identityAnalysis = DeviceFingerprintBuilder.Analyze(
            deviceType,
            deviceHandle,
            string.Empty,
            string.Empty,
            reason,
            devicePathAnalysis,
            setupApiMetadata);

        return new RawInputDeviceInfo
        {
            DeviceHandle = deviceHandle,
            DeviceType = deviceType,
            DeviceName = "(device metadata unavailable)",
            VendorId = string.Empty,
            ProductId = string.Empty,
            Identifier = "Metadata unavailable",
            Details = reason,
            DevicePathAnalysis = devicePathAnalysis,
            SetupApiMetadata = setupApiMetadata,
            IdentityAnalysis = identityAnalysis
        };
    }

    private string BuildVidPidText()
    {
        if (string.IsNullOrWhiteSpace(VendorId) && string.IsNullOrWhiteSpace(ProductId))
        {
            return string.Empty;
        }

        return $"VID_{VendorId} / PID_{ProductId}";
    }

    private static string ValueOrPlaceholder(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(unavailable)" : value;
    }
}
