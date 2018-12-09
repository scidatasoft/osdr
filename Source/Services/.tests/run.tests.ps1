param([string]$p, [string]$s, [string]$target)

# List of services
[string[]]$tsServices = Get-Content .\ts-services.txt
[string[]]$pkServices = Get-Content .\pk-services.txt

function Invoke-Core-Test($svc) {
    dotnet test $svc.Split(";")[1]
    if ($LASTEXITCODE -ne 0) {
        Write-Host "$svc.Split(\";\")[1] tests error occurred" -foregroundcolor "red"
        Write-Host "Execution aborted" -foregroundcolor "red"
        exit
    }
}

function Invoke-Net-Test($svc) {
    $solution = $svc.Split(";")[0];
    $proj = $svc.Split(";")[1];
    nuget.exe restore $solution
    msbuild $proj /verbosity:q
    $test = [System.IO.Path]::GetDirectoryName($proj)+"\bin\debug\"+[System.IO.Path]::GetFileNameWithoutExtension($proj)+".dll"
    tools\xunit.console $test
    if ($LASTEXITCODE -ne 0) {
        Write-Host "$proj tests error occurred" -foregroundcolor "red"
        Write-Host "Execution aborted" -foregroundcolor "red"
        exit
    }
}

if ($p) {
    if ($target -eq "core") {
        Invoke-Core-Test($p+";"+$s)
        exit
    }
    else {
        Invoke-Net-Test($p+";"+$s)
        exit
    }
}

foreach ($svc in $pkServices) {
    Invoke-Core-Test($svc)
}

foreach ($svc in $tsServices) {
<<<<<<< HEAD
	cd ..\$svc
	nuget.exe restore $svc.sln >null
	msbuild Sds.$svc.Tests\Sds.$svc.Tests.csproj /verbosity:q /p:WarningLevel=0
	cd ..\.tests
	tools\xunit.console ..\$svc\Sds.$svc.Tests\bin\debug\Sds.$svc.Tests.dll
	if($LASTEXITCODE -ne 0)
	{
		Write-Host "$svc tests error occurred" -foregroundcolor "red"
		Write-Host "Execution aborted" -foregroundcolor "red"
		exit
	}
}
=======
    Invoke-Net-Test($svc)
}

>>>>>>> master
