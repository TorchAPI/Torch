node {
	stages {
		stage('Checkout') {
			checkout scm
		}

		stage('Acquire SE') {
			bat 'powershell -File jenkins-grab-se.ps1'
			bat 'rmdir GameBinaries'
			bat 'mklink /J GameBinaries "C:/Steam/Data/DedicatedServer64/"'		
		}
		
		stage('Acquire NuGet Packages') {
			bat 'nuget restore Torch.sln'
		}

		stage('Build') {
			bat "\"${tool 'MSBuild'}\" Torch.sln /p:Configuration=Release /p:Platform=x64"
		}

		state('Archive') {
			archive 'bin/x64/Release/Torch.*'
		}
	}
}