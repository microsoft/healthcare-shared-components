// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Health.Operations.Functions.Worker.Testing;

/// <summary>
/// A class for running a local Azure Functions project in a separate <see cref="Process"/> object.
/// </summary>
public static class AzureFunctionsProcess
{
    /// <summary>
    /// Creates a new <see cref="Process"/> that executes <c>func start</c> with the given Azure Functions project.
    /// </summary>
    /// <param name="projectDirectory">The project directory for the Azure Functions.</param>
    /// <param name="enableRaisingEvents">An optional flag for raising events when the process terminates.</param>
    /// <returns>An encompassing <see cref="Process"/> object.</returns>
    /// <exception cref="InvalidOperationException">The process could not be started.</exception>
    public static Process Create(string projectDirectory, bool enableRaisingEvents = false)
        => Create(projectDirectory, ImmutableDictionary<string, string?>.Empty, enableRaisingEvents);

    /// <summary>
    /// Creates a new <see cref="Process"/> that executes <c>func start</c> with the given Azure Functions project.
    /// </summary>
    /// <param name="projectDirectory">The project directory for the Azure Functions.</param>
    /// <param name="environment">A collection of environment variables for the resulting process.</param>
    /// <param name="enableRaisingEvents">An optional flag for raising events when the process terminates.</param>
    /// <returns>An encompassing <see cref="Process"/> object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="projectDirectory"/> or <paramref name="environment"/> is <see langword="null"/>.</exception>"
    /// <exception cref="ArgumentException"><paramref name="projectDirectory"/> is white space.</exception>
    public static Process Create(string projectDirectory, IReadOnlyDictionary<string, string?> environment, bool enableRaisingEvents = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        ArgumentNullException.ThrowIfNull(environment);

        GetFileNameAndArguments(out string fileName, out string argument);
        ProcessStartInfo startInfo = GetFuncCliStartInfo(fileName, argument, projectDirectory, environment);

        return new Process()
        {
            EnableRaisingEvents = enableRaisingEvents,
            StartInfo = startInfo,
        };
    }

    private static ProcessStartInfo GetFuncCliStartInfo(string fileName, string argument, string projectDirectory, IReadOnlyDictionary<string, string?> environment)
    {
        ProcessStartInfo startInfo = new()
        {
            Arguments = argument,
            CreateNoWindow = false,
            ErrorDialog = false,
            FileName = fileName,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = projectDirectory,
        };

        foreach (KeyValuePair<string, string?> pair in environment)
            startInfo.Environment[pair.Key] = pair.Value;

        return startInfo;
    }

    private static void GetFileNameAndArguments(out string fileName, out string argument)
    {
        // TODO: Remove prefix once the tools no longer demand it
        string outputDir = Path.Combine("bin", "output");
        string command = $"func start --no-build --prefix {outputDir}";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
            argument = $"/d /c \"{command}\"";
        }
        else
        {
            fileName = "/bin/sh";
            argument = $"-c \"{command}\"";
        }
    }
}
