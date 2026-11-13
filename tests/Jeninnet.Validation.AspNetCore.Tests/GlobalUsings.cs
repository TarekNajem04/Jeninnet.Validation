//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

global using System.Collections.Generic;
global using System.Globalization;
global using System.Net;
global using System.Net.Http.Json;
global using System.Reflection;
global using System.Text.Json;
global using System.Text.Json.Nodes;
global using System.Text.Json.Serialization;
global using System.Text.Json.Serialization.Metadata;
global using System.Threading;
global using System.Threading.Tasks;
global using Jeninnet.Assertions;
global using Jeninnet.Assertions.MSTest;
global using Jeninnet.Localization.Abstractions;
global using Jeninnet.Validation;
global using Jeninnet.Validation.Abstractions;
global using Jeninnet.Validation.AspNetCore;
global using Jeninnet.Validation.Attributes;
global using Jeninnet.Validation.Configuration;
global using Jeninnet.Validation.Core;
global using Jeninnet.Validation.Extensions;
global using Jeninnet.Validation.Fluent;
global using Jeninnet.Validation.JsonSchema;
global using Jeninnet.Validation.Metadata;
global using Jeninnet.Validation.OpenApi;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Http.HttpResults;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Abstractions;
global using Microsoft.AspNetCore.Mvc.Controllers;
global using Microsoft.AspNetCore.Mvc.Filters;
global using Microsoft.AspNetCore.OpenApi;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.AspNetCore.TestHost;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.OpenApi;
global using Microsoft.VisualStudio.TestTools.UnitTesting;
