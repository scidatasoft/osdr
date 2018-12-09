param (
    [int]$count = 1,
    [string]$type = "",
    [string]$group = "",
    [switch]$build = $false
)

if ($build)
{
  dotnet build
}

if ($type -ne "" -and $group -ne "")
{
  While ($count -ne 0) {
    $count--;
    sleep 5
    Start-Process "powershell.exe" -ArgumentList "-NoExit", "dotnet test Sds.Osdr.Domain.BddTests.csproj --filter $type=$group -v n --no-build --no-restore"
  }
}
else
{
  While ($count -ne 0) {
    $count--;
    sleep 5
    Start-Process "powershell.exe" -ArgumentList "-NoExit", "dotnet test Sds.Osdr.Domain.BddTests.csproj -v n --no-build --no-restore"
  }
}
