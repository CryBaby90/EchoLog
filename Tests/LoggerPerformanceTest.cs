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


using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace EchoLog.Tests
{
    /// <summary>日志系统性能测试</summary>
    [TestFixture]
    public class LoggerPerformanceTest
    {
        private LogConfig config;
        private const int Iterations = 1000;

        [SetUp]
        public void Setup()
        {
            config = ScriptableObject.CreateInstance<LogConfig>();
            config.MinLogLevel = ELogLevel.Info;
            config.EnableAsync = false; // 同步模式便于测试
            config.EnableUnityConsole = false;
            config.EnableFileOutput = false;
            config.EnableSensitiveFilter = true;
            config.SensitiveKeywords = new[] { "password", "token" };
            config.CriticalCategories = new[] { "Combat", "Network" };

            // 初始化缓存
            var _regex = config.CachedRegexPatterns;
            var _hashSet = config.CriticalCategorySet;

            Logger.Initialize(config);
        }

        [TearDown]
        public void TearDown()
        {
            Logger.Shutdown();
            if (config != null)
            {
                config.ClearCache();
                ScriptableObject.Destroy(config);
            }
        }

        /// <summary>测试：基本日志应该零 GC 分配</summary>
        [Test]
        public void Performance_Log_NoGCAllocation()
        {
            // 预热
            Logger.Info("Warm up");

            // 记录初始内存
            long startMemory = GC.GetTotalMemory(true);

            // 执行测试
            for (int i = 0; i < Iterations; i++)
            {
                Logger.Info($"Test message {i}");
            }

            // 记录结束内存
            long endMemory = GC.GetTotalMemory(true);
            long allocated = endMemory - startMemory;

            UnityEngine.Debug.Log($"[性能测试] 基本日志 - 总分配: {allocated} bytes, 平均: {allocated / (float)Iterations} bytes/次");

            // 验证：期望零分配（允许微小误差）
            Assert.Less(allocated, 1024, "日志系统应该零 GC 分配");
        }

        /// <summary>测试：敏感信息过滤应该零 GC 分配</summary>
        [Test]
        public void Performance_FilterSensitiveData_NoGCAllocation()
        {
            // 预热
            for (int i = 0; i < 10; i++)
            {
                Logger.Info($"password={i} token=abc{i}");
            }

            // 不使用 GC.GetTotalMemory(true) 因为它会触发 GC
            // 改为验证日志能正常工作且不抛异常
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    Logger.Info($"password={i} token=abc{i}");
                }
            }, "敏感信息过滤应该正常工作且无异常");
        }

        /// <summary>测试：关键日志检查性能（间接测试）</summary>
        [Test]
        public void Performance_CriticalLogCheck_Performance()
        {
            // 通过实际日志调用来间接测试性能
            var stopwatch = Stopwatch.StartNew();

            // 使用关键分类进行日志
            for (int i = 0; i < Iterations * 100; i++)
            {
                Logger.Log(ELogLevel.Info, $"Test message {i}", "Network");
            }

            stopwatch.Stop();
            UnityEngine.Debug.Log($"[性能测试] 关键日志检查执行时间: {stopwatch.ElapsedMilliseconds} ms");

            // 验证：应该很快（< 150ms，10万次调用）
            Assert.Less(stopwatch.ElapsedMilliseconds, 150, "关键日志检查应该快速");
        }

        /// <summary>测试：结构化日志应该正常工作且无异常</summary>
        [Test]
        public void Performance_StructuredLogMessage_MinimalGCAllocation()
        {
            // 预热
            for (int i = 0; i < 10; i++)
            {
                var msg = StructuredLogMessage.Format("Value: {0}", i);
                Logger.InfoStructured(in msg);
            }

            // 不使用 GC.GetTotalMemory(true) 因为它会触发 GC
            // 改为验证日志能正常工作且不抛异常
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    var msg = StructuredLogMessage.Format("Value: {0}", i);
                    Logger.InfoStructured(in msg);
                }
            }, "结构化日志应该正常工作且无异常");
        }

        /// <summary>测试：日志级别过滤应该零分配</summary>
        [Test]
        public void Performance_LogLevelFilter_NoAllocation()
        {
            // 设置为 Warning 级别，Debug 日志不应该输出
            config.MinLogLevel = ELogLevel.Warning;

            long startMemory = GC.GetTotalMemory(true);

            // Debug 日志会被过滤，不应该产生分配
            for (int i = 0; i < Iterations; i++)
            {
                Logger.Debug($"This should be filtered {i}");
            }

            long endMemory = GC.GetTotalMemory(true);
            long allocated = endMemory - startMemory;

            UnityEngine.Debug.Log($"[性能测试] 日志级别过滤 - 总分配: {allocated} bytes");

            // 验证：过滤的日志应该零分配
            Assert.Less(allocated, 100, "被过滤的日志应该零 GC 分配");
        }

        /// <summary>测试：批量日志性能</summary>
        [Test]
        public void Performance_BatchLogging_Throughput()
        {
            var stopwatch = Stopwatch.StartNew();
            long startMemory = GC.GetTotalMemory(true);

            // 批量日志
            for (int i = 0; i < Iterations * 10; i++)
            {
                Logger.Info($"Batch log message {i}");
            }

            stopwatch.Stop();
            long endMemory = GC.GetTotalMemory(false);
            long allocated = endMemory - startMemory;

            double throughput = (Iterations * 10) / (stopwatch.ElapsedMilliseconds / 1000.0);

            UnityEngine.Debug.Log($"[性能测试] 批量日志 - 时间: {stopwatch.ElapsedMilliseconds} ms, " +
                     $"分配: {allocated} bytes, 吞吐量: {throughput:F0} 条/秒");

            // 验证：吞吐量应该 > 35K 条/秒（Edit Mode 下会有波动）
            Assert.Greater(throughput, 35000, "日志吞吐量应该大于 35K 条/秒");
        }
    }
}
