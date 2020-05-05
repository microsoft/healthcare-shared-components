// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Api.Features.Filters;
using Microsoft.Health.SqlServer.Api.Features.Routing;
using Microsoft.Health.SqlServer.Features.Schema;

namespace Microsoft.Health.SqlServer.Api.Controllers
{
    [HttpExceptionFilter]
    [Route(KnownRoutes.SchemaRoot)]
    public class SchemaController : Controller
    {
        private readonly SchemaInformation _schemaInformation;
        private readonly IScriptProvider _scriptProvider;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly ILogger<SchemaController> _logger;

        public SchemaController(SchemaInformation schemaInformation, IScriptProvider scriptProvider, IUrlHelperFactory urlHelperFactoryFactory, ILogger<SchemaController> logger)
        {
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
            EnsureArg.IsNotNull(scriptProvider, nameof(scriptProvider));
            EnsureArg.IsNotNull(urlHelperFactoryFactory, nameof(urlHelperFactoryFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _schemaInformation = schemaInformation;
            _scriptProvider = scriptProvider;
            _urlHelperFactory = urlHelperFactoryFactory;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route(KnownRoutes.Versions)]
        public ActionResult AvailableVersions()
        {
            _logger.LogInformation("Attempting to get available schemas");
            var urlHelper = _urlHelperFactory.GetUrlHelper(ControllerContext);

            var availableSchemas = new List<object>();
            var currentVersion = _schemaInformation.Current ?? 1;
            for (var version = currentVersion; version <= _schemaInformation.MaximumSupportedVersion; version++)
            {
                var routeValues = new Dictionary<string, object> { { "id", version } };
                string scriptUri = urlHelper.RouteUrl(RouteNames.Script, routeValues);
                availableSchemas.Add(new { id = version, script = scriptUri });
            }

            return new JsonResult(availableSchemas);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route(KnownRoutes.Current)]
        public ActionResult CurrentVersion()
        {
            _logger.LogInformation("Attempting to get current schemas");

            throw new NotImplementedException(Resources.CurrentVersionNotImplemented);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route(KnownRoutes.Script, Name = RouteNames.Script)]
        public async Task<FileContentResult> SqlScriptAsync(int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Attempting to get script for schema version: {id}");
            var currentVersion = _schemaInformation.Current ?? 0;
            bool isDiff = currentVersion >= 1 ? true : false;
            string fileName = isDiff ? $"{id}.diff.sql" : $"{id}.sql";
            return File(await _scriptProvider.GetMigrationScriptAsBytesAsync(id, isDiff, cancellationToken), "application/sql", fileName);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route(KnownRoutes.Compatibility)]
        public ActionResult Compatibility()
        {
            _logger.LogInformation("Attempting to get compatibility");

            throw new NotImplementedException(Resources.CompatibilityNotImplemented);
        }
    }
}
