namespace Kehlet.Generators.AutoInterface.Sample;

public class FromInstanceMembersSource
{
    public void DoSomething()
    {
    }

    public int GetSomething(string str) => str.Length;
}

public class FromInstanceMembersSource2
{
    public void DoSomething2()
    {
    }

    public int GetNumber2(string str) => str.Length;
}

[FromInstanceMembers(typeof(FromInstanceMembersSource), true, typeof(Unit))]
public partial interface IFromInstanceMembers;

public readonly struct FromInstanceMembersStruct(FromInstanceMembersSource source) : IFromInstanceMembers
{
    public FromInstanceMembersSource Instance => source;
}
