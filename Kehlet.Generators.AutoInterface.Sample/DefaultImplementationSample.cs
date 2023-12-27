namespace Kehlet.Generators.AutoInterface.Sample;

[DefaultImplementation]
public partial class DefaultImplementationSample
{
    public void DoSomething(params string[] values)
    {
    }

    public int GetNumber() => 42;

    public string Value => "";
}
