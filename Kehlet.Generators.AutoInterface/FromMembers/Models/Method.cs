using System.Collections.Immutable;

namespace Kehlet.Generators.AutoInterface.Models;

public record Method(string Name, string ReturnType, ReturnTypeEnum ReturnTypeHandling, ImmutableArray<string> Parameters, ImmutableArray<string> Arguments);
