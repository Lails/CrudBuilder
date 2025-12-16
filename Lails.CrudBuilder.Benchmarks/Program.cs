using BenchmarkDotNet.Running;
using Lails.CrudBuilder.Benchmarks.Benchmarks;

namespace Lails.CrudBuilder.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}


