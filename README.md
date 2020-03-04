
# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

# Releasing
To publish Nuget packages, you must create a release using the GitHub release feature. The release should be 
tagged with a version like `1.0.0-master-{yyyymmdd}-{daily iteration}`. For example if you wanted to create a release
on March 4, 2020 the version would be `1.0.0-master-20200304-1`. If you wanted to create a second release on that
date, the version would be `1.0.0-master-20200304-2`.

Publishing a release will envoke the [.NET Core Build & Publish workflow](.github/workflows/dotnetbuildpublish.yml)
and publish the Nugets with the version specified to the GitHub package repository.