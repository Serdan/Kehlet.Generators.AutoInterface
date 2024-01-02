namespace Kehlet.Generators.AutoInterface.Sample;

public class FromInstanceMembersSource
{
    public int Number1
    {
        get => 1;
    }

    public int Number2
    {
        set => DoSomething();
    }

    public int Number3
    {
        get => 1;
        set => DoSomething();
    }

    public void DoSomething()
    {
    }

    public int GetSomething(string str) => str.Length;

    public Task<int> GetNumberAsync() => Task.FromResult(42);
    public Task DoSomethingAsync() => Task.FromResult(42);

    [Obsolete]
    public int ObsTest() => 1;

    [Obsolete]
    public int ObsTest2 => 1;
}

[FromInstanceMembers(typeof(FromInstanceMembersSource), true, typeof(Unit))]
public partial interface IFromInstanceMembers;

public readonly struct FromInstanceMembersStruct(FromInstanceMembersSource source) : IFromInstanceMembers
{
    public FromInstanceMembersSource Instance => source;
}
