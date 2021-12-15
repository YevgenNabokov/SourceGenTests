using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestGenerators.FirstGenerator
{
    [Generator]
    public class FirstGen : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor NotPartialClassWarning = new DiagnosticDescriptor(id: "TESTGEN01",
                                                                                              title: "Class should be marked as partial in order to be extended",
                                                                                              messageFormat: "Class '{0}' should be marked as partial in order to be extended",
                                                                                              category: "FirstGen",
                                                                                              DiagnosticSeverity.Warning,
                                                                                              isEnabledByDefault: true);

        public void Execute(GeneratorExecutionContext context)
        {
            var classes = context.Compilation.SyntaxTrees.SelectMany(t => t.GetRoot().DescendantNodes().Where(d => d.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration)))
                .OfType<ClassDeclarationSyntax>()
                .ToArray();

            var classSymbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            var nsSymbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

            foreach (var classDefinition in classes)
            {
                var semanticModel = context.Compilation.GetSemanticModel(classDefinition.SyntaxTree);

                var classSymbol = semanticModel.GetDeclaredSymbol(classDefinition) as INamedTypeSymbol;

                if (classSymbol.GetAttributes().Any(a => a.AttributeClass.ToDisplayString(classSymbolDisplayFormat) == "CommonLib.Attributes.SomeMarkerAttribute"))
                {
                    if (!classDefinition.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(NotPartialClassWarning, classDefinition.GetLocation(), classSymbol.Name));
                        return;
                    }

                    var sourceCode = $@"
namespace {classSymbol.ContainingNamespace.ToDisplayString(nsSymbolDisplayFormat)}
{{
    public partial class {classSymbol.Name}
    {{
        public int SuperProperty
        {{
            get;
            set;
        }}
    }}
}}";

                    context.AddSource($"{classSymbol.Name}.Generated.cs", SourceText.From(sourceCode, Encoding.UTF8));
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
