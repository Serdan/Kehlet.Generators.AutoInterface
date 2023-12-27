## Interface from static members

Source:

```csharp
public static class SourceType
{
    public static int GetThing(params string[] input) =>
        input.Length;

    public static void DoThing(int a, int b)
    {
    }
}

[FromStaticMembers(typeof(SourceType), implement: true, voidType: typeof(Unit))]
public partial interface ISourceType;
```

Generated:

```csharp
partial interface ISourceType
{
    int GetThing(params string[] input) =>
        SourceType.GetThing(input);

    Unit DoThing(int a, int b)
    {
        SourceType.DoThing(a, b);
        return default;
    }
}
```

## Interface from default implementation

Source:

```csharp
[DefaultImplementation]
public partial class SourceType
{
    public int GetNumber => 42;
    public string GetString(double number = Math.PI) => "";
}
```

Generated:

```csharp
public partial class SourceType : ISourceType;

public partial interface ISourceType
{
    int GetNumber { get; }
    string GetString(double number = Math.PI);
}
```
