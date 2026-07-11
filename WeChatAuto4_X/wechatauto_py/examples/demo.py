import asyncio

from wechat_auto_sdk import WeChatClient
from wechat_auto_sdk import WeChatConfig
from wechat_auto_sdk import WechatFactory

DEFAULT_URI = "ws://localhost:5177/ws"

async def wechat_automation():
    async with WechatFactory.create_factory(
        DEFAULT_URI,
        WeChatConfig(
            # 这里可以设置一些配置信息
        ),
    ) as factory:
        await factory.initialize()
        wechat_list = factory.client_list
        print(
            f"连接服务器({DEFAULT_URI})共有微信客户端{len(wechat_list)}个:{list(wechat_list.keys())}"
        )
        client: WeChatClient = wechat_list["Alex"] # 得到某个微信客户端，支持多微信
        await client.send_message("DroidMirror官方技术支持","hello world!",at_user=["AI.Net_test","智影工坊"])
        # await factory.keep_running()


def main():
    asyncio.run(wechat_automation())


if __name__ == "__main__":
    main()
