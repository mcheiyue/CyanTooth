using System;
using System.IO;

namespace CyanTooth.Core.Helpers;

/// <summary>
/// 增强的线程安全日志器，支持 AppData 存储和简单的日志滚动
/// </summary>
public static class DebugLogger
{
    private static readonly object _lock = new();
    private static readonly string _logDirectory = string.Empty;
    private static readonly string _logPath = "debug.log";
    private const long MaxLogSize = 10 * 1024 * 1024; // 10MB
    private const int MaxArchiveFiles = 5;

    static DebugLogger()
    {
        try
        {
            // 强制统一使用 LocalApplicationData
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _logDirectory = Path.Combine(appData, "CyanTooth", "logs");
            
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            _logPath = Path.Combine(_logDirectory, "debug.log");
            
            // 启动时立即尝试写入，不留空白期
            File.AppendAllText(_logPath, $"{Environment.NewLine}>>>> [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [STARTUP] CyanTooth 启动初始化...{Environment.NewLine}");
            Log("日志引擎准备就绪。");
        }
        catch (Exception ex)
        {
            // 最后的防线
            _logPath = "debug_fallback.log";
            try { File.AppendAllText(_logPath, $"日志初始化失败: {ex.Message}"); } catch { }
        }
    }

    public static void Log(string message, string level = "INFO")
    {
        try
        {
            lock (_lock)
            {
                RotateLogFilesIfNeeded();
                File.AppendAllText(_logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level.ToUpper()}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // 忽略日志写入错误
        }
    }

    public static void LogError(string message, Exception? ex = null)
    {
        string fullMessage = ex != null ? $"{message}{Environment.NewLine}Exception: {ex}" : message;
        Log(fullMessage, "ERROR");
    }

    private static void RotateLogFilesIfNeeded()
    {
        try
        {
            FileInfo fileInfo = new FileInfo(_logPath);
            if (!fileInfo.Exists || fileInfo.Length < MaxLogSize)
            {
                return;
            }

            // 滚动旧日志: debug.4.log -> debug.5.log, ..., debug.log -> debug.1.log
            for (int i = MaxArchiveFiles - 1; i >= 1; i--)
            {
                string oldFile = Path.Combine(_logDirectory, $"debug.{i}.log");
                string newFile = Path.Combine(_logDirectory, $"debug.{i + 1}.log");
                if (File.Exists(oldFile))
                {
                    File.Move(oldFile, newFile, true);
                }
            }

            string firstArchive = Path.Combine(_logDirectory, "debug.1.log");
            File.Move(_logPath, firstArchive, true);
        }
        catch
        {
            // 滚动失败则继续写入当前文件或清空
            try { File.WriteAllText(_logPath, string.Empty); } catch { }
        }
    }
}
