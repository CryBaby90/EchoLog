# EchoLog

[TOC]

## 简介

EchoLog 是一个高性能的 Unity 日志系统，专为现代 Unity 项目设计。

**特性**：
- 零 GC 分配的日志记录
- 支持 Unity DOTS (ECS)
- 异步日志写入
- 结构化日志
- 敏感信息过滤
- 文件日志压缩
- 关键日志分类
- 性能监控

## 安装

### 通过 Git URL 安装

在 Unity Package Manager 中添加：

```
https://github.com/CryBaby90/EchoLog.git#package
```

### 依赖

- Unity 6000.0+
- com.unity.collections 2.5.1+
- com.unity.entities 1.3.2+
- com.unity.mathematics 1.4.0+

## 快速开始

### 1. 创建日志配置

在 `Assets/Resources` 目录下创建 LogConfig：

```
Assets/Resources/DefaultLogConfig.asset
```

在 Inspector 中配置：
- 最低日志级别
- 启用异步写入
- 启用文件输出
- 配置日志目录

### 2. 初始化日志系统

```csharp
using EchoLog;

// 加载配置
LogConfig config = Resources.Load<LogConfig>("DefaultLogConfig");

// 初始化
Logger.Initialize(config);
```

### 3. 记录日志

```csharp
// 基础日志
Logger.Debug("调试信息");
Logger.Info("一般信息");
Logger.Warning("警告信息");
Logger.Error("错误信息");
Logger.Fatal("致命错误");

// 带分类的日志
Logger.Log(ELogLevel.Info, "玩家进入战斗", "Combat");

// 关键日志（无视级别）
Logger.Critical("游戏保存成功", "SaveGame");

// 结构化日志（零 GC）
var msg = StructuredLogMessage.Format("生命值: {0}/{1}", health, maxHealth);
Logger.InfoStructured(in msg);
```

## API 文档

### 日志级别

```csharp
public enum ELogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Fatal = 4
}
```

### 配置选项

| 选项 | 说明 | 默认值 |
|------|------|--------|
| MinLogLevel | 最低日志级别 | Info |
| EnableAsync | 启用异步写入 | true |
| QueueSize | 异步队列大小 | 1000 |
| EnableUnityConsole | 启用 Unity Console | true |
| EnableStackTrace | 启用堆栈跟踪 | true |
| EnableFileOutput | 启用文件输出 | false |
| MaxFileSizeMB | 单文件最大大小（MB） | 10 |
| MaxFiles | 最大保留文件数 | 5 |
| EnableCompression | 启用日志压缩 | true |

## DOTS 日志

EchoLog 提供了专为 Unity DOTS 设计的日志系统：

```csharp
using EchoLog;

// 初始化 DOTS 日志
DOTSLogger.Initialize(World.DefaultGameObjectInjectionWorld);

// 使用 FixedString 记录日志（零 GC）
var message = new FixedString512Bytes();
message.Append("Player position: ");
message.Append(DOTSLogger.FormatFloat3(playerPosition));

DOTSLogger.Info(message, (FixedString64Bytes)"GamePlay");

// 快捷方法
DOTSLogger.Debug(message);
DOTSLogger.Warning(message);
DOTSLogger.Error(message);
```

## 性能优化

EchoLog 设计为零 GC 分配：

- 使用 `ref struct` 避免堆分配
- 延迟求值日志消息
- 缓存正则表达式
- 异步写入队列

### 性能基准

| 操作 | GC 分配 |
|------|---------|
| 基础日志 | ~0 bytes |
| 结构化日志 | ~0 bytes |
| 级别过滤 | 0 bytes |

## 许可证

MIT License
