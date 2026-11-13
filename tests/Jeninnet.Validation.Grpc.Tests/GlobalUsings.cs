//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
global using Google.Protobuf;
global using Grpc.Core;
global using Grpc.Net.Client;
global using Jeninnet.Assertions;
global using Jeninnet.Assertions.MSTest;
global using Jeninnet.Validation;
global using Jeninnet.Validation.Abstractions;
global using Jeninnet.Validation.Fluent;
global using Jeninnet.Validation.Grpc;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.TestHost;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.VisualStudio.TestTools.UnitTesting;
global using GoogleBadRequest = Google.Rpc.BadRequest;
global using GoogleRpcStatus = Google.Rpc.Status;
