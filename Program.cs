using System;

namespace ItDepends
{
  class Program
  {
    static void Main(string[] args)
    {
      if (args.Length < 1)
      {
        Console.WriteLine("Please specify solution file");
        Environment.Exit(1);
      }

      var solutionFile = args[0];
      var solution = Solution.Parser(solutionFile);
      new GraphvizDotOutput().Write("demo.txt", solution);
    }
  }
}
