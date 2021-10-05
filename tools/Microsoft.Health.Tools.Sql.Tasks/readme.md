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

## Know issues
- Using unofficial Sql DacFx nuget manually uploaded in our public feed because of https://github.com/microsoft/DacFx/issues/39
