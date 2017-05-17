properties {
	[string]$base_directory   = resolve-path "..\."
	[string]$release_directory  = "$base_directory\release"
	[string]$source_directory = "$base_directory\src\BoboBrowse.Net"
	[string]$nuget_package_directory = "$release_directory\packagesource"
	#[string]$test_results_directory = "$release_directory\testresults"
	[string]$solutionFile = "$source_directory\BoboBrowse.sln"
	[string]$versionFile = "$source_directory\PackageVersion.proj"

	[string]$packageVersion   = "0.0.0"  
	[string]$version          = "0.0.0"
	[string]$configuration    = "Release"
	[bool]$backupFiles        = $true

	[string]$common_assembly_info = "$source_directory\CommonAssemblyInfo.cs"
	[string]$copyright_year = [DateTime]::Today.Year.ToString() #Get the current year from the system
	[string]$copyright = "Copyright " + $([char]0x00A9) + " BoboBrowse.Net 2011 - $copyright_year"
	[string]$company_name = "BoboBrowse.Net"

	#test paramters
	[string]$frameworks_to_test = "netcoreapp1.0,net451"
	[string]$filter = ""
}

$backedUpFiles = New-Object System.Collections.ArrayList

task default -depends Test

task Clean -description "This task cleans up the build directory" {
	Remove-Item $release_directory -Force -Recurse -ErrorAction SilentlyContinue
	Get-ChildItem $base_directory -Include *.bak -Recurse | foreach ($_) {Remove-Item $_.FullName}
}

task Init -description "This tasks makes sure the build environment is correctly setup" {  
	if ($env:BuildRunner -ne $null -and $env:BuildRunner -eq "MyGet") {		
		$version = $packageVersion
		if ($version.Contains("-") -eq $true) {
			$version = $version.SubString(0, $version.IndexOf("-"))
		}
		echo "Updated version to: $version"
	}

	Write-Host "Base Directory: $base_directory"
	Write-Host "Release Directory: $release_directory"
	Write-Host "Source Directory: $source_directory"
	Write-Host "Output Directory: $output_directory"
	Write-Host "Template Directory: $template_directory"
	Write-Host "Version: $version"
	Write-Host "Package Version: $packageVersion"
	Write-Host "Configuration: $configuration"
	
	#Backup the original CommonAssemblyInfo.cs file
	Ensure-Directory-Exists "$release_directory"
}

task Restore -description "This task runs NuGet package restore" {
	Exec { 
		&dotnet msbuild $solutionFile /t:Restore
	}
}

