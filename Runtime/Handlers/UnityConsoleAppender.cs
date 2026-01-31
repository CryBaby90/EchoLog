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


using System.Text;
using UnityEngine;

namespace EchoLog
{
    /// <summary>Unity Console 输出处理器</summary>
    internal class UnityConsoleAppender : ILogAppender
    {
        private readonly StringBuilder stringBuilder = new StringBuilder(256);

        /// <summary>追加日志到 Unity Console</summary>
        public void Append(in LogEntry entry)
        {
            string message = FormatForUnityConsole(in entry);

            switch (entry.Level)
            {
                case ELogLevel.Debug:
                case ELogLevel.Info:
                    Debug.Log(message);
                    break;

                case ELogLevel.Warning:
                    Debug.LogWarning(message);
                    break;

                case ELogLevel.Error:
                case ELogLevel.Fatal:
                    Debug.LogError(message);
                    break;
            }
        }

        /// <summary>格式化日志消息</summary>
        private string FormatForUnityConsole(in LogEntry entry)
        {
            stringBuilder.Clear();

            // 添加分类标签
            if (!string.IsNullOrEmpty(entry.Category))
            {
                stringBuilder.Append("[");
                stringBuilder.Append(entry.Category);
                stringBuilder.Append("] ");
            }

            // 添加消息
            stringBuilder.Append(entry.Message);

            // 添加堆栈跟踪
            if (!string.IsNullOrEmpty(entry.StackTrace))
            {
                stringBuilder.AppendLine();
                stringBuilder.Append(entry.StackTrace);
            }

            return stringBuilder.ToString();
        }

        /// <summary>刷新缓冲区（Unity Console 不需要）</summary>
        public void Flush()
        {
            // Unity Console 不需要刷新
        }

        /// <summary>释放资源</summary>
        public void Dispose()
        {
            // 无资源需要释放
        }
    }
}
