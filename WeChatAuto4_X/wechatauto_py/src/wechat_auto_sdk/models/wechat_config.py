from pydantic import BaseModel

class WeChatConfig(BaseModel):
    """微信配置类"""
    # 朋友圈监听时间间隔，单位秒
    moments_listen_interval:int = 5
    # 消息监听间隔时间，单位为秒
    monitor_message_interval:int = 5
    # 消息监听时往下滚动的次数，如果监听列表多，建议设置成：10-30
    # 如果监听列表少，建议设置成:5~10，以提高效率 
    monitor_message_max_down_interval : int = 8
    # 好友申请监听间隔时间，单位为秒
    monitor_new_friend_request_interval: int = 20
    # 监听群聊系统消息的间隔时间，单位为秒
    monitor_group_interval: int = 10
    # 会话列表鼠标滚动行数
    conversation_interval:int = 5
    # 当滚动删除朋友圈内容时，最大滚动次数,如果朋友圈内容多，请将此值设置大一些。
    monents_scroll_max_step: int = 30
    # 点击偏移量,单位像素
    # 为了避免每次点击都点击到同一个位置，可以设置一个偏移量，实际点击位置为点击位置减去偏移量的一个随机值
    km_offset_of_click: int = 5
    """
    是否一开始就初始化通讯录所有好友
    如果以wxid为业务核心，强烈开启此选项
    """
    init_adress_book:bool = False
    # 历史消息X偏移距离
    history_message_offset_x:int = 77
    # 历史消息Y偏移距离
    history_message_offset_y:int = 40
    # 历史消息滚动时重试次数
    history_retry_number:int = 6
    # 头像按钮距离微信按钮的Y轴偏移量
    avator_to_weixin_button_offset_y:int = 40
    # 用于消息监听中，返回给回调函数历史消息最大记录数，因为事实上无须读完整个历史消息的。
    max_history_message_fetch_number: int = 20
    # 为了预防全量搜索历史消息设置的阈值
    max_history_fallback_threshold_number:int = 50
    # 消息监听中，首次运行取历史消息的最大数量
    message_first_fetch_number:int = 10
    # 消息监听中，为了消息稳定下来重试次数
    message_stability_retry_number:int = 5



