using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RPipes.Test.PipeReaderTests
{
    [TestClass]
    public class ReadLineTests
    {
        private readonly string[] _files = new[] { "LoremIpsum100.txt", "LoremIpsum1000.txt", "LoremIpsum100000.txt" };
        private readonly long[] _lengths = new long[] { 199, 1999, 199999 };

        [TestMethod]
        public async Task ReadLines()
        {
            for (int i = 0; i < _files.Length; i++)
            {
                using (FileStream fs = new FileStream(_files[i], FileMode.Open, FileAccess.Read))
                {
                    Pipe pipe = new Pipe();
                    Task write = pipe.Writer.FillFrom(fs);
                    int count = 0;
                    await foreach (string item in pipe.Reader.ReadLines())
                    {
                        count++;
                    }

                    Assert.AreEqual(_lengths[i], count);
                    await write.ConfigureAwait(false);
                }
            }
        }
    }
}
