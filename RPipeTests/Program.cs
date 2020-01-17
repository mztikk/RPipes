using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using RPipes;

namespace RPipeTests
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            byte[] bytes = await File.ReadAllBytesAsync("RPipeTests.runtimeconfig.json");
            Pipe pipe = new Pipe();
            Task write = pipe.Writer.FillFrom(bytes);
            byte[] buffer = new byte[1024];
            //Task read = pipe.Reader.PipeReadBytes(buffer);
            //Task readlines = ReadLines(pipe.Reader);
            await foreach (string line in pipe.Reader.ReadLines())
            {
                System.Console.WriteLine(line);
            }

            await Task.WhenAll(write);
        }

        private static async Task ReadLines(PipeReader reader)
        {
            System.Console.WriteLine(await reader.PipeReadLine());
            System.Console.WriteLine(await reader.PipeReadLine());
            System.Console.WriteLine(await reader.PipeReadLine());
            System.Console.WriteLine(await reader.PipeReadLine());
            System.Console.WriteLine(await reader.PipeReadLine());
            System.Console.WriteLine(await reader.PipeReadLine());
            System.Console.WriteLine(await reader.PipeReadLine());
            System.Console.WriteLine(await reader.PipeReadLine());
            System.Console.WriteLine(await reader.PipeReadLine());
        }
    }
}
