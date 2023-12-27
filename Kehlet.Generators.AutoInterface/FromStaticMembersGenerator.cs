using System.Collections.Immutable;
using System.Text;
using Kehlet.Generators.AutoInterface.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Kehlet.Generators.AutoInterface;

[Generator]
public class FromStaticMembersGenerator : IIncrementalGenerator
{
    private const string Namespace = "Kehlet.Functional.Generators";
    private const string AttributeName = "FromStaticMembers";
    private const string FullAttributeName = $"{Namespace}.{AttributeName}";

    private const string AttributeSourceCode = $$"""
        #nullable enable

        namespace {{Namespace}};

        using System;

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

        // Generate the source code.
        context.RegisterSourceOutput(provider, Execute);
    }

    private static bool Filter(SyntaxNode node, CancellationToken token) =>
        node.As<TypeDeclarationSyntax>().Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)) &&
        node.IsKind(SyntaxKind.InterfaceDeclaration);

    private static Target Transform(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        var attribute = context.Attributes.First(x => x.AttributeClass!.ToString() == FullAttributeName);
        var targetType = attribute.ConstructorArguments[0].Value.As<INamedTypeSymbol>();
        var implement = attribute.ConstructorArguments[1].Value.As<bool>();
        var voidType = attribute.ConstructorArguments[2].Value.As<ISymbol?>();

        var members = (from member in targetType.GetMembers()
                       let method = member as IMethodSymbol
                       where member.IsStatic &&
                           member.DeclaredAccessibility is Accessibility.Public &&
                           method?.MethodKind is MethodKind.Ordinary
                       let returnType = (voidType, method.ReturnsVoid) switch
                       {
                           ({ } type, true) => type.Name,
                           _ => method.ReturnType.ToUnqualifiedName()
                       }
                       let parameters = method.Parameters.Select(parameter => parameter.ToUnqualifiedName()).ToImmutableArray()
                       let args = method.Parameters.Select(x => x.Name).ToImmutableArray()
                       let import = (string?[])
                       [
                           method.ReturnType.GetNamespace(),
                           ..method.Parameters.Select(p => p.Type.GetNamespace())
                       ]
                       select new
                       {
                           Namespaces = import,
                           Method = new Method(method.Name, returnType, HasCustomVoidType: method.ReturnsVoid, parameters, args)
                       }).ToArray();

        var methods = members.Select(x => x.Method)
                             .ToImmutableArray();

        var imports = members.SelectMany(member => member.Namespaces)
                             .Union([targetType.GetNamespace(), voidType?.GetNamespace()])
                             .Where(x => x is not null)
                             .Distinct()
                             .Select(x => x!)
                             .ToImmutableArray();

        var symbol = context.TargetSymbol.As<INamedTypeSymbol>();
        var partialType = new PartialType(symbol.Name, symbol.GetNamespace());

        return new(partialType, targetType.ToUnqualifiedName(), implement, methods, imports);
    }

    private static void Execute(SourceProductionContext context, Target target)
    {
        var emitBody = (Target target, Method method, string args) =>
        {
            if (target.Implement is false)
            {
                return ";";
            }

            var methodCall = $"{target.SourceTypeName}.{method.Name}({args});";
            if (method.HasCustomVoidType)
            {
                return $$"""

                        {
                            {{methodCall}}
                            return default;
                        }
                    """;
            }
            else
            {
                return $"""
                     =>
                            {methodCall}
                    """;
            }
        };

        var members = from member in target.Methods
                      let parameters = string.Join(", ", member.Parameters)
                      let args = string.Join(", ", member.Arguments)
                      let impl = emitBody(target, member, args)
                      select $"{member.ReturnType} {member.Name}({parameters}){impl}";

        var ns = target.PartialType.Namespace is null
            ? ""
            : $"\n\nnamespace {target.PartialType.Namespace};";

        var imports = target.Imports
                            .Select(import => $"using {import};")
                            .Apply(x => string.Join("\n", x));

        var type = $$"""
            #nullable enable{{ns}}

            {{imports}}

            partial interface {{target.PartialType.Name}}
            {
                {{string.Join($"\n\n{Tab}", members)}}
            }

            """;

        context.AddSource($"{target.PartialType.Name}.AutoInterface.g.cs", SourceText.From(type, Encoding.UTF8));
    }

    private record Target(PartialType PartialType, string SourceTypeName, bool Implement, ImmutableArray<Method> Methods, ImmutableArray<string> Imports);

    private record Method(string Name, string ReturnType, bool HasCustomVoidType, ImmutableArray<string> Parameters, ImmutableArray<string> Arguments);

    private record PartialType(string Name, string? Namespace);

    private const string Tab = "    ";
}
