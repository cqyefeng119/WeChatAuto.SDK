using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using WeAutoCommon.Simulator;
using RapidOCRLib;
using Emgu.CV.OCR;
using WeAutoCommon.Configs;
using System.Threading.Tasks;

namespace WeChatAuto.Services
{
    public class OCRService
    {
        private OcrLite _OcrEngin = null;
        public OcrLite OcrEngin
        {
            get
            {
                if (_OcrEngin == null)
                    throw new Exception("请先初始化OCR,请按下面步骤检查：1. 模型文件是否正确下载. 2. 是否在WecChatConfig中配置好模型位置.3.是否初始化模型引擎.");
                return _OcrEngin;
            }
        }

        public void InitOCREngin(WeChatConfig config)
        {
            if (!config.EnableOCR)
                return;
            //检查配置是否正确
            _OcrEngin = _CheckModeFiles(config);
            //初始化引擎
            _ = Task.Run(async () =>
            {
                await _OcrEngin.InitModels();
            });
        }

        private OcrLite _CheckModeFiles(WeChatConfig config)
        {
            var detFileName = Path.IsPathRooted(config.OCRDetModeFilePath) ? config.OCRDetModeFilePath : Path.Combine(AppContext.BaseDirectory, "models", config.OCRDetModeFilePath);
            if (!File.Exists(detFileName))
                throw new Exception("错误：Det模型文件不存在！");
            var clsFileName = Path.IsPathRooted(config.OCRClsModeFilePath) ? config.OCRClsModeFilePath : Path.Combine(AppContext.BaseDirectory, "models", config.OCRClsModeFilePath);
            if (!File.Exists(clsFileName))
                throw new Exception("错误：cls模型文件不存在!");
            var recFileName = Path.IsPathRooted(config.OCRRecModeFilePath) ? config.OCRRecModeFilePath : Path.Combine(AppContext.BaseDirectory, "models", config.OCRRecModeFilePath);
            if (!File.Exists(recFileName))
                throw new Exception("错误：rec模型文件不存在!");
            var dictFileName = Path.IsPathRooted(config.OCRDictModeFilePath) ? config.OCRDictModeFilePath : Path.Combine(AppContext.BaseDirectory, "models", config.OCRDictModeFilePath);
            if (!File.Exists(dictFileName))
                throw new Exception("错误：dict字典文件不存在！");
            OcrLite ocrLite = new OcrLite()
            {
                DetPath = detFileName,
                ClsPath = clsFileName,
                RecPath = recFileName,
                KeyDicPath = dictFileName,
            };
            return ocrLite;
        }
    }
}