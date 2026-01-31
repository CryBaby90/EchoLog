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
using System.Text;
using System.Threading;
using UnityEngine;

namespace EchoLog
{
    /// <summary>异步日志队列（独立线程写入，避免阻塞主线程）</summary>
    internal class AsyncLogQueue : MonoBehaviour
    {
        private static AsyncLogQueue instance;
        private readonly Queue<LogEntry> logQueue = new Queue<LogEntry>();
        private readonly object queueLock = new object();
        private Thread writeThread;
        private AutoResetEvent writeEvent;
        private bool isRunning;
        private bool isInitialized;
        private Coroutine performanceMonitorCoroutine;

        /// <summary>复用的日志条目列表（避免每次创建新 List）</summary>
        private readonly List<LogEntry> entriesToWrite = new List<LogEntry>(64);

        // 性能监控相关
        private float fps;
        private long memoryMB;
        private float fpsUpdateInterval = 0.5f;
        private float fpsTimer;
        private int frameCount;

        /// <summary>性能监控的 StringBuilder（避免字符串插值 GC）</summary>
        private readonly StringBuilder perfBuilder = new StringBuilder(128);

        /// <summary>获取单例实例</summary>
        internal static AsyncLogQueue Instance
        {
            get
            {
                if (instance == null)
                {
                    // 查找现有的实例
                    instance = FindFirstObjectByType<AsyncLogQueue>();

                    // 如果没有找到，创建新的
                    if (instance == null)
                    {
                        GameObject go = new GameObject("AsyncLogQueue");
                        instance = go.AddComponent<AsyncLogQueue>();
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            // 确保只有一个实例
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            // 编辑器模式下不使用 DontDestroyOnLoad，避免场景关闭警告
            #if !UNITY_EDITOR
            DontDestroyOnLoad(gameObject); // 跨场景保持
            #endif

            Initialize();
        }

        /// <summary>初始化异步队列</summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            writeEvent = new AutoResetEvent(false);
            isRunning = true;

            writeThread = new Thread(WriteLoop)
            {
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.BelowNormal
            };
            writeThread.Start();

            isInitialized = true;
        }

        /// <summary>启动性能监控</summary>
        internal void StartPerformanceMonitor(float interval)
        {
            if (performanceMonitorCoroutine != null)
                return;

            performanceMonitorCoroutine = StartCoroutine(PerformanceMonitorCoroutine(interval));
        }

        /// <summary>停止性能监控</summary>
        internal void StopPerformanceMonitor()
        {
            if (performanceMonitorCoroutine != null)
            {
                StopCoroutine(performanceMonitorCoroutine);
                performanceMonitorCoroutine = null;
            }
        }

        /// <summary>性能监控协程</summary>
        private System.Collections.IEnumerator PerformanceMonitorCoroutine(float interval)
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);

                // 记录性能日志
                long memoryMB = System.GC.GetTotalMemory(false) / (1024 * 1024);

                // 使用 StringBuilder 避免字符串插值的 GC 分配
                perfBuilder.Clear();
                perfBuilder.Append("性能监控 - FPS: ");
                perfBuilder.Append(fps.ToString("F1"));
                perfBuilder.Append(", 内存: ");
                perfBuilder.Append(memoryMB);
                perfBuilder.Append(" MB");

                Logger.Log(ELogLevel.Info, perfBuilder.ToString(), "Performance");
            }
        }

        private void Update()
        {
            // 计算 FPS
            frameCount++;
            fpsTimer += Time.unscaledDeltaTime;

            if (fpsTimer >= fpsUpdateInterval)
            {
                fps = frameCount / fpsTimer;
                frameCount = 0;
                fpsTimer = 0f;
            }
        }

        /// <summary>入队日志条目</summary>
        /// <param name="entry">日志条目</param>
        internal void Enqueue(in LogEntry entry)
        {
            if (!isInitialized)
                Initialize();

            lock (queueLock)
            {
                logQueue.Enqueue(entry);

                // 限制队列大小（防止内存溢出）
                if (Logger.Config != null && logQueue.Count > Logger.Config.QueueSize)
                {
                    logQueue.Dequeue(); // 丢弃最旧的日志
                }
            }

            writeEvent?.Set();
        }

        /// <summary>写入循环（在独立线程中运行）</summary>
        private void WriteLoop()
        {
            while (isRunning)
            {
                // 等待信号或超时（100ms）
                writeEvent.WaitOne(100);

                // 清空并复用列表（避免分配）
                entriesToWrite.Clear();

                // 批量出队
                lock (queueLock)
                {
                    while (logQueue.Count > 0)
                    {
                        entriesToWrite.Add(logQueue.Dequeue());
                    }
                }

                // 批量写入（使用索引避免 foreach 分配）
                int count = entriesToWrite.Count;
                for (int i = 0; i < count; i++)
                {
                    var entry = entriesToWrite[i];
                    Logger.WriteLog(in entry);
                }
            }
        }

        private void OnDestroy()
        {
            // 编辑器模式下，确保正确清理
            #if UNITY_EDITOR
            Shutdown();
            #endif

            // 清理静态引用
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }

        private void OnDisable()
        {
            // 编辑器停止运行时清理
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Shutdown();
            }
            #endif
        }

        /// <summary>关闭异步队列</summary>
        public void Shutdown()
        {
            // 停止性能监控
            StopPerformanceMonitor();

            if (!isRunning)
                return;

            isRunning = false;
            writeEvent?.Set();

            // 等待写入线程结束（最多 1 秒）
            if (writeThread != null && writeThread.IsAlive)
            {
                if (!writeThread.Join(1000))
                {
                    Debug.LogWarning("异步日志线程未能及时结束");
                }
            }

            writeEvent?.Dispose();
            writeEvent = null;

            // 写入剩余日志
            Flush();

            isInitialized = false;
        }

        /// <summary>刷新剩余日志</summary>
        public void Flush()
        {
            lock (queueLock)
            {
                while (logQueue.Count > 0)
                {
                    var entry = logQueue.Dequeue();
                    Logger.WriteLog(in entry);
                }
            }
        }
    }
}
