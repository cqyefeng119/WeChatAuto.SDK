using System;
using System.Drawing;
using WeAutoCommon.Enums;

namespace WeAutoCommon.Extentions
{
    /// <summary>
    /// 获取一个安全的可用Point
    /// </summary>
    public static class RandomPointExtention
    {
        /// <summary>
        /// 获取一个安全的可用Point
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static Point SafeRandomPoint(this Rectangle rectangle)
        {
            var width = (int)(rectangle.Width * 0.33);  //取1/3的安全位置
            var height = (int)(rectangle.Height * 0.33);

            Random random = new Random((int)DateTime.Now.Ticks);
            var x = rectangle.Left + (int)(rectangle.Width / 2) + random.Next(width * -1, width);
            var y = rectangle.Top + (int)(rectangle.Height / 2) + random.Next(height * -1, height);
            return new Point(x, y);
        }

        /// <summary>
        /// 判断一个item是否在root的安全范围内
        /// </summary>
        /// <param name="item"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        public static bool IsClickSafe(this Rectangle item, Rectangle root)
        {
            return !item.IsEmpty && !root.IsEmpty && item.Y >= root.Y
                        && item.Y + item.Height <= root.Y + root.Height;
        }

        public static DirectionInfoInfo WillMoveDirection(this Rectangle item, Rectangle root)
        {
            if (item.IsEmpty || root.IsEmpty)
                return DirectionInfoInfo.None;

            if (item.Y < root.Y)
                return DirectionInfoInfo.WillUp;

            if (item.Y + item.Height > root.Y + root.Height)
                return DirectionInfoInfo.WillDown;

            return DirectionInfoInfo.None;
        }
    }
}