using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace OpenSense.App.Helpers;

/// <summary>Only page refresh timers belong here; hardware and command timers remain independent.</summary>
internal static class UiActivity
{
    public static void Track(FrameworkElement owner, DispatcherQueueTimer timer, Action refresh)
    {
        void Sync()
        {
            if (owner.IsLoaded && App.Current.IsWindowVisible)
            {
                refresh();
                timer.Start();
            }
            else timer.Stop();
        }
        owner.Loaded += (_, _) =>
        {
            App.Current.WindowVisibilityChanged += Sync;
            Sync();
        };
        owner.Unloaded += (_, _) =>
        {
            App.Current.WindowVisibilityChanged -= Sync;
            timer.Stop();
        };
    }
}
