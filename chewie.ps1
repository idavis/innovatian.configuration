﻿$script:skipFileLoading = $true


# borrowed from psake https://github.com/psake/psake/blob/master/psake.psm1
function Assert-Condition {
  [CmdletBinding()]
  param(
    [Parameter(Position=0,Mandatory=$true)]$conditionToCheck,
    [Parameter(Position=1,Mandatory=$true)]$failureMessage
  )
  if (!$conditionToCheck) { 
    throw ("Assert: " + $failureMessage) 
  }
}

Set-Alias -Name Assert -Value Assert-Condition

function ConvertTo-Chewie {
  [CmdletBinding()]
  param(
    [Parameter(Position=0,Mandatory=$true)]
    [string] $targetDirectory = $null,
    [Parameter(Position=1,Mandatory=$false)]
    [bool] $applyChanges = $false
  )
  if(Test-Path $chewie.nugetFile) {
    Write-ColoredOutput "A NuGetFile already exists. Skipping conversion."  -foregroundcolor DarkGreen
    return
  }

  $repoFileName = ls (Join-Path $targetDirectory *) -recurse -include repositories.config | % { $_.FullName } | Select -first 1
  if($repoFileName) {
    $repoDirectory = Split-Path -Parent $repoFileName
    [xml]$repoFile = Get-Content $repoFileName
    $respositoryFiles = $repoFile.repositories.repository.path | % {
      resolve-path (join-path $repoDirectory $_)
    } | ? { Test-Path $_ }
  }

  if(!$respositoryFiles) {
    # no repositories.config file, or corrupted/emtpy
    $respositoryFiles = ls (Join-Path $targetDirectory *) -recurse -include packages.config | % { $_.FullName }
  }

  if(!$respositoryFiles -or $respositoryFiles.Length -eq 0) {
    Write-ColoredOutput  "There were no packages in this project to convert." -foregroundcolor DarkGreen
    Write-ColoredOutput  "Please execute 'chewie init to generate a template .NuGetFile" -foregroundcolor DarkGreen
    return
  }

  $packages = $respositoryFiles | % { [xml] (Get-Content $_) } | % {$_.packages.package} | % {
    $instance = @{Id=$_.id; Version = $_.version; AllowedVersions = $_.allowedVersions}
    $instance
  } | % {$ids = @{}} {
    $current = $ids.($_.id)
    if($current) {
      if(!$current.allowedVersions -and $_.allowedVersions -ne $null) {
        $current.allowedVersions = $_.allowedVersions
      }
    } else {
      $ids.($_.id) = $_
    }
  } {$ids.Values}

  $packages | ? { $_.allowedVersions } | % { $_.version = $_.allowedVersions }

  Write-ColoredOutput "Creating $($chewie.nugetFile)"  -foregroundcolor DarkGreen
  New-Item $chewie.nugetFile -ItemType File | Out-Null
  Add-Content $chewie.nugetFile "install_to 'packages'"
  Add-Content $chewie.nugetFile "IncludeVersion"
    
  $packages | % { 
    $content = "chew '$($_.id)'"
    if($_.version) {
      $content += " '$($_.version)'"
    }
    Add-Content $chewie.nugetFile $content
  }

  $projectFiles = ls (Join-Path $targetDirectory *) -recurse -include *.csproj,*.vbproj,*.fsproj | % { $_.FullName }
  $projectFilesWithPackageRestore = $projectFiles | % {$restoreInfo = @{}} {
    $fileName = $_
    $content = Get-Content $fileName
    $matches = Select-String "nuget.targets" $fileName
    if($matches) {
      $restoreInfo.$fileName = $matches | Select LineNumber,Line | Add-Member -Type NoteProperty -Name "FileName" -Value $fileName -passthru
    }
  } {$restoreInfo}

  $nugetCommandlinePath = Join-Path $targetDirectory .nuget
  if(Test-Path $nugetCommandlinePath) {
    if($applyChanges){
      Write-ColoredOutput "Deleting .nuget folder: $nugetCommandlinePath" -foregroundcolor Magenta
      Remove-Item -Recurse -Force $nugetCommandlinePath -ErrorAction Continue
    } else{
      Write-ColoredOutput "Please delete the .nuget folder" -foregroundcolor Yellow
    }
  }
  if($repoFileName) {
    if($applyChanges){
      Write-ColoredOutput "Deleting the package repository file $repoFileName." -foregroundcolor Magenta
      Remove-Item -Force $repoFileName -ErrorAction Continue
    } else{
      Write-ColoredOutput "Please delete the package repository file $repoFileName." -foregroundcolor Yellow
    }
  }
  if($respositoryFiles) {
    $respositoryFiles | % {
      if($applyChanges){
        Write-ColoredOutput "Deleting the package config file: $_" -foregroundcolor Magenta
        Remove-Item -Force $_ -ErrorAction Continue
      } else{
        Write-ColoredOutput "Please delete the package config file: $_." -foregroundcolor Yellow
      }
    }
  }
  if($projectFilesWithPackageRestore -ne $null -and $projectFilesWithPackageRestore.Length -gt 0) {
    $projectFilesWithPackageRestore.Keys | % {
      $_ = $projectFilesWithPackageRestore.$_
      if($applyChanges){
        Write-ColoredOutput "Removing NuGet package restore import from project file $($_.FileName)" -foregroundcolor Magenta
        [xml]$xml = Get-Content $_.FileName
        $importNodes = $xml.SelectNodes('//*[local-name()="Import"]')
        $nugetNode = $importNodes | ? {$_.Project.contains("nuget.targets")} | Select -First 1
        $nugetNode.ParentNode.RemoveChild($nugetNode)
        $xml.Save($_.FileName)
      } else{
        Write-ColoredOutput "Please delete the nuget target in project file $($_.FileName)." -foregroundcolor Yellow
        Write-ColoredOutput "`tLine Number $($_.LineNumber): $($_.Line)." -foregroundcolor Yellow
      }
    }
  }
}
function Find-MaxVersion {
  param(
     [Parameter(Position=0,Mandatory=$true)] [PSObject[]]$versions
  )
  if($versions -eq $null -or $versions.Length -eq 0) {
    throw "A version string must be supplied"
  }
  $max = $versions[0]
  foreach($current in $versions) {
    if($max.Version -gt $current.Version) {
      continue
    }

    if($max.Version -lt $current.Version) {
      $max = $current
      continue
    }

    # else equal, must compare pre-release with lexicographic ASCII sort order
    # we only care if the new version's pre-release is higher as everything else is the same
    if($max.Pre -lt $current.Pre) {
      $max = $current
    }
  }
  return $max
}
function Get-InstalledPackageVersion {
  [CmdletBinding()]
  param(
    [Parameter(Position=0,Mandatory=$true)] [string]$packageName
  )
  # three cases
  # 1: we have version number in folder name
  # 2: we have no idea what version we are on, we can try to 
  #      parse files (not good) or try to reinterpret the version string
  #      in the nuget file (hard)
  # 3: parse the $path folder looking for $packageName*.nupkg and extract the version
  #    IF, the user skips version names per folder, the nupkg files have no version (facepalm)
  #    We have to extract the nuspec from the nugpk and extract
  #    ([xml] get-content $file).package.metadata.version
  $zips = Get-ChildItem $chewie.path "$packageName*.nupkg" -recurse
  $versions = $zips | % {Get-VersionFromArchive $packageName $_.FullName} 
  $versions = $versions | ? {$_ -ne $null}
  $greatest = Find-MaxVersion $versions
  return $greatest
}

