using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// Editor window for combining C# scripts with various encoding options and statistics
/// </summary>
public class ScriptCombiner : EditorWindow
{
    private Vector2 scrollPosition;
    private List<string> selectedPaths = new List<string>();
    private Encoding selectedEncoding = Encoding.UTF8;
    private ScriptStatistics statistics = new ScriptStatistics();

    [MenuItem("Tools/Combine Scripts (With Selection)")]
    public static void ShowWindow()
    {
        GetWindow<ScriptCombiner>("Script Combiner");
    }

    #region GUI Rendering
    private void OnGUI()
    {
        RenderEncodingSelection();
        RenderSelectedPaths();
        RenderActionButtons();
        RenderStatistics();
        RenderCombineButton();
    }

    private void RenderEncodingSelection()
    {
        GUILayout.Label("Encoding Selection:", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("UTF-8")) selectedEncoding = Encoding.UTF8;
        if (GUILayout.Button("ANSI (Default)")) selectedEncoding = Encoding.Default;
        if (GUILayout.Button("Windows-1251")) selectedEncoding = Encoding.GetEncoding(1251);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    private void RenderSelectedPaths()
    {
        GUILayout.Label("Selected Files/Folders:", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
        for (int i = 0; i < selectedPaths.Count; i++)
        {
            EditorGUILayout.LabelField($"{i + 1}. {selectedPaths[i]}");
        }
        EditorGUILayout.EndScrollView();
    }

    private void RenderActionButtons()
    {
        if (GUILayout.Button("Add Selected in Project"))
        {
            AddSelectedInProject();
        }

        if (GUILayout.Button("Add Folder"))
        {
            AddFolder();
        }

        if (GUILayout.Button("Clear Selection"))
        {
            selectedPaths.Clear();
            statistics.Clear();
        }
        GUILayout.Space(10);
    }

    private void RenderStatistics()
    {
        GUILayout.Label("Statistics:", EditorStyles.boldLabel);
        GUILayout.Label($"Total Files: {statistics.TotalFiles}");
        GUILayout.Label($"Total Size: {statistics.TotalSizeKB:F2} KB");
        GUILayout.Label($"Total Lines: {statistics.TotalLines}");
        GUILayout.Label($"Classes: {statistics.TotalClasses}");
        GUILayout.Label($"Methods: {statistics.TotalMethods}");
        GUILayout.Label($"Comments: {statistics.TotalComments}");
        GUILayout.Space(10);
    }

    private void RenderCombineButton()
    {
        if (GUILayout.Button("Combine Selected Scripts"))
        {
            if (selectedPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("Info", "Please select files or folders first", "OK");
                return;
            }
            CombineSelectedScripts();
        }
    }
    #endregion

    #region Path Management
    private void AddSelectedInProject()
    {
        bool added = false;
        foreach (UnityEngine.Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) &&
                (Directory.Exists(path) || IsCSharpFile(path)))
            {
                if (!selectedPaths.Contains(path))
                {
                    selectedPaths.Add(path);
                    added = true;
                }
            }
        }

        if (added) UpdateStatistics();
    }

    private void AddFolder()
    {
        string folder = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
        if (!string.IsNullOrEmpty(folder))
        {
            string relativePath = folder.StartsWith(Application.dataPath) ?
                "Assets" + folder.Substring(Application.dataPath.Length) : folder;

            if (!selectedPaths.Contains(relativePath))
            {
                selectedPaths.Add(relativePath);
                UpdateStatistics();
            }
        }
    }
    #endregion

    #region Core Functionality
    private void UpdateStatistics()
    {
        statistics.Clear();
        var processor = new ScriptProcessor();

        foreach (string path in selectedPaths)
        {
            string fullPath = ConvertToFullPath(path);

            if (Directory.Exists(fullPath))
            {
                foreach (string file in Directory.GetFiles(fullPath, "*.cs", SearchOption.AllDirectories))
                {
                    statistics.Add(processor.ProcessFile(file));
                }
            }
            else if (File.Exists(fullPath) && IsCSharpFile(fullPath))
            {
                statistics.Add(processor.ProcessFile(fullPath));
            }
        }

        Repaint();
    }

