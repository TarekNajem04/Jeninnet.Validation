//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

global using System.Globalization;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using Jeninnet.Localization.Abstractions;
global using Jeninnet.Localization.Abstractions.Options;
global using Jeninnet.Localization.Core.Extensions;
global using Jeninnet.Localization.Providers.Memory;
global using Jeninnet.Localization.Providers.Memory.Options;
global using Jeninnet.Validation;
global using Jeninnet.Validation.Abstractions;
global using Jeninnet.Validation.AotSmoke;
global using Jeninnet.Validation.AotSmoke.Validators;
global using Jeninnet.Validation.Attributes;
global using Jeninnet.Validation.Attributes.Common;
global using Jeninnet.Validation.Catalog;
global using Jeninnet.Validation.Configuration;
global using Jeninnet.Validation.Fluent;
global using Jeninnet.Validation.Metadata;
global using Jeninnet.Validation.Rules;
global using Microsoft.Extensions.DependencyInjection;
