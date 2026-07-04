from pydantic import BaseModel


class MomentsOptions(BaseModel):
    # 朋友圈被@的好友
    at_usrs: list[str] | None
    # 朋友圈哪些设定的标签可以看，如果没有设置标签，则全部可见.
    labels: list[str] | None
    # 是否执行操作后关闭朋友圈,默认关闭，也可以设置为False,然后使用者可以手动关闭<see cref="WeChatClient.CloseMoments"/>
    is_close_moments: bool = True
