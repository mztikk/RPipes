using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace RPipes
{
    public static class PipeReaderExtensions
    {
        public static async Task<string> PipeReadLine(this PipeReader reader) => await PipeReadLine(reader, Encoding.UTF8).ConfigureAwait(false);

        private static async Task<string> PipeReadLine(this PipeReader reader, Encoding encoding)
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync().ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = result.Buffer;
                byte[] testChars = new byte[] { (byte)'\n', (byte)'\r' };
                SequencePosition? position = buffer.PositionOfAny(testChars);
                if (position is null)
                {
                    if (result.IsCompleted)
                    {
                        return encoding.GetString(buffer.ToArray());
                    }

                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
                else
                {
                    ReadOnlySequence<byte> line = buffer.Slice(0, position.Value);
                    reader.AdvanceTo(line.End);
                    while (true)
                    {
                        result = await reader.ReadAsync().ConfigureAwait(false);
                        buffer = result.Buffer;
                        byte nextchar = buffer.ToArray()[0];
                        if (nextchar == '\n' || nextchar == '\r')
                        {
                            // advance to next char if its a newline
                            reader.AdvanceTo(buffer.GetPosition(1));
                        }
                        else
                        {
                            // dont consume char, only signal we examined the next one
                            reader.AdvanceTo(buffer.Start, buffer.GetPosition(1));
                            break;
                        }
                    }

                    return encoding.GetString(line.ToArray());
                }
            }
        }

        public static async Task PipeReadBytes(this PipeReader reader, byte[] buffer)
        {
            long wantedBytes = buffer.LongLength;
            ReadResult result = await reader.ReadAsync().ConfigureAwait(false);
            ReadOnlySequence<byte> readBuffer = result.Buffer;
            while (readBuffer.Length < wantedBytes)
            {
                if (result.IsCompleted)
                {
                    throw new IOException($"Not enough data in pipe to read {buffer.Length} bytes");
                }

                reader.AdvanceTo(readBuffer.Start, readBuffer.End);
                result = await reader.ReadAsync().ConfigureAwait(false);
                readBuffer = result.Buffer;
            }

            ReadOnlySequence<byte> bytes = readBuffer.Slice(readBuffer.Start, wantedBytes);
            bytes.CopyTo(buffer);
            reader.AdvanceTo(readBuffer.GetPosition(wantedBytes));
        }
    }
}