    private void CombineSelectedScripts()
    {
        var processor = new ScriptProcessor();
        var combinedScripts = processor.CombineScripts(selectedPaths, statistics, selectedEncoding);

        if (combinedScripts != null)
        {
            EditorUtility.RevealInFinder(combinedScripts);
            EditorUtility.DisplayDialog("Success",
                $"Scripts combined successfully!\n\nStatistics:\n- Files: {statistics.TotalFiles}\n- Size: {statistics.TotalSizeKB:F2} KB\n- Lines: {statistics.TotalLines}\n- Classes: {statistics.TotalClasses}\n- Methods: {statistics.TotalMethods}\n- Comments: {statistics.TotalComments}",
                "OK");
        }
    }
    #endregion

    #region Helper Methods
    private string ConvertToFullPath(string path)
    {
        return path.StartsWith("Assets/") ?
            Path.Combine(Application.dataPath, path.Substring(7)) :
            path;
    }

    private bool IsCSharpFile(string path)
    {
        return Path.GetExtension(path).ToLower() == ".cs";
    }
    #endregion
}

/// <summary>
/// Responsible for processing and analyzing C# script files
/// </summary>
public class ScriptProcessor
{
    /// <summary>
    /// Processes a single file and returns its statistics
    /// </summary>
    public FileStatistics ProcessFile(string filePath)
    {
        try
        {
            string content = FileReader.ReadFileWithAutoEncoding(filePath);
            FileInfo fileInfo = new FileInfo(filePath);

            return new FileStatistics
            {
                FilePath = filePath,
                SizeBytes = fileInfo.Length,
                LineCount = content.Split('\n').Length,
                ClassCount = CountOccurrences(content, "class "),
                MethodCount = CountMethods(content),
                CommentCount = CountComments(content)
            };
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error processing file {filePath}: {e.Message}");
            return new FileStatistics();
        }
    }

    /// <summary>
    /// Combines multiple scripts into a single file with statistics
    /// </summary>
    public string CombineScripts(List<string> paths, ScriptStatistics statistics, Encoding encoding)
    {
        var allScriptPaths = CollectScriptPaths(paths);

        if (allScriptPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "No .cs files found to combine", "OK");
            return null;
        }

        StringBuilder combinedText = BuildCombinedText(allScriptPaths, statistics);
        string outputPath = Path.Combine(Application.dataPath, $"CombinedScripts_{encoding.EncodingName}.txt");

        try
        {
            File.WriteAllText(outputPath, combinedText.ToString(), encoding);
            return outputPath;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error writing combined file: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Error writing file: {e.Message}", "OK");
            return null;
        }
    }

    private List<string> CollectScriptPaths(List<string> paths)
    {
        var allScriptPaths = new List<string>();

        foreach (string path in paths)
        {
            string fullPath = path.StartsWith("Assets/") ?
                Path.Combine(Application.dataPath, path.Substring(7)) :
                path;

            if (Directory.Exists(fullPath))
            {
                allScriptPaths.AddRange(Directory.GetFiles(fullPath, "*.cs", SearchOption.AllDirectories));
            }
            else if (File.Exists(fullPath) && Path.GetExtension(fullPath).ToLower() == ".cs")
            {
                allScriptPaths.Add(fullPath);
            }
        }

        return allScriptPaths.Distinct().ToList();
    }

    private StringBuilder BuildCombinedText(List<string> scriptPaths, ScriptStatistics statistics)
    {
        StringBuilder combinedText = new StringBuilder();

        // Header
        combinedText.AppendLine("// ===== Combined Scripts Header =====");
        combinedText.AppendLine($"// Generation Time: {System.DateTime.Now}");
        combinedText.AppendLine($"// Total Files: {scriptPaths.Count}");
        combinedText.AppendLine("// =================================");
        combinedText.AppendLine();

        // Content
        for (int i = 0; i < scriptPaths.Count; i++)
        {
            string scriptPath = scriptPaths[i];
            try
            {
                string content = FileReader.ReadFileWithAutoEncoding(scriptPath);
                combinedText.AppendLine($"//==== File {i + 1} of {scriptPaths.Count}: {scriptPath} ====");
                combinedText.AppendLine(content);
                combinedText.AppendLine();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error reading file {scriptPath}: {e.Message}");
            }
        }

        // Statistics
        combinedText.AppendLine();
        combinedText.AppendLine("// ============ Statistics =============");
        combinedText.AppendLine($"// Total Files: {statistics.TotalFiles}");
        combinedText.AppendLine($"// Total Size: {statistics.TotalSizeKB:F2} KB");
        combinedText.AppendLine($"// Total Lines: {statistics.TotalLines}");
        combinedText.AppendLine($"// Classes: {statistics.TotalClasses}");
        combinedText.AppendLine($"// Methods: {statistics.TotalMethods}");
        combinedText.AppendLine($"// Comments: {statistics.TotalComments}");
        combinedText.AppendLine("// =====================================");

        return combinedText;
    }

    #region Analysis Methods
    private int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, System.StringComparison.Ordinal)) != -1)
        {
            index += pattern.Length;
            count++;
        }
        return count;
    }

    private int CountMethods(string content)
    {
        return content.Split('\n')
            .Count(line => line.Trim().Contains("(") &&
                          line.Trim().Contains(")") &&
                          IsMethodSignature(line.Trim()));
    }

    private bool IsMethodSignature(string line)
    {
        string[] returnTypes = { "void", "int", "string", "float", "bool", "double", "object" };
        string[] excludedKeywords = { "class", "struct", "interface", "enum" };

        return returnTypes.Any(rt => line.Contains(rt)) &&
               !excludedKeywords.Any(ek => line.Contains(ek));
    }

    private int CountComments(string content)
    {
        return content.Split('\n')
            .Count(line => line.Trim().StartsWith("//") ||
                          line.Trim().Contains("/*") ||
                          line.Trim().Contains("*/"));
    }
    #endregion
}

