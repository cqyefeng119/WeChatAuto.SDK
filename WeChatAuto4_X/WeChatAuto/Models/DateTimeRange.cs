using System.Collections.Generic;

namespace WeChatAuto.Models
{
    internal class DateTimeRange
    {
        public bool IsCheckDate { get; set; } = false;

        public List<TimeOnlyRange> TimeList { get; set; }
    }

}