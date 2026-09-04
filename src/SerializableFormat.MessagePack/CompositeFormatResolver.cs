using MessagePack;
using MessagePack.Formatters;

namespace SerializableFormat.MessagePack;

/// <summary>Resolves the MessagePack formatter for <see cref="SerializableCompositeFormat"/>.</summary>
public sealed class CompositeFormatResolver : IFormatterResolver
{
    private CompositeFormatResolver()
    {
    }

    /// <summary>Gets the shared resolver instance.</summary>
    public static CompositeFormatResolver Instance { get; } = new();

    /// <inheritdoc/>
    public IMessagePackFormatter<T>? GetFormatter<T>()
    {
        if (typeof(T) == typeof(SerializableCompositeFormat))
        {
            return (IMessagePackFormatter<T>)(object)CompositeFormatMessagePackFormatter.Instance;
        }

        return null;
    }
}