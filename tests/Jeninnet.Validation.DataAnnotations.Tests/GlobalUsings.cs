//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;
global using Jeninnet.Assertions;
global using Jeninnet.Assertions.MSTest;
global using Jeninnet.Validation;
global using Jeninnet.Validation.Abstractions;
global using Jeninnet.Validation.Attributes;
global using Jeninnet.Validation.DataAnnotations;
global using Jeninnet.Validation.Fluent;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using DataAnnotationsResult = System.ComponentModel.DataAnnotations.ValidationResult;
global using DataAnnotationsValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;
global using DataAnnotationsValidator = System.ComponentModel.DataAnnotations.Validator;
