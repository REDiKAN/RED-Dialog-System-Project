using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for saving Unity Console logs to text files with filtering and statistics
/// </summary>
public class ConsoleLogSaver : EditorWindow
{
    #region Data Structures
    /// <summary>
    /// Represents a single console log message
    /// </summary>
    [System.Serializable]
    private class LogMessage
    {
        public string Message;
        public string StackTrace;
        public LogType Type;
        public DateTime Time;

        public override string ToString()
        {
            return $"[{Time:HH:mm:ss}] [{Type}] {Message}";
        }
    }

    /// <summary>
    /// Statistics about console logs
    /// </summary>
    private class LogStatistics
    {
        public int TotalMessages { get; set; }
        public int LogCount { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public int FilteredCount { get; set; }
        public float EstimatedSizeKB { get; set; }

        public void Clear()
        {
            TotalMessages = 0;
            LogCount = 0;
            WarningCount = 0;
            ErrorCount = 0;
            FilteredCount = 0;
            EstimatedSizeKB = 0;
        }

        public override string ToString()
        {
            return $"Total: {TotalMessages}, Logs: {LogCount}, Warnings: {WarningCount}, Errors: {ErrorCount}";
        }
    }

    /// <summary>
    /// Represents a saved log file in history
    /// </summary>
    [System.Serializable]
    private class SavedFileInfo
    {
        public string FilePath;
        public string FileName;
        public string SaveTime;
        public int MessageCount;

        public DateTime GetDateTime()
        {
            DateTime.TryParse(SaveTime, out DateTime result);
            return result;
        }
    }
    #endregion

    #region Constants and Static Fields
    private const string LOGS_DIRECTORY = "Assets/ConsoleLogs/";
    private const string EDITOR_PREFS_KEY_PREFIX = "ConsoleLogSaver_";
    private const int MAX_HISTORY_COUNT = 5;
    private const float REFRESH_INTERVAL = 1.0f; // Seconds

    private static readonly List<LogMessage> allLogs = new List<LogMessage>();
    private static bool isInitialized = false;
    #endregion

    #region UI State
    private Vector2 scrollPosition;
    private bool includeLogs = true;
    private bool includeWarnings = true;
    private bool includeErrors = true;
    private int messageLimit = 0;
    private bool clearAfterSave = false;
    private bool newMessagesAvailable = false;
    private float lastRefreshTime = 0;
    private int lastLogCount = 0;
    private bool autoRefresh = true;
    #endregion

    #region Data
    private LogStatistics statistics = new LogStatistics();
    private List<SavedFileInfo> saveHistory = new List<SavedFileInfo>();
    #endregion

    #region Window Management
    [MenuItem("Tools/Console Log Saver")]
    public static void ShowWindow()
    {
        GetWindow<ConsoleLogSaver>("Console Log Saver");
    }

    private void OnEnable()
    {
        InitializeLogCapture();
        LoadSettings();
        RefreshStatistics();
        lastRefreshTime = (float)EditorApplication.timeSinceStartup;

        // Subscribe to update for auto-refresh
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        SaveSettings();
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        // Check for new messages periodically if auto-refresh is enabled
        if (autoRefresh && (EditorApplication.timeSinceStartup - lastRefreshTime > REFRESH_INTERVAL))
        {
            CheckForNewMessages();
            lastRefreshTime = (float)EditorApplication.timeSinceStartup;
        }
    }

    private void InitializeLogCapture()
    {
        if (!isInitialized)
        {
            // Capture logs from this point forward
            Application.logMessageReceived += HandleLogMessage;
            isInitialized = true;
        }
    }

    private void HandleLogMessage(string message, string stackTrace, LogType type)
    {
        allLogs.Add(new LogMessage
        {
            Message = message,
            StackTrace = stackTrace,
            Type = type,
            Time = DateTime.Now
        });

        // Keep only last 10000 messages to prevent memory issues
        if (allLogs.Count > 10000)
        {
            allLogs.RemoveRange(0, allLogs.Count - 10000);
        }
    }
    #endregion

