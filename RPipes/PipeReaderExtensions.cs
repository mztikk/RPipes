using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace RPipes
{
    public static class PipeReaderExtensions
    {
        public static async Task<string?> PipeReadLine(this PipeReader reader) => await PipeReadLine(reader, Encoding.UTF8).ConfigureAwait(false);

        private static async Task<string?> PipeReadLine(this PipeReader reader, Encoding encoding)
        {
            byte[] testChars = new byte[] { (byte)'\n', (byte)'\r' };

            while (true)
            {
                ReadResult result = await reader.ReadAsync().ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = result.Buffer;
                SequencePosition? position = buffer.PositionOfAny(testChars);
                if (position is null)
                {
                    if (result.IsCompleted)
                    {
                        if (buffer.Length == 0)
                        {
                            return null;
                        }

                        string rtn = encoding.GetString(buffer.ToArray());
                        reader.AdvanceTo(buffer.End);
                        return rtn;
                    }

                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
                else
                {
                    ReadOnlySequence<byte> line = buffer.Slice(0, position.Value);
                    string linestring = encoding.GetString(line.ToArray());
                    reader.AdvanceTo(line.End);
                    while (true)
                    {
                        result = await reader.ReadAsync().ConfigureAwait(false);

                        buffer = result.Buffer;
                        if (buffer.Length > 0)
                        {
                            ReadOnlySequence<byte> m = buffer.Slice(0, 2);
                            char c = (char)m.GetAt(0);
                            long advances = 0;
                            if (IsNewLineChar(c))
                            {
                                advances++;
                                if (c == '\r')
                                {
                                    if (m.Length == 1)
                                    {
                                        reader.AdvanceTo(buffer.Start, buffer.GetPosition(advances));
                                        continue;
                                    }

                                    if (m.GetAt(1) == '\n')
                                    {
                                        advances++;
                                    }
                                }
                            }

                            reader.AdvanceTo(buffer.GetPosition(advances));
                            break;
                        }
                        else
                        {
                            if (result.IsCompleted)
                            {
                                reader.AdvanceTo(buffer.End);
                                break;
                            }
                        }
                    }

                    return linestring;
                }
            }
        }

        public static async IAsyncEnumerable<string> ReadLines(this PipeReader reader)
        {
            string line;
            while ((line = await reader.PipeReadLine().ConfigureAwait(false)) is { })
            {
                yield return line;
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

        public static bool IsNewLineChar(char c) => c == '\n' || c == '\r';
    }
}
