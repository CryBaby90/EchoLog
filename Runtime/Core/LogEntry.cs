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
    /// <summary>日志条目结构体</summary>
    internal struct LogEntry
    {
        /// <summary>日志级别</summary>
        internal ELogLevel Level;

        /// <summary>Unix 时间戳（毫秒）</summary>
        internal long Timestamp;

        /// <summary>日志消息</summary>
        internal string Message;

        /// <summary>堆栈跟踪</summary>
        internal string StackTrace;

        /// <summary>日志分类（如 "Combat", "Movement"）</summary>
        internal string Category;

        /// <summary>线程 ID</summary>
        internal int ThreadId;

        /// <summary>结构化数据（可选）</summary>
        internal object Context;
    }
}
