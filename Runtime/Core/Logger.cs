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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace EchoLog
{
    /// <summary>主 Logger 静态类</summary>
    public static partial class Logger
    {
        private static LogConfig config;
        private static readonly List<ILogAppender> appenders = new List<ILogAppender>();
        private static readonly StringBuilder stringBuilder = new StringBuilder(512);
        private static readonly object lockObject = new object();
        private static readonly object emergencyLock = new object();

        /// <summary>紧急日志文件路径</summary>
        private const string EmergencyLogFile = "Logs/GameLogs/Emergency.log";

        /// <summary>紧急日志：直接写入文件（用于日志系统本身出错时）</summary>
        private static void LogEmergency(string level, string message)
        {
            try
            {
                string logDir = Path.Combine(Application.dataPath, "..", "Logs", "GameLogs");
                Directory.CreateDirectory(logDir);

                string logFile = Path.Combine(Application.dataPath, "..", EmergencyLogFile);

                // 使用缓存的 StringBuilder 避免字符串拼接的 GC 分配
                stringBuilder.Clear();
                stringBuilder.Append('[')
                    .Append(DateTime.Now.ToString("HH:mm:ss.fff"))
                    .Append("] [")
                    .Append(level)
                    .Append("] [EMERGENCY] ")
                    .Append(message);

                lock (emergencyLock)
                {
                    File.AppendAllText(logFile, stringBuilder.ToString() + Environment.NewLine);
                }
            }
            catch
            {
                // 紧急日志写入失败时，不再尝试写入，避免死循环
            }
        }

        /// <summary>获取当前配置</summary>
        public static LogConfig Config => config;

        /// <summary>初始化日志系统</summary>
        /// <param name="config">日志配置</param>
        public static void Initialize(LogConfig config)
        {
            if (config == null)
            {
                string msg = "LogConfig 不能为 null";
                UnityEngine.Debug.LogError(msg);
                LogEmergency("ERROR", msg);
                return;
            }

            Logger.config = config;

            // 从配置读取日志级别（编辑器和发布模式都生效）
            MinELogLevel = config.MinLogLevel;

            #if !UNITY_EDITOR
            // 发布模式可以使用更严格的日志级别
            if (config.ReleaseMinLevel > MinELogLevel)
            {
                MinELogLevel = config.ReleaseMinLevel;
            }
            #endif

            // 创建 Appender
            if (config.EnableUnityConsole)
            {
                appenders.Add(new UnityConsoleAppender());
            }

            if (config.EnableFileOutput)
            {
                appenders.Add(new FileAppender(config));
            }

            // 启动性能监控（如果启用）
            if (config.EnableAsync && config.EnablePerformanceLogging)
            {
                AsyncLogQueue.Instance.StartPerformanceMonitor(config.PerformanceLogInterval);
            }

            Critical("日志系统初始化完成", "System");
        }

        /// <summary>当前最低日志级别（可在运行时修改）</summary>
        public static ELogLevel MinELogLevel { get; set; } = ELogLevel.Info;

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>核心日志方法（线程安全）</summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="category">日志分类</param>
        public static void Log(ELogLevel level, string message, string category = null)
        {
            if (config == null)
            {
                // ERROR 和 FATAL 级别：写入紧急日志文件
                if (level >= ELogLevel.Error)
                {
                    string categoryStr = category ?? "General";
                    string fullMsg = $"[日志系统未初始化][{categoryStr}] {message}";
                    UnityEngine.Debug.LogError(fullMsg);
                    LogEmergency(level.ToString(), fullMsg);
                }
                return;
            }

            if (level < MinELogLevel && !IsCriticalLog(category))
                return;

            var entry = new LogEntry
            {
                Level = level,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Message = FilterSensitiveData(message),
                Category = category ?? "General",
                ThreadId = Thread.CurrentThread.ManagedThreadId
            };

            // 堆栈跟踪（Warning 及以上级别）
            if (config.EnableStackTrace && level >= ELogLevel.Warning)
            {
                entry.StackTrace = Environment.StackTrace;
            }

            // 异步或同步写入
            if (config.EnableAsync)
            {
                AsyncLogQueue.Instance.Enqueue(in entry);
            }
            else
            {
                WriteLog(in entry);
            }
        }

        /// <summary>延迟求值日志（避免 GC 分配）</summary>
        /// <param name="level">日志级别</param>
        /// <param name="formatter">消息格式化委托</param>
        /// <param name="category">日志分类</param>
        public static void LogLazy(ELogLevel level, Func<string> formatter, string category = null)
        {
            if (config == null || level < MinELogLevel)
                return;

            string message = formatter.Invoke();
            Log(level, message, category);
        }

        /// <summary>判断是否为关键日志</summary>
        /// <param name="category">日志分类</param>
        /// <returns>是否为关键日志</returns>
        private static bool IsCriticalLog(string category)
        {
            // 使用 HashSet 缓存，O(1) 复杂度
            if (string.IsNullOrEmpty(category))
                return false;

            return config?.CriticalCategorySet?.Contains(category) == true;
        }

        /// <summary>记录关键流程日志（无视日志级别）</summary>
        /// <param name="message">日志消息</param>
        /// <param name="category">日志分类</param>
        public static void Critical(string message, string category = "Critical")
        {
            Log(ELogLevel.Info, message, category);
        }

        /// <summary>记录调试信息</summary>
        public static void Debug(string message) => Log(ELogLevel.Debug, message);

        /// <summary>记录一般信息</summary>
        public static void Info(string message) => Log(ELogLevel.Info, message);

        /// <summary>记录警告</summary>
        public static void Warning(string message) => Log(ELogLevel.Warning, message);

        /// <summary>记录错误</summary>
        public static void Error(string message) => Log(ELogLevel.Error, message);

        /// <summary>记录致命错误</summary>
        public static void Fatal(string message) => Log(ELogLevel.Fatal, message);

        /// <summary>记录异常</summary>
        /// <param name="exception">异常对象</param>
        /// <param name="context">上下文信息</param>
        public static void LogException(Exception exception, string context = null)
        {
            if (exception == null)
                return;

            stringBuilder.Clear();
            stringBuilder.Append("[Exception] ");

            if (!string.IsNullOrEmpty(context))
            {
                stringBuilder.Append(context).Append(": ");
            }

            stringBuilder.Append(exception.Message);
            stringBuilder.AppendLine().Append(exception.StackTrace);

            Log(ELogLevel.Error, stringBuilder.ToString());
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>内部写入方法</summary>
        internal static void WriteLog(in LogEntry entry)
        {
            lock (lockObject)
            {
                foreach (var appender in appenders)
                {
                    try
                    {
                        appender.Append(in entry);
                    }
                    catch (Exception ex)
                    {
                        string msg = $"LogAppender 错误: {ex.Message}";
                        UnityEngine.Debug.LogError(msg);
                        LogEmergency("ERROR", msg);
                    }
                }
            }
        }

        /// <summary>敏感信息过滤</summary>
        /// <param name="message">原始消息</param>
        /// <returns>过滤后的消息</returns>
        private static string FilterSensitiveData(string message)
        {
            if (config == null || !config.EnableSensitiveFilter)
                return message;

            if (string.IsNullOrEmpty(message))
                return message;

            // 使用缓存的正则表达式，避免每次编译
            var cachedPatterns = config.CachedRegexPatterns;
            if (cachedPatterns == null || cachedPatterns.Count == 0)
                return message;

            string filtered = message;
            foreach (var regexPair in cachedPatterns)
            {
                var regex = regexPair.Value;
                if (regex.IsMatch(filtered))
                {
                    filtered = regex.Replace(filtered, $"{regexPair.Key}=***FILTERED***");
                }
            }
            return filtered;
        }

        /// <summary>关闭日志系统</summary>
        public static void Shutdown()
        {
            lock (lockObject)
            {
                foreach (var appender in appenders)
                {
                    try
                    {
                        appender.Flush();
                        appender.Dispose();
                    }
                    catch (Exception ex)
                    {
                        string msg = $"关闭 LogAppender 时出错: {ex.Message}";
                        UnityEngine.Debug.LogError(msg);
                        LogEmergency("ERROR", msg);
                    }
                }
                appenders.Clear();
            }
            Info("日志系统已关闭");
        }
    }
}
