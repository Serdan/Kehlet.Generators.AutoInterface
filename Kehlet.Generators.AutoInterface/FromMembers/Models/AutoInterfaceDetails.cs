using System.Collections.Immutable;

namespace Kehlet.Generators.AutoInterface.Models;

public record AutoInterfaceDetails(
    PartialType PartialType,
    string SourceTypeName,
    bool Implement,
    ImmutableArray<Method> Methods,
    ImmutableArray<Property> Properties,
    ImmutableArray<string> Imports
);
