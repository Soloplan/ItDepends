namespace ItDepends
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text;
  using System.Text.RegularExpressions;

  public class Solution
  {
    public Solution(string file)
    {
      this.Filename = file;
    }

    public string Filename { get; private set; }

    public List<Project> Projects { get; } = new List<Project>();

    public static Solution Parser(string slnFile)
    {
      var solution = new Solution(slnFile);
      var solutionSln = File.ReadAllLines(slnFile, Encoding.UTF8);
      var projectInSlnRegEx = new Regex(@"Project\(""(?<projecttype>{.*})""\) = ""(?<projectname>.*)"", ""(?<projectfile>.*)"", ""{(?<projectguid>.*)}""", RegexOptions.Compiled);
      foreach (var line in solutionSln)
      {
        var projectMatch = projectInSlnRegEx.Match(line);
        if (projectMatch.Success)
        {
          var projectType = projectMatch.Groups["projecttype"].Value;
          var projectName = projectMatch.Groups["projectname"].Value;
          var projectFile = projectMatch.Groups["projectfile"].Value;
          var projectGuid = projectMatch.Groups["projectguid"].Value;

          if (projectFile.Contains(".csproj")
            && !projectFile.Contains(".Test"))
          {
            var project = new Project(solution, projectType, projectName, projectFile, projectGuid);
            solution.Projects.Add(project);
          }
        }
      }

      return solution;
    }
  }

  public class Project
  {
    private string solutionFolder;

    public Project(Solution solution, string projectType, string projectName, string projectPath, string projectGuid)
    {
      this.solutionFolder = Path.GetDirectoryName(solution.Filename);
      this.ProjectType = projectType;
      this.ProjectName = projectName;
      this.ProjectPath = projectPath;
      this.ProjectPath = Path.Combine(solutionFolder, projectPath);
      this.ProjectFile = Path.GetFileName(projectPath);
      this.ProjectGuid = projectGuid;
      this.Parse();
    }

    public string ProjectType { get; }
    public string ProjectName { get; }
    public string ProjectPath { get; }
    public string ProjectFile { get; }
    public string ProjectGuid { get; }

    public List<string> BinaryReferences { get; } = new List<string>();
    public List<string> ProjectReferences { get; } = new List<string>();
    public List<string> PackageReferences { get; } = new List<string>();
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

      var projectReferenceRegEx = new Regex(@"<ProjectReference Include=""(?<projectreference>.*)""\s*", RegexOptions.Compiled);

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
    }
  }
}
