# Changelog

## [1.1.0] - 2026-02-01

### Changed
- **BREAKING**: 所有公共类型添加 Echo 前缀以提升命名空间清晰度
  - `Logger` → `EchoLogger`
  - `LogConfig` → `EchoLogConfig`
  - `ELogLevel` → `EEchoLogLevel`
  - `StructuredLogMessage` → `EchoStructuredLogMessage`
  - `MessageFormatter` → `EchoMessageFormatter`
  - `DOTSLogger` → `EchoDOTSLogger`

### Fixed
- 修复 `MinEEchoLogLevel` 重复 E 前缀问题 → `MinEchoLogLevel`
- 修复 AsyncLogQueue 在编辑器模式下的 DontSaveInEditor 断言错误
- 添加编辑器模式下的 GameObject hideFlags 设置

### Added
- 为重命名的文件添加 Unity .meta 文件

## [1.0.0] - 2025-01-31

### Added
- 初始发布
- 零 GC 分配的日志记录
- Unity DOTS (ECS) 支持
- 异步日志写入
- 结构化日志支持
- 敏感信息过滤
- 文件日志压缩
- 关键日志分类
- 性能监控
