using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Kehlet.Generators.AutoInterface.Extensions;

internal static class UtilityExtensions
{
    public static ImmutableArray<AttributeData> GetAttributes(this ISymbol? symbol, string attributeName)
    {
        return symbol?.GetAttributes()
                     .Where(x => x.AttributeClass?.ToDisplayString() == attributeName)
                     .ToImmutableArray() ?? ImmutableArray<AttributeData>.Empty;
    }

    public static string GetNamespace(this SyntaxNode syntaxNode)
    {
        var candidate = syntaxNode.Parent;
        while (candidate is not null and not BaseNamespaceDeclarationSyntax)
        {
            candidate = candidate.Parent;
        }

        if (candidate is not BaseNamespaceDeclarationSyntax @namespace)
        {
            return "";
        }

        var namespaceName = @namespace.Name.ToString();
        while (@namespace.Parent is BaseNamespaceDeclarationSyntax parent)
        {
            @namespace = parent;
            namespaceName = $"{@namespace.Name}.{namespaceName}";
        }

        return namespaceName;
    }
}
