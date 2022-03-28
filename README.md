# ProbablyFriends
This is an example project that uses a Jenkins pipeline I setup. Thanks for checking it out!

## Overview

The details of the Jenkins pipeline are documented in this readme along with steps to recreate the work I've done. This repository contains a simple ASP .NET 6 web application with Razor pages and a unit test project. The configuration for the Jenkins pipeline is listed below as an XML code block along with a deployment script written in Python.

## Setting up the .NET Project

From Visual Studio 2022 I created a new ASP .NET Core Web App project and selected .NET 6.0 for the framework. I wanted to showcase running unit tests as a build step in the pipeline, so I added a XUnit test project to the solution as well. Within Index.cshtml.cs I wrote a very simple method that returns a list of three names:

```C#
  public List<string> GetFriendNames()
  {
      return new List<string> { "Matt", "Michael", "Andrew" };
  }
```
In order to test this method, I wrote a single unit test that checks that the method really does return three names:

```C#
  [Fact]
  public void AreThreeFriendNamesReturned_GetFriendNames()
  {
      IndexModel indexModel = new IndexModel();

      List<string> friendNames = indexModel.GetFriendNames();

      Assert.True(friendNames.Count == 3);
  }
```
Before heading over to Jenkins, I set the project up in source control by selecting the "Add to Source Control" option and creating a public repository on GitHub.

## Configuring the Jenkins Pipeline

After installing Jenkins I created a new freestyle project. The first configuration I setup was for the GitHub project, selecting the options for GitHub under the general and SCM sections. I then started making the build steps.

### Parameters

This is a string parameter that is fed into the Python script that handles the deployment.
```
DEPLOY_TARGET_PATH
```

This is a string parameter that is also used in the deployment script for determining where backups of the last deploy should be stored.
```
DEPLOY_BACKUP_PATH
```

Another string parameter used as an argument for the runtime when publishing the web app.
```
RUNTIME
```

### Build Steps

The first step is building the entire solution. If this step fails, then there is a problem with either the unit test project or the main project.

```
dotnet build --configuration Release
```

The next step is to run the unit tests. If any tests fail then the pipeline fails.

```
dotnet test
```

The final step in the build is to publish the artifacts. In this case the .NET framework is bundled with the application using the "self-contained" argument.

```
dotnet publish ProbablyFriends --configuration Release --runtime %RUNTIME% --self-contained
```

### Post-Build Steps

Following a successful build the deployment script will kick off using the below command.

```
python Deploy.py "%WORKSPACE%\ProbablyFriends\bin\Release\net6.0\win-x64\publish" "%DEPLOY_TARGET_PATH%" "%DEPLOY_BACKUP_PATH%"
```

Below are the contents of the deployment script. The script will stop the Windows service that hosts the web app, create a backup, move the artifacts to the target folder, and then start the service back up. If an exception occurs during the deployment then a roll back will occur and restore the app to the backup.

```Python
import datetime
import shutil
import os
import sys

_current_time = datetime.datetime.now()
_file_time_stamp = f"{_current_time.year}-{_current_time.hour}-{_current_time.minute}-{_current_time.second}"
_file_name = f"deployment-{_file_time_stamp}"
_backup_file_name = f'backup-{_file_time_stamp}'


def run_deploy(source_path, target_path, backup_path):
    print(f"Running deploy for target path: {target_path}")

    try:
        # Stop Service
        print("Stopping service...")
        os.system("sc stop ProbablyFriends")

        # Create backup
        print("Creating backup...")
        shutil.make_archive(os.path.join(
            backup_path, _backup_file_name), 'zip', target_path)
        
        # Zip artifacts and copy to target
        print("Zipping artifacts and copying to target...")
        shutil.make_archive(
            os.path.join(target_path, _file_name), 'zip', source_path)

        # Unzip artifacts
        print("Unpacking artifacts...")
        shutil.unpack_archive(os.path.join(
            target_path, f"{_file_name}.zip"), target_path)

        # Delete archive
        print("Deleting archive...")
        os.remove(os.path.join(target_path, f"{_file_name}.zip"))

        # Start Service
        print("Starting Service...")
        os.system("sc start ProbablyFriends")

    except:
        roll_back(target_path, backup_path)

    print("Done!")


def roll_back(target_path, backup_path):
    print(f"An Exception occured. Rolling back deploy.")
    os.remove(target_path)
    os.write(target_path, 'x')
    shutil.unpack_archive(os.path.join(
        backup_path, f"{_backup_file_name}.zip"), target_path)


run_deploy(sys.argv[1], sys.argv[2], sys.argv[3])

```


### Email Notification on Build Failures

I configued Jenkins to use Gmail's SMTP server. Upon any failures an email is sent out to my email address.

