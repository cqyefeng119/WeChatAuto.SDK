from datetime import date as dt
from pydantic import BaseModel
from .chat_simple_message import ChatSimpleMessage


class ChatRefer(BaseModel):
    """
    消息引用
    """

    # 日期,如果不设置则不进行日期筛选
    date: dt = dt.min
    # 要引入用的内容，具体请参考:<see cref="ChatSimpleMessage"/>
    message: ChatSimpleMessage
    # 是否关闭查找窗口，默认是关闭，如果设置为false,则不关闭查找窗口，速度会略快，但需要自行关闭查找窗口.
    is_close_search_win: bool = True
