//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using CSharpFunctionalExtensions;
global using ErrorOr;
global using Jeninnet.Assertions;
global using Jeninnet.Assertions.MSTest;
global using Jeninnet.Validation;
global using Jeninnet.Validation.Abstractions;
global using Jeninnet.Validation.Fluent;
global using Jeninnet.Validation.MediatR;
global using Jeninnet.Validation.MediatR.CSharpFunctionalExtensions;
global using Jeninnet.Validation.MediatR.ErrorOr;
global using Jeninnet.Validation.MediatR.FluentResults;
global using MediatR;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using FluentResult = FluentResults.Result<Jeninnet.Validation.MediatR.ResultAdapters.Tests.FluentResultsResponse>;
