// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Health.Tools.Sql.Tasks.Helpers;

namespace Microsoft.Health.Tools.Sql.Tasks.Tasks
{
    public class GenerateFullScript : Task
    {
        private const string MetadataNameFullPath = "FullPath";

        /// <summary>
        /// IntermmediateOutputPath to build the generated sql file
        /// </summary>
        public string IntermediateOutputPath
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies file where this task generates the merged Sql script.
        /// </summary>
        [Required]
        public string OutputFile
        {
            get;
            set;
        }

        /// <summary>
        /// These Sql scripts will be put in a transaction
        /// </summary>
        [Required]
#pragma warning disable CA1819 // Properties should not return arrays
        public ITaskItem[] TSqlScript
#pragma warning restore CA1819 // Properties should not return arrays
        {
            get;
            set;
        }

        /// <summary>
        /// These Sql scripts will be put outside a transaction
        /// </summary>
        [Required]
#pragma warning disable CA1819 // Properties should not return arrays
        public ITaskItem[] SqlScript
#pragma warning restore CA1819 // Properties should not return arrays
        {
            get;
            set;
        }

        /// <summary>
        /// Sql script that defines the transaction condition
        /// </summary>
        [Required]
        public ITaskItem TInitSqlScript
        {
            get;
            set;
        }

        public override bool Execute()
        {
            try
            {
                Log.LogMessage($"IntermediateOutputPath: {IntermediateOutputPath}");
                Log.LogMessage($"OutpuFile: {OutputFile}");

                var intermediateOutputFile = Path.Combine(IntermediateOutputPath, "GenerateFullScript.sql");
                using (SqlScriptWriter sqlScriptWriter = new SqlScriptWriter(intermediateOutputFile))
                {
                    // Write the file headers
                    sqlScriptWriter.WriteLine(SqlGenConstants.GeneratedHeader);

                    sqlScriptWriter.WriteLine(SqlGenConstants.SetXabortOn);

                    // Write the Transaction condition
                    sqlScriptWriter.WriteLine(SqlGenConstants.BeginTransaction);
                    var tInitSqlScript = TInitSqlScript.GetMetadata(MetadataNameFullPath);
                    Log.LogMessage($"Processing Transaction condition TInitSqlScript: {tInitSqlScript}");
                    sqlScriptWriter.Write(SqlScriptParser.ParseSqlFile(TInitSqlScript.GetMetadata(MetadataNameFullPath), Log));

                    foreach (var tsqlscript in TSqlScript)
                    {
                        var tSqlScript = tsqlscript.GetMetadata(MetadataNameFullPath);
                        Log.LogMessage($"Processing script inside transaction TSqlScript: {tSqlScript}");
                        sqlScriptWriter.Write(SqlScriptParser.ParseSqlFile(tSqlScript, Log));
                    }

                    // Final transaction sql
                    sqlScriptWriter.WriteLine(SqlGenConstants.CommitTransaction);
                    sqlScriptWriter.WriteLine(SqlGenConstants.Go);

                    // Write non transaction script
                    foreach (var ss in SqlScript)
                    {
                        var sScript = ss.GetMetadata(MetadataNameFullPath);
                        Log.LogMessage($"Processing script outside transaction SqlScript: {sScript}");
                        sqlScriptWriter.Write(SqlScriptParser.ParseSqlFile(sScript, Log));
                        sqlScriptWriter.WriteLine(SqlGenConstants.Go);
                    }
                }

                if (!Log.HasLoggedErrors)
                {
                    // Copy the intermediate file to final location if everything was successfull
                    File.Copy(intermediateOutputFile, OutputFile, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, showStackTrace: true);
            }

            return !Log.HasLoggedErrors;
        }
    }
}
