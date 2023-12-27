namespace Kehlet.Generators.AutoInterface.Sample;

public class FromInstanceMembersSource
{
    public void DoSomething()
    {
    }

    public int GetNumber(string str) => str.Length;
}

[FromInstanceMembers(typeof(FromInstanceMembersSource), true, typeof(Unit))]
public partial interface IFromInstanceMembers;

public readonly struct FromInstanceMembersStruct(FromInstanceMembersSource source) : IFromInstanceMembers
{
    public FromInstanceMembersSource Instance => source;
}
