using System.Collections.Immutable;
using Kehlet.Generators.AutoInterface.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Kehlet.Generators.AutoInterface;

public partial class InterfaceGenerator
{
    internal class Parser(CancellationToken cancellationToken)
    {
        public DefaultImplementationClass GetClass(GeneratorAttributeSyntaxContext context)
        {
            var attribute = context.Attributes.First(x => x.AttributeClass?.ToString() == FullDefaultImplementationAttributeName);

            var isPublic = !(attribute.ConstructorArguments is { IsDefaultOrEmpty: false } args && args[0].Value is 1);

            var ns = context.TargetSymbol.ContainingNamespace.ToString();
            var symbol = context.TargetSymbol.Cast<INamedTypeSymbol>();

            return new()
            {
                Namespace = ns,
                Accessibility = symbol.DeclaredAccessibility,
                Kind = symbol.TypeKind,
                IsPublic = isPublic,
                Name = symbol.Name,
                TypeParameters = GetTypeParameters(symbol.TypeParameters, allowVariance: true),
                Members = GetPublicMembers(context.SemanticModel, context.TargetNode.Cast<TypeDeclarationSyntax>()),
                Docs = symbol.GetDocumentationCommentXml()
            };
        }

        private ImmutableArray<Member> GetPublicMembers(SemanticModel sm, TypeDeclarationSyntax classDeclarationSyntax)
        {
            if (classDeclarationSyntax.Members.Count == 0)
            {
                return ImmutableArray<Member>.Empty;
            }

            var members = new List<Member>();

            foreach (var member in classDeclarationSyntax.Members)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var memberSymbol = sm.GetDeclaredSymbol(member);
                if (memberSymbol?.DeclaredAccessibility is not Accessibility.Public)
                {
                    continue;
                }

                var attribute = memberSymbol.GetAttributes(FullExcludeAttributeName).FirstOrDefault();
                if (attribute is not null)
                {
                    continue;
                }

                switch (memberSymbol)
                {
                    case IPropertySymbol property:
                        members.Add(new()
                        {
                            Type = property.IsIndexer ? MemberType.Indexer : MemberType.Property,
                            ReturnsByRef = property.ReturnsByRef,
                            ReturnsByRefReadonly = property.ReturnsByRefReadonly,
                            ReturnType = property.Type.ToDisplayString(),
                            Name = property.Name,
                            Parameters = GetParameters(property.Parameters),
                            Accessors = GetAccessors(property),
                            Docs = property.GetDocumentationCommentXml()
                        });
                        break;
                    case IMethodSymbol { MethodKind: MethodKind.Ordinary } method:
                        members.Add(new()
                        {
                            Type = MemberType.Method,
                            ReturnsByRef = method.ReturnsByRef,
                            ReturnsByRefReadonly = method.ReturnsByRefReadonly,
                            ReturnType = method.ReturnType.ToDisplayString(),
                            Name = method.Name,
                            TypeParameters = GetTypeParameters(method.TypeParameters, allowVariance: false),
                            Parameters = GetParameters(method.Parameters),
                            Docs = method.GetDocumentationCommentXml()
                        });
                        break;
                }
            }

            return members.ToImmutableArray();
        }

        private static ImmutableArray<TypeParameter> GetTypeParameters(ImmutableArray<ITypeParameterSymbol> typeParameters, bool allowVariance)
        {
            if (typeParameters.IsDefaultOrEmpty)
            {
                return ImmutableArray<TypeParameter>.Empty;
            }

            var result = ImmutableArray.CreateBuilder<TypeParameter>(typeParameters.Length);
            foreach (var typeParameter in typeParameters)
            {
                if (allowVariance is false)
                {
                    result.Add(new()
                    {
                        Name = typeParameter.Name,
                        Variance = Variance.None
                    });
                    continue;
                }

                var covariant = typeParameter.GetAttributes(FullOutAttributeName).FirstOrDefault() is not null;
                if (covariant)
                {
                    result.Add(new()
                    {
                        Name = typeParameter.Name,
                        Variance = Variance.Covariant
                    });
                    continue;
                }

                var contravariant = typeParameter.GetAttributes(FullInAttributeName).FirstOrDefault() is not null;
                if (contravariant)
                {
                    result.Add(new()
                    {
                        Name = typeParameter.Name,
                        Variance = Variance.Contravariant
                    });
                    continue;
                }

                result.Add(new()
                {
                    Name = typeParameter.Name,
                    Variance = Variance.None
                });
            }

            return result.ToImmutable();
        }

        private static ImmutableArray<Parameter> GetParameters(ImmutableArray<IParameterSymbol> parameters)
        {
            if (parameters.IsDefaultOrEmpty)
            {
                return ImmutableArray<Parameter>.Empty;
            }

            var p = ImmutableArray.CreateBuilder<Parameter>(parameters.Length);
            foreach (var parameter in parameters)
            {
                string? defaultValue = null;
                if (parameter.DeclaringSyntaxReferences is { IsDefaultOrEmpty: false } references)
                {
                    defaultValue = (references[0].GetSyntax() as ParameterSyntax)?.Default?.ToFullString();
                }

                p.Add(new()
                {
                    Declaration = parameter.ToString(),
                    HasDefaultValue = parameter.HasExplicitDefaultValue,
                    DefaultValue = defaultValue
                });
            }

            return p.ToImmutable();
        }

        private static ImmutableArray<string> GetAccessors(IPropertySymbol property)
        {
            var accessors = new List<string>(2);
            if (property.GetMethod is { DeclaredAccessibility: Accessibility.Public })
            {
                accessors.Add("get;");
            }

            if (property.SetMethod is { DeclaredAccessibility: Accessibility.Public } setMethod)
            {
                accessors.Add(setMethod.IsInitOnly ? "init;" : "set;");
            }

            return accessors.ToImmutableArray();
        }
    }

    internal class DefaultImplementationClass
    {
        public string Namespace { get; init; } = "";
        public Accessibility Accessibility { get; init; }
        public TypeKind Kind { get; init; }
        public bool IsPublic { get; init; }
        public string Name { get; init; } = "";
        public ImmutableArray<TypeParameter> TypeParameters { get; init; }
        public ImmutableArray<Member> Members { get; init; }
        public string? Docs { get; init; }
    }

    internal class Member
    {
        public bool ReturnsByRef { get; init; }
        public bool ReturnsByRefReadonly { get; init; }
        public string ReturnType { get; init; } = "";
        public string Name { get; init; } = "";
        public ImmutableArray<TypeParameter> TypeParameters { get; init; }
        public ImmutableArray<Parameter> Parameters { get; init; }
        public ImmutableArray<string> Accessors { get; init; }
        public string? Docs { get; init; }
        public MemberType Type { get; init; }
    }

    internal enum MemberType
    {
        None,
        Indexer,
        Property,
        Method
    }

    internal class Parameter
    {
        public string Declaration { get; init; } = "";
        public bool HasDefaultValue { get; init; }
        public string? DefaultValue { get; init; }

        public override string ToString()
        {
            return HasDefaultValue
                ? $"{Declaration} {DefaultValue}"
                : $"{Declaration}";
        }
    }

    internal class TypeParameter
    {
        public string Name { get; init; } = "";
        public Variance Variance { get; init; }
    }

    internal enum Variance
    {
        None,
        Covariant,
        Contravariant
    }
}
