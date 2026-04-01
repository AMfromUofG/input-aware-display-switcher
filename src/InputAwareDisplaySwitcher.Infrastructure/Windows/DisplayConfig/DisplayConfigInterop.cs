using System.Runtime.InteropServices;

namespace InputAwareDisplaySwitcher.Infrastructure.Windows.DisplayConfig;

internal static class DisplayConfigInterop
{
    internal const int Success = 0;

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int SetDisplayConfig(
        uint numPathArrayElements,
        IntPtr pathArray,
        uint numModeInfoArrayElements,
        IntPtr modeInfoArray,
        uint flags);
}

[Flags]
internal enum SetDisplayConfigFlags : uint
{
    TopologyInternal = 0x00000001,
    TopologyClone = 0x00000002,
    TopologyExtend = 0x00000004,
    TopologyExternal = 0x00000008,
    Validate = 0x00000040,
    Apply = 0x00000080,
    SaveToDatabase = 0x00000200
}
