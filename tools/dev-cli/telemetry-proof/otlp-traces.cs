#region Purpose
// OTLP/HTTP protobuf trace decoding for the telemetry proof: OtlpTraces reads spans and
// ProtoReader walks the protobuf wire format. Included by proof.cs via #:include.
#endregion

static class OtlpTraces
{
  internal static List<ProofSpan> Read(byte[] payload)
  {
    List<ProofSpan> spans = [];
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 1 && wire == 2)
      {
        ReadResourceSpans(reader.ReadBytes(), spans);
      }
      else
      {
        reader.Skip(wire);
      }
    }

    return spans;
  }

  private static void ReadResourceSpans(byte[] payload, List<ProofSpan> spans)
  {
    string serviceName = "";
    ProtoReader reader = new(payload);
    List<byte[]> scopes = [];
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 1 && wire == 2)
      {
        serviceName = ReadServiceName(reader.ReadBytes());
      }
      else if (field == 2 && wire == 2)
      {
        scopes.Add(reader.ReadBytes());
      }
      else
      {
        reader.Skip(wire);
      }
    }

    foreach (byte[] scope in scopes)
    {
      ReadScopeSpans(scope, serviceName, spans);
    }
  }

  private static string ReadServiceName(byte[] payload)
  {
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 1 && wire == 2)
      {
        Dictionary<string, string> attributes = ReadKeyValue(reader.ReadBytes());
        if (string.Equals(attributes.GetValueOrDefault("key"), "service.name", StringComparison.Ordinal))
        {
          return attributes.GetValueOrDefault("value") ?? "";
        }
      }
      else
      {
        reader.Skip(wire);
      }
    }

    return "";
  }

  private static void ReadScopeSpans(byte[] payload, string serviceName, List<ProofSpan> spans)
  {
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 2 && wire == 2)
      {
        spans.Add(ReadSpan(reader.ReadBytes(), serviceName));
      }
      else
      {
        reader.Skip(wire);
      }
    }
  }

  private static ProofSpan ReadSpan(byte[] payload, string serviceName)
  {
    string name = "";
    string traceId = "";
    string spanId = "";
    string parentSpanId = "";
    Dictionary<string, string> attributes = [];
    int statusCode = 0;
    string statusMessage = "";
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      switch (field)
      {
        case 1 when wire == 2:
          traceId = Convert.ToHexString(reader.ReadBytes()).ToLowerInvariant();
          break;
        case 2 when wire == 2:
          spanId = Convert.ToHexString(reader.ReadBytes()).ToLowerInvariant();
          break;
        case 4 when wire == 2:
          parentSpanId = Convert.ToHexString(reader.ReadBytes()).ToLowerInvariant();
          break;
        case 5 when wire == 2:
          name = Encoding.UTF8.GetString(reader.ReadBytes());
          break;
        case 9 when wire == 2:
          Dictionary<string, string> attribute = ReadKeyValue(reader.ReadBytes());
          if (attribute.TryGetValue("key", out string? key) && attribute.TryGetValue("value", out string? value))
          {
            attributes[key] = value;
          }

          break;
        case 15 when wire == 2:
          (statusCode, statusMessage) = ReadStatus(reader.ReadBytes());
          break;
        default:
          reader.Skip(wire);
          break;
      }
    }

    return new ProofSpan
    {
      Name = name,
      TraceId = traceId,
      SpanId = spanId,
      ParentSpanId = parentSpanId,
      ServiceName = serviceName,
      StatusCode = statusCode,
      StatusMessage = statusMessage,
      Attributes = attributes,
    };
  }

  private static (int Code, string Message) ReadStatus(byte[] payload)
  {
    int code = 0;
    string message = "";
    bool sawCode = false;
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 2 && wire == 2)
      {
        message = Encoding.UTF8.GetString(reader.ReadBytes());
      }
      else if (field == 3 && wire == 0)
      {
        code = (int)reader.ReadVarint();
        sawCode = true;
      }
      else if (field == 1 && wire == 0 && !sawCode)
      {
        code = (int)reader.ReadVarint();
      }
      else
      {
        reader.Skip(wire);
      }
    }

    return (code, message);
  }

  private static Dictionary<string, string> ReadKeyValue(byte[] payload)
  {
    Dictionary<string, string> result = [];
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 1 && wire == 2)
      {
        result["key"] = Encoding.UTF8.GetString(reader.ReadBytes());
      }
      else if (field == 2 && wire == 2)
      {
        result["value"] = ReadAny(reader.ReadBytes());
      }
      else
      {
        reader.Skip(wire);
      }
    }

    return result;
  }

  private static string ReadAny(byte[] payload)
  {
    ProtoReader reader = new(payload);
    while (reader.Remaining > 0)
    {
      (int field, int wire) = reader.ReadTag();
      if (field == 1 && wire == 2)
      {
        return Encoding.UTF8.GetString(reader.ReadBytes());
      }

      if (field == 3 && wire == 0)
      {
        return reader.ReadVarint().ToString(CultureInfo.InvariantCulture);
      }

      if (field == 2 && wire == 0)
      {
        return reader.ReadVarint() == 0 ? "false" : "true";
      }

      reader.Skip(wire);
    }

    return "";
  }
}

sealed class ProtoReader
{
  private readonly byte[] Buffer;
  private int Offset;

  internal ProtoReader(byte[] buffer)
  {
    Buffer = buffer;
  }

  internal int Remaining => Buffer.Length - Offset;

  internal (int Field, int Wire) ReadTag()
  {
    int tag = (int)ReadVarint();
    return (tag >> 3, tag & 7);
  }

  internal ulong ReadVarint()
  {
    ulong result = 0;
    int shift = 0;
    while (true)
    {
      byte next = Buffer[Offset++];
      result |= (ulong)(next & 0x7F) << shift;
      if ((next & 0x80) == 0)
      {
        return result;
      }

      shift += 7;
    }
  }

  internal byte[] ReadBytes()
  {
    int length = (int)ReadVarint();
    byte[] slice = Buffer[Offset..(Offset + length)];
    Offset += length;
    return slice;
  }

  internal void Skip(int wire)
  {
    switch (wire)
    {
      case 0:
        ReadVarint();
        break;
      case 1:
        Offset += 8;
        break;
      case 2:
        ReadBytes();
        break;
      case 5:
        Offset += 4;
        break;
      default:
        throw new InvalidOperationException($"Unsupported protobuf wire type {wire}.");
    }
  }
}
