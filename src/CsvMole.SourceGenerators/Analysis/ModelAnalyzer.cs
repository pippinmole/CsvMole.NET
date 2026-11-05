using System.Collections.Immutable;
using CsvMole.SourceGenerators.Diagnostics;
using CsvMole.SourceGenerators.External;
using CsvMole.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace CsvMole.SourceGenerators.Analysis;

internal static class ModelAnalyzer
{
    /// <summary>Analyses a model type; returns null (with diagnostics added) when it has errors.</summary>
    public static ModelInfo? Analyze(ITypeSymbol model, Compilation c, List<DiagnosticInfo> diags)
    {
        var start = diags.Count;
        var typeLocation = model.Locations.FirstOrDefault();
        var ctor = ChooseConstructor(model, diags);
        if (ctor is null) return null;

        // Each entry is a member and/or the constructor parameter it is matched to (by name, ignoring case).
        var entries = new List<(ISymbol? Member, IParameterSymbol? Parameter)>();
        var matched = new HashSet<int>();
        foreach (var member in PublicInstanceMembers(model))
        {
            var parameter = ctor.Parameters.FirstOrDefault(p =>
                !matched.Contains(p.Ordinal) && string.Equals(p.Name, member.Name, StringComparison.OrdinalIgnoreCase));
            if (parameter is not null) matched.Add(parameter.Ordinal);
            entries.Add((member, parameter));
        }
        entries.AddRange(ctor.Parameters.Where(p => !matched.Contains(p.Ordinal)).Select(p => ((ISymbol?)null, (IParameterSymbol?)p)));

        var setsRequiredMembers = ctor.GetAttributes().Any(a => Is(a, "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute"));
        var orders = new HashSet<int>();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var members = ImmutableArray.CreateBuilder<MemberInfo>();
        var hasErrorType = false;

        foreach (var (member, parameter) in entries)
        {
            var symbol = (member ?? parameter)!;
            var location = symbol.Locations.FirstOrDefault();
            var memberAttributes = MemberAttributes(member);
            var parameterAttributes = parameter?.GetAttributes() ?? ImmutableArray<AttributeData>.Empty;
            var orderOnMember = Find(memberAttributes, "CsvMole.CsvOrderAttribute");
            var orderOnParameter = Find(parameterAttributes, "CsvMole.CsvOrderAttribute");
            var columnOnMember = Find(memberAttributes, "CsvMole.CsvColumnAttribute");
            var columnOnParameter = Find(parameterAttributes, "CsvMole.CsvColumnAttribute");
            var orderAttribute = orderOnMember ?? orderOnParameter;
            var columnAttribute = columnOnMember ?? columnOnParameter;
            var converterAttribute = Find(memberAttributes, "CsvMole.CsvConverterAttribute")
                ?? Find(parameterAttributes, "CsvMole.CsvConverterAttribute");
            var order = Argument(orderAttribute) as int?;
            var column = Argument(columnAttribute) as string;
            var isRequired = member is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true };

            if (orderAttribute is null && columnAttribute is null)
            {
                // Unmapped optional/params ctor parameters are simply omitted from the generated call.
                if (parameter is { IsOptional: false, IsParams: false } || (isRequired && !setsRequiredMembers))
                {
                    diags.Add(DiagnosticInfo.Create("CSVM006", location, symbol.Name));
                }
                continue;
            }

            if (orderOnMember is not null && orderOnParameter is not null && !Equals(Argument(orderOnMember), Argument(orderOnParameter)))
            {
                diags.Add(DiagnosticInfo.Create("CSVM004", AttributeLocation(orderOnParameter) ?? location, model.Name,
                    $"conflicting [CsvOrder] on member and constructor parameter '{symbol.Name}'"));
            }

            if (columnOnMember is not null && columnOnParameter is not null
                && !string.Equals(Argument(columnOnMember) as string, Argument(columnOnParameter) as string, StringComparison.OrdinalIgnoreCase))
            {
                diags.Add(DiagnosticInfo.Create("CSVM004", AttributeLocation(columnOnParameter) ?? location, model.Name,
                    $"conflicting [CsvColumn] on member and constructor parameter '{symbol.Name}'"));
            }

            if (parameter is null && !IsAssignable(member!))
            {
                diags.Add(DiagnosticInfo.Create("CSVM005", location, symbol.Name));
            }

            if (order is < 0)
            {
                diags.Add(DiagnosticInfo.Create("CSVM004", AttributeLocation(orderAttribute) ?? location, model.Name, $"negative [CsvOrder] index {order}"));
            }
            else if (order is not null && !orders.Add(order.Value))
            {
                diags.Add(DiagnosticInfo.Create("CSVM004", AttributeLocation(orderAttribute) ?? location, model.Name, $"duplicate [CsvOrder] index {order}"));
            }

            if (columnAttribute is not null && string.IsNullOrWhiteSpace(column))
            {
                diags.Add(DiagnosticInfo.Create("CSVM004", AttributeLocation(columnAttribute) ?? location, model.Name, $"empty [CsvColumn] name on '{symbol.Name}'"));
            }
            else if (column is not null && !columns.Add(column))
            {
                diags.Add(DiagnosticInfo.Create("CSVM004", AttributeLocation(columnAttribute) ?? location, model.Name, $"duplicate [CsvColumn] name '{column}'"));
            }

            // A constructor-assigned value has the parameter's type.
            var type = parameter?.Type ?? (member is IPropertySymbol p ? p.Type : ((IFieldSymbol)member!).Type);
            // An undefined `T?` binds as Nullable<error type>.
            if (type.TypeKind == TypeKind.Error
                || type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } n && n.TypeArguments[0].TypeKind == TypeKind.Error)
            {
                hasErrorType = true; // the compiler already reports it
                continue;
            }

