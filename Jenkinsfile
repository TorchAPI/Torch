node {
	stage('Checkout') {
		checkout scm
		bat 'git pull --tags'
	}

	stage('Acquire SE') {
		bat 'powershell -File Jenkins/jenkins-grab-se.ps1'
		bat 'IF EXIST GameBinaries RMDIR GameBinaries'
		bat 'mklink /J GameBinaries "C:/Steam/Data/DedicatedServer64/"'		
	}

	stage('Acquire NuGet Packages') {
		bat 'nuget restore Torch.sln'
	}

	stage('Build') {
		currentBuild.description = bat(returnStdout: true, script: '@powershell -File Versioning/version.ps1').trim()
		bat "\"${tool 'MSBuild'}msbuild\" Torch.sln /p:Configuration=Release /p:Platform=x64"
	}

	stage('Test') {
		bat 'IF NOT EXIST reports MKDIR reports'
		bat "\"packages/xunit.runner.console.2.2.0/tools/xunit.console.exe\" \"bin-test/x64/Release/Torch.Tests.dll\" \"bin-test/x64/Release/Torch.Server.Tests.dll\" \"bin-test/x64/Release/Torch.Client.Tests.dll\" -parallel none -xml \"reports/Torch.Tests.xml\""
	    step([
	        $class: 'XUnitBuilder',
	        thresholdMode: 1,
	        thresholds: [[$class: 'FailedThreshold', failureThreshold: '1']],
	        tools: [[
	            $class: 'XUnitDotNetTestType',
	            deleteOutputFiles: true,
	            failIfNotNew: true,
	            pattern: 'reports/*.xml',
	            skipNoTestFiles: false,
	            stopProcessingIfError: true
	        ]]
	    ])
	}

	stage('Archive') {
		bat '''IF EXIST bin\\torch-server.zip DEL bin\\torch-server.zip
		IF EXIST bin\\package-server RMDIR /S /Q bin\\package-server
		xcopy bin\\x64\\Release bin\\package-server\\
		del bin\\package-server\\Torch.Client*'''
		bat "powershell -Command \"Add-Type -Assembly System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::CreateFromDirectory(\\\"\$PWD\\bin\\package-server\\\", \\\"\$PWD\\bin\\torch-server.zip\\\")\""
		archiveArtifacts artifacts: 'bin/torch-server.zip', caseSensitive: false, onlyIfSuccessful: true

		bat '''IF EXIST bin\\torch-client.zip DEL bin\\torch-client.zip
		IF EXIST bin\\package-client RMDIR /S /Q bin\\package-client
		xcopy bin\\x64\\Release bin\\package-client\\
		del bin\\package-client\\Torch.Server*'''
		bat "powershell -Command \"Add-Type -Assembly System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::CreateFromDirectory(\\\"\$PWD\\bin\\package-client\\\", \\\"\$PWD\\bin\\torch-client.zip\\\")\""
		archiveArtifacts artifacts: 'bin/torch-client.zip', caseSensitive: false, onlyIfSuccessful: true

		archiveArtifacts artifacts: 'bin/x64/Release/Torch*', caseSensitive: false, fingerprint: true, onlyIfSuccessful: true
	}

	gitVersion = bat(returnStdout: true, script: "@git describe --tags").trim()
	gitSimpleVersion = bat(returnStdout: true, script: "@git describe --tags --abbrev=0").trim()
	if (gitVersion == gitSimpleVersion) {
		stage('Release') {
			withCredentials([usernamePassword(credentialsId: 'e771beac-b3ee-4bc9-82b7-40a6d426d508', usernameVariable: 'USERNAME', passwordVariable: 'PASSWORD')]) {
				powershell "./Jenkins/release.ps1 \"https://api.github.com/repos/TorchAPI/Torch/\" \"$gitSimpleVersion\" \"$USERNAME:$PASSWORD\" @(\"bin/torch-server.zip\", \"bin/torch-client.zip\")"
			}
		}
	}
}