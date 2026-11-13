//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Globalization;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Reflection;
global using System.Threading;
global using System.Threading.Tasks;
global using Jeninnet.Localization.Abstractions;
global using Jeninnet.Validation;
global using Jeninnet.Validation.Abstractions;
global using Jeninnet.Validation.Attributes;
global using Jeninnet.Validation.Configuration;
global using Jeninnet.Validation.Core;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Diagnostics;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Abstractions;
global using Microsoft.AspNetCore.Mvc.Controllers;
global using Microsoft.AspNetCore.Mvc.Filters;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
