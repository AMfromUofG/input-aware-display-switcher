using System.Security.Cryptography;
using System.Text;
using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Zones;

namespace InputAwareDisplaySwitcher.Core.Application;

public sealed class DeviceManagementService
{
    public IReadOnlyList<DeviceManagementEntry> BuildEntries(
        DeviceRegistrySnapshot snapshot,
        IReadOnlyList<RuntimeDeviceObservation>? observations)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var runtimeObservations = observations ?? [];
        var entries = new List<DeviceManagementEntry>();
        var matchedDeviceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var observation in runtimeObservations)
        {
            var matchedDevice = FindMatchedDevice(snapshot, observation);
            if (!string.IsNullOrWhiteSpace(matchedDevice?.DeviceId))
            {
                matchedDeviceIds.Add(matchedDevice.DeviceId);
            }

            entries.Add(CreateEntry(snapshot, observation, matchedDevice));
        }

        foreach (var device in snapshot.Devices)
        {
            if (matchedDeviceIds.Contains(device.DeviceId))
            {
                continue;
            }

            entries.Add(CreateEntry(snapshot, observation: null, persistedDevice: device));
        }

        return entries
            .OrderByDescending(entry => entry.IsDetectedThisSession)
            .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.StableIdentitySummary, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public DeviceRegistrySnapshot ApplyEdits(
        DeviceRegistrySnapshot snapshot,
        DeviceManagementEntry entry,
        string friendlyName,
        string? assignedZoneId,
        DateTimeOffset updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(entry);

        var normalizedFriendlyName = NormalizeFriendlyName(friendlyName, entry);
        var normalizedZoneId = string.IsNullOrWhiteSpace(assignedZoneId)
            ? null
            : assignedZoneId.Trim();

        var updatedDevice = entry.PersistedDevice is null
            ? CreatePersistedDevice(entry, normalizedFriendlyName, normalizedZoneId, updatedAtUtc)
            : entry.PersistedDevice with
            {
                FriendlyName = normalizedFriendlyName,
                AssignedZoneId = normalizedZoneId
            };

        var devices = snapshot.Devices.ToList();
        var existingIndex = devices.FindIndex(device =>
            string.Equals(device.DeviceId, updatedDevice.DeviceId, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            devices[existingIndex] = updatedDevice;
        }
        else
        {
            devices.Add(updatedDevice);
        }

        return snapshot with
        {
            Devices = devices
        };
    }

    private static DeviceManagementEntry CreateEntry(
        DeviceRegistrySnapshot snapshot,
        RuntimeDeviceObservation? observation,
        PersistedDeviceIdentity? persistedDevice)
    {
        var assignedZoneId = persistedDevice?.AssignedZoneId;
        var assignedZone = snapshot.FindZone(assignedZoneId);
        var displayName = ResolveDisplayName(observation, persistedDevice);
        var canPersistEdits = persistedDevice is not null
            || (observation is not null && !string.IsNullOrWhiteSpace(DevicePersistenceKeyResolver.GetPreferredDurableKey(observation)));

        return new DeviceManagementEntry
        {
            EntryId = persistedDevice?.DeviceId
                ?? observation?.SessionDeviceId
                ?? Guid.NewGuid().ToString("N"),
            DisplayName = displayName,
            DeviceKind = persistedDevice?.DeviceKind ?? observation?.DeviceKind ?? DeviceKind.Unknown,
            PersistedDeviceId = persistedDevice?.DeviceId,
            PersistedDevice = persistedDevice,
            Observation = observation,
            IsDetectedThisSession = observation is not null,
            IsAvailableThisSession = observation?.IsAvailableThisSession ?? false,
            IsEnabled = persistedDevice?.IsEnabled ?? true,
            AssignedZoneId = assignedZoneId,
            AssignedZoneName = assignedZone?.Name,
            AssignmentState = ResolveAssignmentState(assignedZoneId, assignedZone),
            StableIdentitySummary = BuildStableIdentitySummary(observation, persistedDevice),
            MetadataSummary = BuildMetadataSummary(observation, persistedDevice),
            LastSeenAtUtc = observation?.LastSeenAtUtc ?? observation?.ObservedAtUtc ?? persistedDevice?.LastConfirmedAtUtc,
            CanPersistEdits = canPersistEdits,
            PersistenceWarning = canPersistEdits
                ? null
                : "This device only exposed a session-scoped handle, so it cannot be safely saved yet."
        };
    }

    private static PersistedDeviceIdentity? FindMatchedDevice(
        DeviceRegistrySnapshot snapshot,
        RuntimeDeviceObservation observation)
    {
        var candidateKeys = observation.GetCandidatePersistenceKeys();
        return snapshot.Devices.FirstOrDefault(device => DevicePersistenceKeyResolver
            .GetPersistenceKeys(device)
            .Intersect(candidateKeys, StringComparer.OrdinalIgnoreCase)
            .Any());
    }

    private static PersistedDeviceIdentity CreatePersistedDevice(
        DeviceManagementEntry entry,
        string friendlyName,
        string? assignedZoneId,
        DateTimeOffset updatedAtUtc)
    {
        var observation = entry.Observation
            ?? throw new InvalidOperationException("A runtime observation is required to create a new persisted device.");

        var preferredDurableKey = DevicePersistenceKeyResolver.GetPreferredDurableKey(observation);
        if (string.IsNullOrWhiteSpace(preferredDurableKey))
        {
            throw new InvalidOperationException("The device does not yet expose a durable persistence key.");
        }

        return new PersistedDeviceIdentity
        {
            DeviceId = CreateDeviceId(observation.DeviceKind, preferredDurableKey),
            FriendlyName = friendlyName,
            DeviceKind = observation.DeviceKind,
            PreferredPersistenceKey = preferredDurableKey,
            IdentityEvidence = new DeviceIdentityEvidence
            {
                RawDevicePath = observation.RawDevicePath,
                NormalizedDevicePath = observation.NormalizedDevicePath,
                InstanceId = observation.InstanceId,
                VendorId = observation.VendorId,
                ProductId = observation.ProductId,
                FriendlyName = observation.FriendlyName
            },
            AssignedZoneId = assignedZoneId,
            LastConfirmedAtUtc = updatedAtUtc,
            IsEnabled = true
        };
    }

    private static string CreateDeviceId(DeviceKind deviceKind, string preferredKey)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(preferredKey));
        var hash = Convert.ToHexString(hashBytes[..6]).ToLowerInvariant();
        return $"{deviceKind.ToString().ToLowerInvariant()}-{hash}";
    }

    private static DeviceAssignmentState ResolveAssignmentState(string? assignedZoneId, ZoneDefinition? assignedZone)
    {
        if (string.IsNullOrWhiteSpace(assignedZoneId))
        {
            return DeviceAssignmentState.Unassigned;
        }

        return assignedZone is null
            ? DeviceAssignmentState.UnknownZone
            : DeviceAssignmentState.Assigned;
    }

    private static string ResolveDisplayName(RuntimeDeviceObservation? observation, PersistedDeviceIdentity? persistedDevice)
    {
        if (!string.IsNullOrWhiteSpace(persistedDevice?.FriendlyName))
        {
            return persistedDevice.FriendlyName;
        }

        if (!string.IsNullOrWhiteSpace(observation?.FriendlyName))
        {
            return observation.FriendlyName;
        }

        if (!string.IsNullOrWhiteSpace(persistedDevice?.IdentityEvidence.FriendlyName))
        {
            return persistedDevice.IdentityEvidence.FriendlyName;
        }

        if (!string.IsNullOrWhiteSpace(persistedDevice?.DeviceId))
        {
            return persistedDevice.DeviceId;
        }

        return observation?.SessionDeviceId ?? "Unnamed device";
    }

    private static string NormalizeFriendlyName(string friendlyName, DeviceManagementEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(friendlyName))
        {
            return friendlyName.Trim();
        }

        return entry.DisplayName;
    }

    private static string BuildStableIdentitySummary(
        RuntimeDeviceObservation? observation,
        PersistedDeviceIdentity? persistedDevice)
    {
        if (!string.IsNullOrWhiteSpace(persistedDevice?.PreferredPersistenceKey))
        {
            return persistedDevice.PreferredPersistenceKey;
        }

        if (observation is null)
        {
            return "No stable identity evidence recorded.";
        }

        return DevicePersistenceKeyResolver.GetPreferredDurableKey(observation)
            ?? "No durable key yet. Runtime handle only.";
    }

    private static string BuildMetadataSummary(
        RuntimeDeviceObservation? observation,
        PersistedDeviceIdentity? persistedDevice)
    {
        var parts = new List<string>();
        var evidence = persistedDevice?.IdentityEvidence;

        AddIfPresent(parts, evidence?.FriendlyName ?? observation?.FriendlyName);
        AddIfPresent(parts, FormatVidPid(evidence?.VendorId ?? observation?.VendorId, evidence?.ProductId ?? observation?.ProductId));
        AddIfPresent(parts, evidence?.Manufacturer);
        AddIfPresent(parts, evidence?.EnumeratorName);

        return parts.Count == 0
            ? "No extra hardware metadata recorded."
            : string.Join(" | ", parts.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string? FormatVidPid(string? vendorId, string? productId)
    {
        if (string.IsNullOrWhiteSpace(vendorId) && string.IsNullOrWhiteSpace(productId))
        {
            return null;
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(vendorId))
        {
            parts.Add($"VID_{vendorId}");
        }

        if (!string.IsNullOrWhiteSpace(productId))
        {
            parts.Add($"PID_{productId}");
        }

        return string.Join(" / ", parts);
    }

    private static void AddIfPresent(ICollection<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add(value);
        }
    }
}
