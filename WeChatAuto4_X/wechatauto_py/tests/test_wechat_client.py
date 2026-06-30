import pytest
import logging
import asyncio
from datetime import datetime, date

from wechat_auto_sdk.enums.navigation_type import NavigationType
from wechat_auto_sdk.wechat_client import WeChatClient

logger = logging.getLogger(__name__)


@pytest.mark.asyncio
async def test_get_ower_info(client: WeChatClient):
    """
    获取本微信的个人信息，包括头像文件位置，wxid,昵称
    """
    info = await client.get_ower_info()
    logger.info("user info=%s", info)
    assert info is not None


@pytest.mark.asyncio
async def test_max_window(client: WeChatClient):
    """
    最大化微信窗口
    """
    await client.max_window()


@pytest.mark.asyncio
async def test_restore_window(client: WeChatClient):
    """
    还原微信窗口
    """
    await client.restore_window()


@pytest.mark.asyncio
async def test_pinned_window(client: WeChatClient):
    """
    置顶微信窗口
    """
    await client.pinned_window()


@pytest.mark.asyncio
async def test_unpined_widnow(client: WeChatClient):
    """
    取消置顶微信窗口
    """
    await client.unpined_widnow()


@pytest.mark.asyncio
async def test_focus_window(client: WeChatClient):
    """
    使主窗口获取焦点
    """
    await client.focus_window()


@pytest.mark.asyncio
async def test_close_search_window(client: WeChatClient):
    """
    关闭查询窗口,如果查询窗口打开则关闭，如果查询窗口没有打开，则不作动作
    """
    await client.close_search_window("秋歌")
    assert True


@pytest.mark.asyncio
async def test_open_subWin_1(client: WeChatClient):
    """
    打开who指定的子窗口
    """
    await client.open_subWin("秋歌")


@pytest.mark.asyncio
async def test_open_subWin_2(client: WeChatClient):
    """
    打开who指定的子窗口
    """
    await client.open_subWin("前端攻城狮")


@pytest.mark.asyncio
async def test_get_owner_window_handler(client: WeChatClient):
    """
    得到本微信窗口句柄
    """
    handler = await client.get_owner_window_handler()
    print(f"本微信[{client.from_wechat}]的句柄为：{handler}")


@pytest.mark.asyncio
async def test_get_owner_window_process_id(client: WeChatClient):
    """
    得到本微信窗口的进程id
    """
    process_id = await client.get_owner_window_process_id()
    print(f"本微信[{client.from_wechat}]的process_id为：{process_id}")


@pytest.mark.asyncio
async def test_switch_navigation(client: WeChatClient):
    """
    切换导航栏
    """
    await client.switch_navigation(navigationType=NavigationType.通讯录)
    await asyncio.sleep(2)
    await client.switch_navigation(NavigationType.微信)


@pytest.mark.asyncio
async def test_close_navWin(client: WeChatClient):
    """
    关闭通过导航栏打开的窗口.
    """
    await client.switch_navigation(navigationType=NavigationType.朋友圈)
    await asyncio.sleep(2)
    await client.close_navWin(NavigationType.朋友圈)


@pytest.mark.asyncio
async def test_click_motify_icon(client: WeChatClient):
    """
    点击任务栏微信图标
    """
    await client.click_motify_icon(1)


@pytest.mark.asyncio
async def test_click_motify_icon_name(client: WeChatClient):
    """
    点击指定微信名称的任务栏图标
    """
    await client.click_motify_icon_name(client.from_wechat)


@pytest.mark.asyncio
async def test_get_all_conversations(client: WeChatClient):
    """获取会话列表所有会话的标题
    考虑到效率，只返回名称列表

    Returns:
        list[str]: 返回会话标题名称列表
    """
    list = await client.get_all_conversations()
    assert len(list) > 0


@pytest.mark.asyncio
async def test_get_visible_conversation_titles(client: WeChatClient):
    """获取会话列表可见会话标题

    Returns:
        list[str]: 可见的会话列表的标题列表
    """
    list = await client.get_visible_conversation_titles()
    print(list)
    assert len(list) > 0


@pytest.mark.asyncio
async def test_get_visible_conversations(client: WeChatClient):
    """获取可见会话列表
    会话信息包含：会话名称、会话未读消息数、会话头像等具体信息

    Returns:
        list[SimpleConversation]: 返回<see cref="Conversation"/>列表
    """
    list = await client.get_visible_conversations()
    print(list)
    assert len(list) > 0


@pytest.mark.asyncio
async def test_search_friend(client: WeChatClient):
    """搜索好友/群聊

    Args:
        who (str): 待搜索的好友/群聊昵称,who - 微信会话列表肉眼可见的名称,如果群有备注，则这个who即为备注名

    Returns:
        bool: 如果找到，返回true,如果没有找到，则返回false.
    """
    result = await client.search_friend("师父")
    assert result
    await asyncio.sleep(2)
    result = await client.search_friend("女女")
    assert result
    await asyncio.sleep(2)
    result = await client.search_friend("测试xxxxxx")
    assert not result


@pytest.mark.asyncio
async def test_locate_conversation(client: WeChatClient):
    """定位会话
    定位会话的用途：可以将会话列表滚动到指定会话的位置，使指定会话可见

    Args:
        who (str): 会话标题

    Returns:
        bool: 如果找到会话，则返回true，否则返回false
    """
    result = await client.locate_conversation("一")
    assert result
    result = await client.locate_conversation("xxxxx")
    assert not result


