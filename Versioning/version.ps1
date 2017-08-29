$gitVersion = git describe --tags
$gitSimpleVersion = git describe --tags --abbrev=0
if ($gitSimpleVersion.Equals($gitVersion)) {
	$buildSalt = 0
} else {
	$gitLatestCommit = git rev-parse HEAD
	$buildSalt = [System.Numerics.BigInteger]::Abs([System.Numerics.BigInteger]::Parse($gitLatestCommit, [System.Globalization.NumberStyles]::HexNumber) % 9988) + 1
}
$dotNetVersion = echo $gitSimpleVersion | Select-String -Pattern "([0-9]+)\.([0-9]+)\.([0-9]+)" | % {$_.Matches} | %{$_.Groups[1].Value+"."+$_.Groups[2].Value+".$buildSalt."+$_.Groups[3].Value}

$fileContent = @"
using System.Reflection;

[assembly: AssemblyVersion("$dotNetVersion")]
[assembly: AssemblyInformationalVersion("$gitVersion")]
"@

echo $fileContent | Set-Content "$PSScriptRoot/AssemblyVersion.cs"

echo "$gitVersion / $dotNetVersion"