/// <summary>
/// Static class for handling file reading operations with automatic encoding detection
/// </summary>
public static class FileReader
{
    /// <summary>
    /// Reads a file with automatic encoding detection
    /// </summary>
    public static string ReadFileWithAutoEncoding(string path)
    {
        byte[] fileBytes = File.ReadAllBytes(path);

        // Check BOM markers
        if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(fileBytes, 3, fileBytes.Length - 3);
        }
        if (fileBytes.Length >= 2 && fileBytes[0] == 0xFF && fileBytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(fileBytes, 2, fileBytes.Length - 2);
        }
        if (fileBytes.Length >= 2 && fileBytes[0] == 0xFE && fileBytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(fileBytes, 2, fileBytes.Length - 2);
        }

        // Try different encodings
        Encoding[] encodingsToTry = { Encoding.UTF8, Encoding.GetEncoding(1251), Encoding.Default };

        foreach (var encoding in encodingsToTry)
        {
            try
            {
                string content = encoding.GetString(fileBytes);
                if (ContainsMeaningfulContent(content))
                {
                    return content;
                }
            }
            catch
            {
                // Continue to next encoding
            }
        }

        return Encoding.UTF8.GetString(fileBytes);
    }

    private static bool ContainsMeaningfulContent(string content)
    {
        return content.Any(c => c >= 'A' && c <= 'z') || // Latin letters
               content.Any(c => c >= 'À' && c <= 'ÿ');   // Cyrillic letters
    }
}

/// <summary>
/// Data structure for storing file statistics
/// </summary>
public struct FileStatistics
{
    public string FilePath;
    public long SizeBytes;
    public int LineCount;
    public int ClassCount;
    public int MethodCount;
    public int CommentCount;
}

/// <summary>
/// Data structure for storing combined script statistics
/// </summary>
public class ScriptStatistics
{
    public int TotalFiles { get; private set; }
    public long TotalSizeBytes { get; private set; }
    public int TotalLines { get; private set; }
    public int TotalClasses { get; private set; }
    public int TotalMethods { get; private set; }
    public int TotalComments { get; private set; }

    public float TotalSizeKB => TotalSizeBytes / 1024f;

    public void Clear()
    {
        TotalFiles = 0;
        TotalSizeBytes = 0;
        TotalLines = 0;
        TotalClasses = 0;
        TotalMethods = 0;
        TotalComments = 0;
    }

    public void Add(FileStatistics fileStats)
    {
        TotalFiles++;
        TotalSizeBytes += fileStats.SizeBytes;
        TotalLines += fileStats.LineCount;
        TotalClasses += fileStats.ClassCount;
        TotalMethods += fileStats.MethodCount;
        TotalComments += fileStats.CommentCount;
    }
}