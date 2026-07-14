# WECHATAUTO.SDK - 面向AI的现代化微信RPA自动化框架

[![.NET](https://img.shields.io/badge/.NET-6.0%2B-blue)](https://dotnet.microsoft.com/)
[![Python](https://img.shields.io/badge/Python-3.13%2B-blue?logo=python&logoColor=white)](https://www.python.org/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

WeChatAuto.SDK 是一款面向 AI 的微信RPA自动化 SDK，基于 .NET 与 UI 自动化技术开发。它专为集成人工智能（如 LLM 上下文交互）场景设计。SDK 提供丰富直观的 API，支持 .NET 现代化特性，比如依赖注入，也提供官方Python SDK，让你轻松在现有系统中集成进微信自动化流程。

## ✨ 特性

- 💬 **完善的消息与社交能力** - 支持发送文字、表情、文件、@提醒、转发消息、群管理、朋友圈操作与通讯录管理，并提供消息监听等常用能力；所有重要接口均覆盖单元测试。
  
- 🛡️ **降低风控风险** - 支持纯软件自动化与结合硬件键鼠模拟器的方案，可根据业务需求与安全等级选择更稳健的执行方式。

- 🚀 **多实例与分布式支持** - 可同时管理多个微信客户端，既可作为本机 SDK 使用，也可通过 WebSocket 长连接实现一台应用控制多台机器的多个微信或多应用控制同一个微信。

- 🔧 **微信版本兼容** - 兼容旧版（如 3.9.12.xx）与新版（如 4.1.11.xx）微信，并持续更新以适配新版客户端。

- 🧩 **多语言支持** - 原生使用 .NET 开发，同时提供官方 Python SDK，方便不同语言生态集成。

- 🔌 **易于集成与高可靠性** - 支持依赖注入，轻量SDK，易嵌入现有系统；采用工业级线程管理与稳定性设计，降低 UI 自动化卡死风险。

- 🤖 **AI 友好** - 原生支持 LLM 上下文对接并内置 MCP Server，便于接入主流智能体与平台，助力智能应用微信自动化/RPA闭环与扩展。

**👉 如需体验**，请点击链接进入: [WeChatAuto.SDK体验指引](./MD/Experience.md)

> 如果觉得有帮助，欢迎点赞、Star和Fork本项目，以免失联，感谢支持！

## 📋 系统要求

- Windows 操作系统
- .NET 6.0 或更高版本 (注：python SDK 不依赖 .NET)

## 🎉 重要说明!!


**WeChatAuto.SDK** 提供两个版本的SDK:

---

#### 🧱 微信 3.9.12.xx

- 纯UI Tree自动化解决方案;
- 仅支持 .NET 平台;
- 源码 100% 开源;
- 文档 100% 开源;

**使用指南**

👉 完整的文档请参考: [WeChatAuto.SDK 3.9.12.xx文档](https://scottfly189.github.io/WeChatAuto.SDK/)

👉 安装不上微信客户端3.9.12.xx？ 请参考: [如何安装3.9.12.xx等微信低版本客户端](https://github.com/scottfly189/WeChatAuto.SDK/issues/2)

👉 SDK源码与DEMO项目演示，请参考: [3.9.12.xx源码及DEMO项目](https://github.com/scottfly189/WeChatAuto.SDK/tree/master/WeChatAuto3_9_12_xx)

---

#### ⚡ 微信 4.1.xx（最新版本）

- 基于微信 4.1.xx 最新微信客户端的持续演进版本，支持 .NET 与 Python 双平台;
- 微信4.1.xx版本为**UI Tree + OCR** 混合自动化解决方案，OCR使用的是本地onnx-runtime模型[RapidOCRCSharp，有需要的也请给RapidOCRSharp点一个赞👍](https://github.com/RapidAI/RapidOCRCSharp)  ;
- 开源情况：核心代码 - 100%开源，文档、视频 - 100%开源,业务代码 - 80%开源 (20%的api属于vip独有);

**使用指南**

👉 完整的文档请参考: [WeChatAuto.SDK 4.x文档](https://scottfly189.github.io/WeChatAuto.SDK/)

👉 自动化不了微信？ 请参考: [最新版微信自动化不了](https://github.com/scottfly189/WeChatAuto.SDK/issues/2)

👉 如需体验最新版的微信自动化RAP，请点击链接进入: [WeChatAuto.SDK体验指引](./MD/Experience.md)


## ⚠️ 注意事项

1. **风控风险**：频繁操作可能触发微信风控机制，建议：
   - 使用键鼠模拟器降低风险
   - 控制操作频率
   - 避免短时间内大量操作

2. **微信版本**：做微信RPA一定要注意微信的版本，请确认微信版本正确的对应了WeChatAuto.SDK的版本;


## 🎈 关于键鼠模拟器

键鼠模拟器是一类专门的硬件设备，能够模拟物理键盘和鼠标的真实输入。相较于直接调用 PostMessage、SetInput 等 API 进行注入，这类传统软件方式往往会留下可被识别的痕迹，极易被微信等应用检测为自动化行为并引发风控。而键鼠模拟器通过硬件底层发送信号，模拟出的输入和人手操作无异，从而高度还原人类使用方式，在风控安全性和隐蔽性方面具备天然优势。

实际测试表明，在微信某些高敏感操作场景（比如群聊内加好友）下，借助键鼠模拟器能有效降低被风控的概率。需要注意的是，即便是手动操作，部分极端高风险情况下也有可能触发风控。因此，强烈建议在高敏感度和易风控场景优先考虑且规范使用键鼠模拟器，以获得更稳定和安全的自动化体验。

本 SDK 同时支持纯软件自动化以及结合硬件键鼠模拟器的自动化操作，满足不同业务需求和安全等级场景下的使用选择。

关于键鼠模拟器更深度的了解，请参见：[键鼠模拟器](https://github.com/scottfly189/SKSimulator)


## 😊 关于VIP

由于时间和精力有限，为了更好地投入研发和持续改进产品，本人目前仅为**已购买VIP服务的客户**提供优先和深入的技术支持。这样做，是希望通过区分服务对象，专注为VIP客户带来更高品质、更有保障的体验。当然，广大普通用户依然欢迎通过 Issue 反馈和交流，只是服务响应的优先级和深度会有所不同。

**🎉 VIP 客户可享受以下专属服务保障：**
- 💡 **BUG 优先响应**：出现 Bug 或有新的 Enhancement ，第一时间响应、定位和解决，保障 VIP 项目的稳定运行;
- 👥 **VIP 技术交流群**：专属 VIP 交流群，优先、及时解答问题，实时高效支持;
- 🚀 **专属 VIP 私有仓库**：VIP 客户将获专属私有仓库，会不定期提供丰富的应用层扩展与独享内容;
- 🚀 **一对一的专属vip服务**: 这是你加入 VIP 的核心理由,微信自动化能力由 WeChatAuto.SDK 提供深度支持，业务系统由你自由扩展，实现技术与业务的高效分工;

**😊 非 VIP 客户：**  

同样欢迎非VIP通过 Issue 提问或反馈问题;

非 VIP 会员私下找我，我会在时间允许情况下进行处理，但响应和解决可能会有延迟，敬请谅解。

如需升级成为 VIP，或了解 VIP 具体权益和支持方案，👉[请与我联系](https://github.com/scottfly189/scottfly189/blob/main/vip.md)。感谢理解与支持，让我有更多精力专注于技术创新与完善！

---

## 🎁 WechatAuto.SDK入门视频

下面的视频以wechatauto.sdk4.x微信最新版来讲解

... 正在完成中


---

## 📝 许可证

本项目采用 MIT 许可证。详见 [LICENSE](LICENSE) 文件。

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 🙏 致谢

在这里感谢那些还在为自由和正义而奋斗的人们🎉🎉

---


## ⚒️ 免责声明

本 SDK 仅供学习和研究使用，请遵守微信使用条款，不得用于任何违法违规用途。使用本 SDK 产生的任何后果由使用者自行承担。

