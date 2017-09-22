def packageAndArchive(buildMode, packageName, exclude) {
	zipFile = "bin\\${packageName}.zip"
	packageDir = "bin\\${packageName}\\"

	bat "IF EXIST ${zipFile} DEL ${zipFile}"
	bat "IF EXIST ${packageDir} RMDIR /S /Q ${packageDir}"

	bat "xcopy bin\\x64\\${buildMode} ${packageDir}"
	if (exclude.length() > 0) {
		bat "del ${packageDir}${exclude}"
	}
	if (buildMode == "Release") {
		bat "del ${packageDir}*.pdb"
	}
	powershell "Add-Type -Assembly System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::CreateFromDirectory(\"\$PWD\\${packageDir}\", \"\$PWD\\${zipFile}\")"
	archiveArtifacts artifacts: zipFile, caseSensitive: false, onlyIfSuccessful: true
}

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
		if (env.BRANCH_NAME == "master") {
			buildMode = "Release"
		} else {
			buildMode = "Debug"
		}
		bat "\"${tool 'MSBuild'}msbuild\" Torch.sln /p:Configuration=${buildMode} /p:Platform=x64 /t:Clean"
		bat "\"${tool 'MSBuild'}msbuild\" Torch.sln /p:Configuration=${buildMode} /p:Platform=x64"
	}

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

	stage('Archive') {
		archiveArtifacts artifacts: "bin/x64/${buildMode}/Torch*", caseSensitive: false, fingerprint: true, onlyIfSuccessful: true

		packageAndArchive(buildMode, "torch-server", "Torch.Client*")

		packageAndArchive(buildMode, "torch-client", "Torch.Server*")
	}

	if (env.BRANCH_NAME == "master") {
		gitVersion = bat(returnStdout: true, script: "@git describe --tags").trim()
		gitSimpleVersion = bat(returnStdout: true, script: "@git describe --tags --abbrev=0").trim()
		if (gitVersion == gitSimpleVersion) {
			stage('${buildMode}') {
				withCredentials([usernamePassword(credentialsId: 'torch-github', usernameVariable: 'USERNAME', passwordVariable: 'PASSWORD')]) {
					powershell "& ./Jenkins/${buildMode}.ps1 \"https://api.github.com/repos/TorchAPI/Torch/\" \"$gitSimpleVersion\" \"$USERNAME:$PASSWORD\" @(\"bin/torch-server.zip\", \"bin/torch-client.zip\")"
				}
			}
		}
	}
}