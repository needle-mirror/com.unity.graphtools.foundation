using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace UnityEngine.VisualScripting
{
    // Credit: https://github.com/joaoportela/CircullarBuffer-CSharp/blob/master/CircularBuffer/CircularBuffer.cs
    // ----------------------------------------------------------------------------
    // "THE BEER-WARE LICENSE" (Revision 42):
    // Joao Portela wrote this file. As long as you retain this notice you
    // can do whatever you want with this stuff. If we meet some day, and you think
    // this stuff is worth it, you can buy me a beer in return.
    // Joao Portela
    // ----------------------------------------------------------------------------
    [PublicAPI]
    public class CircularBuffer<T> : IReadOnlyList<T>, IDisposable where T : IDisposable
    {
        readonly T[] m_Buffer;

        /// <summary>
        /// The _start. Index of the first element in buffer.
        /// </summary>
        int m_Start;

        /// <summary>
        /// The _end. Index after the last element in the buffer.
        /// </summary>
        int m_End;

        /// <summary>
        /// The _size. Buffer size.
        /// </summary>
        int m_Size;

        public CircularBuffer(int capacity)
            : this(capacity, new T[] { })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CircularBuffer{T}"/> class.
        ///
        /// </summary>
        /// <param name='capacity'>
        /// Buffer capacity. Must be positive.
        /// </param>
        /// <param name='items'>
        /// Items to fill buffer with. Items length must be less than capacity.
        /// Suggestion: use Skip(x).Take(y).ToArray() to build this argument from
        /// any enumerable.
        /// </param>
        public CircularBuffer(int capacity, T[] items)
        {
            if (capacity < 1)
            {
                throw new ArgumentException(
                    "Circular buffer cannot have negative or zero capacity.", nameof(capacity));
            }
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
            if (items.Length > capacity)
            {
                throw new ArgumentException(
                    "Too many items to fit circular buffer", nameof(items));
            }

            m_Buffer = new T[capacity];

            Array.Copy(items, m_Buffer, items.Length);
            m_Size = items.Length;

            m_Start = 0;
            m_End = m_Size == capacity ? 0 : m_Size;
        }

        /// <summary>
        /// Maximum capacity of the buffer. Elements pushed into the buffer after
        /// maximum capacity is reached (IsFull = true), will remove an element.
        /// </summary>
        public int Capacity => m_Buffer.Length;

        public bool IsFull => Count == Capacity;

        public bool IsEmpty => Count == 0;

        /// <summary>
        /// Current buffer size (the number of elements that the buffer has).
        /// </summary>
        public int Count => m_Size;

        /// <summary>
        /// Element at the front of the buffer - this[0].
        /// </summary>
        /// <returns>The value of the element of type T at the front of the buffer.</returns>
        public T Front()
        {
            ThrowIfEmpty();
            return m_Buffer[m_Start];
        }

        /// <summary>
        /// Element at the back of the buffer - this[Size - 1].
        /// </summary>
        /// <returns>The value of the element of type T at the back of the buffer.</returns>
        public T Back()
        {
            ThrowIfEmpty();
            return m_Buffer[(m_End != 0 ? m_End : Capacity) - 1];
        }

        public T this[int index]
        {
            get
            {
                if (IsEmpty)
                {
                    throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer is empty", index));
                }
                if (index >= m_Size)
                {
                    throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer size is {1}", index, m_Size));
                }
                int actualIndex = InternalIndex(index);
                return m_Buffer[actualIndex];
            }
            set
            {
                if (IsEmpty)
                {
                    throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer is empty", index));
                }
                if (index >= m_Size)
                {
                    throw new IndexOutOfRangeException(string.Format("Cannot access index {0}. Buffer size is {1}", index, m_Size));
                }
                int actualIndex = InternalIndex(index);
                m_Buffer[actualIndex] = value;
            }
        }

        /// <summary>
        /// Pushes a new element to the back of the buffer. Back()/this[Size-1]
        /// will now return this element.
        ///
        /// When the buffer is full, the element at Front()/this[0] will be
        /// popped to allow for this new element to fit.
        /// </summary>
        /// <param name="item">Item to push to the back of the buffer.</param>
        public void PushBack(T item)
        {
            if (IsFull)
            {
                m_Buffer[m_End].Dispose();
                m_Buffer[m_End] = item;
                Increment(ref m_End);
                m_Start = m_End;
            }
            else
            {
                m_Buffer[m_End] = item;
                Increment(ref m_End);
                ++m_Size;
            }
        }

        /// <summary>
        /// Pushes a new element to the front of the buffer. Front()/this[0]
        /// will now return this element.
        ///
        /// When the buffer is full, the element at Back()/this[Size-1] will be
        /// popped to allow for this new element to fit.
        /// </summary>
        /// <param name="item">Item to push to the front of the buffer.</param>
        public void PushFront(T item)
        {
            if (IsFull)
            {
                Decrement(ref m_Start);
                m_End = m_Start;
                m_Buffer[m_Start].Dispose();
                m_Buffer[m_Start] = item;
            }
            else
            {
                Decrement(ref m_Start);
                m_Buffer[m_Start] = item;
                ++m_Size;
            }
        }

        /// <summary>
        /// Removes the element at the back of the buffer. Decreasing the
        /// Buffer size by 1.
        /// </summary>
        public void PopBack()
        {
            ThrowIfEmpty("Cannot take elements from an empty buffer.");
            Decrement(ref m_End);
            m_Buffer[m_End] = default(T);
            --m_Size;
        }

        /// <summary>
        /// Removes the element at the front of the buffer. Decreasing the
        /// Buffer size by 1.
        /// </summary>
        public void PopFront()
        {
            ThrowIfEmpty("Cannot take elements from an empty buffer.");
            m_Buffer[m_Start] = default(T);
            Increment(ref m_Start);
            --m_Size;
        }

        /// <summary>
        /// Copies the buffer contents to an array, according to the logical
        /// contents of the buffer (i.e. independent of the internal
        /// order/contents)
        /// </summary>
        /// <returns>A new array with a copy of the buffer contents.</returns>
        public T[] ToArray()
        {
            T[] newArray = new T[Count];
            int newArrayOffset = 0;
            var segments = new[] { ArrayOne(), ArrayTwo() };
            foreach (ArraySegment<T> segment in segments)
            {
                Array.Copy(segment.Array, segment.Offset, newArray, newArrayOffset, segment.Count);
                newArrayOffset += segment.Count;
            }
            return newArray;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var segments = new[] { ArrayOne(), ArrayTwo() };
            foreach (ArraySegment<T> segment in segments)
            {
                for (int i = 0; i < segment.Count; i++)
                {
                    yield return segment.Array[segment.Offset + i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ThrowIfEmpty(string message = "Cannot access an empty buffer.")
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Increments the provided index variable by one, wrapping
        /// around if necessary.
        /// </summary>
        /// <param name="index"></param>
        void Increment(ref int index)
        {
            if (++index == Capacity)
            {
                index = 0;
            }
        }

        /// <summary>
        /// Decrements the provided index variable by one, wrapping
        /// around if necessary.
        /// </summary>
        /// <param name="index"></param>
        void Decrement(ref int index)
        {
            if (index == 0)
            {
                index = Capacity;
            }
            index--;
        }

        /// <summary>
        /// Converts the index in the argument to an index in <code>_buffer</code>
        /// </summary>
        /// <returns>
        /// The transformed index.
        /// </returns>
        /// <param name='index'>
        /// External index.
        /// </param>
        int InternalIndex(int index)
        {
            return m_Start + (index < (Capacity - m_Start) ? index : index - Capacity);
        }

        // doing ArrayOne and ArrayTwo methods returning ArraySegment<T> as seen here:
        // http://www.boost.org/doc/libs/1_37_0/libs/circular_buffer/doc/circular_buffer.html#classboost_1_1circular__buffer_1957cccdcb0c4ef7d80a34a990065818d
        // http://www.boost.org/doc/libs/1_37_0/libs/circular_buffer/doc/circular_buffer.html#classboost_1_1circular__buffer_1f5081a54afbc2dfc1a7fb20329df7d5b
        // should help a lot with the code.

        #region Array items easy access.
        // The array is composed by at most two non-contiguous segments,
        // the next two methods allow easy access to those.

        ArraySegment<T> ArrayOne()
        {
            if (m_Start < m_End)
            {
                return new ArraySegment<T>(m_Buffer, m_Start, m_End - m_Start);
            }
            else
            {
                return new ArraySegment<T>(m_Buffer, m_Start, m_Buffer.Length - m_Start);
            }
        }

        ArraySegment<T> ArrayTwo()
        {
            if (m_Start < m_End)
            {
                return new ArraySegment<T>(m_Buffer, m_End, 0);
            }
            else
            {
                return new ArraySegment<T>(m_Buffer, 0, m_End);
            }
        }

        #endregion

        public bool BinarySearch(T item, IComparer<T> comparer, out T value)
        {
            value = default;
            if (Count == 0)
                return false;
            // array one
            int i;
            if (m_Start < m_End)
                i = Array.BinarySearch(m_Buffer, m_Start, m_End - m_Start, item, comparer);
            else
                i = Array.BinarySearch(m_Buffer, m_Start, m_Buffer.Length - m_Start, item, comparer);
            if (i >= 0)
            {
                value = m_Buffer[i];
                return true;
            }

            if (i < m_End) // need to check ArrayTwo
            {
                if (m_Start < m_End)
                    i = Array.BinarySearch(m_Buffer, m_End, 0, item, comparer);
                else
                    i = Array.BinarySearch(m_Buffer, 0, m_End, item, comparer);
            }

            if (i >= 0)
            {
                value = m_Buffer[i];
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            foreach (var item in this)
            {
                item.Dispose();
            }
        }
    }
}
