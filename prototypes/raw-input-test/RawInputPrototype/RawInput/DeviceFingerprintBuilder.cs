namespace RawInputPrototype.RawInput;

internal static class DeviceFingerprintBuilder
{
    public static DeviceIdentityAnalysis Analyze(
        RawInputDeviceType deviceType,
        nint deviceHandle,
        string vendorId,
        string productId,
        string rawInputDetails,
        DevicePathAnalysis pathAnalysis,
        SetupApiDeviceMetadata setupApiMetadata)
    {
        var candidatePersistenceKey = BuildCandidatePersistenceKey(setupApiMetadata, pathAnalysis, vendorId, productId, deviceType);

        var fingerprintParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(setupApiMetadata.DeviceInstanceId))
        {
            fingerprintParts.Add($"instance={setupApiMetadata.DeviceInstanceId}");
        }

        if (!string.IsNullOrWhiteSpace(pathAnalysis.TransportSegment))
        {
            fingerprintParts.Add($"transport={pathAnalysis.TransportSegment}");
        }

        if (!string.IsNullOrWhiteSpace(pathAnalysis.CollectionSuffix))
        {
            fingerprintParts.Add($"collection={pathAnalysis.CollectionSuffix}");
        }

        if (!string.IsNullOrWhiteSpace(setupApiMetadata.PrimaryHardwareId))
        {
            fingerprintParts.Add($"hardware={setupApiMetadata.PrimaryHardwareId}");
        }
        else if (!string.IsNullOrWhiteSpace(vendorId) || !string.IsNullOrWhiteSpace(productId))
        {
            fingerprintParts.Add($"vidpid={FormatVidPid(vendorId, productId)}");
        }

        fingerprintParts.Add($"type={deviceType}");

        var stableCandidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(setupApiMetadata.DeviceInstanceId))
        {
            stableCandidates.Add($"SetupAPI device instance ID {setupApiMetadata.DeviceInstanceId} is the best current primary-key candidate, but still needs verification across reconnects, reboots, and receiver/port changes.");
        }

        if (!string.IsNullOrWhiteSpace(pathAnalysis.NormalizedDeviceInterfacePath))
        {
            stableCandidates.Add($"Raw Input device path {pathAnalysis.NormalizedDeviceInterfacePath} is a candidate interface identifier worth comparing across app restarts and reconnects.");
        }

        if (!string.IsNullOrWhiteSpace(setupApiMetadata.PrimaryHardwareId))
        {
            stableCandidates.Add($"Hardware ID {setupApiMetadata.PrimaryHardwareId} is useful as a fallback match, but it identifies a model/interface rather than a unique physical device.");
        }
        else if (!string.IsNullOrWhiteSpace(vendorId) || !string.IsNullOrWhiteSpace(productId))
        {
            stableCandidates.Add($"VID/PID {FormatVidPid(vendorId, productId)} is only a coarse fallback and is not unique enough on its own.");
        }

        var reconciliationFields = new List<string>();

        if (!string.IsNullOrWhiteSpace(vendorId) || !string.IsNullOrWhiteSpace(productId))
        {
            reconciliationFields.Add($"VID/PID {FormatVidPid(vendorId, productId)}");
        }

        if (!string.IsNullOrWhiteSpace(setupApiMetadata.DisplayName))
        {
            reconciliationFields.Add($"name '{setupApiMetadata.DisplayName}'");
        }

        if (!string.IsNullOrWhiteSpace(setupApiMetadata.Manufacturer))
        {
            reconciliationFields.Add($"manufacturer '{setupApiMetadata.Manufacturer}'");
        }

        if (!string.IsNullOrWhiteSpace(setupApiMetadata.EnumeratorName))
        {
            reconciliationFields.Add($"enumerator '{setupApiMetadata.EnumeratorName}'");
        }

        if (!string.IsNullOrWhiteSpace(setupApiMetadata.LocationInformation))
        {
            reconciliationFields.Add($"location '{setupApiMetadata.LocationInformation}'");
        }

        if (!string.IsNullOrWhiteSpace(pathAnalysis.CollectionSuffix))
        {
            reconciliationFields.Add($"top-level collection {pathAnalysis.CollectionSuffix}");
        }

        if (!string.IsNullOrWhiteSpace(rawInputDetails))
        {
            reconciliationFields.Add($"RID details '{rawInputDetails}'");
        }

        var recommendationSummary = "Treat the Raw Input handle as temporary. Prefer device instance ID when SetupAPI resolves it, fall back to Raw Input path plus hardware IDs and descriptive metadata, and keep manual verification notes for reconnect, reboot, and receiver-change behaviour.";

        return new DeviceIdentityAnalysis
        {
            CandidatePersistenceKey = candidatePersistenceKey,
            CandidateFingerprint = fingerprintParts.Count == 0
                ? "No fingerprint components available."
                : string.Join(" | ", fingerprintParts),
            SessionScopedCandidates = $"Raw Input handle {RawInputInterop.FormatHandle(deviceHandle)} is session-scoped and should not be stored as a durable identity key.",
            PotentiallyStableCandidates = stableCandidates.Count == 0
                ? "No stronger persistence candidates resolved yet. Manual investigation should focus on device path stability and SetupAPI resolution."
                : string.Join(" ", stableCandidates),
            ReconciliationMetadata = reconciliationFields.Count == 0
                ? "No extra reconciliation metadata was available beyond the raw handle."
                : string.Join("; ", reconciliationFields),
            RecommendationSummary = recommendationSummary
        };
    }

    private static string BuildCandidatePersistenceKey(
        SetupApiDeviceMetadata setupApiMetadata,
        DevicePathAnalysis pathAnalysis,
        string vendorId,
        string productId,
        RawInputDeviceType deviceType)
    {
        if (!string.IsNullOrWhiteSpace(setupApiMetadata.DeviceInstanceId))
        {
            return $"instance:{setupApiMetadata.DeviceInstanceId}";
        }

        if (!string.IsNullOrWhiteSpace(pathAnalysis.NormalizedDeviceInterfacePath))
        {
            return $"path:{pathAnalysis.NormalizedDeviceInterfacePath}";
        }

        if (!string.IsNullOrWhiteSpace(setupApiMetadata.PrimaryHardwareId))
        {
            return $"hardware:{setupApiMetadata.PrimaryHardwareId}";
        }

        if (!string.IsNullOrWhiteSpace(vendorId) || !string.IsNullOrWhiteSpace(productId))
        {
            return $"{deviceType.ToString().ToLowerInvariant()}:{FormatVidPid(vendorId, productId)}";
        }

        return "unresolved:no-stable-key-yet";
    }

    private static string FormatVidPid(string vendorId, string productId)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(vendorId))
        {
            parts.Add($"VID_{vendorId}");
        }

        if (!string.IsNullOrWhiteSpace(productId))
        {
            parts.Add($"PID_{productId}");
        }

        return parts.Count == 0
            ? "VID/PID unavailable"
            : string.Join("/", parts);
    }
}
