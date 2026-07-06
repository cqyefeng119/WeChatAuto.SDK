from pydantic import BaseModel


class MessageMonitorOptions(BaseModel):
    """消息监听器选项"""

    # 如果此好友在缓存中不存在，是否获取此好友的用户信息(包括wxid),并更新缓存，对于基于wxid的企业级开发很有用
    fetch_friend_info: bool = False
    # 如果聊天记录中有图片，是否获取图片
    fetch_image: bool = False
    # 如果聊天记录有微信语音，则取出微信语音的内容，这个依赖微信的设置: 设置 --> 通用 --> 打开"聊天中的语音消息自动转成文字"
    fetch_voice_chat: bool = False
    # 如果聊天记录中有红包、转账，是否点击
    click_red_envelope: bool = False
    # 是否预防风控,如果待监控的群不多，建议设置为False,如果监测的群/好友很多，并且聊天很频繁，建议将设置为True.
    # 因为人不可能一天24小时进行操作的,否则极易被微信风控退出。
    is_risk_prevention: bool = False
