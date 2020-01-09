﻿using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace RPipes
{
    public static class PipeWriterExtensions
    {
        public static async Task FillFrom(this PipeWriter writer, Stream stream)
        {
            const int minimumBufferSize = 512;

            while (true)
            {
                Memory<byte> memory = writer.GetMemory(minimumBufferSize);
                int bytesRead = await stream.ReadAsync(memory).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }
                writer.Advance(bytesRead);

                FlushResult result = await writer.FlushAsync().ConfigureAwait(false);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            writer.Complete();
        }

        public static async Task FillFrom(this PipeWriter writer, byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                await writer.FillFrom(ms).ConfigureAwait(false);
            }
        }
    }
}
