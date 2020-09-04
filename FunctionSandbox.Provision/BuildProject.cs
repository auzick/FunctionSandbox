using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;

namespace FunctionSandbox.Provision
{
    public class BuildProject
    {
        public static BuildResult BuildPackage()
        {
            // "Path\To\MSBuild\MSBuild.exe" 
            // /p:configuration="release";
            // platform="any cpu";
            // WebPublishMethod=Package;
            // PackageFileName="\MyFolder\package.zip";
            // DesktopBuildPackageLocation="\MyFolder\package.zip";
            // PackageAsSingleFile=true;
            // PackageLocation="\MyFolder\package.zip";
            // DeployOnBuild=true;
            // DeployTarget=Package

            string projectFileName = @"...\ConsoleApplication3\ConsoleApplication3.sln";
            ProjectCollection pc = new ProjectCollection();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Add("Configuration", "release");
            props.Add("Platform", "any cpu");
            props.Add("WebPublishMethod", "Package");
            props.Add("PackageAsSingleFile", "true");
            props.Add("PackageLocation", @"\packages\package.zip");
            props.Add("PackageFileName", @"\packages\package.zip");
            props.Add("DeployOnBuild", "true");
            props.Add("DeployTarget", "Package");
            BuildRequestData reqData = new BuildRequestData(projectFileName, props, null, new string[] { "Build" }, null);
            return BuildManager.DefaultBuildManager.Build(new BuildParameters(pc), reqData);






            // ProjectCollection pc = new ProjectCollection();
            // BuildParameters bp = new BuildParameters(pc);
            // bp.pl



            // ProjectCollection pc = new ProjectCollection();

            // Dictionary<string, string> GlobalProperty = new Dictionary<string, string>();
            // props.Add("Configuration", "Release");
            // props.Add("Platform", "Any CPU");
            // props.Add("OutputPath", Directory.GetCurrentDirectory() + "\\MyOutput");

            // BuildParameters bp = new BuildParameters(pc);

            // BuildManager.DefaultBuildManager.BeginBuild(bp);
            // BuildRequestData BuildRequest = new BuildRequestData(projectFilePath, props, null, new string[] { "Build" }, null);

            // BuildSubmission BuildSubmission = BuildManager.DefaultBuildManager.PendBuildRequest(BuildRequest);
            // BuildSubmission.Execute();
            // BuildManager.DefaultBuildManager.EndBuild();
            // if (BuildSubmission.BuildResult.OverallResult == BuildResultCode.Failure)
            // {
            //     throw new Exception();
            // }
        }
    }
}