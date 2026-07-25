namespace WeChatAuto.Options
{
    public class VoiceOptions
    {
        /// <summary>
        /// 系统音色
        /// </summary>
        public string Voice { get; set; } = "Cherry";
        /// <summary>
        /// 语言类型,默认"Chinese",可选值有: 'Auto', 'Chinese', 'English', 'German', 'Italian', 'Portuguese', 'Spanish', 'Japanese', 'Korean', 'French' or 'Russian'
        /// </summary>
        public string LanguageType { get; set; } = "Chinese";
        public bool OptimizeInstructions { get; set; } = true;
        public bool IsStream { get; set; } = false;
    }
}