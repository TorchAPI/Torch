param([string] $ApiBase, [string]$tagName, [string]$authinfo, [string[]] $assetPaths)
Add-Type -AssemblyName "System.Web"

$headers = @{
    Authorization = "Basic " + [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($authinfo))
    Accept = "application/vnd.github.v3+json"
}
try
{
	Write-Output("Checking if release with tag " + $tagName + " already exists...")
	$release = Invoke-RestMethod -Uri ($ApiBase+"releases/tags/$tagName") -Method "GET" -Headers $headers
	Write-Output("    Using existing release " + $release.id + " at " + $release.html_url)
} catch {
	Write-Output("    Doesn't exist")
	$rel_arg = @{
	    tag_name=$tagName
	    name="Generated $tagName"
	    body=""
	    draft=$TRUE
		prerelease=$tagName.Contains("alpha") -or $tagName.Contains("beta")
	}
	Write-Output("Creating new release " + $tagName + "...")
	$release = Invoke-RestMethod -Uri ($ApiBase+"releases") -Method "POST" -Headers $headers -Body (ConvertTo-Json($rel_arg))
	Write-Output("    Created new release " + $tagName + " at " + $release.html_url)
}

$assetsApiBase = $release.assets_url
Write-Output("Checking for existing assets...")
$existingAssets = Invoke-RestMethod -Uri ($assetsApiBase) -Method "GET" -Headers $headers
$assetLabels = ($assetPaths | ForEach-Object {[System.IO.Path]::GetFileName($_)})
foreach ($asset in $existingAssets) {
	if ($assetLabels -contains $asset.name) {
		$uri = $asset.url
		Write-Output("    Deleting old asset " + $asset.name + " (id " + $asset.id + "); URI=" + $uri)
		$result = Invoke-RestMethod -Uri $uri -Method "DELETE" -Headers $headers
	}
}
Write-Output("Uploading assets...")
$uploadUrl = $release.upload_url.Substring(0, $release.upload_url.LastIndexOf('{'))
foreach ($asset in $assetPaths) {
	$assetName = [System.IO.Path]::GetFileName($asset)
	$assetType = [System.Web.MimeMapping]::GetMimeMapping($asset)
	$assetData = [System.IO.File]::ReadAllBytes($asset)
	$headerExtra = $headers + @{
		"Content-Type" = $assetType
		Name = $assetName
	}
	$uri = $uploadUrl + "?name=" + $assetName
	Write-Output("    Uploading " + $asset + " as " + $assetType + "; URI=" + $uri)
	$result = Invoke-RestMethod -Uri $uri -Method "POST" -Headers $headerExtra -Body $assetData
	Write-Output("        ID=" + $result.id + ", found at=" + $result.browser_download_url)
}