@pytest.mark.asyncio
async def test_set_do_not_disturb(client: WeChatClient):
    """设置会话消息免打扰

    Args:
        who (str): 要设置的 好友/群聊 名称,可以为空,如果为空，则为当前窗口设置免打扰
        setting (bool): 如果为:true,则设置会话消息免打扰，如果为:false,则：允许消息通知

    Returns:
        bool: 执行消息免打扰结果
    """
    result = await client.set_do_not_disturb("秋歌", True)
    assert result
    await asyncio.sleep(2)
    result = await client.set_do_not_disturb("秋歌", False)
    assert result


@pytest.mark.asyncio
async def test_set_top_most(client: WeChatClient):
    """设置会话置顶

    Args:
        who (str): true:聊天置顶;false:取消聊天置顶
        setting (bool): 要设置的 好友/群聊 名称,可以为空,如果为空，则为当前窗口设置置顶

    Returns:
        bool: 执行会话置顶结果
    """
    result = await client.set_top_most("秋歌", True)
    assert result
    await asyncio.sleep(2)
    result = await client.set_top_most("秋歌", False)
    assert result


@pytest.mark.asyncio
async def test_get_title(client: WeChatClient):
    """获取当前聊天窗口的标题对象

    Returns:
        HeaderInfo: 标题对象
    """
    result = await client.get_title()
    print(result)
    assert result is not None


@pytest.mark.asyncio
async def test_get_only_title(client: WeChatClient):
    """获取当前聊天窗口的标题对象

    Returns:
        HeaderInfo: 标题对象
    """
    result = await client.get_only_title()
    print(result)
    assert result != ""


@pytest.mark.asyncio
async def test_send_message(client: WeChatClient):
    """
    发送文本消息,可以是群聊名称或者好友名称，名称可以为空，如果为空，则给当前聊天窗口发送消息

    Args:
        who (str): 好名/群聊的名称,也就是肉眼所见的标题
        message (str): 消息内容，文本消息内容
        at_user (str | list[str] | None, optional): 被@的好友,可以一个，也可以多个
        chat_refer (ChatRefer | None, optional): 引用的对话内容,请参考<see cref="ChatRefer"/>
    """
    await client.send_message(
        "DroidMirror官方技术支持", "hello world!", at_user=["AI.Net_test", "智影工坊"]
    )


@pytest.mark.asyncio
async def test_send_file(client: WeChatClient):
    """
    发送文件
    """
    with pytest.raises(Exception):
        await client.send_file("AI.Net_test", [])
    with pytest.raises(FileNotFoundError):
        await client.send_file("AI.Net_test", ["xxxx", "xxx"])
    # 发送图片
    await client.send_file(
        "AI.Net_test", ["C:\\Users\\Administrator\\Desktop\\me\\1.png"]
    )
    # 发送文档
    await asyncio.sleep(2)
    await client.send_file(
        "AI.Net_test",
        [
            "C:\\Users\\Administrator\\Desktop\\me\\1.png",
            "C:\\Users\\Administrator\\Desktop\\me\\2024年膳中膳商业计划书222.pdf",
        ],
    )


@pytest.mark.asyncio
async def test_send_emoji(client: WeChatClient):
    """
    发送表情
    """
    await client.send_emoji("AI.Net_test", 1)
    await asyncio.sleep(2)
    await client.send_emoji("", "微笑")


@pytest.mark.asyncio
async def test_send_voice_chat(client: WeChatClient):
    """
    发起单人语音聊天
    """
    await client.send_voice_chat("AI.Net_test")


@pytest.mark.asyncio
async def test_send_vedio_chat(client: WeChatClient):
    """
    发起单人视频聊天
    """
    await client.send_vedio_chat("AI.Net_test")


@pytest.mark.asyncio
async def test_send_voice_chats(client: WeChatClient):
    """
    发起多人语音聊天，适用于群聊发起语音聊天
    """
    await client.send_voice_chats(
        "DroidMirror官方技术支持", ["AI.Net_test", "智影工坊"]
    )


@pytest.mark.asyncio
async def test_send_voice_message(client: WeChatClient):
    """发送语音消息,此功能依赖虚拟声卡：Cable input/Cable output
    请在声音-->设置-->将输入设备改成: Cable output
    如果没有安装虚拟声卡，请在:https://github.com/alexzhao189/wechatautosdk/blob/main/Resources/VBCABLE_Driver_Pack45.zip下载

    Args:
        who (str): 好友昵称或群聊名称,可以为空，如果为空，则给焦点聊天窗口发送语音消息
        file_path (str): 语音文件路径
    """
    await client.send_voice_message(
        "AI.Net_test",
        "D:\\repo\\WeChatAuto.SDK\\WeChatAuto.SDK\\src\\WeChatAuto4_X\\WeChatAuto.Tests\\Assets\\littlecat.wav",
    )


@pytest.mark.asyncio
async def test_get_chatHistory(client: WeChatClient):
    """
    根据日期获取聊天历史
    """
    list = await client.get_chatHistory(fetch_date=date.today())
    await asyncio.sleep(2)
    print(list)
    assert len(list) > 0
    list = await client.get_chatHistory("Admin.net官方", fetch_date=date.today())
    print(list)
    assert len(list) > 0
