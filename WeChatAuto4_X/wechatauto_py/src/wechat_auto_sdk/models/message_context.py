from pydantic import BaseModel

from wechat_auto_sdk.models.simple_message_bubble import SimpleMessageBubble
from wechat_auto_sdk.wechat_client import WeChatClient

class MessageContext(BaseModel):
    # 当前我的微信昵称
    owner_nick_name: str
    # 新消息气泡列表
    new_messages: list[SimpleMessageBubble]
    # 历史消息气泡列表,供大模型参考
    history_messages: list[SimpleMessageBubble]
    # 当前微信客户端,通过Client可以执行发消息等操作
    client: WeChatClient
    