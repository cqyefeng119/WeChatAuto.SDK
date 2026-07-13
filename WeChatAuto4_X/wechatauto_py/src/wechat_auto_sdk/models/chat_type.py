from enum import IntEnum

class ChatType(IntEnum):
    好友 = 0
    企业微信 = 1
    群聊 = 2
    公众号 = 3
    订阅号 = 4
    服务通知 = 5
    腾讯新闻 = 6
    微信团队 = 7
    文件传输助手 = 8
    其他 = 9