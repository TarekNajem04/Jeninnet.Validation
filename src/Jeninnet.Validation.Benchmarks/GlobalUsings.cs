//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

global using System.Globalization;
global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Running;
global using Jeninnet.Localization.Abstractions;
global using Jeninnet.Localization.Core;
global using Jeninnet.Localization.Providers.Memory;
global using Jeninnet.Localization.Providers.Memory.Options;
global using Jeninnet.Validation.Abstractions;
global using Jeninnet.Validation.Attributes;
global using Jeninnet.Validation.Catalog;
global using Jeninnet.Validation.Configuration;
global using Jeninnet.Validation.Core;
global using Jeninnet.Validation.Fluent;
global using Jeninnet.Validation.Localization;
global using Jeninnet.Validation.Rules;
global using Microsoft.Extensions.DependencyInjection;
global using DataAnnotations = System.ComponentModel.DataAnnotations;
global using FluentValidationAbstractValidator = FluentValidation.AbstractValidator<Jeninnet.Validation.Benchmarks.PeerModel>;
global using FvValidationResult = FluentValidation.Results.ValidationResult;
global using JeninnetValidationResult = Jeninnet.Validation.ValidationResult;
