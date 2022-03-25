# ProbablyFriends

This is an example project that is used by a Jenkins pipeline I setup. 



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
