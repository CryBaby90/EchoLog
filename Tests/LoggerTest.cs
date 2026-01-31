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
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoLog.Tests
{
    /// <summary>日志系统测试脚本</summary>
    public class LoggerTest : MonoBehaviour
    {
        [Header("测试配置")]
        [Tooltip("是否启用测试")]
        public bool enableTest = true;

        [Tooltip("测试间隔（秒）")]
        public float testInterval = 2f;

        [Header("日志配置")]
        public LogConfig logConfig;

        private float timer;

        private void Start()
        {
            if (!enableTest)
                return;

            // 如果没有设置配置，自动加载默认配置
            if (logConfig == null)
            {
                logConfig = Resources.Load<LogConfig>("DefaultLogConfig");
                if (logConfig != null)
                {
                    UnityEngine.Debug.Log("[LoggerTest] 自动加载默认日志配置");
                }
            }

            // 确保配置存在
            if (logConfig == null)
            {
                UnityEngine.Debug.LogError("找不到 DefaultLogConfig，请在 Resources 目录下创建日志配置资产");
                return;
            }

            // 初始化日志系统
            Logger.Initialize(logConfig);
            Logger.Info("=== 日志系统测试开始 ===");

            // 初始化 DOTS 日志
            if (World.DefaultGameObjectInjectionWorld != null)
            {
                DOTSLogger.Initialize(World.DefaultGameObjectInjectionWorld);
            }

            // 运行基础测试
            RunBasicTests();

            Logger.Info("测试脚本已启动，将在 Update 中运行测试...");
        }

        private void Update()
        {
            if (!enableTest)
                return;

            timer += Time.deltaTime;

            if (timer >= testInterval)
            {
                timer = 0f;
                RunPeriodicTests();
            }

            // 测试 DOTS 日志（按空格键）
            if (Keyboard.current != null && Keyboard.current[Key.Space].wasPressedThisFrame)
            {
                TestDOTSLogger();
            }

            // 测试异常日志（按 E 键）
            if (Keyboard.current != null && Keyboard.current[Key.E].wasPressedThisFrame)
            {
                TestExceptionLogging();
            }

            // 快速测试 DOTS 不同级别
            if (Keyboard.current != null && Keyboard.current[Key.Digit1].wasPressedThisFrame)
            {
                var msg = new FixedString512Bytes();
                msg.Append("DOTS Debug test at ");
                msg.Append(DOTSLogger.FormatFloat(Time.time));
                DOTSLogger.Debug(msg, (FixedString64Bytes)"QuickTest");
            }

            if (Keyboard.current != null && Keyboard.current[Key.Digit2].wasPressedThisFrame)
            {
                var msg = new FixedString512Bytes();
                msg.Append("DOTS Warning test - FPS: ");
                msg.Append(DOTSLogger.FormatFloat(1f / Time.deltaTime));
                DOTSLogger.Warning(msg, (FixedString64Bytes)"QuickTest");
            }

            if (Keyboard.current != null && Keyboard.current[Key.Digit3].wasPressedThisFrame)
            {
                var msg = new FixedString512Bytes();
                msg.Append("DOTS Error test - Position: ");
                msg.Append(DOTSLogger.FormatFloat3((float3)transform.position));
                DOTSLogger.Error(msg, (FixedString64Bytes)"QuickTest");
            }
        }

        private void RunBasicTests()
        {
            Logger.Info("--- 基础功能测试 ---");

            // 测试各级别日志
            Logger.Debug("这是一条调试信息");
            Logger.Info("这是一条一般信息");
            Logger.Warning("这是一条警告信息");
            Logger.Error("这是一条错误信息");
            Logger.Fatal("这是一条致命错误信息");

            // 测试关键日志（无视日志级别）
            Logger.Critical("游戏核心系统初始化", "System");
            Logger.Critical("玩家数据已保存", "SaveGame");

            // 测试分类日志
            Logger.Log(ELogLevel.Info, "玩家进入战斗区域", "Combat");
            Logger.Log(ELogLevel.Debug, "AI 状态改变: Idle -> Chase", "AI");

            // 测试延迟求值日志
            Logger.LogLazy(ELogLevel.Debug, () =>
            {
                var pos = transform.position;
                var fps = 1f / Time.deltaTime;
                return $"Transform: {pos}, FPS: {fps:F1}";
            });

            // 测试结构化日志
            int health = 100;
            int maxHealth = 150;
            Logger.LogStructured(ELogLevel.Debug,
                StructuredLogMessage.Format("玩家生命值: {0}/{1}", health, maxHealth));

            // 测试结构化日志快捷方法
            var scoreMsg = StructuredLogMessage.Format("当前分数: {0}", 1000);
            Logger.DebugStructured(in scoreMsg, "Game");

            var levelMsg = StructuredLogMessage.Format("玩家等级: {0}", 5);
            Logger.InfoStructured(in levelMsg, "Player");

            var warningMsg = StructuredLogMessage.Format("生命值低于: {0}%", 20);
            Logger.WarningStructured(in warningMsg, "Health");

            var errorMsg = StructuredLogMessage.Format("连接超时: {0}ms", 5000);
            Logger.ErrorStructured(in errorMsg, "Network");

            // 测试 LogMessageBuilder
            var builder = new LogMessageBuilder(256);
            builder.Append("测试日志构建器: ")
                   .Append("Value1=").Append(100)
                   .Append(", Value2=").Append(200);
            Logger.Info(builder.ToString());

            Logger.Info("--- 基础功能测试完成 ---");
        }

        private void RunPeriodicTests()
        {
            Logger.Info("--- 周期性测试 ---");

            // 测试性能日志
            float fps = 1f / Time.deltaTime;
            long memoryMB = GC.GetTotalMemory(false) / (1024 * 1024);

            var builder = new LogMessageBuilder(256);
            builder.Append("性能测试 - FPS: ").Append(fps.ToString("F1"))
                   .Append(", Memory: ").Append(memoryMB)
                   .Append(" MB, Time: ").Append(Time.time.ToString("F2"));
            Logger.Log(ELogLevel.Info, builder.ToString(), "Performance");

            // 测试日志级别过滤
            Logger.Debug("这条 Debug 日志可能不会显示（取决于配置）");
            Logger.Info("这条 Info 日志应该会显示");
            Logger.Warning("这是一条测试警告");

            Logger.Info("--- 周期性测试完成 ---");
        }

        private void TestDOTSLogger()
        {
            Logger.Info("=== DOTS 日志系统测试 ===");

            var pos = (float3)transform.position;
            var message = new FixedString512Bytes();

            // 测试 Debug 级别
            message.Clear();
            message.Append("DOTS Debug - Position: ");
            message.Append(DOTSLogger.FormatFloat3(pos));
            DOTSLogger.Debug(message, (FixedString64Bytes)"DOTSTest");

            // 测试 Info 级别
            message.Clear();
            message.Append("DOTS Info - Time: ");
            message.Append(DOTSLogger.FormatFloat(Time.time));
            DOTSLogger.Info(message, (FixedString64Bytes)"DOTSTest");

            // 测试 Warning 级别
            message.Clear();
            float fps = 1f / Time.deltaTime;
            message.Append("DOTS Warning - Low FPS: ");
            message.Append(DOTSLogger.FormatFloat(fps));
            DOTSLogger.Warning(message, (FixedString64Bytes)"Performance");

            // 测试 Error 级别
            message.Clear();
            message.Append("DOTS Error - Test error at ");
            message.Append(DOTSLogger.FormatFloat(Time.time));
            DOTSLogger.Error(message, (FixedString64Bytes)"DOTSTest");

            // 测试格式化方法
            TestDotsFormatterMethods();

            Logger.Info("=== DOTS 日志测试完成 ===");
        }

        private void TestDotsFormatterMethods()
        {
            Logger.Info("--- DOTS 格式化方法测试 ---");

            // FormatFloat3
            float3 pos3 = new float3(10.5f, 20.3f, 30.7f);
            var msg1 = DOTSLogger.FormatFloat3(pos3);
            DOTSLogger.Debug(msg1, (FixedString64Bytes)"FormatTest");

            // FormatFloat2
            float2 pos2 = new float2(640f, 480f);
            var msg2 = DOTSLogger.FormatFloat2(pos2);
            DOTSLogger.Debug(msg2, (FixedString64Bytes)"FormatTest");

            // FormatInt3
            int3 intPos3 = new int3(100, 200, 300);
            var msg3 = DOTSLogger.FormatInt3(intPos3);
            DOTSLogger.Debug(msg3, (FixedString64Bytes)"FormatTest");

            // FormatInt2
            int2 intPos2 = new int2(1920, 1080);
            var msg4 = DOTSLogger.FormatInt2(intPos2);
            DOTSLogger.Debug(msg4, (FixedString64Bytes)"FormatTest");

            // FormatInt
            var msg5 = DOTSLogger.FormatInt(9999);
            DOTSLogger.Debug(msg5, (FixedString64Bytes)"FormatTest");

            // FormatFloat
            var msg6 = DOTSLogger.FormatFloat(3.14159f);
            DOTSLogger.Debug(msg6, (FixedString64Bytes)"FormatTest");

            Logger.Info("--- DOTS 格式化方法测试完成 ---");
        }

        private void TestExceptionLogging()
        {
            Logger.Info("测试异常日志...");

            try
            {
                // 模拟异常
                int[] arr = new int[5];
                var a = arr[5];

            }
            catch (System.Exception ex)
            {
                Logger.LogException(ex, "TestException");
            }
        }

        private void OnDestroy()
        {
            if (!enableTest)
                return;

            Logger.Info("=== 日志系统测试结束 ===");
            Logger.Shutdown();
        }

        private void OnGUI()
        {
            if (!enableTest)
                return;

            // 绘制测试说明
            GUILayout.BeginArea(new Rect(10, 10, 450, 450));
            GUILayout.Label("=== EchoLog 日志系统测试 ===");
            GUILayout.Label($"当前日志级别: {Logger.MinELogLevel}");
            GUILayout.Label($"配置: {(logConfig ? logConfig.name : "未设置")}");
            GUILayout.Space(10);

            // 基础测试按钮
            GUILayout.Label("--- 基础功能 ---");
            if (GUILayout.Button("测试各级别日志（含 Fatal 和 Critical）"))
            {
                RunBasicTests();
            }

            if (GUILayout.Button("测试敏感信息过滤"))
            {
                Logger.Info("用户登录: username=admin, password=123456, token=abc123");
            }

            if (GUILayout.Button("切换 Debug 级别"))
            {
                Logger.MinELogLevel = Logger.MinELogLevel == ELogLevel.Debug
                    ? ELogLevel.Info
                    : ELogLevel.Debug;
                Logger.Info($"日志级别已切换为: {Logger.MinELogLevel}");
            }

            GUILayout.Space(10);

            // DOTS 测试按钮
            GUILayout.Label("--- DOTS 日志 ---");
            GUILayout.Label("按键测试:");
            GUILayout.Label("  [空格] 测试 DOTS 日志（全级别+格式化）");
            GUILayout.Label("  [E] 测试异常日志");

            if (GUILayout.Button("测试 DOTS 日志（全级别+格式化）"))
            {
                TestDOTSLogger();
            }

            GUILayout.Space(10);

            // 快速测试按钮
            GUILayout.Label("--- 快速测试 ---");
            if (GUILayout.Button("测试 Critical 日志"))
            {
                Logger.Critical("关键流程：游戏核心系统初始化", "System");
                Logger.Critical("关键流程：玩家数据已保存", "SaveGame");
            }

            if (GUILayout.Button("测试 Fatal 日志"))
            {
                Logger.Fatal("致命错误：内存不足，即将崩溃");
            }

            if (GUILayout.Button("测试结构化日志快捷方法"))
            {
                var msg = StructuredLogMessage.Format("测试值: {0}", 42);
                Logger.DebugStructured(in msg, "Test");
                Logger.InfoStructured(in msg, "Test");
                Logger.WarningStructured(in msg, "Test");
                Logger.ErrorStructured(in msg, "Test");
            }

            GUILayout.Space(5);
            GUILayout.Label("--- 按 [1] [2] [3] 键测试 DOTS 不同级别 ---");

            GUILayout.EndArea();
        }
    }
}
