using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace WiredBrainCoffee.Generators;

[Generator]
public class ToStringGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => IsSyntaxTarget(node),
            transform: static (ctx, _) => GetSemanticTarget(ctx))
			.Where(static (target) => target is not null);

        context.RegisterSourceOutput(classes, static (ctx, source) => Execute(ctx, source!));

        context.RegisterPostInitializationOutput(
            static (ctx) => PostInitializationOutput(ctx));
    }

    private static ClassDeclarationSyntax? GetSemanticTarget(
		GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
		foreach (var attributeList in classDeclarationSyntax.AttributeLists)
		{
			foreach (var attribute in attributeList.Attributes)
			{
				var attributeName = attribute.Name.ToString();

				if (attributeName == "GenerateToString" ||
					attributeName == "GenerateToStringAttribute")
				{
					return classDeclarationSyntax;
				}
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
		ClassDeclarationSyntax source)
	{
		if (source.Parent is BaseNamespaceDeclarationSyntax namespaceDeclaration)
		{
			string namespaceName = namespaceDeclaration.Name.ToString();

			var className = source.Identifier.Text;
			var fileName = $"{namespaceName}.{className}.g.cs";

			var stringBuilder = new StringBuilder();
			stringBuilder.Append($@"namespace {namespaceName}
{{
	partial class {className} 
	{{
		public override string ToString()
		{{
			return $""");

			foreach (var member in source.Members)
			{
				if (member is PropertyDeclarationSyntax property &&
					property.Modifiers.Any(SyntaxKind.PublicKeyword))
				{
					var propertyName = property.Identifier.Text;
					stringBuilder.Append($"{propertyName}: {{{propertyName}}}; ");
				}
			}
			
			stringBuilder.Append($@""";
		}}
	}}
}}
");

			ctx.AddSource(fileName, stringBuilder.ToString());
		}
	}
}