            ParseKind kind;
            bool isNullable = false;
            string? underlying = null, converterName = null;
            if (converterAttribute is not null)
            {
                var converter = converterAttribute.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;
                var error = ConverterError(converter, type, c);
                if (error is not null)
                {
                    diags.Add(DiagnosticInfo.Create("CSVM008", AttributeLocation(converterAttribute) ?? location,
                        converter?.ToDisplayString() ?? "?", error));
                }
                kind = ParseKind.Converter;
                // So a missing column gives null; an empty field still goes to the converter.
                isNullable = type.NullableAnnotation == NullableAnnotation.Annotated
                    || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
                converterName = converter?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
            else if (ParseKindClassifier.Classify(type, c) is { } classified)
            {
                (kind, isNullable, underlying) = classified;
            }
            else
            {
                diags.Add(DiagnosticInfo.Create("CSVM003", location, symbol.Name, type.ToDisplayString()));
                continue;
            }

            members.Add(new MemberInfo(
                member?.Name ?? parameter!.Name,
                type.ToDisplayString(ParserAnalyzer.TypeFormat),
                column,
                order ?? -1,
                kind,
                underlying,
                isNullable,
                converterName,
                parameter is null ? MemberAssignment.Initializer : MemberAssignment.Constructor,
                parameter?.Ordinal ?? -1,
                isRequired));
        }

        if (hasErrorType) return null;

        if (members.Count == 0 && !diags.Skip(start).Any())
        {
            diags.Add(DiagnosticInfo.Create("CSVM010", typeLocation, model.Name));
        }

        if (diags.Skip(start).Any(d => Descriptors.Get(d.Id).DefaultSeverity == DiagnosticSeverity.Error)) return null;

        var delimiter = model.GetAttributes().FirstOrDefault(a => Is(a, "CsvMole.CsvDelimiterAttribute"))
            ?.ConstructorArguments.FirstOrDefault().Value as char?;

