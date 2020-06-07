using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ItDepends
{
  using ItDepends.Library;

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

      var existingNodes = new List<string>();
      foreach (var project in solution.Projects)
      {
        var normalizedProjectName = NodeFilter.NormalizeReference(project.ProjectName);
        InitReference(output, existingNodes, normalizedProjectName, Path.GetFileName(project.ProjectName), "box");

        foreach (var reference in project.BinaryReferences.Where(NodeFilter.BinaryParticipateInGraph))
        {
          var normalizedReference = NodeFilter.NormalizeReference(reference);
          InitReference(output, existingNodes, normalizedReference, reference, "box");
        }

        foreach (var reference in project.ProjectReferences)
        {
          var normalizedReference = NodeFilter.NormalizeReference(reference);
          InitReference(output, existingNodes, normalizedReference, Path.GetFileNameWithoutExtension(reference), "box");
        }

        foreach (var reference in project.PackageReferences.Where(NodeFilter.PackageParticipateInGraph))
        {
          var normalizedReference = NodeFilter.NormalizeReference(reference);
          InitReference(output, existingNodes, normalizedReference, reference, "ellipse");
        }
      }

      foreach (var project in solution.Projects)
      {
        foreach (var reference in project.BinaryReferences.Where(NodeFilter.BinaryParticipateInGraph))
        {          
          output.AppendLine($"{NodeFilter.NormalizeReference(project.ProjectName)} -> {NodeFilter.NormalizeReference(reference)} [color=red]");
        }

        foreach (var reference in project.ProjectReferences)
        {
          output.AppendLine($"{NodeFilter.NormalizeReference(project.ProjectName)} -> {NodeFilter.NormalizeReference(reference)} [color=black]");
        }

        foreach (var reference in project.PackageReferences.Where(NodeFilter.PackageParticipateInGraph))
        {
          output.AppendLine($"{NodeFilter.NormalizeReference(project.ProjectName)} -> {NodeFilter.NormalizeReference(reference)} [color=blue]");
        }
      }

      output.AppendLine("}");
      output.AppendLine("</graphviz>");
      File.WriteAllText(file, output.ToString());
    }
  }
}
