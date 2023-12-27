using System.Net;
using Kehlet.Generators;

namespace Kehlet.Generators.AutoInterface.Sample;

public static class FromStaticMembersSample
{
    public static void DoSomething(int number)
    {
    }

    public static int GetNumber(string str) => str.Length;
}

[FromStaticMembers(typeof(FromStaticMembersSample), implement: true, voidType: typeof(Unit))]
public partial interface IFromStaticMembers;

public readonly record struct Unit;

[FromInstanceMembers(typeof(HttpClient), true, typeof(Unit))]
public partial interface IFromInstanceMembers;

public readonly struct FromInstanceMembersStruct(HttpClient client) : IFromInstanceMembers
{
    public HttpClient Instance => client;
}
