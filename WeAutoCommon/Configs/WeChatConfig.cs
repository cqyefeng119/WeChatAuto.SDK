using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using OneOf.Types;
using System.ComponentModel;

namespace WeAutoCommon.Configs
{
    /// <summary>
    /// 微信自动化参数配置类
    /// </summary>
    public class WeChatConfig
    {
        /// <summary>
        /// 下载文件/图片默认保存路径
        /// </summary>
        public string DefaultSavePath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "wxauto_download");

        /// <summary>
        /// 监听消息时间间隔，单位秒
        /// </summary>
        public int ListenInterval { get; set; } = 5;
        /// <summary>
        /// 朋友圈监听时间间隔，单位秒
        /// </summary>
        public int MomentsListenInterval { get; set; } = 10;

        /// <summary>
        /// 新好友监听时间间隔，单位秒
        /// </summary>
        public int NewUserListenerInterval { get; set; } = 5;

        /// <summary>
        /// 监听子窗口时间间隔，单位秒
        /// </summary>
        public int MonitorSubWinInterval { get; set; } = 5;

        /// <summary>
        /// 监听会话列表切换时间间隔，单位：秒
        /// </summary>
        public int ConversationChangeListenerInterval { get; set; } = 5;
        /// <summary>
        /// 消息监听间隔时间，单位为秒
        /// </summary>
        public int MonitorMessageInterval { get; set; } = 5;
        /// <summary>
        /// 消息监听时往下滚动的次数，如果监听列表多，建议设置成：10-30
        /// 如果监听列表少，建议设置成:5~10，以提高效率 
        /// </summary>
        public int MonitorMessageMaxDownInterval { get; set; } = 8;
        /// <summary>
        /// 好友申请监听间隔时间，单位为秒
        /// </summary>
        public int MonitorNewFriendRequestInterval { get; set; } = 20;
        /// <summary>
        /// 监听群聊系统消息的间隔时间，单位为秒
        /// </summary>
        public int MonitorGroupInterval {get;set;} = 10;
        /// <summary>
        /// 会话列表鼠标滚动行数.
        /// </summary>
        public int ConversationInterval { get; set; } = 5;

