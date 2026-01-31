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
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EchoLog
{
    /// <summary>文件输出处理器（支持文件轮转、压缩、归档）</summary>
    internal class FileAppender : ILogAppender
    {
        private readonly EchoLogConfig config;
        private StreamWriter currentWriter;
        private string currentLogFile;
        private long currentFileSize;
        private readonly object fileLock = new object();

        /// <summary>日志级别字符串缓存（避免 ToUpper().PadRight() 的 GC 分配）</summary>
        private static readonly string[] LogLevelStrings =
            { "DEBUG", "INFO ", "WARN ", "ERROR", "FATAL" };

        /// <summary>复用的 StringBuilder（避免每次创建）</summary>
        private readonly StringBuilder lineBuilder = new StringBuilder(512);

        /// <summary>未刷新的日志条数</summary>
        private int pendingFlushCount;

        /// <summary>批量刷新大小</summary>
        private const int BatchFlushSize = 10;

        internal FileAppender(EchoLogConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            InitializeLogFile();
        }

        /// <summary>初始化日志文件</summary>
        private void InitializeLogFile()
        {
            string logDir = Path.Combine(Application.dataPath, "..", config.LogDirectory);
            Directory.CreateDirectory(logDir);

            currentLogFile = Path.Combine(logDir, $"Game_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            currentFileSize = 0;
        }

        /// <summary>追加日志到文件</summary>
        public void Append(in LogEntry entry)
        {
            lock (fileLock)
            {
                CheckFileRotation();

                if (currentWriter == null)
                {
                    currentWriter = new StreamWriter(currentLogFile, true, Encoding.UTF8);
                }

                string logLine = FormatLogLine(in entry);
                currentWriter.WriteLine(logLine);

                // 批量刷新：减少磁盘 I/O
                pendingFlushCount++;
                if (pendingFlushCount >= BatchFlushSize)
                {
                    currentWriter.Flush();
                    pendingFlushCount = 0;
                }

                currentFileSize += Encoding.UTF8.GetByteCount(logLine) + Environment.NewLine.Length;
            }
        }

        /// <summary>检查文件是否需要轮转</summary>
        private void CheckFileRotation()
        {
            if (currentFileSize > config.MaxFileSizeMB * 1024 * 1024)
            {
                CloseCurrentWriter();
                CompressLogFileAsync(currentLogFile);
                InitializeLogFile();
                CleanOldFiles();
            }
        }

        /// <summary>格式化日志行</summary>
        private string FormatLogLine(in LogEntry entry)
        {
            DateTime timestamp = DateTimeOffset.FromUnixTimeMilliseconds(entry.Timestamp).LocalDateTime;

            // 使用复用的 StringBuilder 避免分配
            lineBuilder.Clear();

            // 手动构建时间戳 [HH:mm:ss.fff]
            lineBuilder.Append('[');
            AppendTimestamp(lineBuilder, timestamp);
            lineBuilder.Append("] [");

            // 使用预分配的级别字符串（已格式化为 5 字符宽度）
            int levelIndex = (int)entry.Level;
            if (levelIndex >= 0 && levelIndex < LogLevelStrings.Length)
            {
                lineBuilder.Append(LogLevelStrings[levelIndex]);
            }
            else
            {
                lineBuilder.Append(entry.Level.ToString().ToUpper().PadRight(5));
            }

            lineBuilder.Append("] [");
            lineBuilder.Append(entry.Category);
            lineBuilder.Append("] ");
            lineBuilder.Append(entry.Message);

            // 错误和致命错误级别是否在文件中输出堆栈跟踪
            if (config.EnableFileStackTrace && (entry.Level == EEchoLogLevel.Error || entry.Level == EEchoLogLevel.Fatal))
            {
                if (!string.IsNullOrEmpty(entry.StackTrace))
                {
                    lineBuilder.AppendLine();
                    lineBuilder.Append(entry.StackTrace);
                }
            }

            return lineBuilder.ToString();
        }

        /// <summary>手动追加时间戳（避免格式化字符串的 GC 分配）</summary>
        private static void AppendTimestamp(StringBuilder sb, DateTime dt)
        {
            // HH:mm:ss.fff
            sb.Append((char)('0' + dt.Hour / 10));
            sb.Append((char)('0' + dt.Hour % 10));
            sb.Append(':');
            sb.Append((char)('0' + dt.Minute / 10));
            sb.Append((char)('0' + dt.Minute % 10));
            sb.Append(':');
            sb.Append((char)('0' + dt.Second / 10));
            sb.Append((char)('0' + dt.Second % 10));
            sb.Append('.');
            int ms = dt.Millisecond;
            sb.Append((char)('0' + ms / 100));
            sb.Append((char)('0' + (ms % 100) / 10));
            sb.Append((char)('0' + ms % 10));
        }

        /// <summary>关闭当前写入器</summary>
        private void CloseCurrentWriter()
        {
            currentWriter?.Flush();
            currentWriter?.Dispose();
            currentWriter = null;
        }

        /// <summary>异步压缩日志文件</summary>
        private void CompressLogFileAsync(string filePath)
        {
            if (!config.EnableCompression)
                return;

            Task.Run(() =>
            {
                try
                {
                    string zipPath = filePath + ".zip";

                    // 删除旧压缩文件
                    if (File.Exists(zipPath))
                        File.Delete(zipPath);

                    // 创建新的压缩文件
                    using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                    {
                        archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
                    }

                    // 删除原始日志文件
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"压缩日志文件失败: {ex.Message}");
                }
            });
        }

        /// <summary>清理旧日志文件</summary>
        private void CleanOldFiles()
        {
            try
            {
                string logDir = Path.GetDirectoryName(currentLogFile);
                if (string.IsNullOrEmpty(logDir))
                    return;

                // 直接获取文件，不使用 LINQ
                string[] logFiles = Directory.GetFiles(logDir, "*.log");
                string[] zipFiles = Directory.GetFiles(logDir, "*.zip");

                // 合并数组
                string[] allFiles = new string[logFiles.Length + zipFiles.Length];
                Array.Copy(logFiles, 0, allFiles, 0, logFiles.Length);
                Array.Copy(zipFiles, 0, allFiles, logFiles.Length, zipFiles.Length);

                // 如果文件数量未超过限制，直接返回
                if (allFiles.Length <= config.MaxFiles)
                    return;

                // 按创建时间排序（使用数组排序）
                Array.Sort(allFiles, (a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));

                // 删除超出限制的文件
                for (int i = config.MaxFiles; i < allFiles.Length; i++)
                {
                    try
                    {
                        File.Delete(allFiles[i]);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"删除旧日志文件失败 ({allFiles[i]}): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"清理旧日志文件失败: {ex.Message}");
            }
        }

        /// <summary>刷新缓冲区</summary>
        public void Flush()
        {
            lock (fileLock)
            {
                // 刷新挂起的日志
                if (pendingFlushCount > 0)
                {
                    currentWriter?.Flush();
                    pendingFlushCount = 0;
                }
            }
        }

        /// <summary>释放资源</summary>
        public void Dispose()
        {
            CloseCurrentWriter();
        }
    }
}
