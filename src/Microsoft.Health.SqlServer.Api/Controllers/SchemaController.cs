// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Api.Features.Filters;
using Microsoft.Health.SqlServer.Features.Routing;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.SqlServer.Api.Controllers;

[HttpExceptionFilter]
[Route(KnownRoutes.SchemaRoot)]
public class SchemaController : Controller
{
    private readonly ISchemaDataStore _schemaDataStore;
    private readonly SchemaInformation _schemaInformation;
    private readonly IScriptProvider _scriptProvider;
    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly ILogger<SchemaController> _logger;

    public SchemaController(ISchemaDataStore schemaDataStore, SchemaInformation schemaInformation, IScriptProvider scriptProvider, IUrlHelperFactory urlHelperFactoryFactory, ILogger<SchemaController> logger)
    {
        _schemaDataStore = EnsureArg.IsNotNull(schemaDataStore, nameof(schemaDataStore));
        _schemaInformation = EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
        _scriptProvider = EnsureArg.IsNotNull(scriptProvider, nameof(scriptProvider));
        _urlHelperFactory = EnsureArg.IsNotNull(urlHelperFactoryFactory, nameof(urlHelperFactoryFactory));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    [HttpGet]
    [AllowAnonymous]
    [Route(KnownRoutes.Versions)]
    public JsonResult AvailableVersions()
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
    public async Task<JsonResult> CurrentVersionAsync()
    {
        _logger.LogInformation("Attempting to get current schemas");
        List<CurrentVersionInformation> currentVersions = await _schemaDataStore
            .GetCurrentVersionAsync(HttpContext.RequestAborted)
            .ConfigureAwait(false);

        return new JsonResult(currentVersions);
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
        CompatibleVersions compatibleVersions = await _schemaDataStore
            .GetLatestCompatibleVersionsAsync(HttpContext.RequestAborted)
            .ConfigureAwait(false);

        return new JsonResult(compatibleVersions);
    }
}
