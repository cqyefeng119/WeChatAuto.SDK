using System;

namespace WeChatAuto.Models
{
    public class TimeOnlyRange
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        public TimeOnly StarTime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public TimeOnly EndTime { get; set; }

        public override string ToString()
        {
            return $"{StarTime.ToString("HH:mm:ss")} - {EndTime.ToString("HH:mm:ss")}";
        }
    }

}