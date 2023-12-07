namespace ItDepends.Library
{
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text;
  using System.Text.Json;
  using System.Text.RegularExpressions;

  public class Solution
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="Solution"/> class.
    /// </summary>
    /// <param name="file">The name of the solution file.</param>
    /// <param name="slnfFile">The name of the slnf file (if one was used for filter the solution file).</param>
    public Solution(string file, string slnfFile = null)
    {
      this.Filename = file;
      this.SolutionFilter = slnfFile;
    }

    public string Filename { get; private set; }
    
    /// <summary>
    /// Gets the name of the slnf file (if one was used for filter the solution file).
    /// </summary>
    public string SolutionFilter { get; private set; }

    public List<Project> Projects { get; } = new List<Project>();

    public static Solution ReadFromSlnf(string slnfFile)
    {
      var solutionSlnf = File.ReadAllText(slnfFile, Encoding.UTF8);
      var solutionSlnfAsJson = JsonDocument.Parse(solutionSlnf, new JsonDocumentOptions { AllowTrailingCommas = true });
      var solutionProperty = solutionSlnfAsJson.RootElement.GetProperty("solution");
      var solutionPathProperty = solutionProperty.GetProperty("path");
      var solutionFilterPath = Path.GetDirectoryName(slnfFile);
      var solutionPath = solutionPathProperty.GetString();
      solutionPath = Path.Combine(solutionFilterPath, solutionPath);
      var solutionProjectsProperty = solutionProperty.GetProperty("projects");
      var projectFilter = solutionProjectsProperty.EnumerateArray().Select(x => x.GetString()).ToList();

      return ReadFromSln(solutionPath, projectFilter, slnfFile);
    }

    public static Solution ReadFromSln(string slnFile, IReadOnlyCollection<string> projectFilter = null, string slnfFile = null)
    {
      var solution = new Solution(slnFile, slnfFile);
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
            ////&& !projectFile.Contains(".Test")
            && (projectFilter == null || projectFilter.Contains(projectFile)))
          {
            var project = new Project(solution, projectType, projectName, projectFile, projectGuid);
            solution.Projects.Add(project);
          }
        }
      }

      return solution;
    }
  }
}
