namespace ReqResClient.Core.Options;

public class ReqResClientOptions
{
    public string BaseUrl { get; set; } = "https://reqres.in/api/";
    public int CacheDurationSeconds { get; set; } = 60;
}
