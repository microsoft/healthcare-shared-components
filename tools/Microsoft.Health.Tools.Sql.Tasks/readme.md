# Sql build tasks

This project uses MSBuild framework to build a custom Sql task to generate base schema file

## Testing changes

- Make sure there is enough logging on the code changes
- Any target changes should also be made in Sql.test.target (This allows project reference, instead of nuget reference for testing)
- Build and Pack
``` CLI
dotnet build Microsoft.Health.Tools.Sql.Tasks.csproj
dotnet pack Microsoft.Health.Tools.Sql.Tasks.csproj
```

- `Microsoft.Health.Tools.Sql.Tasks.Tests` is a sample project that consumes the Sql target
- Build and validate

``` CLI
dotnet msbuild Microsoft.Health.Tools.Sql.Tasks.Tests.csproj -v:d
```

### Test the package under multiple SDK hosts

From the repository root, run the package regression with the installed SDK versions:

```powershell
& ./tools/Microsoft.Health.Tools.Sql.Tasks.Tests/Test-Package.ps1 -SdkVersion @('10.0.402', '11.0.100')
```

Use `-DotNetPath` if `dotnet` is not on `PATH`.
The script packs the task, restores it into isolated consumers, and compares the generated SQL with `PackageTests.sql`.
Each SDK runs consumers targeting `net10.0` and `net472` without compiling them.
This checks that task loading depends on the MSBuild host rather than the consumer's target framework.
Each successful case prints `PASS`.
On Windows, pass `-FullFrameworkMSBuildPath` with the path to `MSBuild.exe` to test the full-framework host too.

## Know issues
- Using unofficial Sql DacFx nuget manually uploaded in our public feed because of https://github.com/microsoft/DacFx/issues/39
