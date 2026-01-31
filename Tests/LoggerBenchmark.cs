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


using UnityEngine;
using System;

namespace EchoLog.Tests
{
    /// <summary>日志系统基准测试（运行时使用）</summary>
    /// <remarks>
    /// 使用方法：
    /// 1. 创建一个空的 GameObject
    /// 2. 添加此组件
    /// 3. 运行游戏
    /// 4. 在 Inspector 中点击"运行基准测试"按钮
    /// 5. 查看 Console 输出的性能数据
    /// </remarks>
    public class LoggerBenchmark : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private int testIterations = 10000;
        [SerializeField] private bool runOnStart = false;

        private LogConfig config;

        private void Start()
        {
            // 加载配置
            config = Resources.Load<LogConfig>("DefaultLogConfig");
            if (config == null)
            {
                UnityEngine.Debug.LogError("找不到 DefaultLogConfig，请先创建默认配置");
                return;
            }

            if (runOnStart)
            {
                RunBenchmark();
            }
        }

        [ContextMenu("运行基准测试")]
        public void RunBenchmark()
        {
            if (config == null)
            {
                UnityEngine.Debug.LogError("请先加载 LogConfig");
                return;
            }

            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log("=== 日志系统性能基准测试 ===");
            UnityEngine.Debug.Log("========================================");

            // 初始化日志系统
            Logger.Initialize(config);

            // 预热
            for (int i = 0; i < 100; i++)
            {
                Logger.Info($"Warm up {i}");
            }

            // 运行各项测试
            TestBasicLogging();
            TestLogFiltering();
            TestSensitiveDataFilter();
            TestStructuredLogging();
            TestCriticalLogCheck();

            UnityEngine.Debug.Log("========================================");
            UnityEngine.Debug.Log("=== 基准测试完成 ===");
            UnityEngine.Debug.Log("========================================");

            // 清理
            Logger.Shutdown();
        }

        /// <summary>测试 1：基本日志吞吐量</summary>
        private void TestBasicLogging()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            long startMemory = GC.GetTotalMemory(true);

            for (int i = 0; i < testIterations; i++)
            {
                Logger.Info($"Test message {i}");
            }

            stopwatch.Stop();
            long endMemory = GC.GetTotalMemory(false);
            long allocated = endMemory - startMemory;

            double throughput = testIterations / (stopwatch.ElapsedMilliseconds / 1000.0);

            UnityEngine.Debug.Log($"[基本日志] 时间: {stopwatch.ElapsedMilliseconds} ms, " +
                         $"分配: {allocated} bytes, " +
                         $"平均: {allocated / (float)testIterations} bytes/次, " +
                         $"吞吐量: {throughput:F0} 条/秒");
        }

        /// <summary>测试 2：日志级别过滤</summary>
        private void TestLogFiltering()
        {
            // 设置为 Warning 级别，Debug 日志应该被过滤
            Logger.MinELogLevel = ELogLevel.Warning;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            long startMemory = GC.GetTotalMemory(true);

            // Debug 日志应该被过滤
            for (int i = 0; i < testIterations; i++)
            {
                Logger.Debug($"Filtered message {i}");
            }

            stopwatch.Stop();
            long endMemory = GC.GetTotalMemory(false);
            long allocated = endMemory - startMemory;

            UnityEngine.Debug.Log($"[级别过滤] 时间: {stopwatch.ElapsedMilliseconds} ms, " +
                         $"分配: {allocated} bytes (应该接近 0)");

            // 恢复级别
            Logger.MinELogLevel = config.MinLogLevel;
        }

        /// <summary>测试 3：敏感信息过滤</summary>
        private void TestSensitiveDataFilter()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            long startMemory = GC.GetTotalMemory(true);

            for (int i = 0; i < testIterations; i++)
            {
                Logger.Info($"password={i} token=abc{i} key=xyz{i}");
            }

            stopwatch.Stop();
            long endMemory = GC.GetTotalMemory(false);
            long allocated = endMemory - startMemory;

            UnityEngine.Debug.Log($"[敏感信息过滤] 时间: {stopwatch.ElapsedMilliseconds} ms, " +
                         $"分配: {allocated} bytes, " +
                         $"平均: {allocated / (float)testIterations} bytes/次");
        }

        /// <summary>测试 4：结构化日志</summary>
        private void TestStructuredLogging()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            long startMemory = GC.GetTotalMemory(true);

            for (int i = 0; i < testIterations; i++)
            {
                var msg = StructuredLogMessage.Format("Value: {0}, Count: {1}", i, i * 2);
                Logger.InfoStructured(in msg);
            }

            stopwatch.Stop();
            long endMemory = GC.GetTotalMemory(false);
            long allocated = endMemory - startMemory;

            UnityEngine.Debug.Log($"[结构化日志] 时间: {stopwatch.ElapsedMilliseconds} ms, " +
                         $"分配: {allocated} bytes, " +
                         $"平均: {allocated / (float)testIterations} bytes/次");
        }

        /// <summary>测试 5：关键日志检查性能</summary>
        private void TestCriticalLogCheck()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            long startMemory = GC.GetTotalMemory(true);

            // 通过实际日志调用来测试关键分类检查性能
            int checkIterations = testIterations * 100;
            for (int i = 0; i < checkIterations; i++)
            {
                Logger.Log(ELogLevel.Info, $"Test {i}", "Network");
            }

            stopwatch.Stop();
            long endMemory = GC.GetTotalMemory(false);
            long allocated = endMemory - startMemory;

            UnityEngine.Debug.Log($"[关键日志检查] 时间: {stopwatch.ElapsedMilliseconds} ms, " +
                                 $"分配: {allocated} bytes, " +
                                 $"迭代: {checkIterations}, " +
                                 $"平均: {stopwatch.ElapsedMilliseconds * 1000000.0 / checkIterations:F2} ns/次");
        }

        private void OnDestroy()
        {
            Logger.Shutdown();
        }
    }
}
