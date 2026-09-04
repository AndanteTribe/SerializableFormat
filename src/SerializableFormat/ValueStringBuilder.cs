using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SerializableFormat;

/// <summary>Builds a string using caller-provided initial storage.</summary>
internal ref struct ValueStringBuilder
{
    private Span<char> _buffer;
    private int _length;

    internal ValueStringBuilder(Span<char> initialBuffer)
    {
        _buffer = initialBuffer;
        _length = 0;
    }

    internal int Length
    {
        readonly get => _length;
        set
        {
            Debug.Assert((uint)value <= (uint)_length);
            _length = value;
        }
    }

    internal void Append(char value)
    {
        var length = _length;
        if ((uint)length < (uint)_buffer.Length)
        {
            _buffer[length] = value;
            _length = length + 1;
        }
        else
        {
            GrowAndAppend(value);
        }
    }

    internal void Append(ReadOnlySpan<char> value)
    {
        if (value.Length > _buffer.Length - _length)
        {
            Grow(value.Length);
        }

        value.CopyTo(_buffer.Slice(_length));
        _length += value.Length;
    }

    internal readonly ReadOnlySpan<char> AsSpan() => _buffer.Slice(0, _length);

    internal Span<char> GetAppendSpan() => _buffer.Slice(_length);

    internal void Advance(int count)
    {
        Debug.Assert((uint)count <= (uint)(_buffer.Length - _length));
        _length += count;
    }

    internal void Grow() => Grow(1);

    public override readonly string ToString() => _buffer.Slice(0, _length).ToString();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(char value)
    {
        Grow(1);
        _buffer[_length++] = value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacity)
    {
        var requiredCapacity = checked(_length + additionalCapacity);
        var doubledCapacity = _buffer.Length <= int.MaxValue / 2
            ? _buffer.Length * 2
            : int.MaxValue;
        var newCapacity = Math.Max(requiredCapacity, Math.Max(doubledCapacity, 16));

        var newBuffer = new char[newCapacity];
        _buffer.Slice(0, _length).CopyTo(newBuffer);
        _buffer = newBuffer;
    }
}