task Compile -depends Clean, Init -description "This task compiles the solution" {

	Write-Host "Compiling..." -ForegroundColor Green

	#If build runner is MyGet or version is not passed in, parse it from $packageVersion
	if (($env:BuildRunner -ne $null -and $env:BuildRunner -eq "MyGet") -or $version -eq "0.0.0") {		
		$version = $packageVersion
		if ($version.Contains("-") -eq $true) {
			$version = $version.SubString(0, $version.IndexOf("-"))
		}
		Write-Host "Updated version to: $version" -ForegroundColor White
	}

	#Use only the major version as the assembly version.
	#This ensures binary compatibility unless the major version changes.
	$version-match "(^\d+)"
	$assemblyVersion = $Matches[0]
	$assemblyVersion = "$assemblyVersion.0.0"

	Write-Host "Assembly version set to: $assemblyVersion" -ForegroundColor Blue

	$pv = $packageVersion
	#check for presense of Git
	& where.exe git.exe
	if ($LASTEXITCODE -eq 0) {
		$gitCommit = ((git rev-parse --verify --short=10 head) | Out-String).Trim()
		$pv = "$packageVersion commit:[$gitCommit]"
	}

	try {
		Backup-File $common_assembly_info

		Generate-Assembly-Info `
			-fileVersion $version `
			-file $common_assembly_info

		Exec {
			&dotnet msbuild $solutionFile /t:Build `
				/p:Configuration=$configuration `
				/p:AssemblyVersion=$assemblyVersion `
				/p:InformationalVersion=$pv `
				/p:Company=$company_name `
				/p:Copyright=$copyright
		}
	} finally {
		Restore-File $common_assembly_info
	}
}

task Pack -depends Compile -description "This tasks creates the NuGet packages" {
	#create the nuget package output directory
	Ensure-Directory-Exists $nuget_package_directory

	pushd $base_directory
	$packages = Get-ChildItem -Path "$source_directory\**\*.csproj" -Recurse | ? { !$_.Directory.Name.Contains(".Test") }
	popd

	try {
		Backup-File $versionFile

		Generate-Version-File `
			-version $version `
			-packageVersion $packageVersion `
			-file $versionFile

		foreach ($package in $packages) {
			Write-Host "Creating NuGet package for $package..." -ForegroundColor Magenta
			Exec {
				&dotnet pack $package --output $nuget_package_directory --configuration $configuration --no-build --include-symbols
			}
		}
	} finally {
		Restore-File $versionFile
	}
}

task Test -depends Pack -description "This tasks runs the tests" {  
	Write-Host "Running tests..." -ForegroundColor DarkCyan

	pushd $base_directory
	$testProjects = Get-ChildItem -Path "$source_directory\**\*.csproj" -Recurse | ? { $_.Directory.Name.Contains(".Tests") }
	popd

	Write-Host "frameworks_to_test: $frameworks_to_test" -ForegroundColor Yellow

	$frameworksToTest = $frameworks_to_test -split "\s*?,\s*?"

	foreach ($framework in $frameworksToTest) {
		Write-Host "Running tests for framework: $framework" -ForegroundColor Green

		foreach ($testProject in $testProjects) {
			$testName = $testProject.Directory.Name
			$testExpression = "dotnet.exe test '$testProject' --configuration $configuration --framework $framework --no-build"

			#$testResultDirectory = "$test_results_directory\$framework\$testName"
			#Ensure-Directory-Exists $testResultDirectory
			#$testExpression = "$testExpression --result:$testResultDirectory\TestResult.xml"

			if ($filter -ne $null -and (-Not [System.String]::IsNullOrEmpty($filter))) {
				$testExpression = "$testExpression --filter $filter"
			}

			Exec {
				Invoke-Expression $testExpression
			}
		}
	}
}

function Generate-Version-File {
param(
	[string]$packageVersion,
	[string]$file = $(throw "file is a required parameter.")
)

  $versionFile = "<Project>
	<PropertyGroup>
		<PackageVersion>$packageVersion</PackageVersion>
	</PropertyGroup>
</Project>
"
	$dir = [System.IO.Path]::GetDirectoryName($file)
	Ensure-Directory-Exists $dir

	Write-Host "Generating version file: $file"
	Out-File -filePath $file -encoding UTF8 -inputObject $versionFile
}

function Generate-Assembly-Info {
param(
	[string]$fileVersion,
	[string]$file = $(throw "file is a required parameter.")
)

  $asmInfo = "using System;
using System.Reflection;

[assembly: AssemblyFileVersion(""$fileVersion"")]
"
	$dir = [System.IO.Path]::GetDirectoryName($file)
	Ensure-Directory-Exists $dir

	Write-Host "Generating assembly info file: $file"
	Out-File -filePath $file -encoding UTF8 -inputObject $asmInfo
}

function Backup-Files([string[]]$paths) {
	foreach ($path in $paths) {
		Backup-File $path
	}
}

function Backup-File([string]$path) {
	if ($backupFiles -eq $true) {
		Copy-Item $path "$path.bak" -Force
		$backedUpFiles.Insert(0, $path)
	} else {
		Write-Host "Ignoring backup of file $path" -ForegroundColor DarkRed
	}
}

function Restore-Files([string[]]$paths) {
	foreach ($path in $paths) {
		Restore-File $path
	}
}

function Restore-File([string]$path) {
	if ($backupFiles -eq $true) {
		if (Test-Path "$path.bak") {
			Move-Item "$path.bak" $path -Force
		}
		$backedUpFiles.Remove($path)
	}
}

function Ensure-Directory-Exists([string] $path)
{
	if (!(Test-Path $path)) {
		New-Item $path -ItemType Directory
	}
}