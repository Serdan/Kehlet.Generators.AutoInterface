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

    public AutoInterfaceDetails Parse(GeneratorAttributeSyntaxContext context, Func<ISymbol, bool> predicate)
    {
        TaskType ??= context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Task).FullName!)!;
        ValueTaskType ??= context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(ValueTask).FullName!)!;
        TaskType1 ??= context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Task<>).FullName!)!;
        ValueTaskType1 ??= context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(ValueTask<>).FullName!)!;

        var attribute = context.Attributes.First(x => x.AttributeClass!.ToString() == FullAttributeName);
        var targetType = attribute.ConstructorArguments[0].Value.Cast<INamedTypeSymbol>();
        var implement = attribute.ConstructorArguments[1].Value.Cast<bool>();
        var voidType = attribute.ConstructorArguments[2].Value.As<ITypeSymbol>();

        var methodArray =
            (from member in targetType.GetMembers()
             let method = member as IMethodSymbol
             where method is { DeclaredAccessibility: Accessibility.Public, MethodKind: MethodKind.Ordinary }
                 && predicate(method) && !IsObsolete(method)
             let returnType = (voidType is not null, method.ReturnsVoid, method.ReturnType) switch
             {
                 (true, true, _) => (voidType!, ReturnTypeEnum.Custom),
                 (true, false, { } returnType) when IsTask(returnType) => (TaskType1.Construct(voidType!), ReturnTypeEnum.CustomTask),
                 (true, false, { } returnType) when IsValueTask(returnType) => (ValueTaskType1.Construct(voidType!), ReturnTypeEnum.CustomTask),
                 var (_, _, returnType) => (returnType, ReturnTypeEnum.Normal)
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

        var propertyArray =
            (from member in targetType.GetMembers()
             let property = member as IPropertySymbol
             where property is { DeclaredAccessibility: Accessibility.Public }
                 && predicate(property) && !IsObsolete(property)
             let type = (voidType is not null, property.Type) switch
             {
                 (true, { } type) when IsTask(type) => (TaskType1.Construct(voidType!), ReturnTypeEnum.CustomTask),
                 (true, { } type) when IsValueTask(type) => (ValueTaskType1.Construct(voidType!), ReturnTypeEnum.CustomTask),
                 var (_, type) => (type, ReturnTypeEnum.Normal)
             }
             let import = type.type.GetNamespace()
             select new
             {
                 Namespace = import,
                 Property = new Property(property.Name, property.Type.ToUnqualifiedName(), property.GetMethod is not null, property.SetMethod is not null)
             }).ToArray();


        var methods = methodArray.Select(x => x.Method)
                                 .ToImmutableArray();

        var properties = propertyArray.Select(x => x.Property)
                                      .ToImmutableArray();

        var imports = methodArray.SelectMany(member => member.Namespaces)
                                 .Union([targetType.GetNamespace(), voidType?.GetNamespace()])
                                 .Union(propertyArray.Select(x => x.Namespace))
                                 .Where(x => x is not null)
                                 .Distinct()
                                 .Select(x => x!)
                                 .ToImmutableArray();

        var symbol = context.TargetSymbol.Cast<INamedTypeSymbol>();
        var partialType = new PartialType(symbol.Name, symbol.GetNamespace());

        return new(partialType, targetType.ToUnqualifiedName(), implement, methods, properties, imports);
    }

    private static bool IsObsolete(ISymbol symbol) =>
        symbol.GetAttributes(typeof(ObsoleteAttribute).FullName!).Length > 0;

    private static bool IsTask(ISymbol other) =>
        SymbolEqualityComparer.Default.Equals(TaskType, other);

    private static bool IsValueTask(ISymbol other) =>
        SymbolEqualityComparer.Default.Equals(ValueTaskType, other);
}
