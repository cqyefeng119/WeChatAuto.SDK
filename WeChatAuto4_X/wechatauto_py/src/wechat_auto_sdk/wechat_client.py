from __future__ import annotations

import asyncio
from datetime import date
import uuid
import base64
import json

from pathlib import Path
from collections.abc import Awaitable, Callable

from wechat_auto_sdk.enums.forward_message_type_enums import ForwardMessageTypeEnums
from wechat_auto_sdk.enums.friend_add_result import FriendAddResult
from wechat_auto_sdk.models.add_friends_options import AddFriendsOptions
from wechat_auto_sdk.models.chat_refer import ChatRefer
from wechat_auto_sdk.models.chat_simple_message import ChatSimpleMessage
from wechat_auto_sdk.models.friend_info import FriendInfo
from wechat_auto_sdk.models.friend_request_auto_accept_options import (
    FriendRequestAutoAcceptOptions,
)
from wechat_auto_sdk.models.moments_options import MomentsOptions
from wechat_auto_sdk.models.new_friend_back_item import NewFriendBackItem

from wechat_auto_sdk.models.system_message_context import SystemMessageContext
from wechat_auto_sdk.websocket_client import WebSocketClient
from wechat_auto_sdk.models.owner_info import OwerInfo
from wechat_auto_sdk.models.message_package import MessagePackage
from wechat_auto_sdk.enums.navigation_type import NavigationType
from wechat_auto_sdk.models.simple_conversation import SimpleConversation
from wechat_auto_sdk.models.header_info import HeaderInfo


