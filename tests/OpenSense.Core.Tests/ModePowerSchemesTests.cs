using OpenSense.Core.Control;
namespace OpenSense.Core.Tests;
public sealed class ModePowerSchemesTests
{
    [Theory]
    [InlineData(LaptopMode.Quiet)]
    [InlineData(LaptopMode.Balanced)]
    [InlineData(LaptopMode.Performance)]
    public void Profile_switch_changes_and_verifies_the_assigned_Windows_scheme(LaptopMode mode)
    {
        var backend=new Backend();var c=new ModePowerSchemes(backend);var result=c.Apply(mode);
        Assert.True(result.Confirmed);Assert.Null(result.Error);Assert.Equal(ModePowerSchemes.Id(mode),backend.Current);Assert.Equal(1,backend.Writes);
    }
    [Fact]
    public void Rejected_Windows_change_is_not_reported_as_applied()
    {
        var backend=new Backend { Reject=true };var c=new ModePowerSchemes(backend);var result=c.Apply(LaptopMode.Quiet);
        Assert.False(result.Confirmed);Assert.NotNull(result.Error);
    }
    [Fact]
    public void An_acknowledgement_without_readback_is_not_confirmation()
    {
        var backend=new Backend { Ignore=true };var result=new ModePowerSchemes(backend).Apply(LaptopMode.Performance);
        Assert.False(result.Confirmed);Assert.NotNull(result.Error);
    }
    [Fact]
    public void External_choice_is_reported_without_repeatedly_overwriting_Windows()
    {
        var b=new Backend();var c=new ModePowerSchemes(b);c.Apply(LaptopMode.Quiet);b.Current=Guid.NewGuid();
        Assert.False(c.Apply(LaptopMode.Quiet).Confirmed);Assert.Equal(1,b.Writes);
        c.Reset();Assert.True(c.Apply(LaptopMode.Quiet).Confirmed);Assert.Equal(2,b.Writes);
    }
    [Fact]
    public void A_sequence_of_three_profiles_selects_three_distinct_schemes()
    {
        var b=new Backend();var c=new ModePowerSchemes(b);var ids=new HashSet<Guid?>();
        foreach(var m in Enum.GetValues<LaptopMode>())ids.Add(c.Apply(m).Active);
        Assert.Equal(3,ids.Count);Assert.Equal(3,b.Writes);
    }
    private sealed class Backend : IModePowerSchemes
    {
        public Guid? Current { get; set; } = Guid.NewGuid();public bool Reject { get; init; }public bool Ignore { get; init; }public int Writes { get; private set; }
        public Guid? Active()=>Current;
        public bool SetActive(Guid id){Writes++;if(Reject)return false;if(!Ignore)Current=id;return true;}
    }
}
