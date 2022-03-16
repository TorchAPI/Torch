$buildSalt = $Env:BUILD_NUMBER
$branchName = $Env:BRANCH_NAME
$gitSimpleVersion = git describe --tags --abbrev=0
$simpleVersionStandard = echo $gitSimpleVersion | Select-String -Pattern "([0-9]+)\.([0-9]+)\.([0-9]+)" | % {$_.Matches} | %{$_.Groups[1].Value+"."+$_.Groups[2].Value+"."+$_.Groups[3].Value}
Write-Host "$simpleVersionStandard.$buildSalt"