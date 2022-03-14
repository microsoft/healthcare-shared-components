// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Api.Features.Filters;
using Microsoft.Health.SqlServer.Features.Routing;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Extensions;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Get;

namespace Microsoft.Health.SqlServer.Api.Controllers;

[HttpExceptionFilter]
[Route(KnownRoutes.SchemaRoot)]
public class SchemaController : Controller
{
    private readonly SchemaInformation _schemaInformation;
    private readonly IScriptProvider _scriptProvider;
    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly ILogger<SchemaController> _logger;
    private readonly IMediator _mediator;

    public SchemaController(SchemaInformation schemaInformation, IScriptProvider scriptProvider, IUrlHelperFactory urlHelperFactoryFactory, IMediator mediator, ILogger<SchemaController> logger)
    {
        EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
        EnsureArg.IsNotNull(scriptProvider, nameof(scriptProvider));
        EnsureArg.IsNotNull(urlHelperFactoryFactory, nameof(urlHelperFactoryFactory));
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _schemaInformation = schemaInformation;
        _scriptProvider = scriptProvider;
        _urlHelperFactory = urlHelperFactoryFactory;
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route(KnownRoutes.Versions)]
    public ActionResult AvailableVersions()
    {
        _logger.LogInformation("Attempting to get available schemas");
        IUrlHelper urlHelper = _urlHelperFactory.GetUrlHelper(ControllerContext);

        var availableSchemas = new List<object>();
        var currentVersion = _schemaInformation.Current ?? 1;
        for (var version = currentVersion; version <= _schemaInformation.MaximumSupportedVersion; version++)
        {
            var routeValues = new Dictionary<string, object> { { "id", version } };
            string scriptUri = urlHelper.RouteUrl(RouteNames.Script, routeValues);
            string diffScriptUri = string.Empty;
            if (version > 1)
            {
                diffScriptUri = urlHelper.RouteUrl(RouteNames.Diff, routeValues);
            }

            availableSchemas.Add(new { id = version, script = scriptUri, diff = diffScriptUri });
        }

        return new JsonResult(availableSchemas);
    }

    [HttpGet]
    [AllowAnonymous]
    [Route(KnownRoutes.Current)]
    public async Task<ActionResult> CurrentVersionAsync()
    {
        _logger.LogInformation("Attempting to get current schemas");
        GetCurrentVersionResponse currentVersionResponse = await _mediator.GetCurrentVersionAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        return new JsonResult(currentVersionResponse.CurrentVersions);
    }

    [HttpGet]
    [AllowAnonymous]
    [Route(KnownRoutes.Script, Name = RouteNames.Script)]
    public async Task<FileContentResult> ScriptAsync(int id)
    {
        _logger.LogInformation("Attempting to get script for schema version: {Version}", id);
        string fileName = $"{id}.sql";
        return File(await _scriptProvider.GetScriptAsBytesAsync(id, HttpContext.RequestAborted).ConfigureAwait(false), "application/sql", fileName);
    }

    [HttpGet]
    [AllowAnonymous]
    [Route(KnownRoutes.Diff, Name = RouteNames.Diff)]
    public async Task<FileContentResult> DiffScriptAsync(int id)
    {
        _logger.LogInformation("Attempting to get diff script for schema version: {Version}", id);
        string fileName = $"{id}.diff.sql";
        return File(await _scriptProvider.GetDiffScriptAsBytesAsync(id, HttpContext.RequestAborted).ConfigureAwait(false), "application/sql", fileName);
    }

    [HttpGet]
    [AllowAnonymous]
    [Route(KnownRoutes.Compatibility)]
    public async Task<ActionResult> CompatibilityAsync()
    {
        _logger.LogInformation("Attempting to get compatibility");
        GetCompatibilityVersionResponse compatibleResponse = await _mediator.GetCompatibleVersionAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        return new JsonResult(compatibleResponse.CompatibleVersions);
    }
}
