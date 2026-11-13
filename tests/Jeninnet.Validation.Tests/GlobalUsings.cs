//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

global using System.Collections;
global using System.Collections.Immutable;
global using System.ComponentModel.DataAnnotations;
global using System.Diagnostics;
global using System.Diagnostics.Metrics;
global using System.Globalization;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Nodes;
global using System.Text.RegularExpressions;
global using System.Threading.Tasks;
global using Jeninnet.Assertions;
global using Jeninnet.Assertions.MSTest;
global using Jeninnet.Localization.Abstractions;
global using Jeninnet.Localization.Abstractions.Options;
global using Jeninnet.Localization.Abstractions.Store;
global using Jeninnet.Localization.Core;
global using Jeninnet.Localization.Core.Extensions;
global using Jeninnet.Validation.Abstractions;
global using Jeninnet.Validation.Assertions;
global using Jeninnet.Validation.Catalog;
global using Jeninnet.Validation.ClientRules;
global using Jeninnet.Validation.Configuration;
global using Jeninnet.Validation.Core;
global using Jeninnet.Validation.Diagnostics;
global using Jeninnet.Validation.Extensions;
global using Jeninnet.Validation.Fluent;
global using Jeninnet.Validation.Generators;
global using Jeninnet.Validation.JsonSchema;
global using Jeninnet.Validation.Localization;
global using Jeninnet.Validation.Metadata;
global using Jeninnet.Validation.Rules;
global using Jeninnet.Validation.Sanitization;
global using Jeninnet.Validation.Tests.Helpers;
global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.CodeAnalysis.Diagnostics;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using static Jeninnet.Assertions.FluentActions;
global using IStringLocalizerFactory = Microsoft.Extensions.Localization.IStringLocalizerFactory;
