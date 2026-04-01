using InputAwareDisplaySwitcher.Core.Domain.Devices;
using InputAwareDisplaySwitcher.Core.Domain.Profiles;
using InputAwareDisplaySwitcher.Core.Domain.Zones;

namespace InputAwareDisplaySwitcher.Core.Application;

public sealed class DeviceRegistryService
{
    private readonly IDeviceRegistryStore _store;

    public DeviceRegistryService(IDeviceRegistryStore store)
    {
        _store = store;
    }

    public Task<DeviceRegistrySnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        return _store.LoadAsync(cancellationToken);
    }

    public Task SaveAsync(DeviceRegistrySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        return _store.SaveAsync(snapshot, cancellationToken);
    }

    public async Task RegisterOrUpdateDeviceAsync(PersistedDeviceIdentity device, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        var snapshot = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        Upsert(snapshot.Devices, device, existing => existing.DeviceId, incoming => incoming.DeviceId);
        await _store.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }

    public async Task RegisterOrUpdateZoneAsync(ZoneDefinition zone, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(zone);

        var snapshot = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        Upsert(snapshot.Zones, zone, existing => existing.ZoneId, incoming => incoming.ZoneId);
        await _store.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }

    public async Task RegisterOrUpdateDisplayProfileAsync(DisplayProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var snapshot = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        Upsert(snapshot.DisplayProfiles, profile, existing => existing.DisplayProfileId, incoming => incoming.DisplayProfileId);
        await _store.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
    }

    public DeviceRegistryResolution Resolve(RuntimeDeviceObservation observation, DeviceRegistrySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(snapshot);

        var candidateKeys = observation.GetCandidatePersistenceKeys();
        var matchedDevice = snapshot.Devices.FirstOrDefault(device => GetPersistenceKeys(device)
            .Intersect(candidateKeys, StringComparer.OrdinalIgnoreCase)
            .Any());

        if (matchedDevice is null)
        {
            return new DeviceRegistryResolution
            {
                Status = DeviceRegistryResolutionStatus.UnknownDevice,
                Observation = observation,
                Message = $"No persisted device mapping matched runtime device '{observation.FriendlyName ?? observation.SessionDeviceId}'."
            };
        }

        var matchedKey = GetPersistenceKeys(matchedDevice)
            .Intersect(candidateKeys, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (!matchedDevice.IsEnabled)
        {
            return new DeviceRegistryResolution
            {
                Status = DeviceRegistryResolutionStatus.DeviceDisabled,
                Observation = observation,
                MatchedDevice = matchedDevice,
                MatchedByPersistenceKey = matchedKey,
                Message = $"Persisted device '{matchedDevice.FriendlyName}' is disabled."
            };
        }

        if (string.IsNullOrWhiteSpace(matchedDevice.AssignedZoneId))
        {
            return new DeviceRegistryResolution
            {
                Status = DeviceRegistryResolutionStatus.UnmappedZone,
                Observation = observation,
                MatchedDevice = matchedDevice,
                MatchedByPersistenceKey = matchedKey,
                Message = $"Persisted device '{matchedDevice.FriendlyName}' is not assigned to a zone."
            };
        }

        var zone = snapshot.FindZone(matchedDevice.AssignedZoneId);
        if (zone is null)
        {
            return new DeviceRegistryResolution
            {
                Status = DeviceRegistryResolutionStatus.UnmappedZone,
                Observation = observation,
                MatchedDevice = matchedDevice,
                MatchedByPersistenceKey = matchedKey,
                Message = $"Device '{matchedDevice.FriendlyName}' points to unknown zone '{matchedDevice.AssignedZoneId}'."
            };
        }

        if (!zone.IsEnabled)
        {
            return new DeviceRegistryResolution
            {
                Status = DeviceRegistryResolutionStatus.ZoneDisabled,
                Observation = observation,
                MatchedDevice = matchedDevice,
                Zone = zone,
                MatchedByPersistenceKey = matchedKey,
                Message = $"Zone '{zone.Name}' is disabled."
            };
        }

        var profile = snapshot.FindProfile(zone.PreferredDisplayProfileId);
        if (profile is null || !profile.IsEnabled)
        {
            return new DeviceRegistryResolution
            {
                Status = DeviceRegistryResolutionStatus.ProfileUnavailable,
                Observation = observation,
                MatchedDevice = matchedDevice,
                Zone = zone,
                MatchedByPersistenceKey = matchedKey,
                Message = $"Zone '{zone.Name}' does not resolve to an enabled display profile."
            };
        }

        return new DeviceRegistryResolution
        {
            Status = DeviceRegistryResolutionStatus.Resolved,
            Observation = observation,
            MatchedDevice = matchedDevice,
            Zone = zone,
            TargetProfile = profile,
            MatchedByPersistenceKey = matchedKey,
            Message = $"Device '{matchedDevice.FriendlyName}' resolved to zone '{zone.Name}' and profile '{profile.Name}'."
        };
    }

    private static IEnumerable<string> GetPersistenceKeys(PersistedDeviceIdentity device)
    {
        if (!string.IsNullOrWhiteSpace(device.PreferredPersistenceKey))
        {
            yield return device.PreferredPersistenceKey;
        }

        var evidence = device.IdentityEvidence;

        var instanceKey = RuntimeDeviceObservation.BuildInstanceKey(evidence.InstanceId);
        if (!string.IsNullOrWhiteSpace(instanceKey))
        {
            yield return instanceKey;
        }

        var pathKey = RuntimeDeviceObservation.BuildPathKey(evidence.NormalizedDevicePath);
        if (!string.IsNullOrWhiteSpace(pathKey))
        {
            yield return pathKey;
        }

        var rawPathKey = RuntimeDeviceObservation.BuildRawPathKey(evidence.RawDevicePath);
        if (!string.IsNullOrWhiteSpace(rawPathKey))
        {
            yield return rawPathKey;
        }

        var vidPidKey = RuntimeDeviceObservation.BuildVidPidKey(device.DeviceKind, evidence.VendorId, evidence.ProductId);
        if (!string.IsNullOrWhiteSpace(vidPidKey))
        {
            yield return vidPidKey;
        }
    }

    private static void Upsert<T>(
        IList<T> items,
        T incoming,
        Func<T, string> existingKeySelector,
        Func<T, string> incomingKeySelector)
    {
        var incomingKey = incomingKeySelector(incoming);
        var existingIndex = items
            .Select((item, index) => new { item, index })
            .FirstOrDefault(entry => string.Equals(existingKeySelector(entry.item), incomingKey, StringComparison.OrdinalIgnoreCase))
            ?.index;

        if (existingIndex.HasValue)
        {
            items[existingIndex.Value] = incoming;
            return;
        }

        items.Add(incoming);
    }
}
