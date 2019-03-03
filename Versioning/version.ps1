$buildSalt = $Env:BUILD_NUMBER
$gitVersion = git describe --tags
$gitSimpleVersion = git describe --tags --abbrev=0
$simpleVersionStandard = echo $gitSimpleVersion | Select-String -Pattern "([0-9]+)\.([0-9]+)\.([0-9]+)" | % {$_.Matches} | %{$_.Groups[1].Value+"."+$_.Groups[2].Value+"."+$_.Groups[3].Value}
$dotNetVersion = "$simpleVersionStandard.$buildSalt"
$infoVersion = "$gitSimpleVersion" -replace "([0-9]+)\.([0-9]+)\.([0-9]+)","$dotNetVersion" + "$Env.BRANCH_NAME"

$fileContent = @"
using System.Reflection;

[assembly: AssemblyVersion("$dotNetVersion")]
[assembly: AssemblyInformationalVersion("$infoVersion")]
"@

echo $fileContent | Set-Content "$PSScriptRoot/AssemblyVersion.cs"

echo "$infoVersion"