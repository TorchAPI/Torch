pushd

$steamData = "C:/Steam/Data/"
$steamCMDPath = "C:/Steam/steamcmd/"
$steamCMDZip = "C:/Steam/steamcmd.zip"

Add-Type -AssemblyName System.IO.Compression.FileSystem

if (!(Test-Path $steamData)) {
	mkdir "$steamData"
}
if (!(Test-Path $steamCMDPath)) {
	if (!(Test-Path $steamCMDZip)) {
		(New-Object System.Net.WebClient).DownloadFile("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip", "$steamCMDZip");
	}
	[System.IO.Compression.ZipFile]::ExtractToDirectory($steamCMDZip, $steamCMDPath)
}

cd "$steamData"
& "$steamCMDPath/steamcmd.exe" "+login anonymous" "+force_install_dir $steamData" "+app_update 298740 +beta mptest +betapassword nt7WuDw9kdvB" "+quit"

popd