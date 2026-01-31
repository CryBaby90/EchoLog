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
    /// <summary>日志级别</summary>
    public enum ELogLevel
    {
        /// <summary>调试信息</summary>
        Debug = 0,

        /// <summary>一般信息</summary>
        Info = 1,

        /// <summary>警告</summary>
        Warning = 2,

        /// <summary>错误</summary>
        Error = 3,

        /// <summary>致命错误</summary>
        Fatal = 4,

        /// <summary>无效/未设置</summary>
        None = 5
    }
}
