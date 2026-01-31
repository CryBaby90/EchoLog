using UnityEngine;
using UnityEditor;

namespace EchoLog.Editor
{
    /// <summary>日志配置创建和检查工具</summary>
    public class LoggerConfigEditor
    {
        private const string DefaultConfigPath = "Assets/Resources/DefaultLogConfig.asset";

        [MenuItem("AbyssTavern/Logger/Create Default Log Config")]
        public static void CreateDefaultLogConfig()
        {
            // 检查是否已存在
            if (AssetDatabase.LoadAssetAtPath<LogConfig>(DefaultConfigPath) != null)
            {
                if (EditorUtility.DisplayDialog("配置已存在", "默认 LogConfig 已存在，是否覆盖？", "覆盖", "取消"))
                {
                    AssetDatabase.DeleteAsset(DefaultConfigPath);
                }
                else
                {
                    return;
                }
            }

            // 创建配置
            LogConfig config = ScriptableObject.CreateInstance<LogConfig>();
            config.MinLogLevel = ELogLevel.Info;
            config.EnableAsync = true;
            config.EnableUnityConsole = true;
            config.EnableFileOutput = true;
            config.LogDirectory = "Logs/GameLogs";
            config.MaxFileSizeMB = 10;
            config.MaxFiles = 5;
            config.EnableCompression = true;
            config.EnableStackTrace = true;
            config.EnableFileStackTrace = false;
            config.EnablePerformanceLogging = false;
            config.EnableSensitiveFilter = true;
            config.SensitiveKeywords = new[] { "password", "token", "key" };
            config.CriticalCategories = new[] { "System", "GameFlow", "Network" };
            config.ReleaseMinLevel = ELogLevel.Error;

            // 保存配置
            AssetDatabase.CreateAsset(config, DefaultConfigPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = config;
            EditorUtility.DisplayDialog("成功", $"默认 LogConfig 已创建:\n{DefaultConfigPath}", "确定");
        }

        [MenuItem("AbyssTavern/Logger/Select Default Log Config")]
        public static void SelectDefaultLogConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<LogConfig>(DefaultConfigPath);
            if (config != null)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
            else
            {
                EditorUtility.DisplayDialog("未找到", "默认 LogConfig 不存在，请先创建", "确定");
            }
        }
    }
}
