properties {
	[string]$base_directory   = resolve-path "..\."
	[string]$release_directory  = "$base_directory\release" #was build_directory
	[string]$source_directory = "$base_directory\src"
	[string]$nuget_directory  = "$source_directory\.nuget"
	[string]$output_directory = "$release_directory\packagesource"
	[string]$template_directory = "$base_directory\build\templates"
	[string]$version          = "1.0.0"
	[string]$packageVersion   = "$version-pre"
	[string]$configuration    = "Release"
	[string[]]$target_frameworks = @("net35", "net40", "net45")
	[string]$common_assembly_info = "$source_directory\Common\CommonAssemblyInfo.cs"
}

task default -depends Finalize

task Clean -description "This task cleans up the build directory" {
	Remove-Item $release_directory -Force -Recurse -ErrorAction SilentlyContinue
	
	Write-Host "Base Directory: $base_directory"
	Write-Host "Release Directory: $release_directory"
	Write-Host "Source Directory: $source_directory"
	Write-Host "NuGet Directory: $nuget_directory"
	Write-Host "Output Directory: $output_directory"
	Write-Host "Template Directory: $template_directory"
	Write-Host "Version: $version"
	Write-Host "Package Version: $packageVersion"
	Write-Host "Configuration: $configuration"
}

task Init -description "This tasks makes sure the build environment is correctly setup" {  
	if ($env:BuildRunner -ne $null -and $env:BuildRunner -eq "MyGet") {		
		$version = $packageVersion
		if ($version.Contains("-") -eq $true) {
			$version = $version.SubString(0, $version.IndexOf("-"))
		}
		echo "Updated version to: $version"
	}
	
	#Backup the original CommonAssemblyInfo.cs file
	Ensure-Directory-Exists "$release_directory"
	Move-Item $common_assembly_info "$common_assembly_info.bak" -Force

	#Get the current year from the system
	$year = [DateTime]::Today.Year

	Generate-Assembly-Info `
		-file $common_assembly_info `
		-company "BoboBrowse.Net" `
		-version $version `
		-packageVersion $packageVersion `
		-copyright "Copyright © BoboBrowse.Net 2011 - $year"
}

task Restore -depends Clean -description "This task runs NuGet package restore" {
	exec { 
		&"$nuget_directory\NuGet.exe" restore "$source_directory\BoboBrowse.sln"
	}
}

task Compile -depends Clean, Init, Restore -description "This task compiles the solution" {

	Write-Host "Compiling..." -ForegroundColor Green

	Build-Framework-Versions $target_frameworks
}

task Package -depends Compile -description "This tasks makes creates the NuGet packages" {
	
	#create the nuget package output directory
	Ensure-Directory-Exists "$output_directory"

	Create-BoboBrowse-Package
}

task Finalize -depends Package -description "This tasks finalizes the build" {  
	#Restore the original CommonAssemblyInfo.cs file from backup
	Remove-Item $common_assembly_info -Force -ErrorAction SilentlyContinue
	Move-Item "$common_assembly_info.bak" $common_assembly_info -Force
}

function Create-BoboBrowse-Package {
	$output_nuspec_file = "$release_directory\BoboBrowse.Net\BoboBrowse.Net.nuspec"
	Copy-Item "$template_directory\BoboBrowse.Net\BoboBrowse.Net.nuspec" "$output_nuspec_file"
	
	#copy sources for symbols package
	Copy-Item -Recurse -Filter *.cs -Force "$source_directory\BoboBrowse.Net" "$release_directory\BoboBrowse.Net\src"
	
	exec { 
		&"$nuget_directory\NuGet.exe" pack $output_nuspec_file -Symbols -Version $packageVersion -OutputDirectory $output_directory
	}
}

function Build-Framework-Versions ([string[]] $target_frameworks) {
	#create the build for each version of the framework
	foreach ($target_framework in $target_frameworks) {
		Build-Framework-Version $target_framework
	}
}

function Build-Framework-Version ([string] $target_framework) {
	$target_framework_upper = $target_framework.toUpper()
	$msbuild_configuration = "$target_framework_upper-$configuration"
	$outdir = "$release_directory\bobobrowse.net\lib\$target_framework\"
	
	Write-Host "Compiling BoboBrowse.Net for $target_framework_upper" -ForegroundColor Blue

	exec { 
		msbuild "$source_directory\BoboBrowse.Net\BoboBrowse.Net.csproj" `
			/property:outdir=$outdir `
			/verbosity:quiet `
			/property:Configuration=$msbuild_configuration `
			"/t:Clean;Rebuild" `
			/property:WarningLevel=3 `
			/property:EnableNuGetPackageRestore=true
	}
	
	dir $outdir | ?{ -not($_.Name -match 'BoboBrowse.Net|LuceneExt.Net') } | %{ del $_.FullName }
}

function Ensure-Directory-Exists([string] $path)
{
	if ([System.IO.Path]::GetFileName($path) -eq "") {
		#add a fake file name if it doesn't exist
		$file = "$path\dummy.tmp"
		$dir = [System.IO.Path]::GetDirectoryName($file)
	}
	elseif ($path.EndsWith("\") -eq $true) {
		#add a fake file name and slash if it is missing
		$file = "$pathdummy.tmp"
		$dir = [System.IO.Path]::GetDirectoryName($file)
	} else {
		#assume the path contains a file name
		$dir = $path
	}
	if ([System.IO.Directory]::Exists($dir) -eq $false) {
		Write-Host "Creating directory $dir"
		[System.IO.Directory]::CreateDirectory($dir)
	}
}

function Generate-Assembly-Info
{
param(
	[string]$copyright, 
	[string]$version,
	[string]$packageVersion,
	[string]$company,
	[string]$file = $(throw "file is a required parameter.")
)
  $asmInfo = "using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: CLSCompliantAttribute(false)]
[assembly: ComVisibleAttribute(false)]
[assembly: AssemblyCompanyAttribute(""$company"")]
[assembly: AssemblyCopyrightAttribute(""$copyright"")]
[assembly: AssemblyVersionAttribute(""$version"")]
[assembly: AssemblyInformationalVersionAttribute(""$packageVersion"")]
[assembly: AssemblyFileVersionAttribute(""$version"")]
[assembly: AssemblyDelaySignAttribute(false)]
"
	$dir = [System.IO.Path]::GetDirectoryName($file)
	if ([System.IO.Directory]::Exists($dir) -eq $false)
	{
		Write-Host "Creating directory $dir"
		[System.IO.Directory]::CreateDirectory($dir)
	}

	Write-Host "Generating assembly info file: $file"
	out-file -filePath $file -encoding UTF8 -inputObject $asmInfo
}