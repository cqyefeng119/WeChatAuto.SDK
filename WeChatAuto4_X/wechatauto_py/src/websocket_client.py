import asyncio
import websockets
import uuid

from cancellation_token_source import CancellationTokenSource
from models.request_data import RequestData
from models.message_package import MessagePackage

"""WebSocket客户端"""


class WebSocketClient:
    def __init__(
        self, ws: websockets.ClientConnection, cts: CancellationTokenSource
    ) -> None:
        self.ws: websockets.ClientConnection = ws
        self.token_source: CancellationTokenSource = cts
        self._pending_requests: dict[str, asyncio.Future[str]] = {}

    async def recv_loop(self) -> None:
        """接收数据loop"""
        try:
            while not self.token_source.token.is_set():
                response = await self.ws.recv()
                result = RequestData.model_validate_json(response)
                if result.type == "ping":
                    await self.send_pong()
                    continue
                else:
                    # 业务代码处理
                    future = self._pending_requests.pop(result.request_id, None)
                    if future is None:
                        continue
                    if future.done:
                        continue
                    # 解包，返回服务器实际运行结果的json字符串
                    future.set_result(result.data)
        except websockets.ConnectionClosed:
            pass
        except asyncio.CancelledError:
            raise
        except Exception as ex:
            print(f"recv_loop error: {ex}")

    async def send_pong(self) -> None:
        """回应服务器心跳"""
        await self.ws.send(
            RequestData(
                type="pong", data="", request_id=uuid.uuid4().hex
            ).model_dump_json()
        )

    async def send(self, message: str) -> str:
        """发送结构化数据"""
        request_id = uuid.uuid4().hex
        request = (
            RequestData(type="pong", data="", request_id=request_id)
            if message == "pong"
            else RequestData(type="command", data=message, request_id=request_id)
        )
        loop = asyncio.get_running_loop()
        future = loop.create_future()
        self._pending_requests[request_id] = future
        await self.ws.send(request.model_dump_json())
        return await future
    async def request(self,request: RequestData)->str:
        """发送原始字符串"""
        await self.ws.send(request.model_dump_json())
        loop = asyncio.get_running_loop()
        future = loop.create_future()
        self._pending_requests[request.request_id] = future
        return await future        

    async def send_obj(self, messge: MessagePackage) -> str:
        """发送对象"""
        return await self.send(messge.model_dump_json())
