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
  }
}
