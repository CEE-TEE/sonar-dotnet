﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2021 SonarSource SA
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

extern alias csharp;

using System.Collections.Generic;
using System.Linq;
using csharp::SonarAnalyzer.Rules.CSharp;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Protobuf;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class FileMetadataAnalyzerTest
    {
        [TestMethod]
        [TestCategory("Rule")]
        public void FileMetadataAnalyzer_Autogenerated()
        {
            var autogeneratedFiles = new[]
            {
                @"ExtraEmptyFile.g.cs", // This is added by Verifier, we will skip it when building the project
                @"autogenerated_comment.cs",
                @"autogenerated_comment2.cs",
                @"class.designer.cs",
                @"class.g.cs",
                @"class.g.something.cs",
                @"class.generated.cs",
                @"class_generated.cs",
                @"compiler_generated.cs",
                @"compiler_generated_attr.cs",
                @"debugger_non_user_code.cs",
                @"debugger_non_user_code_attr.cs",
                @"generated_code_attr.cs",
                @"generated_code_attr2.cs",
                @"generated_region.cs",
                @"generated_region_2.cs",
                @"TEMPORARYGENERATEDFILE_class.cs",
            };

            var projectFiles = autogeneratedFiles.Skip(1) // "ExtraEmptyFile.g.cs" is automatically added by Verifier
                .Select(f => $"TestCases\\FileMetadataAnalyzer\\{f}");

            VerifyAllFilesAreGenerated(projectFiles, autogeneratedFiles);
        }

        [TestMethod]
        [TestCategory("Rule")]
        public void FileMetadataAnalyzer_Autogenerated_CSharp9()
        {
            var autogeneratedFiles = new[]
            {
                @"ExtraEmptyFile.g.cs", // This is added by Verifier, we will skip it when building the project
                @"generated_code_attr_local_function.cs"
            };

            var projectFiles = autogeneratedFiles.Skip(1) // "ExtraEmptyFile.g.cs" is automatically added by Verifier
                .Select(f => $"TestCases\\FileMetadataAnalyzer\\{f}");

            VerifyAllFilesAreGenerated(projectFiles, autogeneratedFiles, ParseOptionsHelper.FromCSharp9);
        }

        [TestMethod]
        [TestCategory("Rule")]
        public void FileMetadataAnalyzer_NotAutogenerated()
        {
            var notAutogeneratedFiles = new[]
            {
                @"normal_file.cs",
            };

            var projectFiles = notAutogeneratedFiles
                .Select(f => $"TestCases\\FileMetadataAnalyzer\\{f}");

            Verifier.VerifyUtilityAnalyzer<FileMetadataInfo>(
                projectFiles,
                new FileMetadataAnalyzer_
                {
                    IsEnabled = true,
                    WorkingPath = @"FileMetadataAnalyzer_NotAutogenerated",
                },
                @"FileMetadataAnalyzer_NotAutogenerated\file-metadata.pb",
                (messages) =>
                {
                    messages[0].IsGenerated.Should().BeTrue();
                    messages[0].FilePath.Should().Be("ExtraEmptyFile.g.cs");

                    for (var i = 0; i < notAutogeneratedFiles.Length; i++)
                    {
                        var message = messages[i + 1]; // The first message is for ExtraEmptyFile.g.cs, then is our list
                        message.IsGenerated.Should().BeFalse();
                        message.FilePath.Should().Be(notAutogeneratedFiles[i]);
                    }
                });

        }

        private void VerifyAllFilesAreGenerated(IEnumerable<string> projectFiles,
                                                string[] autogeneratedFiles,
                                                IEnumerable<ParseOptions> parseOptions = null) =>
            Verifier.VerifyUtilityAnalyzer<FileMetadataInfo>(
                projectFiles,
                new FileMetadataAnalyzer_
                {
                    IsEnabled = true,
                    WorkingPath = @"FileMetadataAnalyzer_Autogenerated",
                },
                @"FileMetadataAnalyzer_Autogenerated\file-metadata.pb",
                (messages) =>
                {
                    for (var i = 0; i < autogeneratedFiles.Length; i++)
                    {
                        var message = messages[i];
                        message.IsGenerated.Should().BeTrue();
                        message.FilePath.Should().Be(autogeneratedFiles[i]);
                    }
                },
                parseOptions);

        // We need to set protected properties and this class exists just to enable the analyzer
        // without bothering with additional files with parameters
        private class FileMetadataAnalyzer_ : FileMetadataAnalyzer
        {
            public bool IsEnabled
            {
                get => IsAnalyzerEnabled;
                set => IsAnalyzerEnabled = value;
            }

            public string WorkingPath
            {
                get => OutPath;
                set => OutPath = value;
            }
        }
    }
}
