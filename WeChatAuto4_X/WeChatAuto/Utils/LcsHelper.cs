
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.WindowsAPI;
using WeAutoCommon.Configs;
using WeAutoCommon.Models;
using WeAutoCommon.Utils;
using WeChatAuto.Components;
using WeChatAuto.Services;

namespace WeChatAuto.Utils
{
    public static class LcsHelper
    {
        /// <summary>
        /// 两序列求出LCS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a">旧序列</param>
        /// <param name="b">新序列</param>
        /// <param name="comparer">比较器</param>
        /// <returns></returns>
        public static List<T> Lcs<T>(
            IReadOnlyList<T> a,
            IReadOnlyList<T> b,
            IEqualityComparer<T> comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            int n = a.Count;
            int m = b.Count;

            // dp[i,j] = a[0..i) 和 b[0..j) 的 LCS 长度
            var dp = new int[n + 1, m + 1];

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    if (comparer.Equals(a[i - 1], b[j - 1]))
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        dp[i, j] = Math.Max(
                            dp[i - 1, j],
                            dp[i, j - 1]);
                    }
                }
            }

            // 从右下角开始回溯
            var result = new List<T>();

            int x = n;
            int y = m;

            while (x > 0 && y > 0)
            {
                if (comparer.Equals(a[x - 1], b[y - 1]))
                {
                    result.Add(a[x - 1]);

                    x--;
                    y--;
                }
                else if (dp[x - 1, y] >= dp[x, y - 1])
                {
                    x--;
                }
                else
                {
                    y--;
                }
            }

            result.Reverse();

            return result;
        }

        /// <summary>
        /// 两序列求diff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="oldList">求列表</param>
        /// <param name="newList">新列表</param>
        /// <param name="comparer">比较器</param>
        /// <returns></returns>
        public static List<DiffItem<T>> Diff<T>(
            IReadOnlyList<T> oldList,
            IReadOnlyList<T> newList,
            IEqualityComparer<T> comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            var lcs = Lcs(oldList, newList, comparer);

            var result = new List<DiffItem<T>>();

            int oldIndex = 0;
            int newIndex = 0;
            int lcsIndex = 0;

            while (lcsIndex < lcs.Count)
            {
                var common = lcs[lcsIndex];

                // Old 中到 common 之前的都是 Delete
                while (oldIndex < oldList.Count &&
                       !comparer.Equals(oldList[oldIndex], common))
                {
                    result.Add(new DiffItem<T>(
                        DiffType.Delete,
                        oldList[oldIndex]));

                    oldIndex++;
                }

                // New 中到 common 之前的都是 Insert
                while (newIndex < newList.Count &&
                       !comparer.Equals(newList[newIndex], common))
                {
                    result.Add(new DiffItem<T>(
                        DiffType.Insert,
                        newList[newIndex]));

                    newIndex++;
                }

                // common 本身
                result.Add(new DiffItem<T>(
                    DiffType.Equal,
                    common));

                oldIndex++;
                newIndex++;
                lcsIndex++;
            }

            // Old 剩余
            while (oldIndex < oldList.Count)
            {
                result.Add(new DiffItem<T>(
                    DiffType.Delete,
                    oldList[oldIndex]));

                oldIndex++;
            }

            // New 剩余
            while (newIndex < newList.Count)
            {
                result.Add(new DiffItem<T>(
                    DiffType.Insert,
                    newList[newIndex]));

                newIndex++;
            }

            return result;
        }
    }

    public enum DiffType
    {
        /// <summary>
        /// 相同
        /// </summary>
        Equal,
        /// <summary>
        /// 新增
        /// </summary>
        Insert,
        /// <summary>
        /// 删除
        /// </summary>
        Delete
    }

    public sealed record DiffItem<T>(
    DiffType Type,
    T Value);
}