# This is ONLY if you like to manually pack/publish the package
# Otherwise just build __NuGetPublish project

# to pack any package use command like this
nuget pack SDS.FileStorage.Core.nuspec 

# and to push it on server use
nuget push *.nupkg -source http://nuget.your-company.com/ F793C782-E721-4224-B748-AA5AD52ECEB4 -Verbosity detailed
