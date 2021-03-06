#!/usr/bin/env bash
set -e

# Have to explicitly exclude xunit and test projects from coverage. Excluding all MacOS related files
# Using the FullyQualifiedName filter to run the unit tests only
dotnet test --filter FullyQualifiedName~AppEncryption.Tests --configuration Release --no-build /p:CollectCoverage=true /p:Exclude=\"[xunit*]*,[*.IntegrationTests]*,[*.Tests]*\" /p:CoverletOutputFormat=cobertura /p:ExcludeByFile="../**/MacOS/**/*.cs"