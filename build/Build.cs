using System;
using System.Linq;

using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;

using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    Target Migrations => _ => _
        .DependsOn(SqlServerContainerRun)
        .Executes(() =>
        {
            System.Threading.Thread.Sleep(10000);
            DotNetRun(s => s.SetProjectFile($"{RootDirectory}/Migrations/Migrations.csproj"));
        });

    Target SqlServerContainerRun => _ => _
        .Executes(() =>
        {
            DockerTasks.DockerRun(x => x
                .SetImage("mcr.microsoft.com/mssql/server:2019-latest")
                .SetEnv(new string[] { "ACCEPT_EULA=Y", "SA_PASSWORD=Password_01" })
                .SetPublish("1401:1433")
                .SetDetach(true)
                .SetName("sql1")
                .SetHostname("sql1"));
            System.Threading.Thread.Sleep(10000);
        });

    Target SqlServerContainerStop => _ => _
        .Executes(() =>
        {
            DockerTasks.Docker("stop sql1");
            DockerTasks.Docker("container rm sql1");
        });

    Target IntegrationTests => _ => _
        .DependsOn(Migrations)
        .Executes(() =>
        {

        });

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore());
        });

}
