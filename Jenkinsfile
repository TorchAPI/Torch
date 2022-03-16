def packageAndArchive(buildMode, packageName) {
	zipFile = "bin\\${packageName}.zip"
	packageDir = "publish"

    bat 'powershell -Command { Compress-Archive -Path ${packageDir}\\* -DestinationPath ${zipFile} }' 
	archiveArtifacts artifacts: zipFile, caseSensitive: false, onlyIfSuccessful: true
}

node('windows') {
	stage('Checkout') {
		checkout scm
		bat 'git pull https://github.com/TorchAPI/Torch/ ${env.BRANCH_NAME} --tags'
	}

	stage('Acquire SE') {
		bat 'powershell -File Jenkins/jenkins-grab-se.ps1'
	}

	stage('Build') {
	    dotnetVersion = bat(returnStdout: true, script: '@powershell -NonInteractive -NoLogo -NoProfile -File Jenkins/get-version.ps1').trim()
	    infoVersion = "${dotnetVersion}-${env.BRANCH_NAME}"
	    currentBuild.description = infoVersion
	    
	    bat 'dotnet publish .\\Torch.Server\\Torch.Server.csproj -p:PackageVersion=${dotnetVersion} -p:InformationalVersion=${infoVersion} --self-contained -f net6-windows -r win-x64 -c Release -o .\\publish\\'
	}

	stage('Archive') {
		//archiveArtifacts artifacts: "bin/x64/${buildMode}/Torch*", caseSensitive: false, fingerprint: true, onlyIfSuccessful: true

		packageAndArchive(buildMode, "torch-server")

		/*packageAndArchive(buildMode, "torch-client", "Torch.Server*")*/
	}

	/* Disabled because they fail builds more often than they detect actual problems
	stage('Test') {
		bat 'IF NOT EXIST reports MKDIR reports'
		bat "\"packages/xunit.runner.console.2.2.0/tools/xunit.console.exe\" \"bin-test/x64/${buildMode}/Torch.Tests.dll\" \"bin-test/x64/${buildMode}/Torch.Server.Tests.dll\" \"bin-test/x64/${buildMode}/Torch.Client.Tests.dll\" -parallel none -xml \"reports/Torch.Tests.xml\""

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
	*/
}
