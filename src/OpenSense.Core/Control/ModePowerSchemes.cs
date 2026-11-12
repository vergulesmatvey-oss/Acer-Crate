using OpenSense.Core.Hardware;
namespace OpenSense.Core.Control;
public interface IModePowerSchemes
{
    Guid? Active();
    bool SetActive(Guid id);
}
public sealed class WindowsModePowerSchemes : IModePowerSchemes
{
    public Guid? Active() => PowerPlans.Active();
    public bool SetActive(Guid id) => PowerPlans.SetActivePreservingDisplay(id);
}
public sealed class ModePowerSchemes(IModePowerSchemes backend)
{
    private LaptopMode? last;
    private readonly object gate=new();
    public static Guid Id(LaptopMode mode) => mode switch
    {
        LaptopMode.Quiet => new("6eae192d-02a0-4d44-91ee-cc0000000001"),
        LaptopMode.Balanced => new("6eae192d-02a0-4d44-91ee-cc0000000002"),
        LaptopMode.Performance => new("6eae192d-02a0-4d44-91ee-cc0000000003"),
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };
    public void Reset() { lock(gate) last=null; }
    public (Guid? Active, bool Confirmed, string? Error) Apply(LaptopMode mode)
    { lock(gate) return ApplyCore(mode); }
    private (Guid? Active, bool Confirmed, string? Error) ApplyCore(LaptopMode mode)
    {
        try
        {
            var wanted=Id(mode);
            if(last!=mode)
            {
                if(!backend.SetActive(wanted)) return(backend.Active(),false,"Windows отклонила переключение схемы питания. Проверьте наличие схем Acer Crate.");
                if(backend.Active()!=wanted) return(backend.Active(),false,"Активная схема Windows отличается от запрошенной.");
                last=mode;
            }
            var active=backend.Active();
            return(active,active==wanted,active==wanted?null:"Схема питания Windows изменена отдельно. Повторно выберите профиль Acer Crate для синхронизации.");
        }
        catch(Exception ex) when(ex is not OutOfMemoryException) { return(null,false,ex.Message); }
    }
}
