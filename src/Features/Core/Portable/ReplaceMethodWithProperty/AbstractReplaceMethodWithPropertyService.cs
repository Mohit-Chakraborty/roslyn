﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.ReplaceMethodWithProperty
{
    internal abstract class AbstractReplaceMethodWithPropertyService<TMethodDeclarationSyntax> where TMethodDeclarationSyntax : SyntaxNode
    {
#pragma warning disable CA1822 // Mark members as static - implements interface method for sub-types.
        public async Task<SyntaxNode?> GetMethodDeclarationAsync(CodeRefactoringContext context)
#pragma warning restore CA1822 // Mark members as static
            => await context.TryGetRelevantNodeAsync<TMethodDeclarationSyntax>().ConfigureAwait(false);

        protected static string? GetWarning(GetAndSetMethods getAndSetMethods)
        {
            if (OverridesMetadataSymbol(getAndSetMethods.GetMethod) ||
                OverridesMetadataSymbol(getAndSetMethods.SetMethod))
            {
                return FeaturesResources.Warning_Method_overrides_symbol_from_metadata;
            }

            return null;
        }

        private static bool OverridesMetadataSymbol(IMethodSymbol method)
        {
            for (var current = method; current != null; current = current.OverriddenMethod)
            {
                if (current.Locations.Any(loc => loc.IsInMetadata))
                {
                    return true;
                }
            }

            return false;
        }

        protected static TPropertyDeclaration SetLeadingTrivia<TPropertyDeclaration>(
            ISyntaxFacts syntaxFacts, GetAndSetMethods getAndSetMethods, TPropertyDeclaration property) where TPropertyDeclaration : SyntaxNode
        {
            var getMethodDeclaration = getAndSetMethods.GetMethodDeclaration;
            var setMethodDeclaration = getAndSetMethods.SetMethodDeclaration;
            var finalLeadingTrivia = getAndSetMethods.GetMethodDeclaration.GetLeadingTrivia().ToList();
            var paramList = syntaxFacts.GetParameterList(getMethodDeclaration);

            //If there is a comment on the same line as the method it is contained in trailing trivia for the parameter list
            //If it's there we need to add it to the final comments
            //this is to fix issue 42699, https://github.com/dotnet/roslyn/issues/42699
            if (paramList.GetTrailingTrivia().Any(t => !syntaxFacts.IsWhitespaceOrEndOfLineTrivia(t)))
            {
                //we have a meaningful comment on the parameter list so add it to the trivia list
                finalLeadingTrivia.AddRange(paramList.GetTrailingTrivia());
            }

            if (setMethodDeclaration == null)
            {
                return property.WithLeadingTrivia(finalLeadingTrivia);
            }

            finalLeadingTrivia.AddRange(
                setMethodDeclaration.GetLeadingTrivia()
                                    .SkipWhile(t => syntaxFacts.IsEndOfLineTrivia(t))
                                    .Where(t => !t.IsDirective));

            //If there is a comment on the same line as the method it is contained in trailing trivia for the parameter list
            //If it's there we need to add it to the final comments
            paramList = syntaxFacts.GetParameterList(setMethodDeclaration);
            if (paramList.GetTrailingTrivia().Any(t => !syntaxFacts.IsWhitespaceOrEndOfLineTrivia(t)))
            {
                //we have a meaningful comment on the parameter list so add it to the trivia list
                finalLeadingTrivia.AddRange(paramList.GetTrailingTrivia());
            }

            return property.WithLeadingTrivia(finalLeadingTrivia);
        }
    }
}
