using Kehlet.Generators.AutoInterface.Models;

namespace Kehlet.Generators.AutoInterface;

public static class Emitter
{
    public static string EmitMethod(Method method, bool implement, string source)
    {
        var parameters = string.Join(", ", method.Parameters);
        var arguments = string.Join(", ", method.Arguments);
        var body = EmitMethodBody(method, implement, arguments, source);
        var async = body.async ? "async " : "";
        return $"{async}{method.ReturnType} {method.Name}({parameters}){body.body}";
    }

    public static string EmitProperty(Property property, bool implement, string source)
    {
        var body = EmitPropertyBody(property, implement, source);
        return $"{property.Type} {property.Name}{body}";
    }

    private static (string body, bool async) EmitMethodBody(Method method, bool implement, string args, string source)
    {
        if (implement is false)
        {
            return (";", false);
        }

        var methodCall = $"{source}.{method.Name}({args});";
        switch (method.ReturnTypeHandling)
        {
            case ReturnTypeEnum.Normal:
                var body = $"""
                     =>
                            {methodCall}
                    """;
                return (body, false);
            case ReturnTypeEnum.Custom:
                var body2 = $$"""
                    
                        {
                            {{methodCall}}
                            return default;
                        }
                    """;
                return (body2, false);
            case ReturnTypeEnum.CustomTask:
                var body3 = $$"""
                    
                        {
                            await {{methodCall}}
                            return default;
                        }
                    """;
                return (body3, true);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static string EmitPropertyBody(Property property, bool implement, string source)
    {
        var getGetterBody = () => implement ? $" => {source}.{property.Name};" : ";";
        var getSetterBody = () => implement ? $" => {source}.{property.Name} = value;" : ";";
        var getGetter = () => $"get{getGetterBody()}";
        var getSetter = () => $"set{getSetterBody()}";

        return (property.HasGetter, property.HasSetter) switch
        {
            (true, false) =>
                implement ? getGetterBody() : " { " + getGetter() + " }",
            (false, true) =>
                " { " + getSetter() + " }",
            (true, true) => $$"""
                
                    {
                        {{getGetter()}}
                        {{getSetter()}}
                    }
                """,
            (false, false) => ""
        };
    }
}
