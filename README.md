# ProbablyFriends
This is an example project that is used with a Jenkins pipeline I setup. Thanks for checking it out!

## Overview

The details of the Jenkins pipeline are documented in this readme along with steps to recreate the work I've done. This repository contains a simple ASP .NET 6 web application with Razor pages and a unit test project. The configuration for the Jenkins pipeline is listed below as an XML code block along with a deployment script written in Python.

## Setting up the .NET Project

From Visual Studio 2022 I created a new ASP .NET Core Web App project and selected .NET 6.0 for the framework.

![image](https://user-images.githubusercontent.com/47993107/160245939-16f82c06-d401-4c9b-9c32-4ef0c7f44fb8.png)

![image](https://user-images.githubusercontent.com/47993107/160245977-1d79a834-9ca0-4f0b-b507-7ecb3169e6d3.png)

I wanted to showcase running unit tests as a build step in the pipeline, so I added a unit test project as well.

![image](https://user-images.githubusercontent.com/47993107/160246164-55223456-348c-4a56-8ae4-c7db96c3650f.png)

Within Index.cshtml.cs I wrote a very simple method that returns a list of three names:

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

![image](https://user-images.githubusercontent.com/47993107/160246383-6def832f-4178-4d53-aac2-bd6fbe2c0552.png)

## Configuring the Jenkins Pipeline

After installing Jenkins I created a new freestyle project and began selecting settings for 


## Creating the Windows Service

## Writing the Deployment Script

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
      <command>dotnet publish ProbablyFriends --configuration Release --runtime win-x64 --self-contained</command>
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
        <markBuildUnstable>false</markBuildUnstable>
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
        os.system("sc stop ProbablyFriends")

        # Create backup
        shutil.make_archive(os.path.join(
            backup_path, _backup_file_name), 'zip', target_path)
        
        # Zip artifacts and copy to target
        shutil.make_archive(
            os.path.join(target_path, _file_name), 'zip', source_path)

        # Unzip artifacts
        shutil.unpack_archive(os.path.join(
            target_path, f"{_file_name}.zip"), target_path)

        # Delete archive
        os.remove(os.path.join(target_path, f"{_file_name}.zip"))

        # Start Service
        os.system("sc start ProbablyFriends")

    except:
        roll_back(target_path, backup_path)


def roll_back(target_path, backup_path):
    print(f"An Exception occured. Rolling back deploy.")
    os.remove(target_path)
    os.write(target_path, 'x')
    shutil.unpack_archive(os.path.join(
        backup_path, f"{_backup_file_name}.zip"), target_path)


run_deploy(sys.argv[1], sys.argv[2], sys.argv[3])

```
