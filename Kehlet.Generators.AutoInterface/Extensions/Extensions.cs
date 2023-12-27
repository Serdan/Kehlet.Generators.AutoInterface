using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Kehlet.Generators.AutoInterface.Extensions;

public static class Extensions
{
    public static T? As<T>(this SyntaxNode node)
        where T : SyntaxNode =>
        node as T;

    public static T? As<T>(this ISymbol node)
        where T : class, ISymbol =>
        node as T;

    public static T? As<T>(this object? obj) =>
        obj is T value
            ? value
            : default;

    public static T Cast<T>(this object? obj) =>
        (T) obj!;

    public static T Cast<T>(this SyntaxNode node)
        where T : SyntaxNode =>
        (T) node;

    public static T Cast<T>(this ISymbol symbol)
        where T : ISymbol =>
        (T) symbol;

    public static string? GetNamespace(this ISymbol symbol) =>
        symbol.ContainingNamespace?.IsGlobalNamespace is not false
            ? null
            : symbol.ContainingNamespace.ToString();

    public static string ToUnqualifiedName(this ISymbol symbol) =>
        symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    public static TResult Apply<T, TResult>(this T self, Func<T, TResult> f) => f(self);

    public static ImmutableArray<AttributeData> GetAttributes(this ISymbol? symbol, string attributeName) =>
        symbol?.GetAttributes()
              .Where(x => x.AttributeClass?.ToDisplayString() == attributeName)
              .ToImmutableArray() ?? ImmutableArray<AttributeData>.Empty;

    public static bool IsPartial(this SyntaxNode node) =>
        node.HasModifer(SyntaxKind.PartialKeyword);

    public static bool HasModifer(this SyntaxNode node, SyntaxKind kind) =>
        node.As<TypeDeclarationSyntax>()
            ?.Modifiers.Any(modifier => modifier.IsKind(kind)) is true;
}
