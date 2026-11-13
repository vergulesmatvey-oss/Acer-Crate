using AcerCrate.App.ViewModels;
using AcerCrate.Core.Settings;

namespace AcerCrate.App.Services;

public sealed class PersonalModeService(SettingsService settings, DeviceSession session, FanControlViewModel fans, LightingViewModel lighting) : IDisposable
{
    private bool applying;
    private readonly SemaphoreSlim commands = new(1, 1);
    public bool IsApplying => applying;
    public bool IsSelected => settings.Current.Ui.PersonalModeSelected && settings.Current.PersonalMode.Matches(session.Settings.Profile);
    public void Dispose() => commands.Dispose();
    public static string ModeName(AcerCrate.Core.Control.LaptopMode? mode) => mode switch
    {
        AcerCrate.Core.Control.LaptopMode.Quiet => T("Тихий"), AcerCrate.Core.Control.LaptopMode.Balanced => T("Баланс"),
        AcerCrate.Core.Control.LaptopMode.Performance => T("Производительность"), _ => T("Режим устройства")
    };

    public async Task ApplyAsync()
    {
        await commands.WaitAsync();
        var preset = settings.Current.PersonalMode;
        if (preset.Profile is not {} profile) { commands.Release(); throw new InvalidOperationException(T("Сначала сохраните свой режим во вкладке «Пресеты».")); }
        if (session.State != SessionState.Ready) { commands.Release(); throw new IOException(T("Нет связи с управляющим компонентом.")); }
        applying = true;
        var previous = session.Settings;
        try
        {
            fans.Attach(); lighting.Attach();
            profile = PersonalMode.Prepare(profile, profile.LaptopMode ?? AcerCrate.Core.Control.LaptopMode.Balanced, session.Capabilities);
            await PersonalModeApplication.ApplyAsync(preset with { Profile = profile }, previous,
                value => session.SetProfileAsync(value, requireConnection: true),
                value => session.SetPowerAsync(value, requireConnection: true),
                value => session.SetLightingAsync(value, requireConnection: true),
                () => settings.SaveNow(s => s with { Ui = s.Ui with { PersonalModeSelected = true } }));
            fans.Attach(); lighting.Attach();
        }
        finally { try { fans.Attach(); lighting.Attach(); } finally { applying = false; commands.Release(); } }
    }

    public async Task SelectNativeAsync(AcerCrate.Core.Control.LaptopMode mode)
    {
        await commands.WaitAsync();
        applying = true;
        try
        {
            fans.Attach();
            await session.SetProfileAsync(AcerCrate.Core.Control.LaptopModePolicy.Select(session.Settings.Profile, mode, session.Capabilities), requireConnection: true);
            settings.SaveNow(s => s with { Ui = s.Ui with { PersonalModeSelected = false } });
            fans.Attach();
        }
        finally { applying = false; commands.Release(); }
    }
}
