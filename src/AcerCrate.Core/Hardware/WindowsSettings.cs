using System.Text;
using AcerCrate.Core.Control;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Power;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace AcerCrate.Core.Hardware;

/// <summary>Windows Sticky Keys, as NitroSense's "Sticky key" switch manages it (feature + Shift×5 shortcut).</summary>
public static class StickyKeys
{
    private const STICKYKEYS_FLAGS Managed = STICKYKEYS_FLAGS.SKF_STICKYKEYSON | STICKYKEYS_FLAGS.SKF_HOTKEYACTIVE;

    public static unsafe bool? IsEnabled()
    {
        var keys = new STICKYKEYS { cbSize = (uint)sizeof(STICKYKEYS) };
        if (!PInvoke.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETSTICKYKEYS, keys.cbSize, &keys, 0))
            return null;
        return (keys.dwFlags & Managed) == Managed;
    }

    public static unsafe bool SetEnabled(bool enabled)
    {
        var keys = new STICKYKEYS { cbSize = (uint)sizeof(STICKYKEYS) };
        if (!PInvoke.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETSTICKYKEYS, keys.cbSize, &keys, 0))
            return false;
        keys.dwFlags = enabled ? keys.dwFlags | Managed : keys.dwFlags & ~Managed;
        return PInvoke.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETSTICKYKEYS, keys.cbSize, &keys,
            SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS.SPIF_UPDATEINIFILE | SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS.SPIF_SENDCHANGE);
    }
}

public sealed record PowerPlan(Guid Id, string Name);

/// <summary>Classic Windows power plans (what NitroSense switches on models without operating modes).</summary>
public static class PowerPlans
{
    internal static readonly Guid Balanced = new("381b4222-f694-41f0-9685-ff5bb260df2e");
    private static readonly Guid Processor = new("54533251-82be-4824-96c1-47b60b740d00");
    private static readonly Guid MinimumProcessorState = new("893dee8e-2bef-41e0-89c6-b55d0929964c");
    private static readonly Guid MaximumProcessorState = new("bc5038f7-23e0-4960-96da-33abaf5935ec");
    private static readonly (Guid Min, Guid Max)[] AdditionalProcessorClasses =
    [
        (new("893dee8e-2bef-41e0-89c6-b55d0929964d"), new("bc5038f7-23e0-4960-96da-33abaf5935ed")),
        (new("893dee8e-2bef-41e0-89c6-b55d0929964e"), new("bc5038f7-23e0-4960-96da-33abaf5935ee")),
    ];
    private static readonly Guid ProcessorBoostMode = new("be337238-0d82-4146-a960-4f3749d470c7");

    public static unsafe IReadOnlyList<PowerPlan> List()
    {
        var plans = new List<PowerPlan>();
        for (uint index = 0; ; index++)
        {
            Guid id;
            var size = (uint)sizeof(Guid);
            if (PInvoke.PowerEnumerate(default, null, null, POWER_DATA_ACCESSOR.ACCESS_SCHEME, index, (byte*)&id, &size) != WIN32_ERROR.ERROR_SUCCESS)
                break;
            plans.Add(new PowerPlan(id, ReadName(id)));
        }
        return plans;
    }

    public static unsafe Guid? Active()
    {
        Guid* active;
        if (PInvoke.PowerGetActiveScheme(default, &active) != WIN32_ERROR.ERROR_SUCCESS)
            return null;
        try
        {
            return *active;
        }
        finally
        {
            PInvoke.LocalFree((HLOCAL)active);
        }
    }

    public static unsafe bool SetActivePreservingDisplay(Guid id)
    {
        if (Active() is not {} source) return false;
        if (source == id) return true;
        var subgroup = new Guid("7516b95f-f776-4464-8c53-06167f40cc99");
        Guid[] settings = [new("aded5e82-b909-4619-9949-f5d71dac0bcb"), new("f1fbfde2-a960-4165-9f88-50667911ce96"), new("fbd9aa66-9553-4097-ba44-ed6e9d65eab8"), new("3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e")];
        foreach (var settingValue in settings)
        {
            var setting=settingValue; uint ac,dc;
            if (PInvoke.PowerReadACValueIndex(default,&source,&subgroup,&setting,&ac)!=WIN32_ERROR.ERROR_SUCCESS || PInvoke.PowerReadDCValueIndex(default,&source,&subgroup,&setting,&dc)!=0) return false;
            if (PInvoke.PowerWriteACValueIndex(default,&id,&subgroup,&setting,ac)!=WIN32_ERROR.ERROR_SUCCESS || PInvoke.PowerWriteDCValueIndex(default,&id,&subgroup,&setting,dc)!=0) return false;
        }
        return SetActive(id);
    }

    public static unsafe bool SetActive(Guid id) =>
        PInvoke.PowerSetActiveScheme(default, &id) == WIN32_ERROR.ERROR_SUCCESS;

