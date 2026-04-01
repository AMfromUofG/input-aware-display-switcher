namespace InputAwareDisplaySwitcher.Core.Domain.Devices;

public sealed record DeviceIdentityEvidence
{
    public string? RawDevicePath { get; init; }

    public string? NormalizedDevicePath { get; init; }

    public string? InstanceId { get; init; }

    public string? VendorId { get; init; }

    public string? ProductId { get; init; }

    public string? FriendlyName { get; init; }

    public string? Manufacturer { get; init; }

    public string? EnumeratorName { get; init; }
}
