using System.Linq;

namespace ItDepents.WinForms
{
  internal static class ProjectExtensions
  {
    public static bool IsFullFramework(this string targetFramework)
    {
      return targetFramework == "net472";
    }

    public static bool SupportsNet8(this string[] targetFrameworks)
    {
      if (targetFrameworks == null)
      {
        return false;
      }

      return targetFrameworks.Any(x => x.StartsWith("net8") || x == "netstandard2.0");
    }

    public static bool SupportsNet(this string[] targetFrameworks)
    {
      if (targetFrameworks == null)
      {
        return false;
      }

      return targetFrameworks.Any(x => x.StartsWith("net8") || x.StartsWith("net7") || x.StartsWith("net6") || x == "netstandard2.0");
    }
  }
}