function Get-MaxCompatibleVersion {
  [CmdletBinding()]
  param(
    [Parameter(Position=0,Mandatory=$true)][string]$packageName,
    [Parameter(Position=1,Mandatory=$true)][AllowEmptyString()][string]$versionSpec,
    [Parameter(Position=2,Mandatory=$false)][AllowEmptyString()][string]$source,
    [Parameter(Position=3,Mandatory=$false)][bool]$pre = $false
  )
  [xml]$targets = Get-PackageList $packageName $source
  Assert ($targets.feed.entry -ne $null) ($messages.error_package_not_found -f $packageName)
  $entries = $targets.feed.entry
  if(!$pre) {
    $entries = $entries | ? {$_.properties.IsPrerelease."#text" -eq $false}
  }
  $versions = $entries | %{ (Get-VersionFromString $_.properties.version) }
  $matchingVersions = $versions | ? { Test-VersionCompatibility $versionSpec $_.Version.ToString() }
  $maxCompatibleVersion = Find-MaxVersion $matchingVersions
  return $maxCompatibleVersion
}

function Get-PackageInstallationPaths {
  [CmdletBinding()]
  param(
    [Parameter(Position=0,Mandatory=$true)] [string]$packageName
  )
  if(!(Test-Path $chewie.path)) {return @()}
  $directoryNames = @(gci $chewie.path | ? { $_.PSIsContainer } | %{ $_.FullName })
  if($directoryNames.Length -eq 0) {return @()}
  $mapping = @{}

  $directoryNames | % {
    $path = $_
    $_ = [IO.Path]::GetFileName($_)
    $version = Get-VersionFromString $_
    if($version -ne $null) {
      $_ = $_.Replace($version.Version.ToString(),"")
      if(-not [string]::IsNullOrEmpty($version.Pre)) {
        $_ = $_.Replace($version.Pre,"")
      }
      if(-not [string]::IsNullOrEmpty($version.Build)) {
        $_ = $_.Replace($version.Build,"")
      }
      $_ = $_.Trim('.', '-', '+')
    }
    $mapping.$path = $_
  } | Out-Null
  
  return @($mapping.Keys | ? {$mapping[$_] -eq $packageName})
}

function Get-PackageList {
  param([string]$packageName, [string]$source)
  if([string]::IsNullOrEmpty($source)) { $source = $chewie.feed_uri }
  $url = $source + ($chewie.feed_package_filter -f $packageName)
  $wc = $null
  try {
    $wc = New-Object Net.WebClient
    $wc.UseDefaultCredentials = $true
    [xml]$result = $wc.DownloadString($url)
    return $result
  } finally {
    $wc.Dispose()
  }
}

