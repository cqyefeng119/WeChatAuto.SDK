import pytest
import logging

from wechat_auto_sdk.wechat_client import WeChatClient

logger = logging.getLogger(__name__)

@pytest.mark.asyncio
async def test_get_ower_info(client: WeChatClient):
    info = await client.get_ower_info()
    logger.info("user info=%s", info)
    assert info is not None
