import asyncio
import websockets
import uuid

from wechat_auto_sdk.cancellation_token_source import CancellationTokenSource
from wechat_auto_sdk.models.request_data import RequestData
from wechat_auto_sdk.models.message_package import MessagePackage


class WebSocketClient:
    """WebSocket客户端"""

    def __init__(
        self, ws: websockets.ClientConnection, cts: CancellationTokenSource
    ) -> None:
        self.ws: websockets.ClientConnection = ws
        self.token_source: CancellationTokenSource = cts
        self._pending_requests: dict[str, asyncio.Future[str]] = {}
        self._rev_loop_task: asyncio.Task | None = None

    async def recv_loop(self) -> None:
        """接收数据loop"""
        try:
            while not self.token_source.token.is_set():
                response = await self.ws.recv()
                print("*" * 10, " 收到服务器原始信息 ", "*" * 10)
                print(response)
                print("*" * 42)
                result = RequestData.model_validate_json(response)
                if result.type == "ping":
                    await self.send_pong()
                    continue
                elif result.type == "global":
                    global_future = self._pending_requests.pop(result.request_id, None)
                    if global_future is not None:
                        global_future.set_result(result.data)
                else:
                    # 业务代码处理
                    business_future = self._pending_requests.pop(result.request_id, None)
                    if business_future is None:
                        continue
                    if business_future.done():
                        continue
                    # 解包，返回服务器实际运行结果的json字符串
                    business_future.set_result(result.data)
            print("退出websocket接收数据循环")
        except websockets.ConnectionClosed:
            pass
        except asyncio.CancelledError:
            raise
        except Exception as ex:
            print(f"recv_loop error: {ex}")

    async def start(self) -> None:
        if self._rev_loop_task is not None:
            return
        self._rev_loop_task = asyncio.create_task(
            self.recv_loop(), name="_rev_loop_task_"
        )

    async def send_pong(self) -> None:
        """回应服务器心跳"""
        await self.ws.send(
            RequestData(
                type="pong", data="", request_id=uuid.uuid4().hex
            ).model_dump_json()
        )

    async def send(self, message: str, req_id: str | None = None) -> str:
        """发送结构化数据"""
        request_id = uuid.uuid4().hex if req_id is None else req_id
        request = RequestData(type="command", data=message, request_id=request_id)
        loop = asyncio.get_running_loop()
        future = loop.create_future()
        self._pending_requests[request_id] = future
        await self.ws.send(request.model_dump_json())
        return await future

    async def request(self, request: RequestData) -> str:
        """发送原始字符串"""
        loop = asyncio.get_running_loop()
        future = loop.create_future()
        self._pending_requests[request.request_id] = future
        await self.ws.send(request.model_dump_json())
        return await future

    async def send_obj(self, package: MessagePackage) -> str:
        """发送对象"""
        return await self.send(package.model_dump_json(), package.request_id)
