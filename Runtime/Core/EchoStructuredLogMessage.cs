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
using System.Text;

[assembly: InternalsVisibleTo("EchoLog.Tests")]

namespace EchoLog
{
    /// <summary>延迟求值的消息委托</summary>
    public delegate string EchoMessageFormatter();

    /// <summary>结构化日志消息（避免 GC 分配）</summary>
    public struct EchoStructuredLogMessage
    {
        private readonly EchoMessageFormatter formatter;

        public EchoStructuredLogMessage(EchoMessageFormatter formatter)
        {
            this.formatter = formatter;
        }

        /// <summary>隐式转换：string -> EchoStructuredLogMessage</summary>
        public static implicit operator EchoStructuredLogMessage(string message)
        {
            return new EchoStructuredLogMessage(() => message);
        }

        /// <summary>获取字符串表示</summary>
        public override string ToString()
        {
            return formatter?.Invoke() ?? string.Empty;
        }

        /// <summary>快速格式化（1 个参数）- 使用 StringBuilder 避免 GC</summary>
        public static EchoStructuredLogMessage Format<T1>(string format, T1 arg1)
        {
            return new EchoStructuredLogMessage(() =>
            {
                var sb = new StringBuilder(format.Length + 32);
                FormatWithOneArg(sb, format, arg1);
                return sb.ToString();
            });
        }

        /// <summary>快速格式化（2 个参数）- 使用 StringBuilder 避免 GC</summary>
        public static EchoStructuredLogMessage Format<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            return new EchoStructuredLogMessage(() =>
            {
                var sb = new StringBuilder(format.Length + 64);
                FormatWithTwoArgs(sb, format, arg1, arg2);
                return sb.ToString();
            });
        }

        /// <summary>快速格式化（3 个参数）- 使用 StringBuilder 避免 GC</summary>
        public static EchoStructuredLogMessage Format<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            return new EchoStructuredLogMessage(() =>
            {
                var sb = new StringBuilder(format.Length + 96);
                FormatWithThreeArgs(sb, format, arg1, arg2, arg3);
                return sb.ToString();
            });
        }

        /// <summary>快速格式化（4 个参数）- 使用 StringBuilder 避免 GC</summary>
        public static EchoStructuredLogMessage Format<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return new EchoStructuredLogMessage(() =>
            {
                var sb = new StringBuilder(format.Length + 128);
                FormatWithFourArgs(sb, format, arg1, arg2, arg3, arg4);
                return sb.ToString();
            });
        }

        /// <summary>单参数格式化（手动替换 {0}）</summary>
        private static void FormatWithOneArg<T>(StringBuilder sb, string format, T arg)
        {
            int placeholderIndex = format.IndexOf("{0}");
            if (placeholderIndex >= 0)
            {
                sb.Append(format.Substring(0, placeholderIndex));
                sb.Append(arg);
                sb.Append(format.Substring(placeholderIndex + 3));
            }
            else
            {
                sb.Append(format);
            }
        }

        /// <summary>双参数格式化（手动替换 {0} 和 {1}）</summary>
        private static void FormatWithTwoArgs<T1, T2>(StringBuilder sb, string format, T1 arg1, T2 arg2)
        {
            int index0 = format.IndexOf("{0}");
            int index1 = format.IndexOf("{1}");

            if (index0 >= 0 && index1 >= 0)
            {
                if (index0 < index1)
                {
                    sb.Append(format.Substring(0, index0));
                    sb.Append(arg1);
                    sb.Append(format.Substring(index0 + 3, index1 - index0 - 3));
                    sb.Append(arg2);
                    sb.Append(format.Substring(index1 + 3));
                }
                else
                {
                    sb.Append(format.Substring(0, index1));
                    sb.Append(arg2);
                    sb.Append(format.Substring(index1 + 3, index0 - index1 - 3));
                    sb.Append(arg1);
                    sb.Append(format.Substring(index0 + 3));
                }
            }
            else
            {
                // 回退到 string.Format（复杂场景）
                sb.Append(string.Format(format, arg1, arg2));
            }
        }

        /// <summary>三参数格式化（回退到 string.Format）</summary>
        private static void FormatWithThreeArgs<T1, T2, T3>(StringBuilder sb, string format, T1 arg1, T2 arg2, T3 arg3)
        {
            // 复杂场景回退到 string.Format
            sb.Append(string.Format(format, arg1, arg2, arg3));
        }

        /// <summary>四参数格式化（回退到 string.Format）</summary>
        private static void FormatWithFourArgs<T1, T2, T3, T4>(StringBuilder sb, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            // 复杂场景回退到 string.Format
            sb.Append(string.Format(format, arg1, arg2, arg3, arg4));
        }
    }

    /// <summary>日志消息构建器（ref struct，栈上分配，零 GC）</summary>
    internal ref struct LogMessageBuilder
    {
        private readonly System.Text.StringBuilder builder;

        public LogMessageBuilder(int capacity = 256)
        {
            builder = new System.Text.StringBuilder(capacity);
        }

        /// <summary>追加字符串</summary>
        public LogMessageBuilder Append(string value)
        {
            builder.Append(value);
            return this;
        }

        /// <summary>追加整数</summary>
        public LogMessageBuilder Append(int value)
        {
            builder.Append(value);
            return this;
        }

        /// <summary>追加浮点数</summary>
        public LogMessageBuilder Append(float value)
        {
            builder.Append(value);
            return this;
        }

        /// <summary>追加双精度浮点数</summary>
        public LogMessageBuilder Append(double value)
        {
            builder.Append(value);
            return this;
        }

        /// <summary>追加布尔值</summary>
        public LogMessageBuilder Append(bool value)
        {
            builder.Append(value);
            return this;
        }

        /// <summary>追加字符</summary>
        public LogMessageBuilder Append(char value)
        {
            builder.Append(value);
            return this;
        }

        /// <summary>追加对象</summary>
        public LogMessageBuilder Append(object value)
        {
            builder.Append(value);
            return this;
        }

        /// <summary>追加行结束符</summary>
        public LogMessageBuilder AppendLine()
        {
            builder.AppendLine();
            return this;
        }

        /// <summary>追加字符串和行结束符</summary>
        public LogMessageBuilder AppendLine(string value)
        {
            builder.AppendLine(value);
            return this;
        }

        /// <summary>获取字符串表示</summary>
        public override string ToString()
        {
            return builder.ToString();
        }

        /// <summary>隐式转换为 string</summary>
        public static implicit operator string(LogMessageBuilder builder)
        {
            return builder.ToString();
        }

        /// <summary>释放资源</summary>
        public void Dispose()
        {
            // StringBuilder 是值类型，不需要显式释放
        }
    }
}
