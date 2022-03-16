pushd

$steamData = "C:/Steam/Data/"
$steamCMDPath = "C:/Steam/steamcmd/"
$steamCMDZip = "C:/Steam/steamcmd.zip"

if (!(Test-Path $steamData)) {
	mkdir "$steamData"
}
if (!(Test-Path $steamCMDPath)) {
	if (!(Test-Path $steamCMDZip)) {
		Invoke-WebRequest -OutFile $steamCMDZip https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip
	}
	Expand-Archive $steamCMDZip -DestinationPath $steamCMDPath
}
& "$steamCMDPath/steamcmd.exe" "+login anonymous" "+force_install_dir $steamData" "+app_update 298740" "+quit"

$dataPath = $steamData.Replace("/", "\");
$contentPath = "$dataPath\Content";
if (Test-Path $contentPath) {
	Remove-Item -LiteralPath $contentPath -Force -Recurse
}

cmd /S /C mklink /J .\GameBinaries $dataPath\DedicatedServer64

popd
