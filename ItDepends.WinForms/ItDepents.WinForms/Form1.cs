namespace ItDepents.WinForms
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text;
  using System.Windows.Forms;
  using ItDepends;
  using ItDepends.Library;
  using Microsoft.Msagl.Drawing;
  using Microsoft.Msagl.GraphViewerGdi;

  // https://github.com/microsoft/automatic-graph-layout
  public partial class Form1 : Form
  {
    private object selectedObject;

    private ToolTip toolTip;

    private Solution solution;

    public Form1()
    {
      InitializeComponent();
      this.toolTip = new ToolTip();
    }

    // Graph legend:
    // we distinguish shape and color
    //
    // shape:
    //   * rectangle: project
    //   * ellipse: nuget package

    // color:
    //   * white: net472
    //   * blue: .Net Core 3.1
    //   * beige: .Net Standard
    private Solution LoadSolution(string solutionFile)
    {
      switch (Path.GetExtension(solutionFile))
      {
        case ".slnf": return Solution.ReadFromSlnf(solutionFile);
        default: return Solution.ReadFromSln(solutionFile);
      }
    }

    private void BuildGraph(Solution solution, GraphOptions graphOptions)
    {
      var graph = new Graph("Dependencies");

      foreach (var project in solution.Projects)
      {
        var projectNode = graph.AddNode(NodeFilter.NormalizeReference(project.ProjectName));
        projectNode.UserData = project;
        if (project.ProjectName.Contains(".Interop"))
        {
          projectNode.Attr.FillColor = Color.LightGray;
        }

        if (project.ProjectName.EndsWith(".Test")
            || project.ProjectName.EndsWith(".Tests"))
        {
          projectNode.Attr.FillColor = Color.LightSteelBlue;
        }

        if (project.TargetFrameworks != null)
        {
          // .Net 6? -> pale green color
          if (project.TargetFrameworks.Any(x => x.StartsWith("net6")))
          {
            projectNode.Attr.FillColor = Color.PaleGreen;
          }

          // .Net Standard? -> beige
          if (project.TargetFrameworks.Any(x => x.StartsWith("netstandard")))
          {
            projectNode.Attr.AddStyle(Style.Dashed);
            projectNode.Attr.FillColor = Color.LightYellow;
          }
        }

        if (!project.TargetFrameworks.SupportsNet8()
          && graphOptions.ShowNewCandidatesNet8)
        {
          var allReferencesSupportCore = project.ProjectReferencesAsProjects.All(x => x.TargetFrameworks.SupportsNet8());
          if (allReferencesSupportCore)
          {
            projectNode.Attr.FillColor = Color.GreenYellow;
          }
        }

        if (graphOptions.ShowBinaryReferences)
        {
          foreach (var reference in project.BinaryReferences.Where(NodeFilter.BinaryParticipateInGraph))
          {
            var targetNodeId = NodeFilter.NormalizeReference(reference);
            var edge = graph.AddEdge(projectNode.Id, targetNodeId);
            var targetNode = graph.FindNode(targetNodeId);
            targetNode.Attr.FillColor = Color.LightCoral;
            targetNode.Attr.Shape = Shape.Trapezium;
          }
        }

        // highlight projects whose project name matches the search text
        if (!string.IsNullOrWhiteSpace(graphOptions.SearchText)
          && project.ProjectName.Contains(graphOptions.SearchText))
        {
          projectNode.Attr.FillColor = Color.OrangeRed;
        }
        
        foreach (var reference in project.ProjectReferences)
        {
          var targetNodeId = NodeFilter.NormalizeReference(reference);
          var edge = graph.AddEdge(projectNode.Id, targetNodeId);
        }

        if (graphOptions.ShowPackageReferences)
        {
          foreach (var reference in project.PackageReferences.Where(NodeFilter.PackageParticipateInGraph))
          {
            var targetNodeId = NodeFilter.NormalizeReference(reference);
            var edge = graph.AddEdge(projectNode.Id, targetNodeId);
            var targetNode = graph.FindNode(targetNodeId);
            targetNode.Attr.FillColor = Color.LightSkyBlue;
            targetNode.Attr.Shape = Shape.Trapezium;
          }
        }
      }

      this.uxGraphViewer.CurrentLayoutMethod = LayoutMethod.MDS;
      this.uxGraphViewer.Graph = graph;
      //this.uxGraphViewer.CurrentLayoutMethod = LayoutMethod.Ranking;
      ////graph.LayoutAlgorithmSettings;
    }

    private void uxGraphViewer_Click(object sender, EventArgs e)
    {
      var drawingObject = this.uxGraphViewer.ObjectUnderMouseCursor?.DrawingObject;
      if (drawingObject != null)
      {
        Unselect(selectedObject);
        selectedObject = drawingObject;
      }

      uxStatusLabel.Text = selectedObject?.ToString();

      switch (selectedObject)
      {
        case Node node:
          {
            this.Select(node);
            break;
          }
      }
    }

    private void Select(Node node)
    {
      this.Update(node, 3, Color.Coral);
    }

    private void Unselect(object item)
    {
      switch (item)
      {
        case Node node:
          this.Update(node, 1, Color.Black);
          break;
      }
    }

    private void Update(Node node, int lineWidth, Color color)
    {
      foreach (var edge in node.Edges)
      {
        edge.Attr.LineWidth = lineWidth;
        edge.Attr.Color = color;
      }
    }

    private void uxGraphViewer_MouseMove(object sender, MouseEventArgs e)
    {
    }

    private void uxGraphViewer_ObjectUnderMouseCursorChanged(object sender, ObjectUnderMouseCursorChangedEventArgs e)
    {
      if (e.NewObject == null)
      {
        this.toolTip.Hide(this);
        return;
      }

      var showOnlyFullFrameworkBlockers = ModifierKeys.HasFlag(Keys.Alt);
      string objectText;
      switch (e.NewObject)
      {
        case DEdge edge:
          objectText = $"{edge.Edge.Source} -> {edge.Edge.Target}";
          break;
        case DNode node:
          var project = node.Node.UserData as Project;
          var sb = new StringBuilder();
          sb.AppendLine($"{node.Node.Id} (references {node.Node.OutEdges.Count()} / referenced by {node.Node.InEdges.Count()})");
          if (project != null)
          {
            // if we have a project, evaluate its references
            foreach (var projectReference in project.ProjectReferencesAsProjects.Where(x => x != null))
            {
              var supportedFrameworks = string.Join(";", projectReference.TargetFrameworks);
              if (!showOnlyFullFrameworkBlockers || !projectReference.TargetFrameworks.SupportsNet8())
              {
                sb.AppendLine($"references {projectReference.ProjectName} ({supportedFrameworks})");
              }
            }
          }
          else
          {
            // if we have no project instance we can only evaluate the edges
            foreach (var outEdges in node.Node.OutEdges)
            {
              sb.AppendLine($"references {outEdges.TargetNode.Id}");
            }
          }

          foreach (var inEdges in node.Node.InEdges)
          {
            sb.AppendLine($"referenced by {inEdges.SourceNode.Id}");
          }
          objectText = sb.ToString();
          break;
        default:
          objectText = e.NewObject.ToString();
          break;
      }

      this.toolTip.Show(objectText, this, Cursor.Position, int.MaxValue);
    }

    private void uxOpenSolutionButton_Click(object sender, EventArgs e)
    {
      using (var openFileFialog = new OpenFileDialog())
      {
        openFileFialog.Filter = "C# solution (*.sln)|*.sln|C# solution filter (*.slnf)|*.slnf";
        openFileFialog.CheckFileExists = true;
        if (openFileFialog.ShowDialog(this) == DialogResult.OK)
        {
          this.solution = LoadSolution(openFileFialog.FileName);
          this.BuildGraph(this.solution, new GraphOptions { ShowPackageReferences = this.uxShowPackageReferences.Checked, ShowBinaryReferences = this.uxShowBinaryReference.Checked });
        }
      }
    }

    private void uxShowReferences_CheckedChanged(object sender, EventArgs e)
    {
      this.uxGraphViewer.Refresh();
    }

    private void uxShowBinaryReference_Click(object sender, EventArgs e)
    {
      // toggle binary references
      this.BuildGraph(this.solution, this.BuildGraphOptions());
    }

    private void uxShowPackageReferences_Click(object sender, EventArgs e)
    {
      // toggle package references
      this.BuildGraph(this.solution, this.BuildGraphOptions());
    }

    private void uxSearch_Click(object sender, EventArgs e)
    {
      this.BuildGraph(this.solution, this.BuildGraphOptions());
    }

    private void uxShowNewCandidatesNet8_Click(object sender, EventArgs e)
    {
      // toggle new candidates
      this.BuildGraph(this.solution, this.BuildGraphOptions());
    }

    private GraphOptions BuildGraphOptions()
    {
      return new GraphOptions
      {
        ShowPackageReferences = this.uxShowPackageReferences.Checked,
        ShowBinaryReferences = this.uxShowBinaryReference.Checked,
        ShowNewCandidatesNet8 = this.uxShowNewCandidatesNet8.Checked,
        SearchText = this.uxSearchText.Text,
      };
    }

    private void uxSaveAsGraphViz_Click(object sender, EventArgs e)
    {
      if (this.solution == null)
      {
        return;
      }

      using (var saveFileDialog = new SaveFileDialog())
      {
        if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
        {
          new GraphvizDotOutput().Write(saveFileDialog.FileName, this.solution);
        }
      }
    }

    /// <summary>
    /// If the user presses enter in the search text box, perform the search.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event args of the event.</param>
    private void uxSearchText_KeyPress(object sender, KeyPressEventArgs e)
    {
      // if the user presses enter, perform the search
      if (e.KeyChar == (char)Keys.Enter)
      {
        this.uxSearch.PerformClick();
      }
    }

    private class GraphOptions
    {
      public bool ShowPackageReferences { get; set; }
      public bool ShowBinaryReferences { get; set; }
      public bool ShowNewCandidatesNet8 { get; set; }

      /// <summary>
      /// Gets or sets a search text. Project names containing this text will be highlighted. 
      /// </summary>
      public string SearchText { get; set; }
    }

    /// <summary>
    /// Shows a message box with the number of projects per target framework.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event args of the event.</param>
    private void uxShowSolutionMetrics_Click(object sender, EventArgs e)
    {
      var projectsByTargetFrameworks = new Dictionary<string, List<Project>>();
      var netCoreProjects = new List<Project>();
      var fullFrameworkOnlyProjects = new List<Project>();
      foreach (var project in this.solution.Projects)
      {
        foreach (var targetFramework in project.TargetFrameworks)
        {
          if (!projectsByTargetFrameworks.TryGetValue(targetFramework, out var projects))
          {
            projects = new List<Project>();
            projectsByTargetFrameworks.Add(targetFramework, projects);
          }

          projects.Add(project);
        }

        if (project.TargetFrameworks.SupportsNet())
        {
          netCoreProjects.Add(project);
        }
        else
        {
          fullFrameworkOnlyProjects.Add(project);
        }
      }

      var sb = new StringBuilder();
      sb.AppendLine("Number of projects per target framework");
      sb.AppendLine();
      sb.AppendLine($"Solution: {this.solution.Filename}");
      if (!string.IsNullOrEmpty(this.solution.SolutionFilter))
      {
        sb.AppendLine($"Filter: {this.solution.SolutionFilter}");
      }

      sb.AppendLine($"Total projects in the solution: {this.solution.Projects.Count}");
      foreach (var targetFramework in projectsByTargetFrameworks.Keys.OrderBy(x => x))
      {
        sb.AppendLine($"{targetFramework}: {projectsByTargetFrameworks[targetFramework].Count}");
      }

      sb.AppendLine();
      sb.AppendLine($"Projects that support .Net Core: {netCoreProjects.Count} ({Math.Round(100 * netCoreProjects.Count / (double)this.solution.Projects.Count, 1)} %)");
      sb.AppendLine($"Projects that only target full framework: {fullFrameworkOnlyProjects.Count} ({Math.Round(100 * fullFrameworkOnlyProjects.Count / (double)this.solution.Projects.Count, 1)} %)");

      MessageBox.Show(sb.ToString());
    }
  }
}