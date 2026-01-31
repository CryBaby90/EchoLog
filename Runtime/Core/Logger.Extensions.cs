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

namespace EchoLog
{
    /// <summary>Logger 扩展方法（结构化日志支持）</summary>
    public static partial class Logger
    {
        /// <summary>记录结构化日志</summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">结构化日志消息</param>
        /// <param name="category">日志分类</param>
        public static void LogStructured(ELogLevel level, in StructuredLogMessage message, string category = null)
        {
            if (config == null || level < MinELogLevel)
                return;

            Log(level, message.ToString(), category);
        }

        /// <summary>记录结构化调试信息</summary>
        public static void DebugStructured(in StructuredLogMessage message, string category = null)
        {
            LogStructured(ELogLevel.Debug, in message, category);
        }

        /// <summary>记录结构化一般信息</summary>
        public static void InfoStructured(in StructuredLogMessage message, string category = null)
        {
            LogStructured(ELogLevel.Info, in message, category);
        }

        /// <summary>记录结构化警告</summary>
        public static void WarningStructured(in StructuredLogMessage message, string category = null)
        {
            LogStructured(ELogLevel.Warning, in message, category);
        }

        /// <summary>记录结构化错误</summary>
        public static void ErrorStructured(in StructuredLogMessage message, string category = null)
        {
            LogStructured(ELogLevel.Error, in message, category);
        }

        /// <summary>记录结构化致命错误</summary>
        public static void FatalStructured(in StructuredLogMessage message, string category = null)
        {
            LogStructured(ELogLevel.Fatal, in message, category);
        }
    }
}
