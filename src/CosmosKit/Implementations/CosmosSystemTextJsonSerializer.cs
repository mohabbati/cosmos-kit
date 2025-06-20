﻿using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace CosmosKit;

internal class CosmosSystemTextJsonSerializer : CosmosSerializer
{
    private readonly JsonSerializerOptions _options;

    public CosmosSystemTextJsonSerializer(JsonSerializerOptions options)
    {
        _options = options;
    }

    public override T FromStream<T>(Stream stream)
    {
        if (stream == null || stream.Length == 0)
        {
            return default!;
        }

        if (typeof(T) == typeof(Stream))
        {
            return (T)(object)stream;
        }

        using (stream)
        {
            return JsonSerializer.Deserialize<T>(stream, _options)!;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, input, _options);
        stream.Position = 0;
        return stream;
    }
}
