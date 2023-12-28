using System.Collections.Immutable;
using Kehlet.Generators.AutoInterface.Extensions;
using Kehlet.Generators.AutoInterface.Models;
using Microsoft.CodeAnalysis;

namespace Kehlet.Generators.AutoInterface;

public class Parser(string FullAttributeName)
{
    private static INamedTypeSymbol? TaskType;
    private static INamedTypeSymbol? ValueTaskType;
    private static INamedTypeSymbol? TaskType1;
    private static INamedTypeSymbol? ValueTaskType1;

    public AutoInterfaceDetails Parse(GeneratorAttributeSyntaxContext context, Func<IMethodSymbol, bool> predicate)
    {
        TaskType ??= context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Task).FullName!)!;
        ValueTaskType ??= context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(ValueTask).FullName!)!;
        TaskType1 ??= context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Task<>).FullName!)!;
        ValueTaskType1 ??= context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(ValueTask<>).FullName!)!;

        var attribute = context.Attributes.First(x => x.AttributeClass!.ToString() == FullAttributeName);
        var targetType = attribute.ConstructorArguments[0].Value.Cast<INamedTypeSymbol>();
        var implement = attribute.ConstructorArguments[1].Value.Cast<bool>();
        var voidType = attribute.ConstructorArguments[2].Value.As<ITypeSymbol>();

        var members = (from member in targetType.GetMembers()
                       let method = member as IMethodSymbol
                       where method is { DeclaredAccessibility: Accessibility.Public, MethodKind: MethodKind.Ordinary }
                           && predicate(method)
                       let returnType = (voidType, method.ReturnsVoid, method.ReturnType) switch
                       {
                           ({ } type, true, _) => (type, ReturnTypeEnum.Custom),
                           ({ } type, false, { } returnType) when IsTask(returnType) => (TaskType1.Construct(type), ReturnTypeEnum.CustomTask),
                           ({ } type, false, { } returnType) when IsValueTask(returnType) => (ValueTaskType1.Construct(type), ReturnTypeEnum.CustomTask),
                           _ => (method.ReturnType, ReturnTypeEnum.Normal)
                       }
                       let parameters = method.Parameters.Select(parameter => parameter.ToUnqualifiedName()).ToImmutableArray()
                       let args = method.Parameters.Select(x => x.Name).ToImmutableArray()
                       let import = (string?[])
                       [
                           returnType.Item1.GetNamespace(),
                           ..method.Parameters.Select(p => p.Type.GetNamespace())
                       ]
                       select new
                       {
                           Namespaces = import,
                           Method = new Method(method.Name, returnType.Item1.ToUnqualifiedName(), ReturnTypeHandling: returnType.Item2, parameters, args)
                       }).ToArray();

        var methods = members.Select(x => x.Method)
                             .ToImmutableArray();

        var imports = members.SelectMany(member => member.Namespaces)
                             .Union([targetType.GetNamespace(), voidType?.GetNamespace()])
                             .Where(x => x is not null)
                             .Distinct()
                             .Select(x => x!)
                             .ToImmutableArray();

        var symbol = context.TargetSymbol.Cast<INamedTypeSymbol>();
        var partialType = new PartialType(symbol.Name, symbol.GetNamespace());

        return new(partialType, targetType.ToUnqualifiedName(), implement, methods, imports);
    }

    private static bool IsTask(ISymbol other) =>
        SymbolEqualityComparer.Default.Equals(TaskType, other);

    private static bool IsValueTask(ISymbol other) =>
        SymbolEqualityComparer.Default.Equals(ValueTaskType, other);

}
