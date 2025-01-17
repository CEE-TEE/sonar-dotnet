﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2022 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Extensions;
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class GenericTypeParameterUnused : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S2326";
        private const string MessageFormat = "'{0}' is not used in the {1}.";

        private static readonly DiagnosticDescriptor Rule = DescriptorFactory.Create(DiagnosticId, MessageFormat);
        private static readonly SyntaxKind[] MethodModifiersToSkip =
        {
            SyntaxKind.AbstractKeyword,
            SyntaxKind.VirtualKeyword,
            SyntaxKind.OverrideKeyword
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
                {
                    if (c.SemanticModel.GetDeclaredSymbol(c.Node) is { } declarationSymbol)
                    {
                        CheckGenericTypeParameters(c, declarationSymbol);
                    }
                },
                SyntaxKind.MethodDeclaration,
                SyntaxKindEx.LocalFunctionStatement);

            context.RegisterSyntaxNodeActionInNonGenerated(c =>
                 {
                     if (!c.IsRedundantPositionalRecordContext())
                     {
                         CheckGenericTypeParameters(c, c.ContainingSymbol);
                     }
                 },
                 SyntaxKind.ClassDeclaration,
                 SyntaxKind.InterfaceDeclaration,
                 SyntaxKindEx.RecordClassDeclaration,
                 SyntaxKindEx.RecordStructDeclaration,
                 SyntaxKind.StructDeclaration);
        }

        private static void CheckGenericTypeParameters(SyntaxNodeAnalysisContext c, ISymbol symbol)
        {
            var info = CreateParametersInfo(c);
            if (info?.Parameters is null || info.Parameters.Parameters.Count == 0)
            {
                return;
            }
            var typeParameterNames = info.Parameters.Parameters.Select(x => x.Identifier.Text).ToArray();
            var usedTypeParameters = GetUsedTypeParameters(symbol.DeclaringSyntaxReferences.Select(x => x.GetSyntax()), typeParameterNames, c).ToHashSet();
            foreach (var typeParameter in typeParameterNames.Where(x => !usedTypeParameters.Contains(x)))
            {
                c.ReportIssue(Diagnostic.Create(Rule, info.Parameters.Parameters.First(x => x.Identifier.Text == typeParameter).GetLocation(), typeParameter, info.ContainerName));
            }
        }

        private static ParametersInfo CreateParametersInfo(SyntaxNodeAnalysisContext c) =>
            c.Node switch
            {
                InterfaceDeclarationSyntax interfaceDeclaration => new ParametersInfo(interfaceDeclaration.TypeParameterList, "interface"),
                ClassDeclarationSyntax classDeclaration => new ParametersInfo(classDeclaration.TypeParameterList, "class"),
                StructDeclarationSyntax structDeclaration => new ParametersInfo(structDeclaration.TypeParameterList, "struct"),
                MethodDeclarationSyntax methodDeclaration when IsMethodCandidate(methodDeclaration, c.SemanticModel) => new ParametersInfo(methodDeclaration.TypeParameterList, "method"),
                var wrapper when LocalFunctionStatementSyntaxWrapper.IsInstance(wrapper) => new ParametersInfo(((LocalFunctionStatementSyntaxWrapper)c.Node).TypeParameterList, "local function"),
                var wrapper when RecordDeclarationSyntaxWrapper.IsInstance(wrapper) => new ParametersInfo(((RecordDeclarationSyntaxWrapper)c.Node).TypeParameterList, "record"),
                _ => null
            };

        private static bool IsMethodCandidate(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel) =>
            !methodDeclaration.Modifiers.Any(x => MethodModifiersToSkip.Contains(x.Kind()))
            && methodDeclaration.ExplicitInterfaceSpecifier is null
            && methodDeclaration.HasBodyOrExpressionBody()
            && semanticModel.GetDeclaredSymbol(methodDeclaration) is { } methodSymbol
            && methodSymbol.IsChangeable();

        private static List<string> GetUsedTypeParameters(IEnumerable<SyntaxNode> declarations, string[] typeParameterNames, SyntaxNodeAnalysisContext context) =>
            declarations.SelectMany(x => x.DescendantNodes())
                .OfType<IdentifierNameSyntax>()
                .Where(x => x.Parent is not TypeParameterConstraintClauseSyntax && typeParameterNames.Contains(x.Identifier.ValueText))
                .Select(x => x.EnsureCorrectSemanticModelOrDefault(context.SemanticModel)?.GetSymbolInfo(x).Symbol)
                .Where(x => x is { Kind: SymbolKind.TypeParameter })
                .Select(x => x.Name)
                .ToList();

        private sealed record ParametersInfo(TypeParameterListSyntax Parameters, string ContainerName);
    }
}
