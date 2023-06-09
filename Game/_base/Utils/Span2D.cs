﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

namespace NSS
{
    /// <summary>
    /// Span over a 2 dimensional array 
    /// </summary>
    public ref struct Span2D<T> where T : struct
    {
        public Span<T> Span;
        public readonly int Width;
        public readonly int Height;

        public int ColumnCount => Width;
        public int RowCount => Height; 

        public Span2D(Span<T> span, int rowCount, int columnCount)
        {
            Width = columnCount;
            Height = rowCount;
            Span = span;
        }

        public T this[int row, int column]
        {
            get
            {
                return Span[row * Width + column];
            }
            set
            {
                Span[row * Width + column] = value;
            }
        }
        public Span<T> Slice(int index, int length) => Span.Slice(index, length);
        public Span<T> Row(int row) => Span.Slice(row * Width, Width);
        public ColumnSpan Column(int column) => new Span2D<T>.ColumnSpan(Span, Width, column);

        public Span<T> FirstRow => Row(0);
        public Span<T> LastRow => Row(RowCount - 1);

        public ColumnSpan FirstColumn => Column(0);
        public ColumnSpan LastColumn => Column(ColumnCount - 1); 

        public ref struct ColumnSpan
        {
            public Span<T> Span;
            public int ColumnIndex;
            public int Stride;

            public int ElementCount => Span.Length / Stride;

            public ColumnSpan(Span<T> span, int stride, int column)
            {
                Span = span;
                Stride = stride;
                ColumnIndex = column;
            }

            public T this[int row]
            {
                get
                {
                    return Span[row * Stride + ColumnIndex];
                }
                set
                {
                    Span[row * Stride + ColumnIndex] = value;
                }
            }

            public T[] ToArray()
            {
                T[] a = new T[ElementCount];
                CopyTo(a.AsSpan());
                return a;
            }

            public void CopyTo(Span<T> other) => CopyTo(other, 0, Span.Length / Stride);

            public void CopyTo(Span<T> other, int from, int length)
            {
                for (int j = 0, i = from; i < from + length; i++, j++)
                {
                    other[j] = this[i];
                }
            }

            public void Fill(T value)
            {
                int j = ColumnIndex; 
                for (int i = 0; i < ElementCount; i++, j += Stride)
                {
                    Span[j] = value; 
                }
            }
        }
    }
}
 
