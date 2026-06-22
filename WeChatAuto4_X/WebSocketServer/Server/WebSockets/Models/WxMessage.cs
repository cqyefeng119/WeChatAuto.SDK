using System.Text.Json;
using System.Text.Json.Serialization;

public class WxMessage
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }      // chat / ping /pong / command /echo
    [JsonPropertyName("data")]
    public string? Data { get; set; }
    [JsonPropertyName("connection_id")]
    public string? ConnectionId { get; set; }
}