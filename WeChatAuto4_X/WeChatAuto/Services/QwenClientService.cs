using System.Net.Http;

namespace WeChatAuto.Services;

/// <summary>
/// 千问http客户端
/// </summary>
public class QwenClientService
{
    private readonly HttpClient _http;

    public QwenClientService(HttpClient http)
    {
        _http = http;
    }
}