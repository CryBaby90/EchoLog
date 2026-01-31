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
    /// <summary>日志接口</summary>
    internal interface ILogger
    {
        /// <summary>记录日志</summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        void Log(EEchoLogLevel level, string message);

        /// <summary>记录格式化日志</summary>
        /// <param name="level">日志级别</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">格式化参数</param>
        void LogFormat(EEchoLogLevel level, string format, params object[] args);

        /// <summary>记录调试信息</summary>
        void Debug(string message);

        /// <summary>记录一般信息</summary>
        void Info(string message);

        /// <summary>记录警告</summary>
        void Warning(string message);

        /// <summary>记录错误</summary>
        void Error(string message);

        /// <summary>记录致命错误</summary>
        void Fatal(string message);

        /// <summary>记录异常</summary>
        /// <param name="exception">异常对象</param>
        void LogException(Exception exception);
    }
}
