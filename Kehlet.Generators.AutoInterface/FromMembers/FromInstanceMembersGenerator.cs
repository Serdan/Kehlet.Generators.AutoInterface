using System.Text;
using Kehlet.Generators.AutoInterface.Extensions;
using Kehlet.Generators.AutoInterface.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Kehlet.Generators.AutoInterface;

[Generator]
public class FromInstanceMembersGenerator : IIncrementalGenerator
{
    private const string Namespace = "Kehlet.Generators";
    private const string AttributeName = "FromInstanceMembersAttribute";
    private const string FullAttributeName = $"{Namespace}.{AttributeName}";

    private const string AttributeSourceCode = $$"""
        #nullable enable

        using System;

        namespace {{Namespace}};

        [AttributeUsage(AttributeTargets.Interface)]
        internal class {{AttributeName}}(Type source, bool implement = false, Type? voidType = null) : Attribute
        {
            public Type Source => source;
            public bool Implement => implement;
            public Type? VoidType => voidType;
        }
        """;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                                                     $"{AttributeName}.AutoInterface.g.cs",
                                                     SourceText.From(AttributeSourceCode, Encoding.UTF8)));


        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            FullAttributeName,
            Filter,
            Transform
        );

        context.RegisterSourceOutput(provider, Execute);
    }

    private static bool Filter(SyntaxNode node, CancellationToken token) =>
        node.IsPartial() &&
        node.IsKind(SyntaxKind.InterfaceDeclaration);

    private static Parser Parser { get; } = new(FullAttributeName);

    private static AutoInterfaceDetails Transform(GeneratorAttributeSyntaxContext context, CancellationToken token) =>
        Parser.Parse(context, methodSymbol => methodSymbol.IsStatic is false);

    private static void Execute(SourceProductionContext context, AutoInterfaceDetails target)
    {
        var members = (
            from method in target.Methods
            select Emitter.EmitMethod(method, target.Implement, "Instance")
        ).Apply(ms => string.Join($"\n\n{Tab}", ms));

        var properties = (
            from property in target.Properties
            select Emitter.EmitProperty(property, target.Implement, "Instance")
        ).Apply(ps => string.Join($"\n\n{Tab}", ps));

        var ns = target.PartialType.Namespace is null
            ? ""
            : $"\n\nnamespace {target.PartialType.Namespace};";

        var imports = target.Imports
                            .Select(import => $"using {import};")
                            .Apply(x => string.Join("\n", x));

        var def = target.Implement
            ? $"\n{Tab}protected {target.SourceTypeName} Instance {{ get; }}\n"
            : "";

        var type = $$"""
            #nullable enable

            {{imports}}{{ns}}

            partial interface {{target.PartialType.Name}}
            {{{def}}
                {{properties}}
                
                {{members}}
            }

            """;

        context.AddSource($"{target.PartialType.Name}.g.cs", SourceText.From(type, Encoding.UTF8));
    }

    private const string Tab = "    ";
}
