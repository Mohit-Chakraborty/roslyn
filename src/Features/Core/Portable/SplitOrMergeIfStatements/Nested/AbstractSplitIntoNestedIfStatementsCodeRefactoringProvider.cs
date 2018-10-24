﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractSplitIntoNestedIfStatementsCodeRefactoringProvider
        : AbstractSplitIfStatementCodeRefactoringProvider
    {
        protected sealed override int GetLogicalExpressionKind(ISyntaxKindsService syntaxKinds)
            => syntaxKinds.LogicalAndExpression;

        protected sealed override CodeAction CreateCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
            => new MyCodeAction(createChangedDocument, ifKeywordText);

        protected sealed override Task<SyntaxNode> GetChangedRootAsync(
            Document document,
            SyntaxNode root,
            SyntaxNode ifLikeStatement,
            SyntaxNode leftCondition,
            SyntaxNode rightCondition,
            CancellationToken cancellationToken)
        {
            var ifSyntaxService = document.GetLanguageService<IIfStatementSyntaxService>();

            var innerIfStatement = ifSyntaxService.WithCondition(ifSyntaxService.ToIfStatement(ifLikeStatement), rightCondition);
            var outerIfLikeStatement = ifSyntaxService.WithCondition(ifSyntaxService.WithStatement(ifLikeStatement, innerIfStatement), leftCondition);

            return Task.FromResult(
                root.ReplaceNode(ifLikeStatement, outerIfLikeStatement.WithAdditionalAnnotations(Formatter.Annotation)));
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string ifKeywordText)
                : base(string.Format(FeaturesResources.Split_into_nested_0_statements, ifKeywordText), createChangedDocument)
            {
            }
        }
    }
}