function Get-PackageSources {
  $nugetConfig = "$env:AppData\NuGet\NuGet.config"
  if(!(Test-Path $nugetConfig)) {
    return @{}
  }

  $packageSources = ([xml] (type $nugetConfig)).configuration.packageSources
  $sources = $packageSources | % {$_.add} | % {$set = @{}} {if($_ -ne $null){$set[$_.Key]=$_.Value}} {$set}
  $sources
}

function Get-SafeFilePath {
  param([string]$filePath)
  return (Get-Item $filePath).FullName
}

function Get-VersionFromArchive {
  [CmdletBinding()]
  param(
    [Parameter(Position=0,Mandatory=$true)] [string]$packageName,
    [Parameter(Position=1,Mandatory=$true)] [string]$archiveFile
  )
  $fileName = [IO.Path]::GetFileName($archiveFile)
  $versionFromFileName = Get-VersionFromString $fileName -IsFile
  if($versionFromFileName) {return $versionFromFileName}
  $shell = new-object -com shell.application
  $targetDir = Split-Path "$archiveFile"
  $zipFileName = "$targetDir\$packageName.zip"
  cp "$archiveFile" "$zipFileName"
  $zip = $shell.NameSpace("$zipFileName")
  $specName = "$packageName*.nuspec"
  $targetFolder = $shell.NameSpace($targetDir)
  $target = $zip.Items() | ? {$_.Name -ilike $specName}
  $specName = $target.Name
  $specPath = (Join-Path $targetDir $specName)
  #0x14 is to hide the dialog and overwrite
  $targetFolder.CopyHere($target,0x14)
  [Runtime.Interopservices.Marshal]::ReleaseComObject($shell)
  Remove-Variable "shell"
  [Runtime.Interopservices.Marshal]::ReleaseComObject($zip)
  Remove-Variable "zip"
  [Runtime.Interopservices.Marshal]::ReleaseComObject($targetFolder)
  Remove-Variable "targetFolder"
  [Runtime.Interopservices.Marshal]::ReleaseComObject($target)
  Remove-Variable "target"
  
  rm $zipFileName| out-null
  
  if(!(Test-Path $specPath)){ return $null }
  $versionString = ([xml] (get-content "$specPath")).package.metadata.version
  rm $specPath
  
  $targetVersion = Get-VersionFromString $versionString
  return $targetVersion
}

function Get-VersionFromString {
  [CmdletBinding()]
  param(
    [Parameter(Position=0,Mandatory=$true)][string]$text,
    [Parameter(Position=1,Mandatory=$false)][switch]$isFile
  )

  $pattern = $chewie.VersionPattern
  if($isFile) { 
    $extension = [IO.Path]::GetExtension($text)
    $text = Split-Path -Leaf $text
    $pattern = "$pattern\$extension$"
  }
  if($text -imatch "$pattern" -and $matches.ContainsKey(1)) {
    # $matches[0] is the full file name
    # $matches[1] has the major, minor, and patch version 1.2[.3][.4]
    # $matches[2] is the pre-release version 
    # $matches[3] is the build version
    $versionString = $matches[1]
    $pre = $null
    $build = $null
    if($matches.ContainsKey(2)) { $pre = $matches[2] }
    if($matches.ContainsKey(3)) { $build = $matches[3] }
    $result = New-NuGetVersion $versionString $build $pre
    return $result
  } else {
    $null
  }
}

