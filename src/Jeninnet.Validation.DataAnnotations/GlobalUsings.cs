//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Reflection;
global using System.Runtime.ExceptionServices;
global using Jeninnet.Validation;
global using Jeninnet.Validation.Abstractions;
global using Jeninnet.Validation.Core;
global using Jeninnet.Validation.Metadata;
global using DataAnnotationsValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;
global using JeninnetValidationResult = Jeninnet.Validation.ValidationResult;
global using SkipValidationAttribute = Jeninnet.Validation.Attributes.SkipValidationAttribute;
