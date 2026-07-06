from datetime import datetime

from pydantic import BaseModel

from wechat_auto_sdk.enums.message_type import MessageType


class SimpleMessageBubble(BaseModel):
    # 微信名
    who: str
    # 消息
    message: str
    # 发送日期,仅精确到分钟
    send_date: datetime
    # 消息类型
    message_type: MessageType
    # 如果是图片，则图片的base64内容
    image_base64_str: str