function Invoke-Chew {
  [CmdletBinding()]
  param(
    [Parameter(Position=0,Mandatory=$true)]
    [ValidateSet('install','update', 'uninstall', 'outdated')]
    [string] $task = $null,
    [Parameter(Position=1,Mandatory=$true)] [string]$packageName
  )

  Assert $packageName ($messages.error_invalid_package_name)

  $packageKey = $packageName.ToLower()
  
  if(!$chewie.Packages.Contains($packageKey)) {
    Write-ColoredOutput ($messages.warn_package_not_in_nugetfile -f $packageName) -ForegroundColor Magenta
    Resolve-Chew $packageName -prerelease $prerelease
  }
  $package = $chewie.Packages.$packageKey
  $package.Prerelease = ($package.Prerelease -or $pre)
  if ($chewie.ExecutedDependencies.Contains($packageKey))  { return }

  try {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $chewie.currentpackageName = $packageName
    if ($PSBoundParameters['Verbose']) {
      if ($chewie.packageNameFormat -is [ScriptBlock]) {
        & $chewie.packageNameFormat $task $packageName
      } else {
        Write-ColoredOutput ($chewie.packageNameFormat -f $task, $packageName) -foregroundcolor Cyan
      }
    }
    if($task -eq "uninstall") {
      if(!(Test-PackageInstalled $packageName)) {
        Write-ColoredOutput "Could not uninstall $packageName. It is not installed." -foregroundcolor Magenta
        return
      }
      
      $paths = Get-PackageInstallationPaths $packageName
      $paths | % {
        Write-ColoredOutput "Uninstalling package $($package.name) from $($_|out-string)" -ForegroundColor Green
        Remove-Item -Recurse -Force $_ 
      }
      return
    }

    if($task -eq "outdated") {
      if(Test-Outdated $package.Name $package.Version $package.Source $package.Prerelease) {
        Write-ColoredOutput "Package $($package.name) is outdated" -ForegroundColor Green
      } else {
        Write-ColoredOutput "Package $($package.name) is up-to-date" -ForegroundColor Green
      }
      return
    }
    
    if($task -eq "update") {
      Write-ColoredOutput "Package $($package.name) $($package.version) is being updated." -ForegroundColor Green

      [bool]$isOutdated = Test-Outdated $package.Name $package.Version  $package.Source $package.Prerelease
      if($isOutdated) {
        Write-ColoredOutput "Package $($package.name) is outdated. Updating package." -ForegroundColor Green
        Invoke-Chew "uninstall" $packageName
        Invoke-Chew "install" $packageName
      } else {
        Write-ColoredOutput "Package $($package.name) is up-to-date." -ForegroundColor Green
      }
      return
    }

    if(Test-PackageInstalled $packageName) {
      Write-ColoredOutput "$packageName is already installed." -ForegroundColor Yellow
      return
    }

    $command = Resolve-NugetCommand $package

    Write-Debug "Running: invoke-expression $command"
    invoke-expression $command

    $package.Duration = $stopwatch.Elapsed
  } catch {
    if ($chewie.DebugPreference -eq "Continue") {
      "-"*70
      Write-ColoredOutput ($messages.continue_on_error -f $packageName,$_) -foregroundcolor Magenta
      "-"*70
      $package.Duration = $stopwatch.Elapsed
    }  else {
      throw $_
    }
  }

  $chewie.executeddependencies.Push($packageKey)
}

function Invoke-Chewie {
  param(
    [Parameter(Position=0,Mandatory=$false)]
    [ValidateSet('install','update', 'uninstall', 'outdated')]
    [string] $task = "install",
    [Parameter(Position = 1,Mandatory=$false)]
    [string[]] $packageList = @(),
    [Parameter(Position=2,Mandatory=$false)]
    [string[]] $without = @()
  )
  
  try {
    if($packageList -eq $null -or $packageList.Length -eq 0) {
      # make sure we can execute the nugetfile to set up the dependencies
      Assert (test-path $chewie.nugetFile -pathType Leaf) ($messages.error_nugetfile_file_not_found -f $chewie.nugetFile)
    }

    $chewie.success = $false
  
    $chewie.Packages = @{}
    $chewie.ExecutedDependencies = new-object System.Collections.Stack
    $chewie.callStack = new-object System.Collections.Stack
    $chewie.originalEnvPath = $env:path
    $chewie.originalDirectory = get-location
    $chewie.chews = New-Object Collections.Queue
    
    if(Test-Path $chewie.nugetFile) {
      $chewie.build_script_file = Get-Item $chewie.nugetFile
      Write-Debug "Invoke-NugetFile $($chewie.build_script_file.FullName)"
      Invoke-NugetFile $chewie.build_script_file.FullName
    }

    # Override the .NuGetFile contents for the defaults.
    if($source) { Set-Source $source }
    if($path) { Set-PackagePath $path }

    if ($packageList) {
      foreach ($package in $packageList) {
        Invoke-Chew $task $package
      }
    } elseif ($chewie.Packages) {
      foreach ($package in $chewie.Packages.Keys) {
        Invoke-Chew $task $package
      }
    } else {
      throw $messages.error_no_dependencies
    }
    $chewie.success = $true
  } catch {
    if ($chewie.verboseError) {
      $error_message = "{0}: An Error Occurred. See Error Details Below: `n" -f (Get-Date) 
      $error_message += ("-" * 70) + "`n"
      $error_message += Resolve-Error $_
      $error_message += ("-" * 70) + "`n"
      $error_message += "Script Variables" + "`n"
      $error_message += ("-" * 70) + "`n"
      $error_message += get-variable -scope script | format-table | out-string 
    } else {
      # ($_ | Out-String) gets error messages with source information included. 
      $error_message = "{0}: An Error Occurred: `n{1}" -f (Get-Date), ($_ | Out-String)
    }

    $chewie.success = $false
    
    if (!$chewie.run_by_chewie_build_tester) {
      Write-ColoredOutput $error_message -foregroundcolor Red
    }
  }
}
# borrowed from psake https://github.com/psake/psake/blob/master/psake.psm1
function Invoke-CommandWithThrow {
    [CmdletBinding()]
    param(
      [Parameter(Position=0,Mandatory=$true)]
      [scriptblock]$cmd,
      [Parameter(Position=1,Mandatory=$false)]
      [string]$errorMessage = ($messages.error_bad_command -f $cmd)
    )
    & $cmd
    if ($LastExitCode -ne 0) {
      throw ("Exec: " + $errorMessage)
    }
}

