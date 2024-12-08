﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using EnsureThat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Health.SqlServer.Features.Exceptions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.SqlServer.Api.Features.Filters;

internal sealed class HttpExceptionFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        EnsureArg.IsNotNull(context, nameof(context));

        if (context.Exception == null)
        {
            return;
        }

        if (!context.ExceptionHandled)
        {
            var resultJson = new JObject { ["error"] = context.Exception.Message };

            switch (context.Exception)
            {
                case NotImplementedException:
                    context.Result = new JsonResult(resultJson) { StatusCode = (int)HttpStatusCode.NotImplemented };
                    context.ExceptionHandled = true;
                    break;

                case SqlRecordNotFoundException:
                case FileNotFoundException:
                    context.Result = new JsonResult(resultJson) { StatusCode = (int)HttpStatusCode.NotFound };
                    context.ExceptionHandled = true;
                    break;

                case SqlOperationFailedException:
                    context.Result = new JsonResult(resultJson) { StatusCode = (int)HttpStatusCode.InternalServerError };
                    context.ExceptionHandled = true;
                    break;
            }
        }
    }
}