        return new ModelInfo(
            model.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(ParserAnalyzer.TypeFormat),
            delimiter,
            members.ToImmutable().AsEquatableArray(),
            new ConstructorInfo(
                ctor.Parameters.Select(p => p.Type.ToDisplayString(ParserAnalyzer.TypeFormat)).ToImmutableArray().AsEquatableArray(),
                ctor.Parameters.Select(p => p.Name).ToImmutableArray().AsEquatableArray(),
                setsRequiredMembers));
    }

    private static IMethodSymbol? ChooseConstructor(ITypeSymbol model, List<DiagnosticInfo> diags)
    {
        var location = model.Locations.FirstOrDefault();
        if (model.TypeKind == TypeKind.Error) return null; // the compiler already reports it
        if (model is not INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct, IsAbstract: false } type)
        {
            diags.Add(DiagnosticInfo.Create("CSVM007", location, model.Name, "it must be a non-abstract class or struct"));
            return null;
        }

        // A struct's implicit parameterless constructor is only used when it declares no other public one.
        var candidates = type.InstanceConstructors
            .Where(k => k.DeclaredAccessibility == Accessibility.Public && !(type.IsValueType && k.IsImplicitlyDeclared))
            .ToArray();
        if (candidates.Length == 0 && type.IsValueType
            && type.InstanceConstructors.FirstOrDefault(k => k.IsImplicitlyDeclared) is { } implicitCtor) return implicitCtor;
        if (candidates.Length == 1) return candidates[0];
        if (candidates.Length == 0)
        {
            diags.Add(DiagnosticInfo.Create("CSVM007", location, model.Name, "it has no public constructor"));
            return null;
        }

        var marked = candidates.Where(k => k.GetAttributes().Any(a => Is(a, "CsvMole.CsvConstructorAttribute"))).ToArray();
        if (marked.Length == 1) return marked[0];
        diags.Add(DiagnosticInfo.Create("CSVM007", location, model.Name, marked.Length == 0
            ? $"it has several public constructors and none is marked [CsvConstructor] (candidates: {string.Join(", ", candidates.Select(k => k.ToDisplayString()))})"
            : "several constructors are marked [CsvConstructor]"));
        return null;
    }

    // Public instance properties and fields, most-derived first; a hidden/overridden base member is skipped by name.
    private static IEnumerable<ISymbol> PublicInstanceMembers(ITypeSymbol model)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var t = model; t is not null && t.SpecialType is not (SpecialType.System_Object or SpecialType.System_ValueType); t = t.BaseType)
        {
            foreach (var m in t.GetMembers())
            {
                if (m is IPropertySymbol { IsStatic: false, IsIndexer: false, DeclaredAccessibility: Accessibility.Public }
                        or IFieldSymbol { IsStatic: false, DeclaredAccessibility: Accessibility.Public }
                    && seen.Add(m.Name))
                {
                    yield return m;
                }
            }
        }
    }

    private static bool IsAssignable(ISymbol member) => member switch
    {
        IPropertySymbol p => p.SetMethod is { DeclaredAccessibility: Accessibility.Public },
        IFieldSymbol f => !f.IsReadOnly && !f.IsConst,
        _ => false,
    };

    private static string? ConverterError(INamedTypeSymbol? converter, ITypeSymbol memberType, Compilation c)
    {
        if (converter is null || converter.TypeKind == TypeKind.Error) return "it is not a valid type";
        if (converter.IsUnboundGenericType) return "open generic types are not supported";
        if (converter.IsAbstract) return "it is abstract";

        var converterBase = c.GetTypeByMetadataName("CsvMole.CsvConverter`1");
        var found = converter.BaseType;
        while (found is not null && !SymbolEqualityComparer.Default.Equals(found.OriginalDefinition, converterBase)) found = found.BaseType;
        if (found is null) return "it must derive from CsvMole.CsvConverter<T>";

        // Default comparison: int? and int differ, reference-type nullability is ignored.
        if (!SymbolEqualityComparer.Default.Equals(found.TypeArguments[0], memberType))
        {
            return $"it must derive from CsvMole.CsvConverter<{memberType.ToDisplayString()}>, not {found.ToDisplayString()}";
        }

        if (!converter.InstanceConstructors.Any(k => k.Parameters.IsEmpty && k.DeclaredAccessibility == Accessibility.Public))
        {
            return "it must have a public parameterless constructor";
        }
        return null;
    }

    // A property's own attributes first, then those of the properties it overrides.
    private static ImmutableArray<AttributeData> MemberAttributes(ISymbol? member)
    {
        if (member is not IPropertySymbol property) return member?.GetAttributes() ?? ImmutableArray<AttributeData>.Empty;
        var builder = ImmutableArray.CreateBuilder<AttributeData>();
        for (var p = property; p is not null; p = p.OverriddenProperty) builder.AddRange(p.GetAttributes());
        return builder.ToImmutable();
    }

    private static AttributeData? Find(ImmutableArray<AttributeData> attributes, string fullName) =>
        attributes.FirstOrDefault(a => Is(a, fullName));

    private static object? Argument(AttributeData? attribute) =>
        attribute is { ConstructorArguments.Length: > 0 } ? attribute.ConstructorArguments[0].Value : null;

    private static Location? AttributeLocation(AttributeData? attribute) => attribute?.ApplicationSyntaxReference?.GetSyntax().GetLocation();

    private static bool Is(AttributeData attribute, string fullName) => attribute.AttributeClass?.ToDisplayString() == fullName;
}
