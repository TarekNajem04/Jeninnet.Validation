//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

global using System.Collections.Immutable;
global using System.Composition;
global using System.Globalization;
global using System.Text;
global using Jeninnet.Validation.Analyzers.Misuse;
global using Jeninnet.Validation.Constants;
global using Jeninnet.Validation.Diagnostics;
global using Jeninnet.Validation.Extensions;
global using Jeninnet.Validation.Generators;
global using Jeninnet.Validation.Generators.Emitter;
global using Jeninnet.Validation.Generators.GeneratorModel;
global using Jeninnet.Validation.Generators.Metadata;
global using Jeninnet.Validation.Generators.Metadata.Builders;
global using Jeninnet.Validation.Generators.Models;
global using Jeninnet.Validation.Generators.Validation;
global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.CodeActions;
global using Microsoft.CodeAnalysis.CodeFixes;
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.CodeAnalysis.CSharp.Syntax;
global using Microsoft.CodeAnalysis.Diagnostics;
