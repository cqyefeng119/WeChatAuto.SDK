using System.Diagnostics;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketHandler
{
    private readonly ConnectionManager _manager;
    private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
    private readonly int heatBeatDelay = 5000;  //心跳设置为5秒
    private readonly SocketSessionChannel? channel;
    private WebSocket? ws;

    public WebSocketHandler(ConnectionManager manager, SocketSessionChannel channel)
    {
        _manager = manager;
        this.channel = channel;
    }

    public async Task HandleAsync(WebSocket ws, CancellationToken userToken)
    {
        this.ws = ws;
        var connId = _manager.Add(ws);
        var cts = new CancellationTokenSource();
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, userToken);
        var token = linkedTokenSource.Token;

        // 心跳任务
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        _ = Task.Run(() => Heartbeat(ws, connId, linkedTokenSource, stopwatch), token);

        try
        {
            var buffer = new byte[8192];
            //保持长连接
            while (ws.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                try
                {
                    var result = await ws.ReceiveAsync(buffer, token);  //阻塞读取.
                    var sb = new StringBuilder();
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    while (!result.EndOfMessage)
                    {
                        result = await ws.ReceiveAsync(buffer, token);
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }

                    var raw = sb.ToString();

                    var msg = JsonSerializer.Deserialize<RequestData>(raw);

                    if (msg?.Type == "pong")
                    {
                        stopwatch.Restart();  //重新计时,
                        Console.WriteLine($"[{connId}] - 收到心跳回复");
                        continue;
                    }

                    Console.WriteLine($"[{connId}] {msg?.Data}");
                    if (msg?.Type == "command")
                    {
                        var data = msg.Data;
                        if (string.IsNullOrWhiteSpace(data))
                            continue;
                        var package = JsonSerializer.Deserialize<MessagePackage>(data);
                        var wrapper = MessagePackageWrapper.Create(package!,this);
                        await this.channel!.AddWxMessage(wrapper);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    //只记录，不做处理
                    Console.WriteLine($"处理用户消息时出错，错误原因:{ex.ToString()}");
                    break;
                }
            }
        }
        finally
        {
            stopwatch.Stop();
            linkedTokenSource.Cancel();
            _manager.Remove(connId);
            if (ws.State == WebSocketState.Open)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            }
            Console.WriteLine($"客户端 {connId} 退出连接....");
        }
    }
    public async Task SendAsync(RequestData msg)
    {
        var json = JsonSerializer.Serialize(msg);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _sendLock.WaitAsync();
        try
        {
            await this.ws!.SendAsync(
                bytes,
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }
    private async Task Heartbeat(WebSocket ws, string connId, CancellationTokenSource linkedTokenSource, Stopwatch stopwatch)
    {
        while (!linkedTokenSource.Token.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            if (stopwatch.IsRunning && stopwatch.ElapsedMilliseconds > 3 * heatBeatDelay)
            {
                linkedTokenSource.Cancel();
                ws.Abort();
                break;
            }
            var ping = new RequestData { Type = "ping" };
            await SendAsync(ping);
            await Task.Delay(heatBeatDelay);
        }
    }
}