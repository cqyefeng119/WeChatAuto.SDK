using System.Diagnostics;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WeChatAuto.Components;

public class MessageHandler
{
    private readonly WeChatClientFactory factory;

    public MessageHandler(WeChatClientFactory factory)
    {
        this.factory = factory;
    }
    public async Task<RequestData> HandleAsync(MessagePackageWrapper wrapper)
    {
        var client = factory.GetWeChatClient(wrapper.From);
        RequestData response = new RequestData
        {
            Type = "echo",
            RequestId = wrapper.RequestId,
        };
        switch (wrapper.FuncName)
        {
            case "GetOwerInfo":
                await _ProcessOwnerInfo(client, response);

                break;
            default:
                throw new Exception("不支持的函数名!");
        }
        return response;
    }

    //GetOwerInfo()
    private async Task _ProcessOwnerInfo(WeChatClient client, RequestData response)
    {
        var info = client.GetOwerInfo();
        response.Data = JsonConvert.SerializeObject(info);
    }
}