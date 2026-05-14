using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json;
using OneOf;
using WeAutoCommon.Enums;
using MessagePack;

namespace WeAutoCommon.Models
{
    /// <summary>
    /// 个人信息
    /// </summary>
    [MessagePackObject]
    public class FriendInfo
    {
        /// <summary>
        /// 昵称
        /// </summary>
        [Key(0)]
        public string NickName { get; set; } = "";
        /// <summary>
        /// 备注名
        /// </summary>
        [Key(1)]
        public string MemoName { get; set; } = "";
        /// <summary>
        /// 地区,建议仅供参考
        /// </summary>
        [Key(2)]
        public string Area { get; set; } = "";
        /// <summary>
        /// 标签
        /// </summary>
        [Key(3)]
        public List<string> Lable { get; set; } = new List<string>();
        /// <summary>
        /// 共同群数量
        /// </summary>
        [Key(4)]
        public string SameGroupNumber { get; set; } = "0个";
        /// <summary>
        /// 个性签名
        /// </summary>
        [Key(5)]
        public string Signature { get; set; } = "";
        /// <summary>
        /// 来源
        /// </summary>
        [Key(6)]
        public string Source { get; set; } = "";

        /// <summary>
        /// 微信ID
        /// </summary>
        [Key(7)]
        public string WxId { get; set; } = "";
        /// <summary>
        /// 头像路径
        /// </summary>
        [Key(8)]
        public string AvatarPath { get; set; } = "";
        /// <summary>
        /// 头像Image
        /// <code>
        /// //调用示例
        /// var image = xxxx.AvatarImage;
        /// image.Save(xxxxx)
        /// </code>
        /// </summary>
        [IgnoreMember]
        public Image AvatarImage { get; set; } = null;
        /// <summary>
        /// 查询结果，三种查询结果：已是好友、未查询到或不支持手机号查询、能查询到，但不是好友.
        /// 具体结果请参见:<seealso cref="FriendSearchResultEnums"/>
        /// </summary>
        [Key(9)]
        public FriendSearchResultEnums FriendSearchResult { get; set; }
        /// <summary>
        /// 好友类型，具体参见<see cref="ChatType"/>
        /// </summary>
        [Key(10)]
        public ChatType ChatType { get; set; }
        /// <summary>
        /// 添加好友时间，>4.x版本才有此属性
        /// </summary>
        [Key(11)]
        public string AddDateTime {get;set;}
        /// <summary>
        /// 微信中使用的名称
        /// 本质是: 如果昵称不等于备注名，则以备注名不为主.
        /// </summary>
        [IgnoreMember]
        public string Name
        {
            get
            {
                if (!NickName.Equals(MemoName))
                {
                    return MemoName;
                }
                return NickName;
            }
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}