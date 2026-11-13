//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
global using System;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Linq;
global using System.Reflection;
global using System.Threading;
global using System.Threading.Tasks;
global using Google.Protobuf;
global using Grpc.Core;
global using Grpc.Core.Interceptors;
global using Jeninnet.Validation;
global using Jeninnet.Validation.Abstractions;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using GoogleAny = Google.Protobuf.WellKnownTypes.Any;
global using GoogleBadRequest = Google.Rpc.BadRequest;
global using GoogleRpcStatus = Google.Rpc.Status;
global using GrpcMetadata = Grpc.Core.Metadata;
