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

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.VisualBasic
{
    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    public sealed class ParameterAssignedTo : ParameterAssignedToBase<SyntaxKind, IdentifierNameSyntax>
    {
        protected override ILanguageFacade<SyntaxKind> Language => VisualBasicFacade.Instance;

        protected override bool IsAssignmentToCatchVariable(ISymbol symbol, SyntaxNode node) =>
            // This could mimic the C# variant too, but that doesn't work https://github.com/dotnet/roslyn/issues/6209
            symbol is ILocalSymbol localSymbol
            && localSymbol.Locations.FirstOrDefault() is { } location
            && node.SyntaxTree.GetRoot().FindNode(location.SourceSpan, getInnermostNodeForTie: true) is IdentifierNameSyntax declarationName
            && declarationName.Parent is CatchStatementSyntax catchStatement
            && catchStatement.IdentifierName == declarationName;
    }
}
