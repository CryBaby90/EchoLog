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


namespace EchoLog
{
    /// <summary>日志输出处理器接口</summary>
    internal interface ILogAppender
    {
        /// <summary>追加日志</summary>
        /// <param name="entry">日志条目</param>
        void Append(in LogEntry entry);

        /// <summary>刷新缓冲区</summary>
        void Flush();

        /// <summary>释放资源</summary>
        void Dispose();
    }
}
