# FAQ

## ❓ 运行WechatAuto.sdk的示例自动化不了微信客户端？
回答：请按下面的步骤检查：
- 是否将微信拖到任务栏
  如下图所示:
  ![xx](./status.png)
- 检查windows版本，win10与  win11可以运行;
- 如果是最新版本的微信4.1.xx,请检查是否有UI Tree,如果没有UI Tree,请参考 [我的最新版微信没有UI Tree,这个怎么办？](https://github.com/scottfly189/WeChatAuto.SDK/issues/3)
  > 查看UI Tree的方法：下载并安装 UI Tree查看工具，如: inspect.exe或者FlaUInspect等，[下载链接](https://github.com/scottfly189/WeChatAuto.SDK/tree/master/Tools)
- 如果经过上面的步骤还不行，请联系作者，并将你的代码发给他看;

## ❓ WechatAuto.sdk可以直接运行吗？
回答：不行，这个是一个SDK开发包，需要整合在你的项目中使用，如果你要运行，可以:
1. 直接运行作者提供的示例项目，示例项目中已经集成了WechatAuto.sdk;
2. 你可以在你的项目中集成WechatAuto.sdk,然后运行你的项目;
3. 可以向作者索要示例代码;


## ❓ 请问WeChatAuto.SDK支持最新版的微信吗？
回答：支持最新版本的微信，并且会随着微信的版本更新而更新;

## ❓ WeChatAuto.SDK开启录屏不成功？
问题：我使用了下面的代码开启了录屏后，运行被卡在在那里不往下运行了

```
WeAutomation.Initialize(builder.Services, options =>
{
    //开启调试模式，调试模式会在获得焦点时边框高亮，生产环境建议关闭
    options.DebugMode = true;
    //开启录制视频功能，录制的视频会保存在项目的运行目录下的Videos文件夹中
    options.EnableRecordVideo = true;  
});
```

回答：WechatAuto.SDK使用ffmpeg进行录制，如果系统没有安装ffmpeg，第一次运行会自动从官网下载ffmpeg.exe,此文件比较大，并且需要梯子，如果没有开梯子或者梯子质量不好，会产生上述情况，建议按下面的步骤解决：

1. 开VPN,等第一次下载完;
2. 手动上传ffmpeg.exe文件至项目运行的ffmepg目录下:
- 开发环境为：bin\Debug\net9.0-windows\ffmpeg\ffmpeg.exe
- 运行环境为: 你的执行文件所在目录\ffmpeg\ffmpeg.exe

