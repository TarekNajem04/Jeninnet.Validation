//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

global using System.Collections.Immutable;
global using System.Diagnostics;
global using System.Globalization;
global using System.Reflection;
global using System.Text;
global using Jeninnet.Assertions;
global using Jeninnet.Assertions.MSTest;
global using Jeninnet.Validation;
global using Jeninnet.Validation.Analyzers;
global using Jeninnet.Validation.Analyzers.Tests.Infrastructure;
global using Jeninnet.Validation.CodeFixes;
global using Jeninnet.Validation.Diagnostics;
global using Jeninnet.Validation.Generators;
global using Jeninnet.Validation.Generators.Emitter;
global using Jeninnet.Validation.Generators.GeneratorModel;
global using Jeninnet.Validation.Generators.Metadata;
global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.CodeActions;
global using Microsoft.CodeAnalysis.CodeFixes;
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.CodeAnalysis.CSharp.Syntax;
global using Microsoft.CodeAnalysis.Diagnostics;
global using Microsoft.CodeAnalysis.Text;
global using Microsoft.VisualStudio.TestTools.UnitTesting;
