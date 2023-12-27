using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Kehlet.Generators.AutoInterface;

public partial class InterfaceGenerator
{
    internal static class Emitter
    {
        private const string Indent = "    ";

        private static string? AccessModifierMap(Accessibility accessibility) => accessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.Public => "public",
            _ => null
        };

        private static string? TypeKindMap(TypeKind kind) => kind switch
        {
            TypeKind.Class => "class",
            TypeKind.Struct => "struct",
            _ => null
        };

        public static string Emit(DefaultImplementationClass @class)
        {
            var builder = new StringBuilder();

            builder.AppendLine("#nullable enable");
            if (@class.Namespace.Length > 0)
            {
                builder.AppendLine($"\nnamespace {@class.Namespace};");
            }

            if (AccessModifierMap(@class.Accessibility) is not { } accessModifier)
            {
                return "";
            }

            if (TypeKindMap(@class.Kind) is not { } typeKind)
            {
                return "";
            }

            var typeParameters = EmitTypeParameters(@class.TypeParameters, withVariance: false);

            builder.AppendLine($"""

                               {accessModifier} partial {typeKind} {@class.Name}{typeParameters} : I{@class.Name}{typeParameters};

                               """);

            AppendDocs(builder, @class.Docs);

            var accessibility = @class.IsPublic ? "public" : "internal";

            builder.Append($"{accessibility} partial interface I{@class.Name}");
            AppendTypeParameters(builder, @class.TypeParameters, withVariance: true);

            builder.AppendLine("\n{");
            AppendMembers(builder, @class.Members);
            builder.AppendLine("}");

            return builder.ToString();
        }

        private static void AppendMembers(StringBuilder builder, ImmutableArray<Member> members)
        {
            if (members.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var member in members)
            {
                switch (member.Type)
                {
                    case MemberType.Property when member.Accessors.IsDefaultOrEmpty:
                    case MemberType.Indexer when (member.Accessors.IsDefaultOrEmpty || member.Parameters.IsDefaultOrEmpty):
                        continue;
                }

                AppendDocs(builder, member.Docs, Indent);

                builder.Append(Indent);

                if (member.ReturnsByRefReadonly)
                {
                    builder.Append("ref readonly ");
                }
                else if (member.ReturnsByRef)
                {
                    builder.Append("ref ");
                }

                builder.Append($"{member.ReturnType} ");

                switch (member.Type)
                {
                    case MemberType.Indexer:
                        AppendIndexerSignature(builder, member);
                        break;
                    case MemberType.Property:
                        AppendPropertySignature(builder, member);
                        break;
                    case MemberType.Method:
                        AppendMethodSignature(builder, member);
                        break;
                }
            }
        }

        private static void AppendIndexerSignature(StringBuilder builder, Member member)
        {
            builder.Append("this[");
            builder.Append(string.Join(", ", member.Parameters));
            builder.Append("] { ");
            builder.Append(string.Join(" ", member.Accessors));
            builder.AppendLine(" }");
        }

        private static void AppendPropertySignature(StringBuilder builder, Member member)
        {
            builder.Append($"{member.Name} {{ ");
            builder.Append(string.Join(" ", member.Accessors));
            builder.AppendLine(" }");
        }

        private static void AppendMethodSignature(StringBuilder builder, Member member)
        {
            builder.Append(member.Name);
            AppendTypeParameters(builder, member.TypeParameters, withVariance: false);
            builder.Append("(");
            builder.Append(string.Join(", ", member.Parameters));
            builder.AppendLine(");");
        }

        private static void AppendTypeParameters(StringBuilder builder, ImmutableArray<TypeParameter> typeParameters, bool withVariance)
        {
            if (typeParameters.IsDefaultOrEmpty)
            {
                return;
            }

            builder.Append("<");
            foreach (var typeParameter in typeParameters)
            {
                AppendTypeParameter(builder, typeParameter, withVariance);
            }

            builder.Append(">");

            static void AppendTypeParameter(StringBuilder builder, TypeParameter typeParameter, bool withVariance)
            {
                var variance = "";
                if (withVariance)
                {
                    variance = typeParameter.Variance switch
                    {
                        Variance.Covariant => "out ",
                        Variance.Contravariant => "in ",
                        _ => ""
                    };
                }

                builder.Append($"{variance}{typeParameter.Name}");
            }
        }

        private static string EmitTypeParameters(ImmutableArray<TypeParameter> parameters, bool withVariance)
        {
            if (parameters.IsDefaultOrEmpty)
            {
                return "";
            }

            var builder = new StringBuilder();

            AppendTypeParameters(builder, parameters, withVariance);

            return builder.ToString();
        }

        private static void AppendDocs(StringBuilder builder, string? docs, string indent = "")
        {
            if (docs is null)
            {
                return;
            }

            var lines = docs.Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                builder.AppendLine($"{indent}/// {line}");
            }
        }
    }

    private const string DefaultImplementationAttribute = $$"""
        #nullable enable

        namespace {{AttributeNamespace}};

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
        internal class {{DefaultImplementationAttributeName}} : Attribute
        {
            public {{DefaultImplementationAttributeName}}(Accessibility accessibility = Accessibility.Public)
            {
                Accessibility = accessibility;
            }
        
            public Accessibility Accessibility { get; }
        }

        internal enum Accessibility
        {
            Public,
            Internal
        }

        [AttributeUsage(AttributeTargets.GenericParameter)]
        internal class {{InAttributeName}} : Attribute { }

        [AttributeUsage(AttributeTargets.GenericParameter)]
        internal class {{OutAttributeName}} : Attribute { }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
        internal class {{ExcludeAttributeName}} : Attribute { }

        """;
}
