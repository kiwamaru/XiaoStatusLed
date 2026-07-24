using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

public static class ComPortFinder
{
    private const uint DIGCF_PRESENT = 0x00000002;
    private const uint DIGCF_DEVICEINTERFACE = 0x00000010;

    private static readonly Guid
        GuidDevClassPorts =
        new("4D36E978-E325-11CE-BFC1-08002BE10318");

    [DllImport("setupapi.dll",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    private static extern IntPtr SetupDiGetClassDevs(
        ref Guid classGuid,
        string? enumerator,
        IntPtr hwndParent,
        uint flags);

    [DllImport("setupapi.dll",
        SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInfo(
        IntPtr deviceInfoSet,
        uint memberIndex,
        ref SP_DEVINFO_DATA deviceInfoData);

    [DllImport("setupapi.dll",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    private static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        byte[]? propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize);

    [DllImport("setupapi.dll",
        SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(
        IntPtr deviceInfoSet);

    private const uint SPDRP_HARDWAREID = 0x00000001;
    private const uint SPDRP_FRIENDLYNAME = 0x0000000C;

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    public static IReadOnlyList<ComPortInfo> FindPorts(
        string vid,
        string pid)
    {
        var result = new List<ComPortInfo>();
        Guid guidDevClassPorts = GuidDevClassPorts;
        IntPtr deviceInfoSet =
            SetupDiGetClassDevs(
                ref guidDevClassPorts,
                null,
                IntPtr.Zero,
                DIGCF_PRESENT);

        if (deviceInfoSet == IntPtr.Zero ||
            deviceInfoSet == new IntPtr(-1))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error());
        }

        try
        {
            for (uint index = 0; ; index++)
            {
                var deviceInfoData =
                    new SP_DEVINFO_DATA
                    {
                        cbSize =
                            (uint)Marshal.SizeOf<
                                SP_DEVINFO_DATA>()
                    };

                if (!SetupDiEnumDeviceInfo(
                        deviceInfoSet,
                        index,
                        ref deviceInfoData))
                {
                    break;
                }

                string hardwareId =
                    GetDeviceProperty(
                        deviceInfoSet,
                        ref deviceInfoData,
                        SPDRP_HARDWAREID);

                if (!IsTargetDevice(
                        hardwareId,
                        vid,
                        pid))
                {
                    continue;
                }

                string friendlyName =
                    GetDeviceProperty(
                        deviceInfoSet,
                        ref deviceInfoData,
                        SPDRP_FRIENDLYNAME);

                string? portName =
                    ExtractComPort(friendlyName);

                if (portName != null)
                {
                    result.Add(
                        new ComPortInfo(
                            portName,
                            hardwareId,
                            friendlyName));
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(
                deviceInfoSet);
        }

        return result;
    }

    private static bool IsTargetDevice(
        string hardwareId,
        string vid,
        string pid)
    {
        return hardwareId.Contains(
                   $"VID_{vid}",
                   StringComparison.OrdinalIgnoreCase)
            &&
            hardwareId.Contains(
                   $"PID_{pid}",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractComPort(
        string friendlyName)
    {
        var match =
            System.Text.RegularExpressions.Regex.Match(
                friendlyName,
                @"\((COM\d+)\)");

        return match.Success
            ? match.Groups[1].Value
            : null;
    }

    private static string GetDeviceProperty(
        IntPtr deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint property)
    {
        uint propertyType;
        uint requiredSize;

        SetupDiGetDeviceRegistryProperty(
            deviceInfoSet,
            ref deviceInfoData,
            property,
            out propertyType,
            null,
            0,
            out requiredSize);

        if (requiredSize == 0)
        {
            return string.Empty;
        }

        byte[] buffer =
            new byte[requiredSize];

        if (!SetupDiGetDeviceRegistryProperty(
                deviceInfoSet,
                ref deviceInfoData,
                property,
                out propertyType,
                buffer,
                (uint)buffer.Length,
                out requiredSize))
        {
            return string.Empty;
        }

        return Encoding.Unicode
            .GetString(buffer)
            .TrimEnd('\0');
    }
}

public sealed record ComPortInfo(
    string PortName,
    string HardwareId,
    string FriendlyName);