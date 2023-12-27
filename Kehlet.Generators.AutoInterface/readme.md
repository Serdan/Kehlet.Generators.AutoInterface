## Interface from static members

Source:

```csharp
public static class FromStaticMembersSample
{
    public static void DoSomething(int number)
    {
    }

    public static int GetNumber(string str) => str.Length;
}

[FromStaticMembers(typeof(FromStaticMembersSample), implement: true, voidType: typeof(Unit))]
public partial interface IInstance;

public readonly record struct Unit;
```

Generated:

```csharp
partial interface IInstance
{
    Unit DoSomething(int number)
    {
        FromStaticMembersSample.DoSomething(number);
        return default;
    }

    int GetNumber(string str) =>
        FromStaticMembersSample.GetNumber(str);
}

```

## Interface from default implementation

Source:

```csharp
[DefaultImplementation]
public partial class DefaultImplementationSample
{
    public void DoSomething(params string[] values)
    {
    }

    public int GetNumber() => 42;

    public string Value => "";
}

```

Generated:

```csharp
public partial class DefaultImplementationSample : IDefaultImplementationSample;

public partial interface IDefaultImplementationSample
{
    void DoSomething(params string[] values);
    int GetNumber();
    string Value { get; }
}

```
