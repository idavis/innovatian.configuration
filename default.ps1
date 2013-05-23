properties {
  Write-Output "Loading general properties"
  $base = @{}
  $base.dir = Resolve-Path .
  
  $solution = @{}
  $solution.name = "Lucid.Configuration"
  $solution.file = "$($base.dir)\$($solution.name).sln"
  
  $build = @{}
  $build.dir = "$($base.dir)\bin"
  $build.configuration = "Debug"
  
  Write-Output "Loading nunit properties"
  $nunit = @{}
  $nunit.runner = (Get-ChildItem "$($base.dir)\*" -recurse -include nunit-console-x86.exe).FullName
  
  Write-Output "Loading MSBuild properties"
  $msbuild = @{}
  $msbuild.logfilename = "MSBuildOutput.txt"
  $msbuild.logfilepath = "$($build.dir)"
  $msbuild.max_cpu_count = [System.Environment]::ProcessorCount
  $msbuild.build_in_parralel = $true
  $msbuild.logger = "FileLogger,Microsoft.Build.Engine"
  $msbuild.platform = "Any CPU"
  
  Write-Output "Loading Nemerle properties"
  $NemerleVersion = "Net-3.5"
  $TargetFrameworkVersion = "3.5"
}

task default -depends build, test

task build -depends build-35

task build-35 {
  $NemerleVersion = "Net-3.5"
  $TargetFrameworkVersion = "v3.5"
  Invoke-MsBuild
}

task build-40 {
  $NemerleVersion = "Net-4.0"
  $TargetFrameworkVersion = "v4.0"
  Invoke-MsBuild
}

task build-45 {
  $NemerleVersion = "Net-4.5"
  $TargetFrameworkVersion = "v4.5"
  Invoke-MsBuild
}

function Invoke-MsBuild {
  if(!(Test-Path "$($msbuild.logfilepath)")) {
    New-Item -ItemType Directory -Path "$($msbuild.logfilepath)" | Out-Null
  }
  $command = "msbuild /property:NemerleVersion=$NemerleVersion " +
                 "/property:TargetFrameworkVersion=$TargetFrameworkVersion " +
				 "/m:$($msbuild.max_cpu_count) " +
				 "/p:BuildInParralel=$msbuild.build_in_parralel " +
				 "/logger:$($msbuild.logger);logfile=`"$($msbuild.logfilepath)\$($msbuild.logfilename)`" " +
				 "/p:Configuration=$($build.configuration) " +
				 "/p:Platform=$($msbuild.platform) " +
				 "/p:OutDir=`"$($build.dir)`" " +
				 "`"$($solution.file)`""
				 
  $message = "Error executing command: {0}"
  $errorMessage = $message -f $command
  exec { msbuild /property:NemerleVersion=$NemerleVersion `
                 /property:TargetFrameworkVersion=$TargetFrameworkVersion `
				 /m:"$($msbuild.max_cpu_count)" `
				 /p:BuildInParralel=$msbuild.build_in_parralel `
				 /logger:"$($msbuild.logger);logfile=`"$($msbuild.logfilepath)\$($msbuild.logfilename)`"" `
				 /p:Configuration="$($build.configuration)" `
				 /p:Platform="$($msbuild.platform)" `
				 /p:OutDir="`"$($build.dir)`"" `
				 "`"$($solution.file)`"" } $errorMessage
}

task test {
  Invoke-TestRunner @(Join-Path $build.dir "Lucid.Configuration.Tests.dll")
}

function Invoke-TestRunner {
  param(
    [Parameter(Position=0,Mandatory=$true)]
    [string[]]$dlls = @()
  )

  Assert ((Test-Path($nunit.runner)) -and (![string]::IsNullOrEmpty($nunit.runner))) "NUnit runner could not be found"
  
  if ($dlls.Length -le 0) { 
     Write-Output "No tests defined"
     return 
  }
  Write-Output "$($nunit.runner) $dlls"
  exec { & $nunit.runner $dlls /noshadow }
}