    #region GUI Rendering
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        RenderAutoRefreshToggle();
        RenderFilterSettings();
        RenderQuickPresets();
        RenderPreviewStatistics();
        RenderActionButtons();
        RenderSaveHistory();

        EditorGUILayout.EndScrollView();
    }

    private void RenderAutoRefreshToggle()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Auto-refresh:", GUILayout.Width(80));
        autoRefresh = GUILayout.Toggle(autoRefresh, "");
        GUILayout.EndHorizontal();

        if (!autoRefresh && GUILayout.Button("Manual Refresh", GUILayout.Width(120)))
        {
            RefreshStatistics();
        }

        GUILayout.Space(10);
    }

    private void RenderFilterSettings()
    {
        GUILayout.Label("Log Filters", EditorStyles.boldLabel);

        // Filter toggles
        GUILayout.BeginHorizontal();
        includeLogs = GUILayout.Toggle(includeLogs, "📝 Logs", GUILayout.Width(100));
        includeWarnings = GUILayout.Toggle(includeWarnings, "⚠️ Warnings", GUILayout.Width(100));
        includeErrors = GUILayout.Toggle(includeErrors, "❌ Errors", GUILayout.Width(100));
        GUILayout.EndHorizontal();

        // Message limit
        GUILayout.BeginHorizontal();
        GUILayout.Label("Message Limit (0 = all):", GUILayout.Width(150));
        int newLimit = EditorGUILayout.IntField(messageLimit, GUILayout.Width(100));
        if (newLimit != messageLimit)
        {
            messageLimit = Mathf.Max(0, newLimit);
            RefreshStatistics();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
    }

    private void RenderQuickPresets()
    {
        GUILayout.Label("Quick Presets", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save All", GUILayout.Width(100)))
        {
            includeLogs = includeWarnings = includeErrors = true;
            RefreshStatistics();
        }

        if (GUILayout.Button("Errors Only", GUILayout.Width(100)))
        {
            includeLogs = includeWarnings = false;
            includeErrors = true;
            RefreshStatistics();
        }

        if (GUILayout.Button("Warnings & Errors", GUILayout.Width(120)))
        {
            includeLogs = false;
            includeWarnings = includeErrors = true;
            RefreshStatistics();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
    }

    private void RenderPreviewStatistics()
    {
        GUILayout.Label("Preview & Statistics", EditorStyles.boldLabel);

        // New messages indicator
        if (newMessagesAvailable)
        {
            EditorGUILayout.HelpBox("New messages available! Click 'Manual Refresh' or enable auto-refresh.", MessageType.Info);
        }

        // Statistics
        string statsText = $"Total captured: {allLogs.Count}\n" +
                          $"Will be saved: {statistics.FilteredCount}\n" +
                          $"Breakdown: Logs: {statistics.LogCount}, Warnings: {statistics.WarningCount}, Errors: {statistics.ErrorCount}\n" +
                          $"Estimated size: {statistics.EstimatedSizeKB:F2} KB";

        EditorGUILayout.HelpBox(statsText, MessageType.None);

        // Show sample of what will be saved
        if (statistics.FilteredCount > 0)
        {
            GUILayout.Label("Sample of messages to save:", EditorStyles.miniBoldLabel);

            var filteredLogs = GetFilteredLogs();
            int sampleCount = Mathf.Min(3, filteredLogs.Count);

            for (int i = 0; i < sampleCount; i++)
            {
                var log = filteredLogs[i];
                string preview = log.Message.Length > 50 ?
                    log.Message.Substring(0, 50) + "..." :
                    log.Message;

                GUILayout.Label($"  • [{log.Time:HH:mm:ss}] {preview}", EditorStyles.miniLabel);
            }

            if (filteredLogs.Count > sampleCount)
            {
                GUILayout.Label($"  ... and {filteredLogs.Count - sampleCount} more", EditorStyles.miniLabel);
            }
        }

        GUILayout.Space(10);
    }

    private void RenderActionButtons()
    {
        // Main save button
        bool canSave = statistics.FilteredCount > 0;
        EditorGUI.BeginDisabledGroup(!canSave);

        if (GUILayout.Button("SAVE CONSOLE LOG", GUILayout.Height(40)))
        {
            SaveConsoleLog();
        }

        EditorGUI.EndDisabledGroup();

        if (!canSave)
        {
            EditorGUILayout.HelpBox("No messages to save with current filters.", MessageType.Warning);
        }

        // Clear after save option
        clearAfterSave = GUILayout.Toggle(clearAfterSave, "Clear captured logs after save");

        GUILayout.Space(10);
    }

    private void RenderSaveHistory()
    {
        if (saveHistory.Count == 0) return;

        GUILayout.Label("Recent Saves", EditorStyles.boldLabel);

        for (int i = 0; i < saveHistory.Count; i++)
        {
            var fileInfo = saveHistory[i];

            GUILayout.BeginHorizontal();

            // Display file info
            string displayName = fileInfo.FileName.Length > 30 ?
                "..." + fileInfo.FileName.Substring(fileInfo.FileName.Length - 27) :
                fileInfo.FileName;

            string timeDisplay = DateTime.TryParse(fileInfo.SaveTime, out DateTime time) ?
                time.ToString("HH:mm:ss") :
                fileInfo.SaveTime;

            GUILayout.Label($"{i + 1}. {displayName} ({timeDisplay})", GUILayout.Width(220));

            // Action buttons
            if (GUILayout.Button("Show", GUILayout.Width(50)))
            {
                if (File.Exists(fileInfo.FilePath))
                {
                    EditorUtility.RevealInFinder(fileInfo.FilePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("File Not Found",
                        $"The file no longer exists:\n{fileInfo.FilePath}",
                        "OK");
                }
            }

            if (GUILayout.Button("Open", GUILayout.Width(50)))
            {
                if (File.Exists(fileInfo.FilePath))
                {
                    System.Diagnostics.Process.Start(fileInfo.FilePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("File Not Found",
                        $"The file no longer exists:\n{fileInfo.FilePath}",
                        "OK");
                }
            }

            GUILayout.EndHorizontal();
        }
    }
    #endregion

    #region Core Functionality
    private void CheckForNewMessages()
    {
        // Since we're capturing logs in real-time, just check if count changed
        if (allLogs.Count != lastLogCount)
        {
            newMessagesAvailable = true;
            lastLogCount = allLogs.Count;
            Repaint();
        }
    }

    private void RefreshStatistics()
    {
        statistics.Clear();
        statistics.TotalMessages = allLogs.Count;

        // Count by type
        foreach (var log in allLogs)
        {
            switch (log.Type)
            {
                case LogType.Log:
                    statistics.LogCount++;
                    break;
                case LogType.Warning:
                    statistics.WarningCount++;
                    break;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    statistics.ErrorCount++;
                    break;
            }
        }

        // Apply filters and count filtered messages
        var filteredLogs = GetFilteredLogs();
        statistics.FilteredCount = filteredLogs.Count;

        // Estimate file size
        int estimatedChars = 0;
        foreach (var log in filteredLogs)
        {
            estimatedChars += log.Message.Length + 20; // For timestamp and formatting
            if (!string.IsNullOrEmpty(log.StackTrace))
                estimatedChars += log.StackTrace.Length;
        }
        statistics.EstimatedSizeKB = estimatedChars / 1024f;

        newMessagesAvailable = false;
        Repaint();
    }

    private List<LogMessage> GetFilteredLogs()
    {
        var filtered = new List<LogMessage>();

        foreach (var log in allLogs)
        {
            bool shouldInclude = false;

            switch (log.Type)
            {
                case LogType.Log when includeLogs:
                case LogType.Warning when includeWarnings:
                case LogType.Error when includeErrors:
                case LogType.Exception when includeErrors:
                case LogType.Assert when includeErrors:
                    shouldInclude = true;
                    break;
            }

            if (shouldInclude)
            {
                filtered.Add(log);
            }
        }

        // Apply message limit if specified (take last N messages)
        if (messageLimit > 0 && filtered.Count > messageLimit)
        {
            filtered = filtered.Skip(filtered.Count - messageLimit).ToList();
        }

        return filtered;
    }

    private void SaveConsoleLog()
    {
        var filteredLogs = GetFilteredLogs();

        if (filteredLogs.Count == 0)
        {
            EditorUtility.DisplayDialog("No Messages",
                "No messages match the current filters. Nothing to save.",
                "OK");
            return;
        }

        // Ensure directory exists
        if (!Directory.Exists(LOGS_DIRECTORY))
        {
            Directory.CreateDirectory(LOGS_DIRECTORY);
            AssetDatabase.Refresh();
        }

        // Generate filename with timestamp
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"ConsoleLog_{timestamp}.txt";
        string filePath = Path.Combine(LOGS_DIRECTORY, fileName);

        try
        {
            // Build file content
            StringBuilder content = new StringBuilder();

            // Header
            content.AppendLine("// ===== Console Log Export =====");
            content.AppendLine($"// Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($"// Filters: Logs: {(includeLogs ? "✓" : "✗")}, " +
                              $"Warnings: {(includeWarnings ? "✓" : "✗")}, " +
                              $"Errors: {(includeErrors ? "✓" : "✗")}");
            content.AppendLine($"// Message Limit: {messageLimit} (0 = unlimited)");
            content.AppendLine($"// Total messages captured: {allLogs.Count}");
            content.AppendLine($"// Messages saved: {filteredLogs.Count} " +
                              $"(Logs: {statistics.LogCount}, " +
                              $"Warnings: {statistics.WarningCount}, " +
                              $"Errors: {statistics.ErrorCount})");
            content.AppendLine("// ==============================");
            content.AppendLine();

            // Messages (in chronological order)
            for (int i = 0; i < filteredLogs.Count; i++)
            {
                var log = filteredLogs[i];
                content.AppendLine($"[{log.Time:yyyy-MM-dd HH:mm:ss}] [{log.Type}]");
                content.AppendLine(log.Message);

                if (!string.IsNullOrEmpty(log.StackTrace))
                {
                    content.AppendLine("Stack Trace:");
                    content.AppendLine(log.StackTrace);
                }

                if (i < filteredLogs.Count - 1)
                {
                    content.AppendLine(new string('-', 60));
                }
            }

            // Footer with statistics
            content.AppendLine();
            content.AppendLine("// ===== End of Log =====");
            content.AppendLine($"// Total saved: {filteredLogs.Count} messages");
            content.AppendLine($"// File size: {GetFileSizeString(filePath, content.Length)}");
            content.AppendLine($"// Generated by Console Log Saver");

            // Write to file
            File.WriteAllText(filePath, content.ToString(), Encoding.UTF8);

            // Add to history
            AddToHistory(new SavedFileInfo
            {
                FilePath = filePath,
                FileName = fileName,
                SaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                MessageCount = filteredLogs.Count
            });

            // Clear captured logs if requested
            if (clearAfterSave)
            {
                allLogs.Clear();
                RefreshStatistics();
            }

            // Refresh asset database
            AssetDatabase.Refresh();

            // Show success dialog
            EditorUtility.DisplayDialog("Success",
                $"Console log saved successfully!\n\n" +
                $"File: {fileName}\n" +
                $"Messages saved: {filteredLogs.Count}\n" +
                $"Location: {LOGS_DIRECTORY}",
                "OK");

            // Update statistics
            RefreshStatistics();
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error",
                $"Failed to save console log:\n{e.Message}",
                "OK");
            Debug.LogError($"Console Log Saver error: {e}");
        }
    }

    private string GetFileSizeString(string filePath, int contentLength)
    {
        // Estimate based on content length (UTF-8: 1-4 bytes per char, average ~1.5)
        double estimatedBytes = contentLength * 1.5;

        if (estimatedBytes < 1024)
            return $"{estimatedBytes:F0} B";
        else if (estimatedBytes < 1024 * 1024)
            return $"{(estimatedBytes / 1024):F2} KB";
        else
            return $"{(estimatedBytes / (1024 * 1024)):F2} MB";
    }
    #endregion

    #region Helper Methods
    private void AddToHistory(SavedFileInfo fileInfo)
    {
        saveHistory.Insert(0, fileInfo);

        // Keep only the latest files
        if (saveHistory.Count > MAX_HISTORY_COUNT)
        {
            saveHistory.RemoveRange(MAX_HISTORY_COUNT, saveHistory.Count - MAX_HISTORY_COUNT);
        }

        SaveHistoryToPrefs();
    }
    #endregion

    #region Settings Persistence
    private void LoadSettings()
    {
        includeLogs = EditorPrefs.GetBool(EDITOR_PREFS_KEY_PREFIX + "IncludeLogs", true);
        includeWarnings = EditorPrefs.GetBool(EDITOR_PREFS_KEY_PREFIX + "IncludeWarnings", true);
        includeErrors = EditorPrefs.GetBool(EDITOR_PREFS_KEY_PREFIX + "IncludeErrors", true);
        messageLimit = EditorPrefs.GetInt(EDITOR_PREFS_KEY_PREFIX + "MessageLimit", 0);
        clearAfterSave = EditorPrefs.GetBool(EDITOR_PREFS_KEY_PREFIX + "ClearAfterSave", false);
        autoRefresh = EditorPrefs.GetBool(EDITOR_PREFS_KEY_PREFIX + "AutoRefresh", true);

        LoadHistoryFromPrefs();
    }

    private void SaveSettings()
    {
        EditorPrefs.SetBool(EDITOR_PREFS_KEY_PREFIX + "IncludeLogs", includeLogs);
        EditorPrefs.SetBool(EDITOR_PREFS_KEY_PREFIX + "IncludeWarnings", includeWarnings);
        EditorPrefs.SetBool(EDITOR_PREFS_KEY_PREFIX + "IncludeErrors", includeErrors);
        EditorPrefs.SetInt(EDITOR_PREFS_KEY_PREFIX + "MessageLimit", messageLimit);
        EditorPrefs.SetBool(EDITOR_PREFS_KEY_PREFIX + "ClearAfterSave", clearAfterSave);
        EditorPrefs.SetBool(EDITOR_PREFS_KEY_PREFIX + "AutoRefresh", autoRefresh);

        SaveHistoryToPrefs();
    }

    private void LoadHistoryFromPrefs()
    {
        saveHistory.Clear();

        string historyJson = EditorPrefs.GetString(EDITOR_PREFS_KEY_PREFIX + "History", "");
        if (!string.IsNullOrEmpty(historyJson))
        {
            try
            {
                var historyArray = JsonUtility.FromJson<SavedFileInfoArray>(
                    "{\"array\":" + historyJson + "}");

                if (historyArray != null && historyArray.array != null)
                {
                    saveHistory = historyArray.array
                        .Where(f => File.Exists(f.FilePath))
                        .Take(MAX_HISTORY_COUNT)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load save history: {e.Message}");
            }
        }
    }

    private void SaveHistoryToPrefs()
    {
        try
        {
            var historyArray = new SavedFileInfoArray { array = saveHistory.ToArray() };
            string historyJson = JsonUtility.ToJson(historyArray);

            // Remove the wrapper
            if (historyJson.StartsWith("{\"array\":") && historyJson.EndsWith("}"))
            {
                historyJson = historyJson.Substring(9, historyJson.Length - 10);
            }

            EditorPrefs.SetString(EDITOR_PREFS_KEY_PREFIX + "History", historyJson);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to save history: {e.Message}");
        }
    }

    [System.Serializable]
    private class SavedFileInfoArray
    {
        public SavedFileInfo[] array;
    }
    #endregion
}