Set-Alias -Name Exec -Value Invoke-CommandWithThrow

function Invoke-NuGetFile {
  param([string]$nugetFile)
  $nugetFile = (Get-SafeFilePath $nugetFile)
  [string]$content = Get-Content $nugetFile -Delimiter ([Environment]::NewLine)
  Invoke-Expression $content
}
# borrowed from psake https://github.com/psake/psake/blob/master/psake.psm1
DATA messages {
convertfrom-stringdata @'
    error_invalid_package_name = package name should not be null or empty string.
    error_package_name_does_not_exist = package {0} does not exist.
    error_bad_command = Error executing command {0}.
    error_duplicate_package_name = package {0} has already been defined.
    error_nugetfile_file_not_found = Could not find the NuGetFile {0}.
    error_invalid_version_spec = The version specification {0} is not valid.
    error_no_valid_nuget_command_found = Neither the NuGet command line nor the NuGet PowerShell commands are available.
    error_package_not_found = The package {0} was not found. Please correct the name or try another feed source.
    warn_package_not_in_nugetfile = {0} is not in the NuGetFile. Be sure to add this dependency if you want to keep it.
    Success = Chewie Succeeded!
'@
}

Import-LocalizedData -BindingVariable messages -ErrorAction SilentlyContinue

function Load-Configuration {
  param(
    [string] $configdir = $PSScriptRoot
  )
  if(!$script:chewie) {
    $script:chewie = @{}
  }
  $chewie.version = "2.0.0beta"
  $chewie.originalErrorActionPreference = $global:ErrorActionPreference
  $chewie.default_group_name = "default"
  $chewie.path = 'lib'
  if($nugetFile) {
    $chewie.nugetFile = $nugetFile
  } else {
    $chewie.nugetFile = ".NugetFile"
  }
  $chewie.packageNameFormat = "Executing task {0} for package {1}"
  $chewie.logo = "Chewie version {0}`nCopyright (c) 2012 Eric Ridgeway, Ian Davis" -f $chewie.version
  $chewie.verboseError = $false
  $chewie.coloredOutput = $true
  $chewie.version_packages = $false
  $chewie.sources = (Get-PackageSources)
  $chewie.DebugPreference = "SilentlyContinue"
  $chewie.success = $false
  $chewie.feed_uri = "http://nuget.org/api/v2/"
  $chewie.feed_package_filter = "FindPackagesById()?id='{0}'"
  # Many packages don't support semver
  #$semver = '(\d+\.\d+\.\d+)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?'
  # So the patch and fourth are optional :/
  #$chewie.VersionPattern = '(\d+\.\d+(?:\.\d+)?(?:\.\d+)?)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?'
  # But as also have to support NuGet's choice to ignore the - with a pre-release
  $chewie.VersionPattern = '(\d+\.\d+(?:\.\d+)?(?:\.\d+)?)(?:-?([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?'

  $configFilePath = (join-path $configdir "chewie-config.ps1")

  if (test-path $configFilePath -pathType Leaf) {
    try {
      . $configFilePath
    } catch {
      throw "Error Loading Configuration from chewie-config.ps1: " + $_
    }
  }
}

function New-Group {
  [CmdletBinding()]  
  param(
    [Parameter(Position=0,Mandatory=$true)][string]$name = $null,
    [Parameter(Position=1,Mandatory=$true)][scriptblock]$action = $null
  )
  $groupKey = $name.ToLower()    

  $defaultGroupName = $chewie.default_group_name
  try {
    $chewie.default_group_name = $name
    & $action
  } finally {
    $chewie.default_group_name = $defaultGroupName
  }
}

function New-NuGetVersion {
  [CmdletBinding()]  
  param(
    [Parameter(Position=0,Mandatory=$true)][string]$versionString = $null,
    [Parameter(Position=1,Mandatory=$false)][string]$build = $null,
    [Parameter(Position=2,Mandatory=$false)][string]$prerelease = $null
  )
  $targetVersion = $null
  if([Version]::TryParse($versionString, [ref] $targetVersion)) {
    $result =  New-Object PSObject |
      Add-Member -PassThru NoteProperty Version $targetVersion |
      Add-Member -PassThru NoteProperty Pre $prerelease |
      Add-Member -PassThru NoteProperty Build $build |
      Add-Member -PassThru -Force ScriptMethod ToString { ("{0}-{1}-{2}" -f @($this.Version, $this.Pre, $this.Build)).Trim('-') }
    $result
  } else {
    $null
  }
}

function New-NuSpecFile {
  param($fileName, $nugetFile = ".\.NuGetFile")
  # parse the .NuGetFile to build the dependency tree,
  # add a few other parameters for generating a template.
}

