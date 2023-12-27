using System.Text;
using Kehlet.Generators.AutoInterface.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Kehlet.Generators.AutoInterface;

[Generator]
public partial class InterfaceGenerator : IIncrementalGenerator
{
    private const string AttributeNamespace = "Kehlet.Generators";
    private const string DefaultImplementationAttributeName = "DefaultImplementationAttribute";
    private const string FullDefaultImplementationAttributeName = $"{AttributeNamespace}.{DefaultImplementationAttributeName}";
    private const string DefaultImplementationAttributeFileName = $"{DefaultImplementationAttributeName}.g.cs";
    private const string InAttributeName = "InAttribute";
    private const string FullInAttributeName = $"{AttributeNamespace}.{InAttributeName}";
    private const string OutAttributeName = "OutAttribute";
    private const string FullOutAttributeName = $"{AttributeNamespace}.{OutAttributeName}";
    private const string ExcludeAttributeName = "ExcludeAttribute";
    private const string FullExcludeAttributeName = $"{AttributeNamespace}.{ExcludeAttributeName}";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(i => i.AddSource(DefaultImplementationAttributeFileName, SourceText.From(DefaultImplementationAttribute, Encoding.UTF8)));

        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            FullDefaultImplementationAttributeName,
            Filter,
            Transform
        );

        context.RegisterSourceOutput(provider, Execute);
    }

    private static bool Filter(SyntaxNode node, CancellationToken _) =>
        node.IsPartial();

    private static DefaultImplementationClass Transform(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        var parser = new Parser(token);
        return parser.GetClass(context);
    }

    private static void Execute(SourceProductionContext context, DefaultImplementationClass @class)
    {
        var result = Emitter.Emit(@class);
        context.AddSource(@class.Name + ".AutoInterface.g.cs", SourceText.From(result, Encoding.UTF8));
    }
}