![image](https://user-images.githubusercontent.com/47993107/160306441-3de6773c-5d17-438d-be41-0efde5b16f07.png)


## Creating the Windows Service

I used the following PowerShell command to create a new Windows Service. By default Kestrel uses port 5000 to host the app.

```PowerShell
New-Service -Name ProbablyFriends -BinaryPathName "C:\APPLICATIONS\ProbablyFriends\ProbablyFriends.exe" -Description "Probably Friends Web App" -DisplayName "Probably Friends" -StartupType Automatic
```

## Job Config File

```XML
<?xml version='1.1' encoding='UTF-8'?>
<project>
  <actions/>
  <description>Build pipeline for ProbablyFriends site.</description>
  <keepDependencies>false</keepDependencies>
  <properties>
    <com.coravy.hudson.plugins.github.GithubProjectProperty plugin="github@1.34.3">
      <projectUrl>https://github.com/andrewettensohn/ProbablyFriends/</projectUrl>
      <displayName></displayName>
    </com.coravy.hudson.plugins.github.GithubProjectProperty>
    <hudson.model.ParametersDefinitionProperty>
      <parameterDefinitions>
        <hudson.model.StringParameterDefinition>
          <name>DEPLOY_TARGET_PATH</name>
          <description>The target path that the application will be deployed to.</description>
          <defaultValue>C:\APPLICATIONS\ProbablyFriends</defaultValue>
          <trim>false</trim>
        </hudson.model.StringParameterDefinition>
        <hudson.model.StringParameterDefinition>
          <name>DEPLOY_BACKUP_PATH</name>
          <description>Backups of the application will go into this folder during deployment</description>
          <defaultValue>C:\APPLICATIONS\ProbablyFriendsBackups</defaultValue>
          <trim>false</trim>
        </hudson.model.StringParameterDefinition>
        <hudson.model.StringParameterDefinition>
          <name>RUNTIME</name>
          <description>This parameter will be used when publishing artifacts. Example: for Ubuntu 18.04 &quot;ubuntu.18.04-x64&quot; or for Windows 64-bit &quot;win-x64&quot;.</description>
          <defaultValue>win-x64</defaultValue>
          <trim>false</trim>
        </hudson.model.StringParameterDefinition>
      </parameterDefinitions>
    </hudson.model.ParametersDefinitionProperty>
  </properties>
  <scm class="hudson.plugins.git.GitSCM" plugin="git@4.10.3">
    <configVersion>2</configVersion>
    <userRemoteConfigs>
      <hudson.plugins.git.UserRemoteConfig>
        <url>https://github.com/andrewettensohn/ProbablyFriends</url>
      </hudson.plugins.git.UserRemoteConfig>
    </userRemoteConfigs>
    <branches>
      <hudson.plugins.git.BranchSpec>
        <name>*/master</name>
      </hudson.plugins.git.BranchSpec>
    </branches>
    <doGenerateSubmoduleConfigurations>false</doGenerateSubmoduleConfigurations>
    <submoduleCfg class="empty-list"/>
    <extensions/>
  </scm>
  <canRoam>true</canRoam>
  <disabled>false</disabled>
  <blockBuildWhenDownstreamBuilding>false</blockBuildWhenDownstreamBuilding>
  <blockBuildWhenUpstreamBuilding>false</blockBuildWhenUpstreamBuilding>
  <triggers/>
  <concurrentBuild>false</concurrentBuild>
  <builders>
    <hudson.tasks.BatchFile>
      <command>dotnet build --configuration Release</command>
      <configuredLocalRules/>
    </hudson.tasks.BatchFile>
    <hudson.tasks.BatchFile>
      <command>dotnet test</command>
      <configuredLocalRules/>
    </hudson.tasks.BatchFile>
    <hudson.tasks.BatchFile>
      <command>dotnet publish ProbablyFriends --configuration Release --runtime %RUNTIME% --self-contained</command>
      <configuredLocalRules/>
    </hudson.tasks.BatchFile>
  </builders>
  <publishers>
    <hudson.tasks.ArtifactArchiver>
      <artifacts>ProbablyFriends/bin/Release/net6.0/**/*</artifacts>
      <allowEmptyArchive>false</allowEmptyArchive>
      <onlyIfSuccessful>false</onlyIfSuccessful>
      <fingerprint>false</fingerprint>
      <defaultExcludes>true</defaultExcludes>
      <caseSensitive>true</caseSensitive>
      <followSymlinks>false</followSymlinks>
    </hudson.tasks.ArtifactArchiver>
    <org.jenkinsci.plugins.postbuildscript.PostBuildScript plugin="postbuildscript@3.1.0-375.v3db_cd92485e1">
      <config>
        <scriptFiles/>
        <groovyScripts/>
        <buildSteps>
          <org.jenkinsci.plugins.postbuildscript.model.PostBuildStep>
            <results>
              <string>SUCCESS</string>
            </results>
            <role>BOTH</role>
            <executeOn>BOTH</executeOn>
            <buildSteps>
              <hudson.tasks.BatchFile>
                <command>python Deploy.py &quot;%WORKSPACE%\ProbablyFriends\bin\Release\net6.0\win-x64\publish&quot; &quot;%DEPLOY_TARGET_PATH%&quot; &quot;%DEPLOY_BACKUP_PATH%&quot;</command>
                <configuredLocalRules/>
              </hudson.tasks.BatchFile>
            </buildSteps>
            <stopOnFailure>true</stopOnFailure>
          </org.jenkinsci.plugins.postbuildscript.model.PostBuildStep>
        </buildSteps>
        <markBuildUnstable>true</markBuildUnstable>
      </config>
    </org.jenkinsci.plugins.postbuildscript.PostBuildScript>
    <hudson.tasks.Mailer plugin="mailer@408.vd726a_1130320">
      <recipients>andrewettensohn@gmail.com</recipients>
      <dontNotifyEveryUnstableBuild>false</dontNotifyEveryUnstableBuild>
      <sendToIndividuals>false</sendToIndividuals>
    </hudson.tasks.Mailer>
  </publishers>
  <buildWrappers>
    <hudson.plugins.build__timeout.BuildTimeoutWrapper plugin="build-timeout@1.20">
      <strategy class="hudson.plugins.build_timeout.impl.AbsoluteTimeOutStrategy">
        <timeoutMinutes>5</timeoutMinutes>
      </strategy>
      <operationList/>
    </hudson.plugins.build__timeout.BuildTimeoutWrapper>
  </buildWrappers>
</project>
```
