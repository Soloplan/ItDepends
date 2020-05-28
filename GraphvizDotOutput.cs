using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ItDepends
{
  public class GraphvizDotOutput
  {
    private void InitReference(StringBuilder output, List<string> existingNodes, string nodeName, string label, string shape)
    {
      if (!existingNodes.Contains(nodeName))
      {
        output.AppendLine($"{nodeName} [label=\"{label}\", shape={shape}]");
        existingNodes.Add(nodeName);
      }
    }

    public void Write(string file, Solution solution)
    {
      var output = new StringBuilder();
      output.AppendLine("<graphviz dot>");
      output.AppendLine("digraph sample {");

      Func<string, bool> binaryParticipateInGraph = x => !x.StartsWith("DevExpress.") && !x.StartsWith("System.") && (x != "System") && (x != "Microsoft.CSharp");
      Func<string, bool> packageParticipateInGraph = x => (x != "JetBrains.Annotations");
      var existingNodes = new List<string>();
      foreach (var project in solution.Projects)
      {
        var normalizedProjectName = NormalizeReference(project.ProjectName);
        InitReference(output, existingNodes, normalizedProjectName, Path.GetFileName(project.ProjectName), "box");

        foreach (var reference in project.BinaryReferences.Where(binaryParticipateInGraph))
        {
          var normalizedReference = NormalizeReference(reference);
          InitReference(output, existingNodes, normalizedReference, reference, "box");
        }

        foreach (var reference in project.ProjectReferences)
        {
          var normalizedReference = NormalizeReference(reference);
          InitReference(output, existingNodes, normalizedReference, Path.GetFileNameWithoutExtension(reference), "box");
        }

        foreach (var reference in project.PackageReferences.Where(packageParticipateInGraph))
        {
          var normalizedReference = NormalizeReference(reference);
          InitReference(output, existingNodes, normalizedReference, reference, "ellipse");
        }
      }

      foreach (var project in solution.Projects)
      {
        foreach (var reference in project.BinaryReferences.Where(binaryParticipateInGraph))
        {          
          output.AppendLine($"{NormalizeReference(project.ProjectName)} -> {NormalizeReference(reference)} [color=red]");
        }

        foreach (var reference in project.ProjectReferences)
        {
          output.AppendLine($"{NormalizeReference(project.ProjectName)} -> {NormalizeReference(reference)} [color=black]");
        }

        foreach (var reference in project.PackageReferences.Where(packageParticipateInGraph))
        {
          output.AppendLine($"{NormalizeReference(project.ProjectName)} -> {NormalizeReference(reference)} [color=blue]");
        }
      }

      output.AppendLine("}");
      output.AppendLine("</graphviz>");
      File.WriteAllText(file, output.ToString());
    }

    private string NormalizeReference(string name)
    {
      return name.ToLower().Substring(name.LastIndexOf("\\") + 1).Replace(".", "_").Replace("_csproj", "").Replace("-", "_");
    }
  }
}
