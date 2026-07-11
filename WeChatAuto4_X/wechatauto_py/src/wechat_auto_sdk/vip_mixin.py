from __future__ import annotations

import base64
from collections.abc import Callable, Awaitable
from datetime import time
import json
from pathlib import Path
import uuid

from typing import TYPE_CHECKING, cast

from wechat_auto_sdk.models.message_context import MessageContext
from wechat_auto_sdk.models.message_monitor_options import MessageMonitorOptions
from wechat_auto_sdk.models.moments_options import MomentsOptions
from wechat_auto_sdk.models.system_message_context import SystemMessageContext
from wechat_auto_sdk.models.time_only_range import TimeOnlyRange


from wechat_auto_sdk.models.friend_request_auto_accept_options import (
    FriendRequestAutoAcceptOptions,
)
from wechat_auto_sdk.models.message_package import MessagePackage
from wechat_auto_sdk.models.new_friend_back_item import NewFriendBackItem

if TYPE_CHECKING:
    from wechat_auto_sdk.wechat_client import WeChatClient


class VipMixin:
    @property
    def client(self) -> WeChatClient:
        return self  # type: ignore[return-value]

    async def add_friend_request_auto_accept_listener(
        self,
        options: FriendRequestAutoAcceptOptions,
        callback: Callable[[list[NewFriendBackItem], WeChatClient], Awaitable[None]],
    ):
        """新好友添加监听
        实现的功能:
        1. 自动通过好友申请
        2. 根据设定的关键词过滤好友申请的打招呼文本，只有包含关键词的打招呼内容才会被通过
        3. 通过好友申请时，可以设置后缀,以区分不同类型的好友,方便后续的自动化实现
        4. 通过好友申请时，可以设置特定的微信标签，以方便后续的自动化与好友管理
        5. 也可以通过好友申请后，删除申请记录

        Args:
            options (FriendRequestAutoAcceptOptions): 配置选项，请参考<see cref="FriendRequestAutoAcceptOptions"/>类
            callback (Callable[[list[NewFriendBackItem],WeChatClient],Awaitable[None]]): 通过后的回调函数
        """
        request_id = uuid.uuid4().hex
        request_package = MessagePackage(
            request_id=request_id,
            func_Name="AddFriendRequestAutoAcceptListener",
            options=FriendRequestAutoAcceptOptions.model_dump_json(options),
            from_wechat=self.client.from_wechat,
        )
        await self.client.socket.send_friend_request_auto_accept(
            request_package, request_id, callback, self.client
        )

    async def add_group_system_message_listener(
        self,
        nick_names: list[str],
        callback: Callable[[SystemMessageContext, WeChatClient], Awaitable[None]],
    ) -> None:
        """加系统消息监听，以实现如： 检测到群主邀请好友后发送欢迎消息等功能
        注意：仅适用于群聊，不适用个人,个人请使用下面的开放式/固定式监听，另外，不支持注册监听后再新增待监听的群聊


        Args:
            nick_names (list[str]): 群聊昵称，可以多个
            callback (Callable): 回调函数,由用户提供,参数：消息上下文<see cref="SystemMessageContext"/>
        """
        if not nick_names:
            raise ValueError("错误：参数nick_names不能为空！")

        request_id = uuid.uuid4().hex
        request_package = MessagePackage(
            request_id=request_id,
            func_Name="AddGroupSystemMessageListener",
            options=json.dumps(nick_names),
            from_wechat=self.client.from_wechat,
        )
        await self.client.socket.send_group_system_monitor(
            request_package, request_id, nick_names, callback, self.client
        )

    async def add_message_listener(
        self,
        nick_names: list[str],
        callback: Callable[[MessageContext, WeChatClient], Awaitable[None]],
        options: MessageMonitorOptions | None = None,
        is_open_monitor: bool = False,
    ) -> None:
        """添加消息监听，用户需要提供一个回调函数，当有消息时，会调用此回调函数
        参考<see cref="MessageContext"/>
        消息监听最主要以事件触发的方式，当消息过来的时候，监听才会运行.
        使用规则：
        1. 仅能监听“允许消息通知”的好友/群聊,所以需要监听的好友/群聊不要设置为“消息免打扰”;
        2. 如果要避免太多消息影响，请将不监听的好友/群设置为“消息免打扰”，以提高监听性能;
        3. 此方法可以多次添加，第一次添加时会启动消息监听，第二次（类似的:第三次等）添加效果等同<see cref="AddListeningFriend"/>方法
        4. 为了减少会话窗口的滚动，建议不要添加太多消息“置顶”，以提高监听效率.
        执行逻辑:
        1. 启动消息监听器时，SDK会将会话列表整个循环一遍，会自动点击并回调用户设定的方法，以防止遗漏消息
        2. 以后的监听过程会增量监听，以提高效率.


        Args:
            nickNames (list[str]): 好友昵称,可以是一个，也可以是多个好友/群聊,如果开启开放式监听，前面的nickNames可以为空
            callback (Callable[[MessageContext,WeChatClient],Awaitable[None]]): 回调函数,由用户提供,参数：消息上下文<see cref="MessageContext"/>
            is_open_monitor (bool, optional): 是否开启开放式监听，默认不开放(值为false）,如果开启开放式监听，前面的nickNames可以为空，所谓的开放式监听的含义是：无须固定好友/群监听，只要此好友/群没有设置“消息免打挠”就可以监听. Defaults to False.
            options (MessageMonitorOptions | None, optional): 监听选项，具体请参见<see cref="MessageMonitorOptions"/>. Defaults to None.
        """
        request_id = uuid.uuid4().hex
        request_package = MessagePackage(
            request_id=request_id,
            func_Name="AddMessageListener",
            options=json.dumps(
                {
                    "nick_names": json.dumps(nick_names),
                    "options": MessageMonitorOptions.model_dump_json(options)
                    if options is not None
                    else None,
                    "is_open_monitor": str(is_open_monitor),
                }
            ),
            from_wechat=self.client.from_wechat,
        )
        await self.client.socket.send_message_listener(
            request_package, request_id, nick_names, callback,self.client
        )

    async def add_message_listener_with_time(
        self,
        nick_names: list[str],
        callback: Callable[[MessageContext, WeChatClient], Awaitable[None]],
        start_time: time,
        end_time: time,
        is_open_monitor: bool = False,
        options: MessageMonitorOptions | None = None,
    ) -> None:
        """添加一个从什么时候开始，什么时候结束的消息监听，用户需要提供一个回调函数，当有消息时，会调用此回调函数
        参考<see cref="MessageContext"/>
        适用于固定时间自动化的场景
        使用规则：
        1. 仅能监听“允许消息通知”的好友/群聊,所以需要监听的好友/群聊不要设置为“消息免打扰”;
        2. 如果要避免太多消息影响，请将不监听的好友/群设置为“消息免打扰”，以提高监听性能;
        3. 此方法可以多次添加，第一次添加时会启动消息监听，第二次（类似的第三次等）添加效果等同<see cref="AddListeningFriend"/>方法
        4. 为了减少会话窗口的滚动，建议不要添加太多消息“置顶”，以提高监听效率.
        执行逻辑:
        1. 启动消息监听器时做做一次全量扫描，即：SDK会将会话列表整个循环一编，会自动点击并回调用户设定的方法，以防止遗漏消息
        2. 以后的监听过程会增量监听，以提高效率.


        Args:
            nick_names (list[str]): 好友昵称,可以是一个，也可以是多个好友/群聊
            callback (Callable[[MessageContext,WeChatClient],Awaitable[None]]): 回调函数,由用户提供,参数：消息上下文<see cref="MessageContext"/>
            start_time (time): 开始时间
            end_time (time): 结束时间
            is_open_monitor (bool, optional): 是否开启开放式监听，默认不开放(值为false）,如果开启开放式监听，前面的nickNames可以为空，所谓的开放式监听的含义是：无须固定好友/群监听，只要此好友/群没有设置“消息免打挠”就可以监听
            options (MessageMonitorOptions | None, optional): 监听选项，具体请参见<see cref="MessageMonitorOptions"/>.
        """
        request_id = uuid.uuid4().hex
        request_package = MessagePackage(
            request_id=request_id,
            func_Name="AddMessageListener_With_Time",
            options=json.dumps(
                {
                    "nick_names": json.dumps(nick_names),
                    "options": MessageMonitorOptions.model_dump_json(options)
                    if options is not None
                    else None,
                    "is_open_monitor": str(is_open_monitor),
                    "start_time": str(start_time),
                    "end_time": str(end_time),
                }
            ),
            from_wechat=self.client.from_wechat,
        )
        await self.client.socket.send_message_listener(
            request_package, request_id, nick_names, callback, self.client
        )

    async def add_message_listener_with_range(
        self,
        nick_names: list[str],
        callback: Callable[[MessageContext, WeChatClient], Awaitable[None]],
        range: list[TimeOnlyRange],
        is_open_monitor: bool = False,
        options: MessageMonitorOptions | None = None,
    ):
        """加一天中多个时间段的消息监听，用户需要提供一个回调函数，当有消息时，会调用此回调函数
        参考<see cref="MessageContext"/>
        适用于一天内多次固定时间进行自动化操作的场景
        使用规则：
        1. 仅能监听“允许消息通知”的好友/群聊,所以需要监听的好友/群聊不要设置为“消息免打扰”;
        2. 如果要避免太多消息影响，请将不监听的好友/群设置为“消息免打扰”，以提高监听性能;
        3. 此方法可以多次添加，第一次添加时会启动消息监听，第二次（类似的第三次等）添加效果等同<see cref="AddListeningFriend"/>方法
        4. 为了减少会话窗口的滚动，建议不要添加太多消息“置顶”，以提高监听效率.</para>
        执行逻辑:
        1. 启动消息监听器时做做一次全量扫描，即：SDK会将会话列表整个循环一编，会自动点击并回调用户设定的方法，以防止遗漏消息
        2. 以后的监听过程会增量监听，以提高效率.


        Args:
            nick_names (list[str]): 好友昵称,可以是一个，也可以是多个好友/群聊
            callback (Callable[[MessageContext,WeChatClient],Awaitable[None]]): 回调函数,由用户提供,参数：消息上下文<see cref="MessageContext"/>
            range (list[TimeOnlyRange]): 一天中的多个时间段,如果设定多个时间段，监听器在这些时间段内开始/结束监听,时间段类请参考:<see cref="TimeOnlyRange"/>,另注意：可以跨天，如设置为:23:00 ~ 02:00,则表示当天23:00至明天02:00
            is_open_monitor (bool, optional): 是否开启开放式监听，默认不开放(值为false）,如果开启开放式监听，前面的nickNames可以为空，所谓的开放式监听的含义是：无须固定好友/群监听，只要此好友/群没有设置“消息免打挠”就可以监听. Defaults to False.
            options (MessageMonitorOptions | None, optional): 监听选项，具体请参见<see cref="MessageMonitorOptions"/>. Defaults to None.
        """
        request_id = uuid.uuid4().hex
        request_package = MessagePackage(
            request_id=request_id,
            func_Name="AddMessageListener_With_Range",
            options=json.dumps(
                {
                    "nick_names": json.dumps(nick_names),
                    "options": MessageMonitorOptions.model_dump_json(options)
                    if options is not None
                    else None,
                    "is_open_monitor": str(is_open_monitor),
                    "range": json.dumps(range),
                }
            ),
            from_wechat=self.client.from_wechat,
        )
        await self.client.socket.send_message_listener(
            request_package, request_id, nick_names, callback, self.client
        )

    async def open_moments(self) -> bool:
        """打开朋友圈,如果未打开，则打开朋友圈，如果已经打开了，则窗口提前到顶端

        Returns:
            bool: 是否打开
        """
        result = await self.client._do_remote_function("OpenMoments", "")
        return result.lower() == "true"

    async def close_moments(self) -> None:
        """关闭朋友圈"""
        await self.client._do_remote_function("CloseMoments", "")

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
        result = await self.client._do_remote_function(
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
        result = await self.client._do_remote_function("RemoveMoments", content)
        return result.lower() == "true"

    async def pause_message_listener(self) -> None:
        """
        暂停消息监听
        """
        await self.client._do_remote_action("PauseMessageListener", "")

    async def resume_message_listener(self) -> None:
        """
        恢复消息监听
        """
        await self.client._do_remote_action("ResumeMessageListener", "")

    async def add_listening_friend(self, who: str) -> None:
        """监听过程中添加好友

        Args:
            who (str): 好友名称
        """
        await self.client._do_remote_action("AddListeningFriend", who)

    async def remove_listening_friend(self, who: str) -> None:
        """监听过程中移除被监听中的好友/群聊

        Args:
            who (str): 好友/群聊名称
        """
        await self.client._do_remote_action("RemoveListeningFriend", who)

    async def pause_new_friend_listener(self):
        """暂停好友申请监听"""
        await self.client._do_remote_action("PauseNewFriendListener", "")

    async def resume_new_friend_listener(self):
        """恢复好友申请监听"""
        await self.client._do_remote_action("ResumeNewFriendListener", "")
