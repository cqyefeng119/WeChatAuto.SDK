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
using RapidOCRLib.Models;
using Emgu.CV;
using Emgu.CV.Structure;
using WeChatAuto.Utils;
using System.Linq;
using FlaUI.Core.Input;
using WeAutoCommon.Utils;

namespace WeChatAuto.Services
{
    public class OCRService
    {
        private OcrLite _OcrEngin = null;
        private Semaphore semaphore = new Semaphore(1, 1);
        public OcrLite OcrEngin
        {
            get
            {
                if (_OcrEngin == null)
                    throw new Exception("请先初始化OCR,请按下面步骤检查：1. 模型文件是否正确下载. 2. 是否在WecChatConfig中配置好模型位置.3.是否初始化模型引擎.");
                return _OcrEngin;
            }
        }

        /// <summary>
        /// 通过自动化元素获取OpenCV的Mat对象
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public Mat GetMatFromElement(AutomationElement element)
        {
            element.Stability();
            var bmp = element.Capture();
            using Bitmap clone = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(clone))
            {
                g.DrawImage(bmp, 0, 0);
            }
            Mat mat = clone.ToMat();
            return mat;
        }
        /// <summary>
        /// OCR识别，找出特定文字的中心坐标,此坐标针对屏幕
        /// 仅支持竖向方向的截取
        /// </summary>
        /// <param name="rootElement">基础自动化元素</param>
        /// <param name="rioRadio">坚向方向的截取比例，如0.5,只取原因上半部分</param>
        /// <param name="findStr">找寻的字符串</param>
        /// <param name="isTest">是否测试</param>
        /// <param name="Index">索引，如果返回字符串有多个,以第几个为准</param>
        /// <param name="retryNumber">如果没有找到，重试次数</param>
        /// <returns></returns>
        public Point OCRVerticalDetect(AutomationElement rootElement, float rioRadio, string findStr, bool isTest = false, int Index = 0, int retryNumber = 2)
        {
            using Mat mat1 = GetMatFromElement(rootElement);
            Rectangle rio = new Rectangle(0, 0, mat1.Width, (int)(mat1.Height * rioRadio));
            using Mat mat2 = new Mat(mat1, rio);
            using Bitmap bitmap = mat2.ToBitmap();
            var index = 0;
            while (index < retryNumber)
            {
                OcrResult result = Detect(bitmap, 0, mat2.Width > mat2.Height ? mat2.Width : mat2.Height, 0.5f, 0.5f, 1.6f, false, false);
                var blockTexts = result.TextBlocks.Where(x => !string.IsNullOrWhiteSpace(x.Text) && (x.Text.Contains(findStr) || findStr.Contains(x.Text)));
                if (blockTexts.Count() > 0)
                {
                    var count = 0;
                    foreach (var item in blockTexts)
                    {
                        if (count == index)
                        {
                            var pointList = item.BoxPoints;
                            var point = new Point(((pointList[2].X - pointList[0].X) / 2 + pointList[0].X), ((pointList[2].Y - pointList[0].Y) / 2) + pointList[0].Y);
                            if (isTest)
                            {
                                CvInvoke.Imshow("wechatauto.sdk", result.BoxImg);
                                CvInvoke.WaitKey();
                            }
                            return new Point(rootElement.BoundingRectangle.X + point.X, rootElement.BoundingRectangle.Y + point.Y);
                        }
                        count++;
                    }
                }
                else
                {
                    index++;
                }
            }
            return Point.Empty;
        }

        //屏步进行ocr识别
        public async Task<OcrResult> DetectAsync(Bitmap bitmap, int padding = 0, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
            float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            semaphore.WaitOne();
            try
            {
                return await OcrEngin.DetectAsync(bitmap, padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle);
            }
            finally
            {
                semaphore.Release();
            }
        }
        //同步进行ocr识别
        public OcrResult Detect(Bitmap bitmap, int padding = 0, int maxSideLen = 1024, float boxScoreThresh = 0.5f, float boxThresh = 0.3f,
            float unClipRatio = 1.6f, bool doAngle = true, bool mostAngle = false)
        {
            semaphore.WaitOne();
            try
            {
                return OcrEngin.Detect(bitmap, padding, maxSideLen, boxScoreThresh, boxThresh, unClipRatio, doAngle, mostAngle);
            }
            finally
            {
                semaphore.Release();
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
            var detFileName = Path.IsPathRooted(config.OCRDetModelFilePath) ? config.OCRDetModelFilePath : Path.Combine(AppContext.BaseDirectory, "models", config.OCRDetModelFilePath);
            if (!File.Exists(detFileName))
                throw new Exception("错误：Det模型文件不存在！");
            var clsFileName = Path.IsPathRooted(config.OCRClsModelFilePath) ? config.OCRClsModelFilePath : Path.Combine(AppContext.BaseDirectory, "models", config.OCRClsModelFilePath);
            if (!File.Exists(clsFileName))
                throw new Exception("错误：cls模型文件不存在!");
            var recFileName = Path.IsPathRooted(config.OCRRecModelFilePath) ? config.OCRRecModelFilePath : Path.Combine(AppContext.BaseDirectory, "models", config.OCRRecModelFilePath);
            if (!File.Exists(recFileName))
                throw new Exception("错误：rec模型文件不存在!");
            var dictFileName = Path.IsPathRooted(config.OCRDictModelFilePath) ? config.OCRDictModelFilePath : Path.Combine(AppContext.BaseDirectory, "models", config.OCRDictModelFilePath);
            if (!File.Exists(dictFileName))
                throw new Exception("错误：dict字典文件不存在！");
            OcrLite ocrLite = new OcrLite()
            {
                DetPath = detFileName,
                ClsPath = clsFileName,
                RecPath = recFileName,
                KeyDicPath = dictFileName,
                ThreadNum = (int)(Environment.ProcessorCount * 0.7),
            };
            return ocrLite;
        }
    }
}