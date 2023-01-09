using System.Linq;

namespace ItDepents.WinForms
{
  internal static class ProjectExtensions
  {
    public static bool IsFullFramework(this string targetFramework)
    {
      return targetFramework == "net472";
    }

    public static bool SupportsNetCore(this string[] targetFrameworks)
    {
      if (targetFrameworks == null)
      {
        return false;
      }

      return targetFrameworks.Any(x => !x.IsFullFramework());
    }

    public static bool SupportsNetCore31(this string[] targetFrameworks)
    {
      if (targetFrameworks == null)
      {
        return false;
      }

      return targetFrameworks.Any(x => x == "netcoreapp3.1" || x == "netstandard2.0");
    }

    public static bool SupportsNet6(this string[] targetFrameworks)
    {
      if (targetFrameworks == null)
      {
        return false;
      }

      return targetFrameworks.Any(x => x.StartsWith("net6") || x == "netstandard2.0");
    }
  }
}