        /// <summary>
        /// 当滚动删除朋友圈内容时，最大滚动次数,如果朋友圈内容多，请将此值设置大一些。
        /// </summary>
        public int MonentsScrollMaxSetp { get; set; } = 30;
        /// <summary>
        /// 是否启用OCR,但是一些功能由于腾迅的限制必须要启用OCR
        /// 如果启用OCR,则必须保证OCR模型在models目录下，并且需要配置好模型文件名称
        /// </summary>
        public bool EnableOCR { get; set; } = false;
        /// <summary>
        /// ocr-det模型路径
        /// </summary>
        public string OCRDetModelFilePath { get; set; } = "ch_PP-OCRv5_mobile_det.onnx";
        /// <summary>
        /// ocr-cls模型路径
        /// </summary>
        public string OCRClsModelFilePath { get; set; } = "ch_ppocr_mobile_v2.0_cls_infer.onnx";
        /// <summary>
        /// ocr-rec模型路径
        /// </summary>
        public string OCRRecModelFilePath { get; set; } = "ch_PP-OCRv5_rec_mobile_infer.onnx";
        /// <summary>
        /// OCR的字典文件路径
        /// </summary>
        public string OCRDictModelFilePath { get; set; } = "ppocrv5_dict.txt";
        /// <summary>
        /// 是否启用调试模式
        /// </summary>
        public bool DebugMode { get; set; } = false;
        /// <summary>
        /// 出错后捕获UI保存路径
        /// 默认保存到当前目录下的Capture文件夹,可以修改为其他路径
        /// </summary>
        public string CaptureUIPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Captures");
        /// <summary>
        /// 是否启用视频录制
        /// </summary>
        public bool EnableRecordVideo { get; set; } = false;
        /// <summary>
        /// 视频录制保存路径
        /// 默认保存到当前目录下的Video文件夹,可以修改为其他路径
        /// </summary>
        public string TargetVideoPath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Videos");
        /// <summary>
        /// 是否启用鼠标键盘模拟器
        /// 启用后，键盘鼠标操作会通过模拟器进行操作，而不是通过windows automation进行操作。
        /// 注意：需要购买键鼠模拟器，并在此处启用。
        /// </summary>
        public bool EnableMouseKeyboardSimulator { get; set; } = false;
        /// <summary>
        /// 键鼠模拟器设备VID
        /// </summary>
        public int KMDeviceVID { get; set; } = 0x1701;
        /// <summary>
        /// 键鼠模拟器设备PID
        /// </summary>
        public int KMDevicePID { get; set; } = 0x2612;
        /// <summary>
        /// 键鼠模拟器校验数据
        /// </summary>
        public string KMVerifyUserData { get; set; } = "4F6A21981BE675822DEE7B9BC39F3791";
        /// <summary>
        /// 点击偏移量,单位像素
        /// 为了避免每次点击都点击到同一个位置，可以设置一个偏移量，实际点击位置为点击位置减去偏移量的一个随机值
        /// </summary>
        public int KMOffsetOfClick { get; set; } = 5;
        /// <summary>
        /// 配置键鼠模拟器输出字符串编码类型
        /// 0: 输出ANSI字符串
        /// 1：输出Unicode字符串
        /// 2: 输出ANSI字符串，速度更快，但受输入法影响更大
        /// 3: 输出UNICODE字符串，速度更快，但受输入法影响更磊
        /// 4: 使用剪贴板粘贴输出字符串。优点是输出字符多时速度更快且不受输入法影响
        /// </summary>
        public int KMOutputStringType { get; set; } = 0;
        /// <summary>
        /// 配置键鼠模拟器鼠标移动模式
        /// </summary>
        public int KMMouseMoveMode { get; set; } = 0;
        /// <summary>
        /// 进程DPI感知值,如果使用库的应用已经设置DPI感知，此参数无效。
        /// 0: 不设置,进程对DPI完全不知晓，按逻辑像素绘制，可能会出现点击不准确的情况。
        /// 1: PROCESS_SYSTEM_DPI_AWARE 默认值,进程只根据主显示器DPI绘制，DPI感知生效。
        /// 2: PROCESS_PER_MONITOR_DPI_AWARE，进程根据每个显示器DPI绘制,DPI感知生效。
        /// </summary>
        public int ProcessDpiAwareness { get; set; } = 1;
        /// <summary>
        /// 是否一开始就初始化通讯录所有好友
        /// 如果以wxid为业务核心，强烈开启此选项.
        /// </summary>
        public bool InitAdressBook { get; set; } = false;
        /// <summary>
        /// 历史消息X偏移距离
        /// </summary>
        public int HistoryMessageOffset_X = 95;
        /// <summary>
        /// 历史消息Y偏移距离
        /// </summary>
        public int HistoryMessageOffset_Y = 50;
        /// <summary>
        /// 历史消息滚动时重试次数
        /// </summary>
        public int HistoryRetryNumber = 6;
        /// <summary>
        /// 头像按钮距离微信按钮的Y轴偏移量
        /// </summary>
        public int AvatorToWeixinButtonOffsetY = 50;
        /// <summary>
        /// 用于消息监听中，返回给回调函数历史消息最大记录数，因为事实上无须读完整个历史消息的。
        /// </summary>
        public int MaxHistoryMessageFetchNumber { get; set; } = 20;
        /// <summary>
        /// 为了预防全量搜索历史消息设置的阈值
        /// </summary>
        public int MaxHistoryFallbackthresholdNumber {get;set;} = 50;
        /// <summary>
        /// 消息监听中，首次运行取历史消息的最大数量.
        /// </summary>
        public int MessageFirstFetchNumber { get; set; } = 10;
        /// <summary>
        /// 消息监听中，为了消息稳定下来重试次数
        /// </summary>
        public int MessageStabilityRetryNumber { get; set; } = 5;
    }

    public static class Language
    {
        public static string CurrentLanguage { get; set; } = Language.Chinese;
        public const string Chinese = "Cn";
        public const string ChineseTraditional = "CnT";
        public const string English = "En";
    }
}