    internal static unsafe bool ConfigureProcessorLimit(Guid source, int percent)
    {
        var id = ProcessorPowerLimit.SchemeId;
        if (Active() != id)
        {
            if (!DeleteProcessorLimit()) return false;
            var destination = &id;
            if (PInvoke.PowerDuplicateScheme(default, &source, &destination) != WIN32_ERROR.ERROR_SUCCESS)
                return false;
            var name = Encoding.Unicode.GetBytes("Acer Crate - CPU limit\0");
            fixed (byte* raw = name)
            {
                if (PInvoke.PowerWriteFriendlyName(default, &id, null, null, raw, (uint)name.Length) != WIN32_ERROR.ERROR_SUCCESS)
                {
                    DeleteProcessorLimit();
                    return false;
                }
            }
        }
        if (!WriteProcessorSetting(id, MinimumProcessorState, ProcessorPowerLimit.MinimumPercent) ||
            !WriteProcessorSetting(id, MaximumProcessorState, (uint)percent) ||
            !WriteProcessorSetting(id, ProcessorBoostMode, 0))
            return false;

        // Hybrid CPUs have separate settings for their additional efficiency classes.
        foreach (var (min, max) in AdditionalProcessorClasses)
        {
            if (HasProcessorSetting(id, max) &&
                (!WriteProcessorSetting(id, min, ProcessorPowerLimit.MinimumPercent) ||
                 !WriteProcessorSetting(id, max, (uint)percent)))
                return false;
        }
        return true;
    }

    private static unsafe bool HasProcessorSetting(Guid id, Guid setting)
    {
        var subgroup = Processor;
        uint value;
        return PInvoke.PowerReadACValueIndex(default, &id, &subgroup, &setting, &value) == WIN32_ERROR.ERROR_SUCCESS;
    }

    private static unsafe bool WriteProcessorSetting(Guid id, Guid setting, uint value)
    {
        var subgroup = Processor;
        uint ac, dc;
        return PInvoke.PowerWriteACValueIndex(default, &id, &subgroup, &setting, value) == WIN32_ERROR.ERROR_SUCCESS &&
            PInvoke.PowerWriteDCValueIndex(default, &id, &subgroup, &setting, value) == 0 &&
            PInvoke.PowerReadACValueIndex(default, &id, &subgroup, &setting, &ac) == WIN32_ERROR.ERROR_SUCCESS && ac == value &&
            PInvoke.PowerReadDCValueIndex(default, &id, &subgroup, &setting, &dc) == 0 && dc == value;
    }

    internal static unsafe bool DeleteProcessorLimit()
    {
        var id = ProcessorPowerLimit.SchemeId;
        if (Active() is not { } active || active == id) return false;
        var result = (WIN32_ERROR)PInvoke.PowerDeleteScheme(default, &id);
        return result is WIN32_ERROR.ERROR_SUCCESS or WIN32_ERROR.ERROR_FILE_NOT_FOUND or WIN32_ERROR.ERROR_NOT_FOUND;
    }

    private static unsafe string ReadName(Guid id)
    {
        uint size = 0;
        if (PInvoke.PowerReadFriendlyName(default, &id, null, null, null, &size) != WIN32_ERROR.ERROR_SUCCESS || size == 0)
            return id.ToString();
        var buffer = new byte[size];
        fixed (byte* raw = buffer)
        {
            if (PInvoke.PowerReadFriendlyName(default, &id, null, null, raw, &size) != WIN32_ERROR.ERROR_SUCCESS)
                return id.ToString();
        }
        return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    }
}

/// <summary>Restarts Windows (after a GPU mode switch).</summary>
public static class WindowsRestart
{
    public static unsafe bool RestartNow()
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        if (!PInvoke.OpenProcessToken(process.SafeHandle,
                Windows.Win32.Security.TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | Windows.Win32.Security.TOKEN_ACCESS_MASK.TOKEN_QUERY,
                out var token))
            return false;

        using (token)
        {
            if (!PInvoke.LookupPrivilegeValue(null, PInvoke.SE_SHUTDOWN_NAME, out var luid))
                return false;
            var privileges = new Windows.Win32.Security.TOKEN_PRIVILEGES { PrivilegeCount = 1 };
            privileges.Privileges[0] = new Windows.Win32.Security.LUID_AND_ATTRIBUTES
            {
                Luid = luid,
                Attributes = Windows.Win32.Security.TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED,
            };
            if (!PInvoke.AdjustTokenPrivileges(token, false, &privileges, []))
                return false;
        }

        return PInvoke.InitiateShutdown(null, null, 0,
            Windows.Win32.System.Shutdown.SHUTDOWN_FLAGS.SHUTDOWN_RESTART,
            Windows.Win32.System.Shutdown.SHUTDOWN_REASON.SHTDN_REASON_MAJOR_APPLICATION
            | Windows.Win32.System.Shutdown.SHUTDOWN_REASON.SHTDN_REASON_MINOR_RECONFIG
            | Windows.Win32.System.Shutdown.SHUTDOWN_REASON.SHTDN_REASON_FLAG_PLANNED) == 0;
    }
}
