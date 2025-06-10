using System.Text.Json.Serialization;

namespace ReqResClient.Core.Models;

public class SingleUserApiResponse
{
    [JsonPropertyName("data")]
    public UserListApiResponse.UserDto Data { get; set; } = null!;
}
