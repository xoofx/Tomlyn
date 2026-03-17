// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Tomlyn.Serialization.Internal;

internal struct PooledArrayBuilder<T> : IDisposable
{
    private T[] _buffer;
    private int _count;

    public PooledArrayBuilder(int initialCapacity)
    {
        _buffer = initialCapacity > 0 ? ArrayPool<T>.Shared.Rent(initialCapacity) : Array.Empty<T>();
        _count = 0;
    }

    public int Count => _count;

    public void Add(T item)
    {
        if ((uint)_count < (uint)_buffer.Length)
        {
            _buffer[_count++] = item;
            return;
        }

        Grow();
        _buffer[_count++] = item;
    }

    public T[] ToArrayAndReturn()
    {
        if (_count == 0)
        {
            Return();
            return Array.Empty<T>();
        }

        var result = new T[_count];
        Array.Copy(_buffer, result, _count);
        Return();
        return result;
    }

    private void Grow()
    {
        var nextSize = _buffer.Length == 0 ? 16 : _buffer.Length * 2;
        var newBuffer = ArrayPool<T>.Shared.Rent(nextSize);
        if (_count > 0)
        {
            Array.Copy(_buffer, newBuffer, _count);
        }

        var count = _count;
        Return();
        _buffer = newBuffer;
        _count = count;
    }

    private void Return()
    {
        var buffer = _buffer;
        if (buffer.Length != 0)
        {
#if NETSTANDARD2_0
            Array.Clear(buffer, 0, Math.Min(_count, buffer.Length));
#else
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(buffer, 0, Math.Min(_count, buffer.Length));
            }
#endif
            ArrayPool<T>.Shared.Return(buffer);
        }

        _buffer = Array.Empty<T>();
        _count = 0;
    }

    public void Dispose() => Return();
}
