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


using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace EchoLog
{
    /// <summary>DOTS 日志请求组件</summary>
    internal struct LogRequestComponent : IComponentData
    {
        internal EEchoLogLevel Level;
        internal FixedString512Bytes Message;
        internal FixedString64Bytes Category;
        internal long Timestamp;
    }

    /// <summary>DOTS 日志请求标签（用于查询）</summary>
    internal struct LogRequestTag : IComponentData { }

    /// <summary>DOTS 日志 System</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    internal partial struct DOTSSystemLogger : ISystem
    {
        private EntityQuery query;

        public void OnCreate(ref SystemState state)
        {
            query = state.GetEntityQuery(typeof(LogRequestComponent));
        }

        public void OnDestroy(ref SystemState state)
        {
            // 清理所有未处理的日志请求
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (request, entity) in SystemAPI.Query<LogRequestComponent>().WithEntityAccess())
            {
                EchoLogger.Log(request.Level, request.Message.ToString(), request.Category.ToString());
                ecb.DestroyEntity(entity);
            }
            ecb.Playback(state.EntityManager);
        }

        public void OnUpdate(ref SystemState state)
        {
            // 处理所有日志请求
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (request, entity) in SystemAPI.Query<LogRequestComponent>().WithEntityAccess())
            {
                // 转换为托管字符串并输出到主日志系统
                EchoLogger.Log(request.Level, request.Message.ToString(), request.Category.ToString());
                ecb.DestroyEntity(entity);
            }
            ecb.Playback(state.EntityManager);
        }
    }

    /// <summary>DOTS 日志静态接口</summary>
    public static class EchoDOTSLogger
    {
        private static World defaultWorld;
        private static bool isInitialized;

        /// <summary>初始化 DOTS 日志系统</summary>
        /// <param name="world">默认世界</param>
        public static void Initialize(World world)
        {
            if (world == null)
            {
                UnityEngine.Debug.LogError("DOTSLogger: World 不能为 null");
                return;
            }

            defaultWorld = world;
            isInitialized = true;
        }

        /// <summary>记录日志（通过创建临时 Entity）</summary>
        /// <typeparam name="T">Category 类型（unmanaged）</typeparam>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息（FixedString）</param>
        /// <param name="category">日志分类</param>
        public static void Log<T>(EEchoLogLevel level, FixedString512Bytes message, T category) where T : unmanaged
        {
            if (!isInitialized || defaultWorld == null)
                return;

            // 创建临时 Entity 来传递日志请求
            var em = defaultWorld.EntityManager;
            var entity = em.CreateEntity();

            var request = new LogRequestComponent
            {
                Level = level,
                Message = message,
                Category = new FixedString64Bytes(category.ToString()),
                Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            em.AddComponentData(entity, request);
        }

        /// <summary>记录调试信息</summary>
        public static void Debug<T>(FixedString512Bytes message, T category) where T : unmanaged
        {
            Log(EEchoLogLevel.Debug, message, category);
        }

        /// <summary>记录一般信息</summary>
        public static void Info<T>(FixedString512Bytes message, T category) where T : unmanaged
        {
            Log(EEchoLogLevel.Info, message, category);
        }

        /// <summary>记录警告</summary>
        public static void Warning<T>(FixedString512Bytes message, T category) where T : unmanaged
        {
            Log(EEchoLogLevel.Warning, message, category);
        }

        /// <summary>记录错误</summary>
        public static void Error<T>(FixedString512Bytes message, T category) where T : unmanaged
        {
            Log(EEchoLogLevel.Error, message, category);
        }

        // ========== 格式化帮助方法 ==========

        /// <summary>格式化 float3</summary>
        public static FixedString512Bytes FormatFloat3(float3 value)
        {
            var fs = new FixedString512Bytes();
            fs.Append("(");
            fs.Append(value.x);
            fs.Append(", ");
            fs.Append(value.y);
            fs.Append(", ");
            fs.Append(value.z);
            fs.Append(")");
            return fs;
        }

        /// <summary>格式化 float2</summary>
        public static FixedString512Bytes FormatFloat2(float2 value)
        {
            var fs = new FixedString512Bytes();
            fs.Append("(");
            fs.Append(value.x);
            fs.Append(", ");
            fs.Append(value.y);
            fs.Append(")");
            return fs;
        }

        /// <summary>格式化 int2</summary>
        public static FixedString512Bytes FormatInt2(int2 value)
        {
            var fs = new FixedString512Bytes();
            fs.Append("(");
            fs.Append(value.x);
            fs.Append(", ");
            fs.Append(value.y);
            fs.Append(")");
            return fs;
        }

        /// <summary>格式化 int3</summary>
        public static FixedString512Bytes FormatInt3(int3 value)
        {
            var fs = new FixedString512Bytes();
            fs.Append("(");
            fs.Append(value.x);
            fs.Append(", ");
            fs.Append(value.y);
            fs.Append(", ");
            fs.Append(value.z);
            fs.Append(")");
            return fs;
        }

        /// <summary>格式化整数</summary>
        public static FixedString512Bytes FormatInt(int value)
        {
            var fs = new FixedString512Bytes();
            fs.Append(value);
            return fs;
        }

        /// <summary>格式化浮点数</summary>
        public static FixedString512Bytes FormatFloat(float value)
        {
            var fs = new FixedString512Bytes();
            fs.Append(value);
            return fs;
        }
    }
}
