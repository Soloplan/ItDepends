namespace ItDepends.Library
{
  using System.Collections.Generic;
  using System.IO;
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
}
