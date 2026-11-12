using OpenSense.Core.Control;
using OpenSense.Core.Lighting;

namespace OpenSense.Core.Settings;

public static class PersonalModeApplication
{
    public static async Task ApplyAsync(PersonalMode mode, MachineSettings previous,
        Func<ControlProfile, Task> setProfile, Func<PowerSettings, Task> setPower,
        Func<LightingConfig, Task> setLighting, Action commit)
    {
        var profile = mode.Profile ?? throw new InvalidOperationException("Свой режим ещё не сохранён.");
        var controlAttempted = false; var powerAttempted = false; var lightingAttempted = false;
        try
        {
            controlAttempted = true; await setProfile(profile);
            if (mode.Power is {} power) { powerAttempted = true; await setPower(power); }
            if (mode.Lighting is {} lighting) { lightingAttempted = true; await setLighting(lighting); }
            commit();
        }
        catch (Exception original)
        {
            var errors = new List<Exception>();
            async Task<bool> Restore(Func<Task> action)
            {
                try { await action(); return true; }
                catch (Exception error) { errors.Add(error); return false; }
            }
            if (lightingAttempted) await Restore(() => setLighting(previous.Lighting));
            if (powerAttempted) await Restore(() => setPower(previous.Power));
            var restored = !controlAttempted || await Restore(() => setProfile(previous.Profile));
            if (!restored)
                await Restore(() => setProfile(LaptopModePolicy.Normalize(previous.Profile with { Mode = FanControlMode.Auto, AutoBoost = false })));
            if (errors.Count > 0) throw new AggregateException("Применение прервано. Возврат всех параметров не подтверждён; проверьте состояние устройства.", new[] { original }.Concat(errors));
            throw;
        }
    }
}
