using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace RPipes
{
    public static class ReadOnlySequenceExtensions
    {
        /// <summary>
        /// Returns position of first occurrence of item in the <see cref="ReadOnlySequence{T}"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SequencePosition? PositionOfAny<T>(in this ReadOnlySequence<T> source, in ReadOnlySpan<T> values) where T : IEquatable<T> => PositionOfAnyMultiSegment(source, values);

        private static SequencePosition? PositionOfAnyMultiSegment<T>(in ReadOnlySequence<T> source, in ReadOnlySpan<T> values) where T : IEquatable<T>
        {
            SequencePosition position = source.Start;
            SequencePosition result = position;
            while (source.TryGet(ref position, out ReadOnlyMemory<T> memory))
            {
                int index = memory.Span.IndexOfAny(values);
                if (index != -1)
                {
                    return source.GetPosition(index, result);
                }
                else if (position.GetObject() == null)
                {
                    break;
                }

                result = position;
            }

            return null;
        }

        public static T GetAt<T>(this ReadOnlySequence<T> sequence, int i)
        {
            int count = 0;
            foreach (ReadOnlyMemory<T> m in sequence)
            {
                int total = count + m.Length;
                if (total <= i)
                {
                    count = total;
                    continue;
                }

                int index = i - count;
                return m.Span[index];
            }

            throw new IndexOutOfRangeException();
        }
    }
}
