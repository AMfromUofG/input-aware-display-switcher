using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace RawInputPrototype.RawInput;

internal static class RawInputParser
{
    public static bool TryReadEvent(nint lParam, nint wParam, out ParsedRawInputEvent? parsedEvent, out string? error)
    {
        parsedEvent = null;
        error = null;

        try
        {
            var headerSize = (uint)Marshal.SizeOf<RawInputInterop.RAWINPUTHEADER>();
            uint dataSize = 0;

            var queryResult = RawInputInterop.GetRawInputData(lParam, RawInputInterop.RID_INPUT, nint.Zero, ref dataSize, headerSize);
            if (queryResult == uint.MaxValue)
            {
                error = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                return false;
            }

            if (dataSize == 0)
            {
                error = "GetRawInputData reported an empty payload.";
                return false;
            }

            var buffer = Marshal.AllocHGlobal((int)dataSize);

            try
            {
                var readResult = RawInputInterop.GetRawInputData(lParam, RawInputInterop.RID_INPUT, buffer, ref dataSize, headerSize);
                if (readResult == uint.MaxValue)
                {
                    error = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                    return false;
                }

                var rawInput = Marshal.PtrToStructure<RawInputInterop.RAWINPUT>(buffer);

                parsedEvent = rawInput.header.dwType switch
                {
                    RawInputInterop.RIM_TYPEKEYBOARD => ParseKeyboardEvent(rawInput, wParam),
                    RawInputInterop.RIM_TYPEMOUSE => ParseMouseEvent(rawInput, wParam),
                    RawInputInterop.RIM_TYPEHID => new ParsedRawInputEvent
                    {
                        Timestamp = DateTime.Now,
                        DeviceHandle = rawInput.header.hDevice,
                        DeviceType = RawInputDeviceType.Hid,
                        InputSource = DescribeInputSource(wParam),
                        Summary = "HID input payload received. This prototype only renders keyboard and mouse details."
                    },
                    _ => new ParsedRawInputEvent
                    {
                        Timestamp = DateTime.Now,
                        DeviceHandle = rawInput.header.hDevice,
                        DeviceType = RawInputDeviceType.Unknown,
                        InputSource = DescribeInputSource(wParam),
                        Summary = $"Unsupported raw input type {rawInput.header.dwType}."
                    }
                };

                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private static ParsedRawInputEvent ParseKeyboardEvent(RawInputInterop.RAWINPUT rawInput, nint wParam)
    {
        var keyboard = rawInput.data.keyboard;
        var keyState = (keyboard.Flags & RawInputInterop.RI_KEY_BREAK) != 0 ? "Up" : "Down";
        var keyName = DescribeVirtualKey(keyboard.VKey);
        var flags = DescribeKeyboardFlags(keyboard.Flags);

        return new ParsedRawInputEvent
        {
            Timestamp = DateTime.Now,
            DeviceHandle = rawInput.header.hDevice,
            DeviceType = RawInputDeviceType.Keyboard,
            InputSource = DescribeInputSource(wParam),
            Summary = $"{keyState} {keyName} (VK 0x{keyboard.VKey:X4}, Make 0x{keyboard.MakeCode:X2}, Flags {flags}, Message 0x{keyboard.Message:X4})"
        };
    }

    private static ParsedRawInputEvent ParseMouseEvent(RawInputInterop.RAWINPUT rawInput, nint wParam)
    {
        var mouse = rawInput.data.mouse;
        var fragments = new List<string>();

        if (mouse.lLastX != 0 || mouse.lLastY != 0)
        {
            var moveMode = (mouse.usFlags & RawInputInterop.MOUSE_MOVE_ABSOLUTE) != 0 ? "absolute" : "relative";
            fragments.Add($"{moveMode} move dx={mouse.lLastX}, dy={mouse.lLastY}");
        }

        AppendMouseButtonFragments(mouse.usButtonFlags, mouse.usButtonData, fragments);

        if ((mouse.usFlags & RawInputInterop.MOUSE_ATTRIBUTES_CHANGED) != 0)
        {
            fragments.Add("attributes changed");
        }

        if (fragments.Count == 0)
        {
            fragments.Add("No movement or button delta reported.");
        }

        return new ParsedRawInputEvent
        {
            Timestamp = DateTime.Now,
            DeviceHandle = rawInput.header.hDevice,
            DeviceType = RawInputDeviceType.Mouse,
            InputSource = DescribeInputSource(wParam),
            Summary = string.Join(", ", fragments)
        };
    }

    private static void AppendMouseButtonFragments(ushort buttonFlags, ushort buttonData, List<string> fragments)
    {
        if ((buttonFlags & RawInputInterop.RI_MOUSE_LEFT_BUTTON_DOWN) != 0)
        {
            fragments.Add("left down");
        }

        if ((buttonFlags & RawInputInterop.RI_MOUSE_LEFT_BUTTON_UP) != 0)
        {
            fragments.Add("left up");
        }

        if ((buttonFlags & RawInputInterop.RI_MOUSE_RIGHT_BUTTON_DOWN) != 0)
        {
            fragments.Add("right down");
        }

        if ((buttonFlags & RawInputInterop.RI_MOUSE_RIGHT_BUTTON_UP) != 0)
        {
            fragments.Add("right up");
        }

        if ((buttonFlags & RawInputInterop.RI_MOUSE_MIDDLE_BUTTON_DOWN) != 0)
        {
            fragments.Add("middle down");
        }

        if ((buttonFlags & RawInputInterop.RI_MOUSE_MIDDLE_BUTTON_UP) != 0)
        {
            fragments.Add("middle up");
        }

        if ((buttonFlags & RawInputInterop.RI_MOUSE_BUTTON_4_DOWN) != 0)
        {
            fragments.Add("button 4 down");
        }

        if ((buttonFlags & RawInputInterop.RI_MOUSE_BUTTON_4_UP) != 0)
        {
            fragments.Add("button 4 up");
        }

        if ((buttonFlags & RawInputInterop.RI_MOUSE_BUTTON_5_DOWN) != 0)
        {
            fragments.Add("button 5 down");
        }

        if ((buttonFlags & RawInputInterop.RI_MOUSE_BUTTON_5_UP) != 0)
        {
            fragments.Add("button 5 up");
        }

        if ((buttonFlags & RawInputInterop.RI_MOUSE_WHEEL) != 0)
        {
            fragments.Add($"vertical wheel {unchecked((short)buttonData)}");
        }

        if ((buttonFlags & RawInputInterop.RI_MOUSE_HWHEEL) != 0)
        {
            fragments.Add($"horizontal wheel {unchecked((short)buttonData)}");
        }
    }

    private static string DescribeInputSource(nint wParam)
    {
        return wParam.ToInt64() switch
        {
            RawInputInterop.RIM_INPUT => "Foreground",
            RawInputInterop.RIM_INPUTSINK => "Background",
            _ => $"Code {wParam.ToInt64()}"
        };
    }

    private static string DescribeVirtualKey(ushort virtualKey)
    {
        if (virtualKey is >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A)
        {
            return ((char)virtualKey).ToString();
        }

        try
        {
            var key = KeyInterop.KeyFromVirtualKey(virtualKey);
            if (key != Key.None)
            {
                return key.ToString();
            }
        }
        catch
        {
            // Some virtual key codes do not map cleanly into WPF's Key enum.
        }

        return $"VK 0x{virtualKey:X4}";
    }

    private static string DescribeKeyboardFlags(ushort flags)
    {
        var fragments = new List<string>();

        if ((flags & RawInputInterop.RI_KEY_BREAK) != 0)
        {
            fragments.Add("Break");
        }
        else
        {
            fragments.Add("Make");
        }

        if ((flags & RawInputInterop.RI_KEY_E0) != 0)
        {
            fragments.Add("E0");
        }

        if ((flags & RawInputInterop.RI_KEY_E1) != 0)
        {
            fragments.Add("E1");
        }

        return string.Join('|', fragments);
    }
}
