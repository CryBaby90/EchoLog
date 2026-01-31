// Copyright (c) 2026 HuYa
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.


using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[assembly: InternalsVisibleTo("EchoLog.Tests")]

namespace EchoLog
{
    /// <summary>日志配置（ScriptableObject）</summary>
    [CreateAssetMenu(menuName = "AbyssTavern/Log Config")]
    public partial class LogConfig : ScriptableObject
    {
        [Header("全局设置")]
        [Tooltip("最低日志级别")]
        public ELogLevel MinLogLevel = ELogLevel.Info;

        [Tooltip("启用异步写入")]
        public bool EnableAsync = true;

        [Tooltip("异步队列大小")]
        public int QueueSize = 1000;

        [Header("Unity Console")]
        [Tooltip("启用 Unity Console 输出")]
        public bool EnableUnityConsole = true;

        [Tooltip("启用堆栈跟踪")]
        public bool EnableStackTrace = true;

        [Header("文件输出")]
        [Tooltip("启用文件输出")]
        public bool EnableFileOutput = false;

        [Tooltip("日志目录")]
        public string LogDirectory = "Logs/GameLogs";

        [Tooltip("单个文件最大大小（MB）")]
        public int MaxFileSizeMB = 10;

        [Tooltip("最大保留文件数")]
        public int MaxFiles = 5;

        [Tooltip("启用日志压缩")]
        public bool EnableCompression = true;

        [Tooltip("错误和致命错误级别是否在文件中输出堆栈跟踪")]
        public bool EnableFileStackTrace = false;

        [Header("关键日志")]
        [Tooltip("关键日志分类列表（无视日志级别强制输出）")]
        public string[] CriticalCategories = new string[0];

        [Header("性能日志")]
        [Tooltip("启用性能监控")]
        public bool EnablePerformanceLogging = false;

        [Tooltip("性能日志记录间隔（秒）")]
        public float PerformanceLogInterval = 1f;

        [Header("敏感信息过滤")]
        [Tooltip("启用敏感信息过滤")]
        public bool EnableSensitiveFilter = true;

        [Tooltip("敏感关键词")]
        public string[] SensitiveKeywords = { "password", "token", "key" };

        [Header("发布模式覆盖")]
        [Tooltip("发布模式最低日志级别")]
        public ELogLevel ReleaseMinLevel = ELogLevel.Error;

        #region 性能优化缓存

        /// <summary>缓存的正则表达式（运行时初始化）</summary>
        [NonSerialized]
        private Dictionary<string, Regex> cachedRegexPatterns;

        /// <summary>关键分类集合缓存（运行时初始化）</summary>
        [NonSerialized]
        private HashSet<string> criticalCategorySet;

        /// <summary>获取缓存的正则表达式模式</summary>
        internal Dictionary<string, Regex> CachedRegexPatterns
        {
            get
            {
                if (cachedRegexPatterns == null)
                {
                    InitializeRegexCache();
                }
                return cachedRegexPatterns;
            }
        }

        /// <summary>获取关键分类集合</summary>
        internal HashSet<string> CriticalCategorySet
        {
            get
            {
                if (criticalCategorySet == null)
                {
                    InitializeCriticalCategorySet();
                }
                return criticalCategorySet;
            }
        }

        /// <summary>初始化正则表达式缓存</summary>
        private void InitializeRegexCache()
        {
            cachedRegexPatterns = new Dictionary<string, Regex>(StringComparer.Ordinal);

            if (SensitiveKeywords == null || SensitiveKeywords.Length == 0)
                return;

            foreach (var keyword in SensitiveKeywords)
            {
                if (string.IsNullOrEmpty(keyword))
                    continue;

                string pattern = $"{keyword}=\\S+";
                cachedRegexPatterns[keyword] = new Regex(
                    pattern,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                );
            }
        }

        /// <summary>初始化关键分类集合</summary>
        private void InitializeCriticalCategorySet()
        {
            criticalCategorySet = new HashSet<string>(StringComparer.Ordinal);

            if (CriticalCategories == null || CriticalCategories.Length == 0)
                return;

            foreach (var category in CriticalCategories)
            {
                if (!string.IsNullOrEmpty(category))
                {
                    criticalCategorySet.Add(category);
                }
            }
        }

        /// <summary>清理缓存</summary>
        internal void ClearCache()
        {
            if (cachedRegexPatterns != null)
            {
                cachedRegexPatterns.Clear();
                cachedRegexPatterns = null;
            }

            criticalCategorySet = null;
        }

        #endregion
    }
}
