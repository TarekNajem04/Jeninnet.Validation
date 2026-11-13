//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Net;
global using System.Net.Http.Json;
global using System.Text.Json.Nodes;
global using System.Threading;
global using System.Threading.Tasks;
global using Jeninnet.Assertions;
global using Jeninnet.Assertions.MSTest;
global using Jeninnet.Validation;
global using Jeninnet.Validation.Abstractions;
global using Jeninnet.Validation.Core;
global using Jeninnet.Validation.Extensions.Validation;
global using Jeninnet.Validation.Fluent;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.TestHost;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.Validation;
global using Microsoft.VisualStudio.TestTools.UnitTesting;
