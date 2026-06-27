import asyncio
import uuid
import json

import websockets

from wechat_client import WeChatClient
from models.wechat_config import WeChatConfig
from cancellation_token_source import CancellationTokenSource
from websocket_client import WebSocketClient
from models.request_data import RequestData


class WechatFactory:
    """
    客户端工厂，目的是为了支持多微信
    每台Websocket服务器对应一个WechatFactory
    在获取到WechatFactory对象前，请确保WechatAuto.SDK的websocket服务端是打开状态
    """

    def __init__(self, uri: str, config: WeChatConfig) -> None:
        self.client_list: dict[str, WeChatClient] = {}
        self.uri = uri
        self.config = config
        self.cts = CancellationTokenSource()

    async def initialize(self):
        """初始化器，最主要用于初始化WeChatClient对象及WebSocketClient对象"""
        await self._fetch_wechat_client()

    async def _fetch_wechat_client(self) -> None:
        """获取远程websocket服务器的所有打开微信"""
        request_data = RequestData(type="global", data="", request_id=uuid.uuid4().hex)
        result = await self.socket_client.request(request_data)
        response = RequestData.model_validate_json(result)
        wechat_list = json.loads(response.data)
        for who in wechat_list:
            self.client_list[who] = WeChatClient(who, self.socket_client)

    def __getitem__(self, key):
        return self.client_list[key]

    async def __aenter__(self):
        self.ws = await websockets.connect(self.uri)
        # 初始化websocket client.
        self.socket_client = WebSocketClient(self.ws, self.cts)
        return self

    async def __aexit__(self, exc_type, exc, tb):
        await self.close()
        return False

    async def close(self):
        """关闭websocket"""
        self.cts.cancel()
        if self.ws.state == websockets.State.CONNECTING:
            await self.ws.close(websockets.CloseCode.NORMAL_CLOSURE, "正常关闭")

    # 类工厂
    @classmethod
    def create_factory(cls, uri, config: WeChatConfig):
        return cls(uri, config)

    # 保持等候状态
    async def keep_running(self) -> None:
        loop = asyncio.get_running_loop()
        future = loop.create_future()
        await future