function Resolve-Chew {
  [CmdletBinding()]  
  param(
    [Parameter(Position=0,Mandatory=$true)][string]$name = $null,
    [Parameter(Position=1,Mandatory=$false)][alias("v")][string]$version = $null,
    [Parameter(Position=2,Mandatory=$false)][alias("group")][string[]]$groups = $null,
    [Parameter(Position=3,Mandatory=$false)][alias("s")][string]$source = $null,
    [Parameter(Position=4,Mandatory=$false)][alias("p")][switch]$prerelease = $false
  )
  if($groups -eq $null) {
    $groups = @($chewie.default_group_name)
  }

  if(!$source) {
    $source = $chewie.default_source
  }
  
  $newpackage = @{
    Name = $name
    Version = $version
    Prerelease = $prerelease
    Groups = $groups
    Duration = [System.TimeSpan]::Zero
    Source = $source
  }
  
  $packageKey = $name.ToLower()  
  
  Assert (!$chewie.Packages.ContainsKey($packageKey)) ($messages.error_duplicate_package_name -f $name)

  $chewie.Packages.$packageKey = $newpackage
}

Set-Alias -Name Chew -Value Resolve-Chew
# borrowed from Jeffrey Snover http://blogs.msdn.com/powershell/archive/2006/12/07/resolve-error.aspx
function Resolve-Error($ErrorRecord = $Error[0]) {
  $error_message = "`nErrorRecord:{0}ErrorRecord.InvocationInfo:{1}Exception:{2}"
  $formatted_errorRecord = $ErrorRecord | format-list * -force | out-string
  $formatted_invocationInfo = $ErrorRecord.InvocationInfo | format-list * -force | out-string
  $formatted_exception = ""
  $Exception = $ErrorRecord.Exception
  for ($i = 0; $Exception; $i++, ($Exception = $Exception.InnerException)) {
    $formatted_exception += ("$i" * 70) + "`n"
    $formatted_exception += $Exception | format-list * -force | out-string
    $formatted_exception += "`n"
  }

  return $error_message -f $formatted_errorRecord, $formatted_invocationInfo, $formatted_exception
}


function Resolve-NugetCommand {
  [CmdletBinding()]  
  param(
    [Parameter(Position=0,Mandatory=$true)][Hashtable]$package = $null
  )
  $nuGetIsInPath = @(get-command nuget.bat*,nuget.exe*,nuget.cmd* -ErrorAction SilentlyContinue).Length -gt 0
  $command = ""
  $here = "$(Split-Path -parent $script:MyInvocation.MyCommand.path)"
  $localInstall = (Join-Path $here "NuGet.exe")
  if($nuGetIsInPath) {
    $command += "NuGet install" 
  } elseif(Test-Path $localInstall) {
    $command =  $localInstall + " install"
  } else {
    Assert (@(get-command install-package -ErrorAction SilentlyContinue).Length -eq 1) $messages.error_no_valid_nuget_command_found
    $command += "install-package"  
  }
  
  $command += " $($package.name)"
  if($chewie.version_packages -ne $true){$command += " -x"}
  if(!(Test-Path $chewie.path)) { mkdir $chewie.path | Out-Null }
  $command += " -o $(Get-SafeFilePath $chewie.path)"
  $maxVersion = Get-MaxCompatibleVersion $package.Name $package.Version $package.source $package.Prerelease
  $versionString = "$maxVersion".Trim()
  if(![string]::IsNullOrEmpty($versionString)) { $command += " -version $versionString" }
  $source = $package.source
  if(![string]::IsNullOrEmpty($source)) { $command += " -source $source" }
  $command
}

function Set-PackagePath {
  param(
    [Parameter(Position=0,Mandatory=$true)]
    [string] $path = ""
  )
  Write-Debug "Attempting to determine path: $path"
  if(![System.IO.Path]::IsPathRooted($path)) {
    $path = (Join-Path $pwd $path)
  }
  Write-Debug "Setting path to: $path"
  $chewie.path = $path
}

Set-Alias -Name install_to -Value Set-PackagePath
function Set-Source {
  param(
    [Parameter(Position=0,Mandatory=$true)]
    [string] $source = $null
  )
  if($chewie.sources[$source]) {
    $chewie.default_source = $chewie.sources[$source]
  }
  else {
    $chewie.default_source = $source
  }
}

Set-Alias -Name Source -Scope Script -Value Set-Source

function Test-Outdated {
  [CmdletBinding()]
  param(
    [Parameter(Position=0,Mandatory=$true)][string]$packageName,
    [Parameter(Position=1,Mandatory=$true)][AllowEmptyString()][string]$versionSpec,
    [Parameter(Position=2,Mandatory=$false)][AllowEmptyString()][string]$source,
    [Parameter(Position=3,Mandatory=$false)][bool]$pre = $false
  )
  if(!(Test-PackageInstalled $packageName)) {
    Write-ColoredOutput "$packageName is not installed." -ForegroundColor Yellow
    return $true
  }
  $maxCompatibleVersion = Get-MaxCompatibleVersion $packageName $versionSpec $source $pre
  $installedVersion = Get-InstalledPackageVersion $packageName
  $trueMaxVersion = Find-MaxVersion @($maxCompatibleVersion,$installedVersion)
  [bool]$upToDate = "$trueMaxVersion" -eq "$installedVersion"
  return !$upToDate
}

