using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using static SyntaxTransformer.Utilities;

namespace SyntaxTransformer.Transformers
{
    /// <summary>
    /// Transformer used to generated/add API attributes to controller classes
    /// </summary>
    public class APIAttributeTransformer : CSharpSyntaxRewriter
    {

        /// <summary>
        /// Model containing the parsed source code to be evaluated
        /// </summary>
        private readonly SemanticModel SemanticModel;

        /// <summary>
        /// Collection of attributes to be added to method declarations
        /// </summary>
        private readonly List<Type> _requestResultAttributes = new();

        /// <summary>
        /// Construct a new <see cref="APIAttributeTransformer"/>
        /// </summary>
        /// <param name="semanticModel"></param>
        public APIAttributeTransformer(SemanticModel semanticModel) => SemanticModel = semanticModel;

        /// <inheritdoc cref="CSharpSyntaxRewriter.VisitMethodDeclaration"/>
        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            base.VisitMethodDeclaration(node);
            if(_requestResultAttributes.Count > 0)
                node = node.WithAttributeLists(new SyntaxList<AttributeListSyntax>(EnumerateAttributes(node.AttributeLists)));
            _requestResultAttributes.Clear();
            return node;
        }

         /// <inheritdoc cref="CSharpSyntaxRewriter.VisitClassStatement"/>
        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.BaseList is not null && node.BaseList.Types.Any(b => b.Type is not null && SemanticModel.GetTypeInfo(b.Type).Type?.ToDisplayString() == nameof(ControllerBase)))
            {
                bool isSame(AttributeListSyntax a1, AttributeListSyntax a2) => a1.GetText().ToString().Trim().Equals(a2.GetText().ToString().Trim());
                IEnumerable<AttributeListSyntax> attributesToAdd = APIClassAttributes().Where(attr => !node.AttributeLists.Any(at => !(isSame(at, attr))));
                SyntaxList<AttributeListSyntax> updatedAttributes = node.AttributeLists.AddRange(attributesToAdd);
                node = node.WithAttributeLists(new SyntaxList<AttributeListSyntax>(updatedAttributes));
            }
            return base.VisitClassDeclaration(node);
        }

        /// <inheritdoc cref="CSharpSyntaxRewriter.VisitReturnStatement"/>
        public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (node.Expression is ObjectCreationExpressionSyntax expr)
            {
                VerifyObjectCreationSyntax(expr);
            }
            else if (node.Expression is ConditionalExpressionSyntax cond)
            {
                VerifyObjectCreationSyntax(cond.WhenFalse as ObjectCreationExpressionSyntax);
                VerifyObjectCreationSyntax(cond.WhenTrue as ObjectCreationExpressionSyntax);
            }
            else if (node.Expression is InvocationExpressionSyntax invoke && invoke.Expression is IdentifierNameSyntax name)
            {
                string suffix = invoke.ArgumentList.Arguments.Count > 0 ? "Object" : string.Empty;
                VerifyInternal($"{name.Identifier.ValueText}{suffix}Result".Trim());
            }
            return base.VisitReturnStatement(node);
        }


        /// <summary>
        /// Enumerate the current attributes, appending new ones as needed
        /// </summary>
        /// <param name="currentList">
        /// Collection of currently used attributes
        /// </param>
        /// <returns>
        /// Collection of attribute lists to be used on a node
        /// </returns>
        private IEnumerable<AttributeListSyntax> EnumerateAttributes(SyntaxList<AttributeListSyntax> currentList)
        {
            IEnumerable<AttributeListSyntax> combinedList = new List<AttributeListSyntax>(currentList);
            foreach (AttributeSyntax attribute in _requestResultAttributes.Distinct().Select(CreateResultAttribute))
            {
                AttributeListSyntax toAdd = SyntaxFactory.AttributeList(new SeparatedSyntaxList<AttributeSyntax>().Add(attribute));
                if (!currentList.Any(a => a.GetText().ToString().Trim().Equals(toAdd.GetText().ToString().Trim())))
                    combinedList = combinedList.Append(toAdd);
            }
            return combinedList;

        }

        /// <summary>
        /// Verifies that a node represents an object creation expression
        /// </summary>
        /// <param name="expr">
        /// The Expression to be evaluated
        /// </param>
        private void VerifyObjectCreationSyntax(ObjectCreationExpressionSyntax? expr)
        {
            if (expr is null)
                return;

            VerifyInternal(expr.Type.GetText().ToString());
        }

        private void VerifyInternal(string rawName)
        {
            if (FindMatchingType<StatusCodeResult, ObjectResult>(rawName) is Type realType)
                _requestResultAttributes.Add(realType);
        }


        /// <summary>
        /// Create a Result attribute based on the provided type
        /// </summary>
        /// <param name="type">
        /// The type value for which to create the result attribute
        /// </param>
        /// <returns>
        /// The newly created attribute node
        /// </returns>
        private static AttributeSyntax CreateResultAttribute(Type type)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.GetProperty | BindingFlags.Instance;
            object? value = type.BaseType is not null && !type.BaseType.Equals(typeof(ObjectResult))
                                        ? Activator.CreateInstance(type)
                                        : Activator.CreateInstance(type, new object());
            int? statusCode = Convert.ToInt32(type.GetProperty("StatusCode", flags)?.GetValue(value));
            return CreateAttribute<ProducesResponseTypeAttribute>($"(typeof({type.Name}), {statusCode})");
        }

        /// <summary>
        /// Generate a list of attributes to be used on class declarations
        /// </summary>
        /// <returns>
        /// Collection of created attribute objects
        /// </returns>
        private static List<AttributeListSyntax> APIClassAttributes()
        {
            return new List<AttributeListSyntax>()
            {
                SyntaxFactory.AttributeList(new SeparatedSyntaxList<AttributeSyntax>().Add(CreateAttribute<AuthorizeAttribute>())),
                SyntaxFactory.AttributeList(new SeparatedSyntaxList<AttributeSyntax>().Add(CreateAttribute<ApiControllerAttribute>())),
                SyntaxFactory.AttributeList(new SeparatedSyntaxList<AttributeSyntax>().Add(CreateAttribute<RouteAttribute>($"(\"api/[controller]\")")))
            };
        }

       
    }
}