$buildSalt = $ENV:BUILD_NUMBER
$branchName = $ENV:BRANCH_NAME

# Writing build salt and branch name

$gitSimpleVersion = git describe --tags --abbrev=0 2>$null
if (!$gitSimpleVersion) {
    $gitSimpleVersion = "0.0.1" # Default version
}

$simpleVersionStandard = echo $gitSimpleVersion | Select-String -Pattern "([0-9]+)\.([0-9]+)\.([0-9]+)" | % {$_.Matches} | %{$_.Groups[1].Value+"."+$_.Groups[2].Value+"."+$_.Groups[3].Value}
$dotNetVersion = "$simpleVersionStandard.$buildSalt"
$infoVersion = -join(("$gitSimpleVersion" -replace "([0-9]+)\.([0-9]+)\.([0-9]+)","$dotNetVersion"), "-", "$branchName")

$fileContent = @"
using System.Reflection;

[assembly: AssemblyVersion("$dotNetVersion")]
[assembly: AssemblyInformationalVersion("$infoVersion")]
"@

echo $fileContent | Set-Content "$PSScriptRoot/AssemblyVersion.cs"

echo "Information Version: $infoVersion"
