from pydantic import BaseModel


class AddFriendsOptions(BaseModel):
    """增加好友选项"""

    # 间隔时间,以秒为单位，默认为三秒，如果担心风控，可以把此时间设置长一点
    interval_time: int = 5
    # 是否关闭增加朋友窗口,默认关闭，可以设置为false不关闭
    is_close_win: bool = True
    # 加好友时打招呼内容,如果为空，则保持微信默认
    say_hi: str = ""
    suffix: str = ""
    label: str = ""
