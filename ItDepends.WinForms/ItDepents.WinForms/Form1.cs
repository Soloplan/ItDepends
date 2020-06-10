namespace ItDepents.WinForms
{
  using System;
  using System.Linq;
  using System.Text;
  using System.Windows.Forms;
  using ItDepends.Library;
  using Microsoft.Msagl.Drawing;
  using Microsoft.Msagl.GraphViewerGdi;

  // https://github.com/microsoft/automatic-graph-layout
  public partial class Form1 : Form
  {
    private object selectedObject;

    private ToolTip toolTip;

    public Form1()
    {
      InitializeComponent();
      this.toolTip = new ToolTip();
    }

    private void LoadSolution(string solutionFile)
    {
      var solution = Solution.Parser(solutionFile);
      var graph = new Graph("Dependencies");

      foreach (var project in solution.Projects)
      {
        var projectNode = graph.AddNode(NodeFilter.NormalizeReference(project.ProjectName));
        foreach (var reference in project.BinaryReferences.Where(NodeFilter.BinaryParticipateInGraph))
        {
          var targetNodeId = NodeFilter.NormalizeReference(reference);
          var edge = graph.AddEdge(projectNode.Id, targetNodeId);
          var targetNode = graph.FindNode(targetNodeId);
          targetNode.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightYellow;
          targetNode.Attr.Shape = Shape.Trapezium;
        }

        foreach (var reference in project.ProjectReferences)
        {
          var targetNodeId = NodeFilter.NormalizeReference(reference);
          var edge = graph.AddEdge(projectNode.Id, targetNodeId);
        }

        foreach (var reference in project.PackageReferences.Where(NodeFilter.PackageParticipateInGraph))
        {
          var targetNodeId = NodeFilter.NormalizeReference(reference);
          var edge = graph.AddEdge(projectNode.Id, targetNodeId);
          var targetNode = graph.FindNode(targetNodeId);
          targetNode.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightSkyBlue;
          targetNode.Attr.Shape = Shape.Trapezium;
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
      this.Update(node, 3);
    }

    private void Unselect(object item)
    {
      switch (item)
      {
        case Node node:
          this.Update(node, 1);
          break;
      }
    }

    private void Update(Node node, int lineWidth)
    {
      foreach (var edge in node.Edges)
      {
        edge.Attr.LineWidth = lineWidth;
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

      string objectText;
      switch (e.NewObject)
      {
        case DEdge edge:
          objectText = $"{edge.Edge.Source} -> {edge.Edge.Target}";
          break;
        case DNode node:
          var sb = new StringBuilder();
          sb.AppendLine($"{node.Node.Id} (references {node.Node.OutEdges.Count()} / referenced by {node.Node.InEdges.Count()})");
          foreach (var outEdges in node.Node.OutEdges)
          {
            sb.AppendLine($"references {outEdges.TargetNode.Id}");
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
        openFileFialog.Filter = "C# solution (*.sln)|*.sln";
        openFileFialog.CheckFileExists = true;
        if (openFileFialog.ShowDialog(this) == DialogResult.OK)
        {
          LoadSolution(openFileFialog.FileName);
        }
      }
    }

    private void uxShowReferences_CheckedChanged(object sender, EventArgs e)
    {
      this.uxGraphViewer.Refresh();
    }
  }
}