class WeChatClient:
    """微信客户端类，在多微信中，是对一个微信的抽象"""

    def __init__(self, from_wechat: str, socket: WebSocketClient) -> None:
        self.from_wechat = from_wechat
        self.socket = socket
        self._group_system_monitor_started = False  # 是否启动系统群聊消息监控

    async def get_ower_info(self) -> OwerInfo:
        """
        获取本微信的个人信息，包括头像文件位置，wxid,昵称
        """
        request_package = MessagePackage(
            request_id=uuid.uuid4().hex,
            func_Name="GetOwerInfo",
            from_wechat=self.from_wechat,
            options="",
        )
        result = await self.socket.send_obj(request_package)
        # 处理本地图片
        owner_info = OwerInfo.model_validate_json(result)
        local_file = Path.cwd() / "Avator" / Path(owner_info.avator_path).name
        bytes = base64.b64decode(owner_info.upload)
        if not local_file.parent.exists():
            local_file.parent.mkdir(parents=True, exist_ok=True)
        local_file.write_bytes(bytes)
        owner_info.avator_path = str(local_file.resolve())
        return owner_info

    async def max_window(self) -> None:
        """
        最大化微信窗口
        """
        request_package = MessagePackage(
            request_id=uuid.uuid4().hex,
            func_Name="Max",
            options="",
            from_wechat=self.from_wechat,
        )
        await self.socket.send_obj(request_package)

    async def restore_window(self) -> None:
        """
        还原微信窗口
        """
        request_package = MessagePackage(
            request_id=uuid.uuid4().hex,
            func_Name="Restore",
            options="",
            from_wechat=self.from_wechat,
        )
        await self.socket.send_obj(request_package)

    async def pinned_window(self) -> None:
        """
        置顶微信窗口
        """
        request_package = MessagePackage(
            request_id=uuid.uuid4().hex,
            func_Name="Pinned",
            options="",
            from_wechat=self.from_wechat,
        )
        await self.socket.send_obj(request_package)

    async def unpined_widnow(self) -> None:
        """
        取消置顶微信窗口
        """
        request_package = MessagePackage(
            request_id=uuid.uuid4().hex,
            func_Name="UnPinned",
            options="",
            from_wechat=self.from_wechat,
        )
        await self.socket.send_obj(request_package)

    async def _do_remote_action(self, action_name: str, options: str):
        """执行远程websocket服务端的方法"""
        request_package = MessagePackage(
            request_id=uuid.uuid4().hex,
            func_Name=action_name,
            options=options,
            from_wechat=self.from_wechat,
        )
        await self.socket.send_obj(request_package)

    async def _do_remote_function(self, func_name: str, options: str) -> str:
        """执行远程websocket服务端的函数并返回值"""
        request_package = MessagePackage(
            request_id=uuid.uuid4().hex,
            func_Name=func_name,
            from_wechat=self.from_wechat,
            options=options,
        )
        return await self.socket.send_obj(request_package)

    async def focus_window(self) -> None:
        """
        使主窗口获取焦点
        """
        request_package = MessagePackage(
            request_id=uuid.uuid4().hex,
            func_Name="Focus",
            options="",
            from_wechat=self.from_wechat,
        )
        await self.socket.send_obj(request_package)

    async def close_search_window(self, who: str) -> None:
        """关闭查询窗口,如果查询窗口打开则关闭，如果查询窗口没有打开，则不作动作"""
        await self._do_remote_action("CloseSearchWindow", who)

    async def open_subWin(self, who: str) -> None:
        """打开who指定的子窗口"""
        await self._do_remote_action("OpenSubWin", who)

    async def get_owner_window_handler(self) -> int:
        """得到本微信窗口句柄"""
        handler = await self._do_remote_function("GetHandler", "")
        return int(handler)

    async def get_owner_window_process_id(self) -> int:
        """得到本微信窗口的进程id"""
        process_id = await self._do_remote_function("GetProcessId", "")
        return int(process_id)

    async def switch_navigation(self, navigationType: NavigationType) -> None:
        """切换导航栏"""
        await self._do_remote_action("SwitchNavigation", str(navigationType))

    async def close_navWin(self, navigationType: NavigationType) -> None:
        """
        关闭通过导航栏打开的窗口.
        仅支持聊天文件、朋友圈、视频号、看一看、搜一搜、小程序面板等窗口
        """
        await self._do_remote_action("CloseNavWin", str(navigationType))

    async def click_motify_icon(self, index: int) -> None:
        """点击任务栏微信图标

        Args:
            index (int): 图标索引，从1开始,索引范围不能越界
        """
        await self._do_remote_action("ClickNotifyIcon", str(index))

    async def click_motify_icon_name(self, wechat_name: str) -> None:
        """点击指定微信名称的任务栏图标

        Args:
            wechat_name (str): 微信名称
        """
        await self._do_remote_action("ClickNotifyIcon", wechat_name)

    async def get_all_conversations(self) -> list[str]:
        """获取会话列表所有会话的标题
        考虑到效率，只返回名称列表

        Returns:
            list[str]: 返回会话标题名称列表
        """
        result_str = await self._do_remote_function("GetAllConversations", "")
        return json.loads(result_str)

    async def get_visible_conversation_titles(self) -> list[str]:
        """获取会话列表可见会话标题

        Returns:
            list[str]: 可见的会话列表的标题列表
        """
        result_str = await self._do_remote_function("GetVisibleConversationTitles", "")
        return json.loads(result_str)

    async def get_visible_conversations(self) -> list[SimpleConversation]:
        """获取可见会话列表
        会话信息包含：会话名称、会话未读消息数、会话头像等具体信息

        Returns:
            list[SimpleConversation]: 返回<see cref="Conversation"/>列表
        """
        result_str = await self._do_remote_function("GetVisibleConversations", "")
        return json.loads(result_str)

    async def search_friend(self, who: str) -> bool:
        """搜索好友/群聊

        Args:
            who (str): 待搜索的好友/群聊昵称,who - 微信会话列表肉眼可见的名称,如果群有备注，则这个who即为备注名

        Returns:
            bool: 如果找到，返回true,如果没有找到，则返回false.
        """
        result_str = await self._do_remote_function("SearchFriend", who)
        return result_str.lower() == "true"

    async def locate_conversation(self, who: str) -> bool:
        """定位会话
        定位会话的用途：可以将会话列表滚动到指定会话的位置，使指定会话可见

        Args:
            who (str): 会话标题

        Returns:
            bool: 如果找到会话，则返回true，否则返回false
        """
        result_str = await self._do_remote_function("LocateConversation", who)
        return result_str.lower() == "true"

    async def set_do_not_disturb(self, who: str, setting: bool) -> bool:
        """设置会话消息免打扰

        Args:
            who (str): 要设置的 好友/群聊 名称,可以为空,如果为空，则为当前窗口设置免打扰
            setting (bool): 如果为:true,则设置会话消息免打扰，如果为:false,则：允许消息通知

        Returns:
            bool: 执行消息免打扰结果
        """
        result_str = await self._do_remote_function(
            "SetDoNotDisturb", json.dumps({"who": who, "setting": setting})
        )
        return result_str.lower() == "true"

    async def set_top_most(self, who: str, setting: bool) -> bool:
        """设置会话置顶

        Args:
            who (str): true:聊天置顶;false:取消聊天置顶
            setting (bool): 要设置的 好友/群聊 名称,可以为空,如果为空，则为当前窗口设置置顶

        Returns:
            bool: 执行会话置顶结果
        """
        result_str = await self._do_remote_function(
            "SetTopMost", json.dumps({"who": who, "setting": setting})
        )
        return result_str.lower() == "true"

    async def get_title(self) -> HeaderInfo:
        """获取当前聊天窗口的标题对象

        Returns:
            HeaderInfo: 标题对象
        """
        result_str: str = await self._do_remote_function("GetTitle", "")
        return HeaderInfo.model_validate_json(result_str)

    async def focuse_sender_input(self) -> None:
        """
        当前窗口的Sender输入区域点击，以获得焦点，也可以取消系统的消息提醒或者关闭右侧Pane等作用
        """
        await self._do_remote_action("FocuseSenderInput", "")

    async def get_only_title(self) -> str:
        """获取当前标窗的标题

        Returns:
            str: 当前窗口的标题名称
        """
        result_str: str = await self._do_remote_function("GetOnlyTitle", "")
        return result_str

    async def send_message(
        self,
        who: str,
        message: str,
        *,
        at_user: str | list[str] | None = None,
        chat_refer: ChatRefer | None = None,
    ) -> None:
        """
        发送文本消息,可以是群聊名称或者好友名称，名称可以为空，如果为空，则给当前聊天窗口发送消息

        Args:
            who (str): 好名/群聊的名称,也就是肉眼所见的标题
            message (str): 消息内容，文本消息内容
            at_user (str | list[str] | None, optional): 被@的好友,可以一个，也可以多个
            chat_refer (ChatRefer | None, optional): 引用的对话内容,请参考<see cref="ChatRefer"/>
        """
        at_list = (
            []
            if at_user is None
            else [at_user]
            if isinstance(at_user, str)
            else at_user
        )
        await self._do_remote_action(
            "SendMessage",
            json.dumps(
                {
                    "who": who,
                    "message": message,
                    "atUser": json.dumps(at_list),
                    "refer": json.dumps(chat_refer),
                }
            ),
        )

    async def send_file(self, who: str, files: list[str]) -> None:
        """发送文件

        Args:
            who (str): 好友/群聊，可以为空,如果为空，则发送到当前聊天窗口
            files (list[str]): 文件路径列表
        """
        if len(files) == 0:
            raise Exception(f"参数{files}错误： 包含的文件名不能为空!")
        # 检查文件是否都存在
        missing_file = next((file for file in files if not Path(file).is_file()), None)
        if missing_file is not None:
            raise FileNotFoundError(missing_file)
        # 读取文件，并且转为base64字符上传
        upload = {
            file: base64.b64encode(Path(file).read_bytes()).decode("utf-8")
            for file in files
        }
        await self._do_remote_action(
            "SendFile",
            json.dumps(
                {"who": who, "files": json.dumps(files), "upload": json.dumps(upload)}
            ),
        )

    async def send_emoji(
        self, who: str, emoji: int | str, at_user: list[str] | None = None
    ) -> None:
        """发送表情

        Args:
            who (str): 被发送消息的好友名称/群聊名称
            emoji (int | str): 表情名称或者描述或者索引
            at_user (list[str]): 被@的好友列表
        """
        await self._do_remote_action(
            "SendEmoji",
            json.dumps({"who": who, "emoji": emoji, "atUser": json.dumps(at_user)}),
        )

    async def send_voice_chat(self, who: str) -> None:
        """发起单人语音聊天

        Args:
            who (str): 好友昵称,可以为空，如果为空，则发送到当前聊天窗口
        """
        await self._do_remote_action("SendVoiceChat", who)

    async def send_vedio_chat(self, who: str) -> None:
        """发起单人视频聊天

        Args:
            who (str): 好友昵称,可以为空，如果为空，则发送到当前聊天窗口
        """
        await self._do_remote_action("SendVedioChat", who)

    async def send_voice_chats(self, who: str, partner: list[str]) -> None:
        """发起多人语音聊天，适用于群聊发起语音聊天

        Args:
            who (str): 群聊名称,可以为空，如果为空，则发送到当前聊天窗口
            partner (list[str]): 参与者，好友昵称列表,必须是群聊成员
        """
        await self._do_remote_action(
            "SendVoiceChats", json.dumps({"who": who, "partner": json.dumps(partner)})
        )

    async def send_voice_message(self, who: str, file_path: str) -> None:
        """发送语音消息,此功能依赖虚拟声卡：Cable input/Cable output
        请在声音-->设置-->将输入设备改成: Cable output
        如果没有安装虚拟声卡，请在:https://github.com/alexzhao189/wechatautosdk/blob/main/Resources/VBCABLE_Driver_Pack45.zip下载

        Args:
            who (str): 好友昵称或群聊名称,可以为空，如果为空，则给焦点聊天窗口发送语音消息
            file_path (str): 语音文件路径
        """
        if not file_path:
            raise Exception("错误：参数file_path不能为空！")
        path = Path(file_path)
        if not path.is_file():
            raise FileNotFoundError()
        file_base64 = base64.b64encode(Path(file_path).read_bytes()).decode("utf-8")
        await self._do_remote_action(
            "SendVoiceMessage",
            json.dumps({"who": who, "filePath": file_path, "upload": file_base64}),
        )

    async def _get_chat_history_current_window(
        self,
        fetch_date: date | None,
    ) -> list[ChatSimpleMessage]:
        """根据日期获取当前聊天窗口的聊天历史

        Args:
            date (datetime): 查询日期,如果为空，则为当天日期

        Returns:
            list[ChatSimpleMessage]: 返回<see cref="ChatSimpleMessage"/>列表
        """
        if fetch_date is None:
            fetch_date = date.today()
        result = await self._do_remote_function(
            "GetChatHistory_Current_Window", fetch_date.strftime("%Y-%m-%d")
        )
        return json.loads(result)

    async def _get_chatHistory_who(
        self, who: str, fetch_date: date | None
    ) -> list[ChatSimpleMessage]:
        """根据日期获取聊天历史

        Args:
            client (WeChatClient): 微信名称，可以是好友/群聊的微信名称,可以为空，如果为空，则获取当前聊天窗口的历史记录

        Returns:
            list[ChatSimpleMessage]: 查询日期,如果不传，则是当天日期
        """
        if fetch_date is None:
            fetch_date = date.today()
        result = await self._do_remote_function(
            "GetChatHistory_Who",
            json.dumps({"who": who, "fetch_date": fetch_date.strftime("%Y-%m-%d")}),
        )
        return json.loads(result)

    async def get_chatHistory(
        self, who: str | None = None, fetch_date: date | None = None
    ) -> list[ChatSimpleMessage]:
        """根据日期获取聊天历史

        Args:
            fetch_date (date | None): 查询日期,如果不传，则是当天日期
            who (str | None, optional): 微信名称，可以是好友/群聊的微信名称,可以为空，如果为空，则获取当前聊天窗口的历史记录
        """
        if who is None:
            return await self._get_chat_history_current_window(fetch_date=fetch_date)
        else:
            return await self._get_chatHistory_who(who, fetch_date)

    async def tap_who(self, who: str, prev_scroll_number: int = 30) -> bool:
        """拍一拍
        注意：只能拍一拍当前聊天窗口的好友,一般结合消息监听使用.
        只有两个地方可以拍一拍：一个是群聊中，一个是好友聊天窗口（非企业微信,企业微信聊天不能拍一拍),自己不能拍一拍自己

        Args:
            who (str): 要拍一拍的好友昵称
            prev_scroll_number (int, optional): 如果当前页找不到，往前滚动的次数. Defaults to 30.
        """
        result = await self._do_remote_function(
            "TapWho", json.dumps({"who": who, "prev_scroll_number": prev_scroll_number})
        )
        return result.lower() == "true"

    async def forward_multiple_message(
        self,
        who: str,
        to: list[str],
        f_type: ForwardMessageTypeEnums = ForwardMessageTypeEnums.ForwardMerge,
        row_count: int = 5,
    ) -> bool:
        """转发多条消息,默认转发最后5条消息，可以自行指定转发多少条消息

        Args:
            who (str): 被转发消息的好友/群聊,可以为空，则转发本窗口的消息
            to (list[str]): 要转发给谁,可以设置多个好友/群聊
            f_type (ForwardMessageTypeEnums, optional): 消息转发类型，详情请参见<see cref="ForwardMessageTypeEnums"/>
            row_count (int, optional): 要转发多少条消息，默认是最后的5条消息,如果当前没有5条，则转发所有消息
        """
        result = await self._do_remote_function(
            "ForwardMultipleMessage",
            json.dumps(
                {
                    "who": who,
                    "to": json.dumps(to),
                    "f_type": f_type.value,
                    "row_count": row_count,
                }
            ),
        )
        return result.lower() == "true"

    async def forward_single_message(
        self, who: str, message: str, to: list[str], prev_scroll_number: int = 30
    ) -> bool:
        """转发单条消息

        Args:
            who (str): 要转发的好友昵称
            message (str): 要转发的消息内容
            to (list[str]): 要转发给谁,可以是多个好友
            prev_scroll_number (int, optional): 如果当前页找不到，往前滚动的次数. Defaults to 30.

        Returns:
            bool: True - 发送成功 False - 发送失败
        """
        if not who:
            raise ValueError("参数: who 不能为空")
        if not to:
            raise ValueError("参数: to 不能为空")

        result: str = await self._do_remote_function(
            "ForwardSingleMessage",
            json.dumps(
                {
                    "who": who,
                    "message": message,
                    "to": json.dumps(to),
                    "prev_scroll_number": prev_scroll_number,
                }
            ),
        )
        return result.lower() == "true"

    async def open_add_friens_win(self):
        """
        打开新增朋友窗口
        如果未打开新增朋友窗口，则打开新增朋友窗口，如果已打开“新增朋友”窗口，则不做动作.
        """
        await self._do_remote_action("OpenAddFriensWin", "")

    async def close_add_friend_win(self):
        """
        关闭新增朋友窗口
        """
        await self._do_remote_action("CloseAddFriendWin", "")

    async def add_friends(
        self, friends: list[str], options: AddFriendsOptions | None = None
    ) -> dict[str, FriendAddResult]:
        """通过手机号码、微信号查找并添加好友

        Args:
            friends (list[str]): 手机号码或者微信号列表
            options (AddFriendsOptions | None, optional): 增加朋友选项，具体请参考<see cref="AddFriendsOptions"/>. Defaults to None.

        Returns:
            dict[str,FriendAddResult]: 添加好友结果列表，详情请参见<see cref="FriendAddResult"/>
        """
        result = await self._do_remote_function(
            "AddFriends",
            json.dumps(
                {"friends": json.dumps(friends), "options": json.dumps(options)}
            ),
        )
        return json.loads(result)

    async def is_owner_chat_group(self, group_name: str) -> bool:
        """是否是自有群

        Args:
            group_name (str): 群聊名称

        Returns:
            bool: 是否是自有群
        """
        if not group_name:
            raise ValueError("错误：群名不能为空！")
        result = await self._do_remote_function("IsOwnerChatGroup", group_name)
        return result.lower() == "true"

    async def get_group_owner(self, group_name: str) -> str:
        """获取群主

        Args:
            group_name (str): 群聊名称

        Returns:
            str: 群主昵称
        """
        if not group_name:
            raise ValueError("错误：群名不能为空！")
        return await self._do_remote_function("GetGroupOwner", group_name)

    async def add_owner_chat_group_member(
        self, group_name: str, member_name: list[str]
    ) -> None:
        """添加群聊成员，适用于自有群

        Args:
            group_name (str): 群聊名称,可以为空，如果为空，则在焦点聊天群聊中添加群聊成员
            member_name (list[str]): 成员名称
        """
        if not member_name:
            raise ValueError("错误：参数 member_name 不能为空")
        await self._do_remote_action(
            "AddOwnerChatGroupMember",
            json.dumps(
                {"group_name": group_name, "member_name": json.dumps(member_name)}
            ),
        )

    async def create_owner_chat_group(
        self, group_name: str, first_who: str, member_name: list[str]
    ) -> bool:
        """创建群聊,如果存在，则打开群聊，否则创建一个新群聊

        Args:
            group_name (str): 群聊名称,不能与之前的群聊名称重复
            first_who (str): 首个成员名称，必须是好友，不能是群聊名称，用来创建群聊定位,可以为空，如果为空，则以当前聊天的好友为基准创建群聊
            member_name (list[str]): 成员名称,成员数量要大于0

        Returns:
            bool: 是否创建成功
        """
        result = await self._do_remote_function(
            "CreateOwnerChatGroup",
            json.dumps(
                {
                    "group_name": group_name,
                    "first_who": first_who,
                    "member_name": json.dumps(member_name),
                }
            ),
        )
        return result.lower() == "true"

    async def change_owner_chat_group_name(
        self, old_group_name: str, new_group_name: str
    ) -> bool:
        """修改群名，适用于自有群群名修改

        Args:
            old_group_name (str): 旧群名称
            new_group_name (str): 新群名称

        Returns:
            bool: 是否修改成功
        """
        result = await self._do_remote_function(
            "ChangeOwnerChatGroupName",
            json.dumps(
                {"old_group_name": old_group_name, "new_group_name": new_group_name}
            ),
        )
        return result.lower() == "true"

    async def change_chat_group_nick_name(
        self, group_name: str, nick_name: str
    ) -> bool:
        """修改自己在群中的昵称

        Args:
            group_name (str): 群名,可以为空，如果为空，则修改焦点群聊的自己在群中的昵称
            nick_name (str): 昵称，如果为空，则删除自己在本群中的昵称

        Returns:
            bool: 是否修改成功
        """
        result = await self._do_remote_function(
            "ChangeChatGroupNickName",
            json.dumps({"group_name": group_name, "nick_name": nick_name}),
        )
        return result.lower() == "true"

    async def change_chat_group_memo(self, group_name: str, new_memo: str) -> bool:
        """改变群备注,群备注仅自己可见.

        Args:
            group_name (str): 群聊名称,可以为空，如果为空，则改变焦点聊天群的备注
            new_memo (str): 新备注，可以为空，如果为空，则删除本群备注

        Returns:
            bool: 是否修改成功
        """
        result = await self._do_remote_function(
            "ChangeChatGroupMemo",
            json.dumps({"group_name": group_name, "new_memo": new_memo}),
        )
        return result.lower() == "true"

    async def update_group_notice(self, group_name: str, group_notice: str) -> bool:
        """更新群聊公告,仅适用于自有群

        Args:
            group_name (str): 群聊名称，可以为空字符串，如果为空，则更新焦点聊天群聊窗口的公告
            group_notice (str): 群聊公告

        Returns:
            bool: 是否修改成功
        """
        result = await self._do_remote_function(
            "UpdateGroupNotice",
            json.dumps({"group_name": group_name, "group_notice": group_notice}),
        )
        return result.lower() == "true"

    async def get_chat_group_member_list(self, group_name: str) -> list[str]:
        """获取群聊成员列表

        Args:
            group_name (str): 群聊名称,可以为空，如果为空，则获取的是焦点聊天群聊的成员列表

        Returns:
            list[str]: 群聊成员列表
        """
        result = await self._do_remote_function("GetChatGroupMemberList", group_name)
        return json.loads(result)

    async def remove_owner_chat_group_member(
        self, group_name: str, member_name: list[str]
    ) -> bool:
        """移除群聊成员,适用于自有群

        Args:
            group_name (str): 群聊名称,可以为空，如果为空，则从焦点聊天群聊中移除好友
            member_name (str): 成员名称

        Returns:
            bool: 操作结果
        """
        if not member_name:
            raise ValueError("错误：member_name不能为空!")
        result = await self._do_remote_function(
            "RemoveOwnerChatGroupMember",
            json.dumps(
                {"group_name": group_name, "member_name": json.dumps(member_name)}
            ),
        )
        return result.lower() == "true"

    async def quit_chat_group(
        self, group_name: str, clear_history: bool = True
    ) -> None:
        """退出群聊

        Args:
            group_name (str): 群聊名称
            clear_history (bool, optional): 是否清除历史消息. Defaults to True.
        """
        if not group_name:
            raise ValueError("错误：group_name参数不能为空！")
        await self._do_remote_action(
            "QuitChatGroup",
            json.dumps({"group_name": group_name, "clear_history": clear_history}),
        )

    async def invite_chat_group_member(
        self, group_name: str, members: list[str], invite_reason_if_need: str = ""
    ) -> bool:
        """邀请群聊成员,适用于外部群

        Args:
            group_name (str): 群聊名称,可以为空，如果为空，则在本焦点群聊窗口邀请好友
            members (list[str]): 被邀请的成员名称列表,要求在自己的通讯录中
            invite_reason_if_need (str): 邀请原因，只在群管理员开启了 进群需要群主或者管理员确认 功能时有效，可以为空

        Returns:
            bool: 操作结果
        """
        if not members:
            raise ValueError("错误：members 不能为空")
        result = await self._do_remote_function(
            "InviteChatGroupMember",
            json.dumps(
                {
                    "group_name": group_name,
                    "members": json.dumps(members),
                    "invite_reason_if_need": invite_reason_if_need,
                }
            ),
        )
        return result.lower() == "true"

    async def add_chat_group_member_to_friends(
        self,
        group_name: str,
        member_name: list[str],
        options: AddFriendsOptions | None = None,
    ) -> dict[str, FriendAddResult]:
        """添加群聊里面的好友为自己的好友,适用于从外部群中添加好友为自己的好友
        此操作为微信严风控操作，因为微信对于一天加好友应该有数量限定，建议分批次加，一次不要超过20-30个，时间延长为4小时或者一天后

        Args:
            group_name (str): 群聊名称,可以为空，如果为空，则在本焦点群聊窗口邀请好友
            member_name (list[str]): 成员名称列表,考虑风控,建议先运行<see cref="Group.GetChatGroupMemberList(string)"/>获取群聊的成员列表，然后分批增加
            options (AddFriendsOptions): 好友选项，可以增加好友时设置备注后缀、打招呼内容及标签等，方便分类管理

        Returns:
            dict[str,FriendAddResult]: 返回每个好友增加情况的字典
        """
        if not member_name:
            raise ValueError("错误：参数 member_name 不能为空！")
        result = await self._do_remote_function(
            "AddChatGroupMemberToFriends",
            json.dumps(
                {
                    "group_name": group_name,
                    "member_name": json.dumps(member_name),
                    "options": json.dumps(options),
                }
            ),
        )
        return json.loads(result)

    async def get_all_friends(self, from_cache: bool = True) -> list[FriendInfo]:
        """获取所有好友的信息列表,具体请考<see cref="FriendInfo"/>类说明.</para>
        注意：只会获取通讯录中的联系人、企业微信联系人和群聊的记录,公众号，服务号，我的企业等特殊账号不会获取.
        1.如果是企业微信，会剔除@xxxx后缀，以保持一致性
        2.如果好友/群聊/企业微信联系人等有备注，则备注会覆盖昵称显示.
        3.注意：如果微信联系人有重名，此方法会仅获取/保存一个联系人，所以运行此方法前:建议好友/群聊/企业微信联系人有重名时，通过手工的方式添加备注，以保持区分.
        4.普通联系人可以获取wxid,其他的如：群聊/企业微信联系人无法获取wxid.
        5. 此方法运行结果会保存在cache中,默认为true,从cache中获取数据，如果设置为false,则重新刷新一遍通讯录,cache也会同步更新，建议实际开发过程中运行一遍从通讯录获取好友信息的操作,并且做好添加好友时的同步工作（在一些监听的场景，如果读取到此好友没有wxid,也会自动获取,并同步更新cache）
        6. 不必太过于担心cache过期的问题，因为实际需要识别wxid的业务场景中，如果碰到新好友在cache中没有数据，会自动获取好友的信息并更新cahce，所以也不必太担心cache的过时问题

        Args:
            from_cache (bool, optional): 是否从cahce中获取数据. Defaults to True.

        Returns:
            list[FriendInfo]: 好友列表
        """
        result = await self._do_remote_function("GetAllFriends", str(from_cache))
        return json.loads(result)

    async def get_all_friend_names(self) -> list[str]:
        """获取所有好友名称列表.（通过通讯录）
        如果好友有昵称与备注，优先选择备注名
        注意：如果是企业微信，会剔除@xxxx后缀，以保持一致性.
        Returns:
            list[str]: 好友名称列表
        """
        result = await self._do_remote_function("GetAllFriendNames", "")
        return json.loads(result)

    async def passed_all_new_friend(
        self,
        options: FriendRequestAutoAcceptOptions,
        passed_callback: Callable[
            [list[NewFriendBackItem], WeChatClient], Awaitable[None]
        ]
        | None = None,
    ) -> list[NewFriendBackItem]:
        """通过加好友添加申请

        Args:
            options (FriendRequestAutoAcceptOptions): 配置对象，具体参见<see cref="FriendRequestAutoAcceptOptions"/>
            passed_callback (Callable[[list[NewFriendBackItem], WeChatClient], Awaitable[None]] | None, optional): 通过好友申请后的自定义回调. Defaults to None.

        Returns:
            list[NewFriendBackItem]: _description_
        """
        options_dumps = FriendRequestAutoAcceptOptions.model_dump_json(options)
        result = await self._do_remote_function("PassedAllNewFriend", options_dumps)
        result_object: list[NewFriendBackItem] = json.loads(result)
        if passed_callback and result_object:
            await passed_callback(result_object, self)

        return result_object

    async def open_moments(self) -> bool:
        """打开朋友圈,如果未打开，则打开朋友圈，如果已经打开了，则窗口提前到顶端

        Returns:
            bool: 是否打开
        """
        result = await self._do_remote_function("OpenMoments", "")
        return result.lower() == "true"

    async def close_moments(self) -> None:
        """关闭朋友圈"""
        await self._do_remote_function("CloseMoments", "")

    async def add_moments(
        self,
        image_files: list[str],
        content: str = "",
        options: MomentsOptions | None = None,
    ) -> bool:
        """发送朋友圈

        Args:
            image_files (list[str]): 图片列表，可以一个，也可以多个,如果是多个文件，要求在同一个目录中
            content (str): 朋友圈内容
            options (MomentsOptions | None, optional): 发送选项，请参考<see cref="MomentsOptions"/></param>. Defaults to None.
        """
        if not image_files:
            raise ValueError("错误：图片列表不能为空！")
        # 检查文件是否都存在
        missing_file = next(
            (file for file in image_files if not Path(file).is_file()), None
        )
        if missing_file is not None:
            raise ValueError("错误：参数 image_files 列表中有一些图片文件不存在！")
        upload = {
            file: base64.b64encode(Path(file).read_bytes()).decode("utf-8")
            for file in image_files
        }
        result = await self._do_remote_function(
            "AddMoments",
            json.dumps(
                {
                    "image_files": json.dumps(image_files),
                    "content": content,
                    "options": options.model_dump_json()
                    if options is not None
                    else None,
                    "upload": json.dumps(upload),
                }
            ),
        )
        return result.lower() == "true"

    async def remove_moments(self, content: str) -> bool:
        """移除自己发送的朋友圈

        Args:
            content (str): 朋友圈文字内容

        Returns:
            bool: 是否成功删除
        """
        if not content:
            raise ValueError("错误：能数 content 内容不能为空！")
        result = await self._do_remote_function("RemoveMoments", content)
        return result.lower() == "true"

    async def add_group_system_message_listener(
        self,
        nick_names: list[str],
        callback: Callable[[SystemMessageContext,WeChatClient], Awaitable[None]],
    ) -> None:
        """加系统消息监听，以实现如： 检测到群主邀请好友后发送欢迎消息等功能
        注意：仅适用于群聊，不适用个人,个人请使用下面的开放式/固定式监听，另外，不支持注册监听后再新增待监听的群聊


        Args:
            nick_names (list[str]): 群聊昵称，可以多个
            callback (Callable): 回调函数,由用户提供,参数：消息上下文<see cref="SystemMessageContext"/>
        """
        if not nick_names:
            raise ValueError("错误：参数nick_names不能为空！")
        if self._group_system_monitor_started:
            return
        self._group_system_monitor_started = True
        self._group_system_task = asyncio.create_task(
            self._add_group_system_message_listener_core(nick_names, callback),
            name="group_system_message_monitor",
        )

    async def _add_group_system_message_listener_core(
        self,
        nick_names: list[str],
        callback: Callable[[SystemMessageContext,WeChatClient], Awaitable[None]],
    ) -> None:
        request_id = uuid.uuid4().hex
        request_package = MessagePackage(
            request_id=request_id,
            func_Name="AddGroupSystemMessageListener",
            options=json.dumps(nick_names),
            from_wechat=self.from_wechat,
        )
        await self.socket.send_group_system_monitor(
            request_package, request_id, nick_names, callback, self
        )

    async def pause_message_listener(self) -> None:
        """
        暂停消息监听
        """
        await self._do_remote_action("PauseMessageListener", "")

    async def resume_message_listener(self) -> None:
        """
        恢复消息监听
        """
        await self._do_remote_action("ResumeMessageListener", "")

    async def add_listening_friend(self, who: str) -> None:
        """监听过程中添加好友

        Args:
            who (str): 好友名称
        """
        await self._do_remote_action("AddListeningFriend", who)

    async def remove_listening_friend(self, who: str) -> None:
        """监听过程中移除被监听中的好友/群聊

        Args:
            who (str): 好友/群聊名称
        """
        await self._do_remote_action("RemoveListeningFriend", who)

    async def keep_running(self) -> None:
        """
        保存运行状态，即异步阻塞状态
        """
        await asyncio.Future(loop=asyncio.get_event_loop())
