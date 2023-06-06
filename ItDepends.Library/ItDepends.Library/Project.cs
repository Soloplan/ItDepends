namespace ItDepends.Library
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text;
  using System.Text.RegularExpressions;

  public class Project
  {
    private readonly string solutionFolder;

    private readonly Solution solution;

    public Project(Solution solution, string projectType, string projectName, string projectPath, string projectGuid)
    {
      this.solution = solution;
      this.solutionFolder = Path.GetDirectoryName(solution.Filename);
      this.ProjectType = projectType;
      this.ProjectName = projectName;
      this.ProjectPath = projectPath;
      this.ProjectPath = Path.Combine(this.solutionFolder, projectPath);
      this.ProjectFile = Path.GetFileName(projectPath);
      this.ProjectGuid = projectGuid;
      this.Parse();
    }

    public string ProjectType { get; }
    public string ProjectName { get; }
    public string ProjectPath { get; }
    public string ProjectFile { get; }
    public string ProjectGuid { get; }
    public string[] TargetFrameworks { get; private set; }

    public List<string> BinaryReferences { get; } = new List<string>();
    public List<string> ProjectReferences { get; } = new List<string>();
    public List<string> PackageReferences { get; } = new List<string>();

    private List<Project> projectReferencesAsProjects;

    public List<Project> ProjectReferencesAsProjects
    {
      get
      {
        // did we built the complete list in the past? then return it.
        if (this.projectReferencesAsProjects != null)
        {
          return this.projectReferencesAsProjects;
        }

        // no complete list yet. Try to build it now
        var projectReferences = new List<Project>();
        foreach (var projectReference in this.ProjectReferences)
        {
          var projectFileName = Path.GetFileName(projectReference);
          var referencedProject = solution.Projects.FirstOrDefault(x => x.ProjectFile == projectFileName);
          if (referencedProject != null)
          {
            projectReferences.Add(referencedProject);
          }
        }

        // if we found all projects, remember them for later calls
        if (projectReferences.Count == this.ProjectReferences.Count)
        {
          // found all project references
          this.projectReferencesAsProjects = projectReferences;
        }

        // return what we found (might not be all projects)
        return projectReferences;
      }
    }

    public IEnumerable<string> References
    {
      get
      {
        return this.BinaryReferences.Union(this.ProjectReferences).Union(this.PackageReferences);
      }
    }

    public void Parse()
    {
      // <Reference Include="DevExpress.Data.v19.1, Version=19.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a, processorArchitecture=MSIL" />
      // <Reference Include="System.Data" /> 
      var binaryReferenceWithVersionRegEx = new Regex(@"<Reference Include=""(?<binaryreference>.*), Version=(?<version>.*), Culture=(?<culture>.*), PublicKeyToken=(?<publickeytoken>.*)(, processorArchitecture=(?<processorArchitecture>.*))?""\s*\/>", RegexOptions.Compiled);
      var binaryReferenceRegEx = new Regex(@"<Reference Include=""(?<binaryreference>.*)""\s*\/>", RegexOptions.Compiled);

      var projectReferenceRegEx = new Regex(@"<ProjectReference Include=""(?<projectreference>.*?)""\s*", RegexOptions.Compiled); // match non-greedy!

      // <PackageReference Include="Microsoft.DependencyValidation.Analyzers">
      var packageReferenceRegEx = new Regex(@"<PackageReference Include=""(?<packagereference>[a-zA-Z0-9\.]*)""", RegexOptions.Compiled);

      var project = File.ReadAllLines(this.ProjectPath);
      foreach (var line in project)
      {
        if (line.Contains("ProjectReference"))
        {
          // break;
        }

        var binaryReferenceWithVersionMatch = binaryReferenceWithVersionRegEx.Match(line);
        if (binaryReferenceWithVersionMatch.Success)
        {
          var binaryReference = binaryReferenceWithVersionMatch.Groups["binaryreference"].Value;
          this.BinaryReferences.Add(binaryReference);
        }
        else
        {
          var binaryMatch = binaryReferenceRegEx.Match(line);
          if (binaryMatch.Success)
          {
            var binaryReference = binaryMatch.Groups["binaryreference"].Value;
            this.BinaryReferences.Add(binaryReference);
          }
        }

        var projectReferenceMatch = projectReferenceRegEx.Match(line);
        if (projectReferenceMatch.Success)
        {
          var projectReference = projectReferenceMatch.Groups["projectreference"].Value;
          var effectiveProjectReference = Path.Combine(this.solutionFolder, projectReference);
          this.ProjectReferences.Add(effectiveProjectReference);
        }

        var packageReferenceMatch = packageReferenceRegEx.Match(line);
        if (packageReferenceMatch.Success)
        {
          var packageReference = packageReferenceMatch.Groups["packagereference"].Value;
          this.PackageReferences.Add(packageReference);
        }
      }

      if (File.Exists(this.ProjectPath))
      {
        var projectCsproj = File.ReadAllLines(this.ProjectPath, Encoding.UTF8);
        var targetFrameworksRegEx = new Regex(@"<TargetFrameworks>(?<targetframeworks>.*)<\/TargetFrameworks>");
        var targetFrameworkRegEx = new Regex(@"<TargetFramework>(?<targetframework>.*)<\/TargetFramework>");
        var targetFrameworkVersionRegEx = new Regex(@"<TargetFrameworkVersion>(?<targetframework>.*)<\/TargetFrameworkVersion>"); // non-SDK (aka "legacy") csproj format

        foreach (var line in projectCsproj)
        {
          var targetFrameworks = targetFrameworksRegEx.Match(line);
          if (targetFrameworks.Success)
          {
            this.TargetFrameworks = targetFrameworks.Groups["targetframeworks"].Value.Split(';');
            if (this.TargetFrameworks.Length == 0)
            {
              this.TargetFrameworks = new[] { targetFrameworks.Groups["targetframeworks"].Value };
            }
          }
          else
          {
            var targetFrameworkMatch = targetFrameworkRegEx.Match(line);
            if (targetFrameworkMatch.Success)
            {
              this.TargetFrameworks = new[] { targetFrameworkMatch.Groups["targetframework"].Value };
            }
            else
            {
              var targetFrameworkVersionMatch = targetFrameworkVersionRegEx.Match(line);
              if (targetFrameworkVersionMatch.Success)
              {
                var legacyTargetFrameworkVersion = targetFrameworkVersionMatch.Groups["targetframework"].Value;
                legacyTargetFrameworkVersion = legacyTargetFrameworkVersion.Replace("v4.7.2", "net472");
                this.TargetFrameworks = new[] { legacyTargetFrameworkVersion };
              }
            }
          }
        }
      }
    }
  }
}