node {
	stage 'Checkout'
		checkout scm

	stage 'Build'
		bat 'powershell -File jenkins-grab-se.ps1'
		bat 'rmdir GameBinaries'
		bat 'mklink /J GameBinaries "C:/Steam/Data/DedicatedServer64/"'
		bat 'nuget restore Torch.sln'
		bat "\"${tool 'MSBuild'}\" Torch.sln /p:Configuration=Release /p:Platform=x64"

	state 'Archive'
		archive 'bin/x64/Release/Torch.*'
}