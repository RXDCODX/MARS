using System.Text.Json;
using Google.Protobuf;

namespace MARS.Shared.Grpc;

public static class MarsGrpcJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static ByteString Serialize(object? value)
    {
        if (value is null)
        {
            return ByteString.Empty;
        }

        return ByteString.CopyFrom(
            JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), Options)
        );
    }
}