function Test-PackageInstalled {
  [CmdletBinding()]
  param(
    [Parameter(Position=0,Mandatory=$true)] [string]$packageName
  )
  return (Get-PackageInstallationPaths $packageName).Length -gt 0
}
function Test-VersionCompatibility {
  [CmdletBinding()]
  param(
    [Parameter(Position=0,Mandatory=$true)][AllowEmptyString()][string]$versionSpec,
    [Parameter(Position=1,Mandatory=$true)][string]$versionString
  )
  if([string]::IsNullOrEmpty($versionSpec)) {return $true}
  if($versionSpec.StartsWith('(')) {
   $exlusiveLowerBound = $true
  }
  if($versionSpec.EndsWith(')')) {
   $exlusiveUpperBound = $true
  }
  $exactMatch = $versionSpec.StartsWith('[') -and $versionSpec.EndsWith(']') -and ($versionSpec.Contains(',') -eq $false)
  $versionSpec = $versionSpec.Trim('[',']','(',')')
  $lower,$upper = $versionSpec.Split(',')
  
  Assert (!($exlusiveLowerBound -and $exlusiveUpperBound -and [string]::IsNullOrEmpty($upper) -and ($versionSpec.Contains(',') -eq $false))) ($messages.error_invalid_version_spec -f $versionSpec)
  $current = (Get-VersionFromString $versionString).Version
  if([string]::IsNullOrEmpty($lower)) {$lower = (New-NuGetVersion "0.0").Version}
  if([string]::IsNullOrEmpty($upper)) {
    if($exactMatch) {
      $upper = $lower
    } else {
      $upper = (New-NuGetVersion "$([int]::MaxValue).$([int]::MaxValue).$([int]::MaxValue).$([int]::MaxValue)").Version
    }
  }
  
  if($exlusiveLowerBound) {
    if($exlusiveUpperBound) {
      return $lower -lt $current -and $current -lt $upper
    } else {
      return $lower -lt $current -and $current -le $upper
    }
  } else {
    if($exlusiveUpperBound) {
      return $lower -le $current -and $current -lt $upper
    } else {
      return $lower -le $current -and $current -le $upper
    }
  }
}

function Update-NuSpecDependencies {
  param($fileName, $nugetFile = ".\.NuGetFile")
  # parse the .NuGetFile to build the dependency tree
  # get the current spec file as xml and replace the dependencies node.
}
function Use-VersionPackageNumbers {
  param(
    [Parameter(Position=0,Mandatory=$false)]
    [string] $value = $true
  )
  $chewie.version_packages = $value
}

Set-Alias -Name Version_Packages -Value Use-VersionPackageNumbers

Set-Alias -Name VersionPackages -Value Use-VersionPackageNumbers

Set-Alias -Name IncludeVersion -Value Use-VersionPackageNumbers
# borrowed from psake https://github.com/psake/psake/blob/master/psake.psm1
function Write-ColoredOutput {
  param(
    [string] $message,
    [System.ConsoleColor] $foregroundcolor
  )
  if ($chewie.coloredOutput -eq $true) {
    if (($Host.UI -ne $null) -and ($Host.UI.RawUI -ne $null)) {
      $previousColor = $Host.UI.RawUI.ForegroundColor
      $Host.UI.RawUI.ForegroundColor = $foregroundcolor
    }
  }

  $message

  if ($previousColor -ne $null) {
    $Host.UI.RawUI.ForegroundColor = $previousColor
  }
}

