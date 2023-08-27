using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SyntaxTransformer
{
    internal static class Utilities
    {
        /// <summary>
        /// Finds all types deriving from the provided base type
        /// </summary>
        /// <returns>
        /// Collection of types that are derivatives of T
        /// </returns>
        public static List<Type>? FindAllDerivedTypes<T>()
        {
            Type derivedType = typeof(T);
            return Assembly.GetAssembly(typeof(T))
                ?.GetTypes()
                .Where(t => t != derivedType && derivedType.IsAssignableFrom(t)).ToList();

        }

        /// <summary>
        /// Find the specified types using the specified string
        /// </summary>
        public static Type? FindMatchingType<T1, T2>(string typeName) where T1 : class where T2 : class
        {
            return FindAllDerivedTypes<T1>()?.Find(t => t.Name.Equals(typeName)) ?? FindAllDerivedTypes<T2>()?.Find(t => t.Name.Equals(typeName));
        }


        /// <summary>
        /// Create an Attribute Node object
        /// </summary>
        /// <param name="type">
        /// The type for which to create attribute
        /// </param>
        /// <param name="parameters">
        /// Parameters to be passed to be used in the attribute contruction
        /// </param>
        /// <returns>
        /// Newly created <see cref="AttributeSyntax" instance/>
        /// </returns>
        public static AttributeSyntax CreateAttribute(Type type, string? parameters = null)
        {          

            NameSyntax name = SyntaxFactory.ParseName(type.Name.Replace("Attribute", string.Empty));
            AttributeArgumentListSyntax? arguments = parameters is null ? null : SyntaxFactory.ParseAttributeArgumentList(parameters);
            AttributeSyntax attribute = SyntaxFactory.Attribute(name, arguments);
            return attribute;
        }

         /// <summary>
        /// Create an Attribute Node object
        /// </summary>
        /// <param name="parameters">
        /// Parameters to be passed to be used in the attribute contruction
        /// </param>
        /// <returns>
        /// Newly created <see cref="AttributeSyntax" instance/>
        /// </returns>
        public static AttributeSyntax CreateAttribute<TAttribute>(string? parameters = null) where TAttribute : Attribute
        {
            return CreateAttribute(typeof(TAttribute), parameters);
        }

    }
}