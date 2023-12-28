namespace Kehlet.Generators.AutoInterface.Sample;

public class FromInstanceMembersSource
{
    public void DoSomething()
    {
    }

    public int GetSomething(string str) => str.Length;

    public Task<int> GetNumberAsync() => Task.FromResult(42);
    public Task DoSomethingAsync() => Task.FromResult(42);
}

[FromInstanceMembers(typeof(FromInstanceMembersSource), true, typeof(Unit))]
public partial interface IFromInstanceMembers;

public readonly struct FromInstanceMembersStruct(FromInstanceMembersSource source) : IFromInstanceMembers
{
    public FromInstanceMembersSource Instance => source;
}
