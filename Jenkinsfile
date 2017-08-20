node {
	stage('Checkout') {
		checkout scm
	}

	stage('Acquire SE') {
		bat 'powershell -File jenkins-grab-se.ps1'
		bat 'IF EXIST GameBinaries RMDIR GameBinaries'
		bat 'mklink /J GameBinaries "C:/Steam/Data/DedicatedServer64/"'		
	}

	stage('Acquire NuGet Packages') {
		bat 'nuget restore Torch.sln'
	}

	stage('Build') {
		bat "\"${tool 'MSBuild'}msbuild\" Torch.sln /p:Configuration=Release /p:Platform=x64"
	}

	stage('Archive') {
		archive 'bin/x64/Release/Torch.*'
	}
}