function Write-Documentation() {
  Get-Help chewie
}
function chewie {
  [CmdletBinding()]
  param(
  [Parameter(Position=0,Mandatory=$true,ParameterSetName='install')]
  [switch] $install = $true,
  [Parameter(Position=0,Mandatory=$true,ParameterSetName='update')]
  [switch] $update = $false,
  [Parameter(Position=0,Mandatory=$true,ParameterSetName='uninstall')]
  [switch] $uninstall = $false,
  [Parameter(Position=0,Mandatory=$true,ParameterSetName='outdated')]
  [switch] $outdated = $false,
  [Parameter(Position=0,Mandatory=$true,ParameterSetName='init')]
  [switch] $init = $false,
  [Parameter(Position=0,Mandatory=$true,ParameterSetName='convert')]
  [switch] $convert = $false,
  [Parameter(Position=0,Mandatory=$true,ParameterSetName='downloadNuGet')]
  [switch] $downloadNuGet = $false,
  [Parameter(Position=0,Mandatory=$true,ParameterSetName='taskbound',HelpMessage="You must specify which task to execute.")]
  [ValidateSet('install','update', 'uninstall', 'outdated', 'init', 'help', '?', 'convert', 'downloadNuGet')]
  [string] $task,
  [Parameter(ParameterSetName='install')]
  [Parameter(ParameterSetName='update')]
  [Parameter(ParameterSetName='uninstall')]
  [Parameter(ParameterSetName='outdated')]
  [Parameter(ParameterSetName='taskbound')]
  [Parameter(ParameterSetName='init')]
  [Parameter(Position=1, Mandatory=$false)]
  [AllowEmptyString()]
  [AllowNull()]
  [string]$package = "",
  [Parameter(ParameterSetName='install')]
  [Parameter(ParameterSetName='update')]
  [Parameter(ParameterSetName='uninstall')]
  [Parameter(ParameterSetName='outdated')]
  [Parameter(ParameterSetName='init')]
  [Parameter(ParameterSetName='taskbound')]
  [AllowEmptyString()]
  [AllowNull()]
  [Parameter(Position=2,Mandatory=$false)]
  [string] $path,
  [Parameter(ParameterSetName='install')]
  [Parameter(ParameterSetName='update')]
  [Parameter(ParameterSetName='uninstall')]
  [Parameter(ParameterSetName='outdated')]
  [Parameter(ParameterSetName='init')]
  [Parameter(ParameterSetName='taskbound')]
  [AllowEmptyString()]
  [AllowNull()]
  [Parameter(Position=3,Mandatory=$false)]
  [string] $source,
  [Parameter(ParameterSetName='install')]
  [Parameter(ParameterSetName='update')]
  [Parameter(ParameterSetName='uninstall')]
  [Parameter(ParameterSetName='outdated')]
  [Parameter(ParameterSetName='init')]
  [Parameter(ParameterSetName='taskbound')]
  [AllowEmptyString()]
  [AllowNull()]
  [Parameter(Position=4,Mandatory=$false)]
  [string] $nugetFile = $null,
  [Parameter(ParameterSetName='taskbound')]
  [Parameter(ParameterSetName='install')]
  [Parameter(ParameterSetName='update')]
  [Parameter(ParameterSetName='outdated')]
  [Parameter(Position=5,Mandatory=$false)]
  [switch] $pre,
  [Parameter(ParameterSetName='taskbound')]
  [Parameter(Position=1,Mandatory=$false,ParameterSetName='convert')]
  [switch] $applyChanges,
  [Parameter(ParameterSetName='install')]
  [Parameter(ParameterSetName='update')]
  [Parameter(ParameterSetName='uninstall')]
  [Parameter(ParameterSetName='outdated')]
  [Parameter(ParameterSetName='init')]
  [Parameter(ParameterSetName='convert')]
  [Parameter(ParameterSetName='downloadNuGet')]
  [Parameter(ParameterSetName='taskbound')]
  [Parameter(Mandatory=$false)]
  [switch] $nologo = $false
  )
  if($PSCmdlet.ParameterSetName -ne "taskbound") {
    $task = $PSCmdlet.ParameterSetName
  } else {
    Set-Variable $task -Value $true -ErrorAction SilentlyContinue
  }

  $here = (Split-Path -parent $script:MyInvocation.MyCommand.path)

  if(!$skipFileLoading) {
    Resolve-Path $here\functions\*.ps1 | % { 
      if ($PSBoundParameters['Verbose']) {
        Write-ColoredOutput "Loading $_" -foregroundcolor Yellow
      }
      . $_.ProviderPath
    }
  }

  Load-Configuration
  
  if ($PSBoundParameters['Debug']) {
    $chewie.DebugPreference = "Continue"
    $chewie.verboseError = $true
    $PSBoundParameters.Keys | % { Write-ColoredOutput "$_ $($PSBoundParameters[$_])" -foregroundcolor Yellow }
    Write-ColoredOutput "Parameter Set Chosen $($PSCmdlet.ParameterSetName)" -foregroundcolor Yellow
  }

  if (-not $nologo) {
    Write-Output $chewie.logo
  }

  if ($help -or ($task -eq "?") -or ($task -eq "-?") -or ($task -eq "help") -or ($task -eq "-help")) {
    Write-Documentation
    return
  }

  if($downloadNuGet) {
    $webClient = New-Object System.Net.WebClient
    $webClient.DownloadFile("https://nuget.org/nuget.exe", (Join-Path $here "NuGet.exe"));
    return
  }

  if($nugetFile) { $chewie.nugetFile = $nugetFile }

  if($task -eq "init") {
    if(!(Test-Path $chewie.nugetFile)) {
      Write-Output "Creating $($chewie.nugetFile)"
      New-Item $chewie.nugetFile -ItemType File | Out-Null
      if($source) {
        Add-Content $chewie.nugetFile "source '$source'"
      }
      if($path) {
        Add-Content $chewie.nugetFile "install_to '$path'"
      } else {
        Add-Content $chewie.nugetFile "install_to 'lib'"
      }
      if($package) {
        Add-Content $chewie.nugetFile "chew '$package'"
      }
    }
    return
  }

  if($task -eq "convert") {
    if([string]::IsNullOrEmpty($path)) {
      $path = Resolve-Path .
    }
    ConvertTo-Chewie $path $applyChanges
    return
  }
  Write-Debug "Invoke-Chewie $task $package $without"
  Invoke-Chewie $task $package $without
}

chewie @args
