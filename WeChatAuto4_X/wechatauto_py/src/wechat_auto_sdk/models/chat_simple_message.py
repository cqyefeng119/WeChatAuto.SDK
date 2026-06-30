from pydantic import BaseModel
from datetime import datetime


class ChatSimpleMessage(BaseModel):
    # 微信名称
    who: str
    # 消息
    message: str
    # 消息日期,字符串格式
    send_date_time: str
    # 消息日期,日期时间格式
    date_time: datetime
    # 唯一字符串
    unique_string: str
