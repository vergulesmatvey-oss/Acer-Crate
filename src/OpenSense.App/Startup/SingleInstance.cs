using System.Security.AccessControl;
using System.Security.Principal;
using System.Diagnostics;

namespace OpenSense.App.Startup;

/// <summary>
/// One OpenSense per session. The running instance (elevated when it is a portable copy) owns a named
/// event; a second launch, elevated or not, signals it to show its window and exits. The event's DACL
/// grants authenticated users only Modify/Synchronize so a non-elevated launch can signal without a UAC prompt.
/// (Windows App SDK's AppInstance cannot redirect across integrity levels, which OpenSense needs.)
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private readonly EventWaitHandle _activate;
    private readonly EventWaitHandle _exit;
    private readonly RegisteredWaitHandle _activateRegistration;
    private readonly RegisteredWaitHandle _exitRegistration;

    private SingleInstance(EventWaitHandle activate, EventWaitHandle exit)
    {
        _activate = activate;
        _exit = exit;
        _activateRegistration = ThreadPool.RegisterWaitForSingleObject(activate, (_, _) => Activated?.Invoke(), null, Timeout.Infinite, executeOnlyOnce: false);
        _exitRegistration = ThreadPool.RegisterWaitForSingleObject(exit, (_, _) => ExitRequested?.Invoke(), null, Timeout.Infinite, executeOnlyOnce: true);
    }

    /// <summary>Raised on a thread-pool thread when another launch asks this instance to show itself.</summary>
    public event Action? Activated;

    /// <summary>Raised on a thread-pool thread when the installer asks this instance to quit.</summary>
    public event Action? ExitRequested;

    public static bool Preview { get; set; }
    private static string EventName(string purpose) => (Preview ? @"Local\OpenSense.Preview." : @"Local\OpenSense.") + purpose;

    /// <summary>Signals an already running instance. Returns false if there is none.</summary>
    public static bool SignalExisting() => Signal(EventName("Activate"), activate: true);

    /// <summary>Asks the running instance to quit and waits (up to <paramref name="timeout"/>) until it has.</summary>
    public static bool RequestExit(TimeSpan timeout)
    {
        if (!Signal(EventName("Exit")))
            return true; // nothing running
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (!EventWaitHandleAcl.TryOpenExisting(EventName("Activate"), EventWaitHandleRights.Synchronize, out var alive))
                return true;
            alive.Dispose();
            Thread.Sleep(200);
        }
        return false;
    }

    private static bool Signal(string name, bool activate = false)
    {
        if (!EventWaitHandleAcl.TryOpenExisting(name, EventWaitHandleRights.Modify, out var existing))
            return false;
        using (existing)
        {
            if (activate) GrantForeground();
            existing.Set();
        }
        return true;
    }

    private static void GrantForeground()
    {
        using var current = Process.GetCurrentProcess();
        foreach (var process in Process.GetProcessesByName(current.ProcessName))
        using (process)
        {
            try
            {
                if (process.Id != current.Id && process.SessionId == current.SessionId)
                    Windows.Win32.PInvoke.AllowSetForegroundWindow((uint)process.Id);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                // An instance may have exited while the activation event was being opened.
            }
        }
    }

    /// <summary>Becomes the running instance, or returns null if another one won the race (and was signalled).</summary>
    public static SingleInstance? Claim()
    {
        var security = Security();
        var handle = EventWaitHandleAcl.Create(false, EventResetMode.AutoReset, EventName("Activate"), out var createdNew, security);
        if (!createdNew)
        {
            GrantForeground();
            using (handle)
                handle.Set();
            return null;
        }
        var exit = EventWaitHandleAcl.Create(false, EventResetMode.AutoReset, EventName("Exit"), out _, security);
        return new SingleInstance(handle, exit);
    }

    private static EventWaitHandleSecurity Security()
    {
        var security = new EventWaitHandleSecurity();
        security.AddAccessRule(new EventWaitHandleAccessRule(
            new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
            EventWaitHandleRights.Modify | EventWaitHandleRights.Synchronize, AccessControlType.Allow));
        using (var identity = WindowsIdentity.GetCurrent())
        {
            security.AddAccessRule(new EventWaitHandleAccessRule(identity.User!, EventWaitHandleRights.FullControl, AccessControlType.Allow));
        }
        return security;
    }

    public void Dispose()
    {
        _activateRegistration.Unregister(null);
        _exitRegistration.Unregister(null);
        _activate.Dispose();
        _exit.Dispose();
    }
}
