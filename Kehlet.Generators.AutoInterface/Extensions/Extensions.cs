using Microsoft.CodeAnalysis;

namespace Kehlet.Generators.AutoInterface.Extensions;

public static class Extensions
{
    public static T As<T>(this SyntaxNode node)
        where T : SyntaxNode =>
        (T) node;

    public static T As<T>(this ISymbol node)
        where T : ISymbol =>
        (T) node;

    public static T As<T>(this object? obj) =>
        (T) obj!;

    public static string? GetNamespace(this ISymbol symbol) =>
        symbol.ContainingNamespace?.IsGlobalNamespace is not false
            ? null
            : symbol.ContainingNamespace.ToString();

    public static string ToUnqualifiedName(this ISymbol symbol) =>
        symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    public static TResult Apply<T, TResult>(this T self, Func<T, TResult> f) => f(self);
}
