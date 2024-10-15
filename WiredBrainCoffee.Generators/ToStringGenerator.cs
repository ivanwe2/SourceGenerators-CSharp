using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using WiredBrainCoffee.Generators.Model;

namespace WiredBrainCoffee.Generators;

[Generator]
public class ToStringGenerator : IIncrementalGenerator
{
    private static Dictionary<string, int> _countPerFileName = [];
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => IsSyntaxTarget(node),
            transform: static (ctx, _) => GetSemanticTarget(ctx))
            .Where(static (target) => target is not null);

        context.RegisterSourceOutput(classes, static (ctx, source) => Execute(ctx, source));

        context.RegisterPostInitializationOutput(
            static (ctx) => PostInitializationOutput(ctx));
    }

    private static ClassToGenerate? GetSemanticTarget(
        GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        var attributeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName(
            "WiredBrainCoffee.Generators.GenerateToStringAttribute");

        if (classSymbol is null ||
            attributeSymbol is null)
        {
            return null;
        }

        foreach (var attributeData in classSymbol.GetAttributes())
        {
            if (attributeSymbol.Equals(attributeData.AttributeClass,
                SymbolEqualityComparer.Default))
            {
                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                var className = classSymbol.Name;
                var propertyNames = new List<string>();

                foreach (var memberSymbol in classSymbol.GetMembers())
                {
                    if (memberSymbol.Kind == SymbolKind.Property &&
                        memberSymbol.DeclaredAccessibility == Accessibility.Public)
                    {
                        propertyNames.Add(memberSymbol.Name);
                    }
                }

                return new ClassToGenerate(namespaceName, className, propertyNames);
            }
        }

        return null;
    }

    private static bool IsSyntaxTarget(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclaration
            && classDeclaration.AttributeLists.Count > 0;
    }

    private static void PostInitializationOutput(IncrementalGeneratorPostInitializationContext ctx)
    {
        ctx.AddSource(
            "WiredBrainCoffee.Generators.GenerateToStringAttribute.g.cs",
            @"namespace WiredBrainCoffee.Generators
{
	internal class GenerateToStringAttribute : System.Attribute {}
}");
    }

    private static void Execute(
        SourceProductionContext ctx,
        ClassToGenerate? classToGenerate)
    {
        if (classToGenerate == null)
            return;

        string namespaceName = classToGenerate.NamespaceName;

        var className = classToGenerate.ClassName;
        var fileName = $"{namespaceName}.{className}.g.cs";

        if (_countPerFileName.ContainsKey(fileName))
        {
            _countPerFileName[fileName]++;
        }
        else
        {
            _countPerFileName.Add(fileName, 1);
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.Append($@"// Generation count: {_countPerFileName[fileName]}
namespace {namespaceName}
{{
	partial class {className} 
	{{
		public override string ToString()
		{{
			return $""");

        foreach (var propertyName in classToGenerate.Properties)
        {
            stringBuilder.Append($"{propertyName}: {{{propertyName}}}; ");
        }

        stringBuilder.Append($@""";
		}}
	}}
}}
");

        ctx.AddSource(fileName, stringBuilder.ToString());
    }
}
