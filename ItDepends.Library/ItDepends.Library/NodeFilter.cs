namespace ItDepends.Library
{
  public static class NodeFilter
  {
    public static bool BinaryParticipateInGraph(string nodeName)
    {
      return !nodeName.StartsWith("DevExpress.") && !nodeName.StartsWith("System.") && (nodeName != "System") && (nodeName != "Microsoft.CSharp");
    }

    public static bool PackageParticipateInGraph(string nodeName)
    {
      return nodeName != "JetBrains.Annotations";
    }

    public static string NormalizeReference(string name)
    {
      return name.ToLower().Substring(name.LastIndexOf("\\") + 1).Replace(".", "_").Replace("_csproj", "").Replace("-", "_");
    }
  }
}
