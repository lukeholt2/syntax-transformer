using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SyntaxTransformer.Transformers
{
    /// <summary>
    /// Transformer used to replace instances of the `var` keyword with explicit type
    /// </summary>
    public class ReplaceVarTransformer : CSharpSyntaxRewriter
    {
        /// <summary>
        /// The semantic model containing the parsed source code
        /// </summary>
        private readonly SemanticModel SemanticModel;

        /// <summary>
        /// Construct a new <see cref="ReplaceVarTransformer"/>
        /// </summary>
        /// <param name="semanticModel">
        /// The model containing the parsed souce code to be evaluated.
        /// </param>
        public ReplaceVarTransformer(SemanticModel semanticModel) => SemanticModel = semanticModel;

        /// <inheritdoc cref="CSharpSyntaxRewriter.VisitForEachStatement"/>
        public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
        {
            TypeSyntax variableTypeName = node.Type;
            if (node.Expression is not MemberAccessExpressionSyntax memberSyntax)
                return node;

            ITypeSymbol? typeInfo = (SemanticModel.GetTypeInfo(memberSyntax.Name).Type as INamedTypeSymbol)?.TypeArguments.First();

            TypeSyntax varTypeName = IdentifierName(typeInfo?.Name ?? string.Empty)
                   .WithLeadingTrivia(variableTypeName.GetLeadingTrivia())
                   .WithTrailingTrivia(variableTypeName.GetTrailingTrivia());

            if(node.Statement is BlockSyntax block)
            {
                StatementSyntax TryReplace(StatementSyntax node) => node is LocalDeclarationStatementSyntax local
                    ? ReplaceVarCommon(local)
                    : node;

                SyntaxList<StatementSyntax> statements = new(block.Statements.Select(TryReplace));
                BlockSyntax newBlock = Block(block.OpenBraceToken, statements, block.CloseBraceToken);
                node = node.WithStatement(newBlock);
            }

            return node.WithType(varTypeName);
        }


         /// <inheritdoc cref="CSharpSyntaxRewriter.VisitForStatement"/>
        public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
        {
            VariableDeclaratorSyntax? declarator = node.Declaration?.Variables.FirstOrDefault();
            TypeSyntax? variableTypeName = node.Declaration?.Type;

            if (variableTypeName is null || !variableTypeName.IsVar)
                return node;

            ITypeSymbol? variableType = SemanticModel.GetSymbolInfo(variableTypeName).Symbol as ITypeSymbol;

            TypeInfo? initializerInfo = declarator?.Initializer?.Value is not null 
                                                ? SemanticModel.GetTypeInfo(declarator.Initializer.Value) 
                                                : null;

            if (variableType is not null && variableType.Equals(initializerInfo?.Type, SymbolEqualityComparer.Default))
            {
                TypeSyntax varTypeName = IdentifierName(variableType.ToDisplayString())
                    .WithLeadingTrivia(variableTypeName.GetLeadingTrivia())
                    .WithTrailingTrivia(variableTypeName.GetTrailingTrivia());

                VariableDeclaratorSyntax fixedDeclarator = VariableDeclarator(declarator!.Identifier, declarator.ArgumentList, declarator.Initializer);
                return node.WithDeclaration(VariableDeclaration(varTypeName).AddVariables(fixedDeclarator));
            }

            return node;
        }

         /// <inheritdoc cref="CSharpSyntaxRewriter.VisitLocalDeclarationStatement"/>
        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node) => ReplaceVarCommon(node);

        /// <summary>
        /// Performs the actual replacement of the 'var' node.
        /// Evaluates a variable declaration to determine if it is defined as a 'var'
        /// </summary>
        /// <param name="node">
        /// The Syntax Node to be evaluated
        /// </param>
        /// <returns>
        /// The updated LocalDeclarationStatementSyntax node
        /// </returns>
        private LocalDeclarationStatementSyntax ReplaceVarCommon(LocalDeclarationStatementSyntax node)
        {
             if (node.Declaration.Variables.Count > 1)
                return node;
            if (node.Declaration.Variables[0].Initializer == null)
                return node;

            VariableDeclaratorSyntax declarator = node.Declaration.Variables.First();
            TypeSyntax variableTypeName = node.Declaration.Type;

            if (!variableTypeName.IsVar)
                return node;

            ITypeSymbol? variableType = SemanticModel
                .GetSymbolInfo(variableTypeName)
                .Symbol as ITypeSymbol;
            
            TypeInfo? initializerInfo = declarator?.Initializer?.Value is not null 
                                                ? SemanticModel.GetTypeInfo(declarator.Initializer.Value) 
                                                : null;
            
            if (variableType is not null && variableType.Equals(initializerInfo?.Type ?? initializerInfo?.ConvertedType, SymbolEqualityComparer.Default))
            {
                TypeSyntax varTypeName = IdentifierName(variableType.ToMinimalDisplayString(SemanticModel, declarator?.GetLocation().SourceSpan.Start ?? 0))
                    .WithLeadingTrivia(variableTypeName.GetLeadingTrivia())
                    .WithTrailingTrivia(variableTypeName.GetTrailingTrivia());

                return node.ReplaceNode(variableTypeName, varTypeName);
            }

            return node;
        }
    }
}


