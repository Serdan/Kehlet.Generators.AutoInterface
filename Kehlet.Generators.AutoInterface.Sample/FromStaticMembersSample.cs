namespace Kehlet.Generators.AutoInterface.Sample;

public static class FromStaticMembersSample
{
    public static int Number1
    {
        get => 1;
    }

    public static int Number2
    {
        set => DoSomething(value);
    }

    public static int Number3
    {
        get => 1;
        set => DoSomething(value);
    }

    public static void DoSomething(int number)
    {
    }

    public static int GetNumber(string str) => str.Length;

    public static Task DoSomethingAsync() => Task.CompletedTask;

    public static ValueTask DoSomethingAsync2() => ValueTask.CompletedTask;
}

[FromStaticMembers(typeof(FromStaticMembersSample), implement: true, voidType: typeof(Unit))]
public partial interface IFromStaticMembers;

public readonly record struct Unit;
