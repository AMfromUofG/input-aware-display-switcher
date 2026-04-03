using InputAwareDisplaySwitcher.Core.Domain.Devices;

namespace InputAwareDisplaySwitcher.Core.Application;

internal static class DevicePersistenceKeyResolver
{
    public static IEnumerable<string> GetPersistenceKeys(PersistedDeviceIdentity device)
    {
        ArgumentNullException.ThrowIfNull(device);

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

    public static string? GetPreferredDurableKey(RuntimeDeviceObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        return RuntimeDeviceObservation.BuildInstanceKey(observation.InstanceId)
            ?? RuntimeDeviceObservation.BuildPathKey(observation.NormalizedDevicePath)
            ?? RuntimeDeviceObservation.BuildRawPathKey(observation.RawDevicePath)
            ?? RuntimeDeviceObservation.BuildVidPidKey(observation.DeviceKind, observation.VendorId, observation.ProductId);
    }
}
