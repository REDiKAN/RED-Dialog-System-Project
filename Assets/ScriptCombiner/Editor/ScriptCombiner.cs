using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using File = System.IO.File;
using Directory = System.IO.Directory;
using Path = System.IO.Path;
using System.IO;

/// <summary>
/// Editor window for combining C# scripts with advanced processing options
/// </summary>
public class ScriptCombiner : EditorWindow
{
    // === GUI State ===
    private Vector2 scrollPosition;
    private Vector2 previewScrollPosition;
    private List<string> selectedPaths = new List<string>();
    private Encoding selectedEncoding = Encoding.UTF8;
    private ScriptStatistics statistics = new ScriptStatistics();

    // Preview State
    private string previewContent = "";
    private int selectedTab = 0;
    private string[] tabTitles = { "Configuration", "Preview" };

    // === Feature Toggles ===
    private bool consolidateUsings = true;
    private bool enableExclusions = false;
    private string exclusionPatterns = "Test, Temp, AssemblyInfo";

    private bool cleanupCode = false;
    private bool removeComments = false;
    private bool removeEmptyLines = false;
    private bool removeRegions = false;

    private bool detailedStats = false;

    [MenuItem("Tools/Combine Scripts (With Selection)")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScriptCombiner>("Script Combiner");
        window.minSize = new Vector2(450, 600);
    }

    #region GUI Rendering
    private void OnGUI()
    {
        // Main Toolbar
        selectedTab = GUILayout.Toolbar(selectedTab, tabTitles);

        if (selectedTab == 0)
        {
            RenderConfigTab();
        }
        else
        {
            RenderPreviewTab();
        }
    }

    private void RenderConfigTab()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Spacing from top
        GUILayout.Space(5);

        // --- SECTION 1: SETTINGS ---
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        RenderSectionHeader("⚙️ Generation Settings");

        GUILayout.Space(5);
        RenderEncodingSelection();
        EditorGUILayout.Space(5);
        RenderAdvancedOptions();
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // --- SECTION 2: SELECTION ---
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        RenderSectionHeader("📂 File Selection");

        RenderSelectedPaths();
        GUILayout.Space(5);
        RenderActionButtons();
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // --- SECTION 3: OUTPUT ---
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        RenderSectionHeader("📊 Statistics & Output");

        RenderStatistics();
        GUILayout.Space(5);
        RenderOutputButtons();
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }

    private void RenderPreviewTab()
    {
        EditorGUILayout.HelpBox("Preview of the generated text. Remember to click 'Regenerate' if you changed settings.", MessageType.Info);
        GUILayout.Space(5);

        if (GUILayout.Button("Regenerate Preview", GUILayout.Height(30)))
        {
            GeneratePreviewContent();
        }

        GUILayout.Space(5);

        // Use a different style to make it look like a code editor
        previewScrollPosition = EditorGUILayout.BeginScrollView(previewScrollPosition, GUILayout.ExpandHeight(true));
        EditorGUI.BeginDisabledGroup(true);

        // Custom style for code area
        var textStyle = new GUIStyle(EditorStyles.textArea)
        {
            fontSize = 12,
            font = EditorStyles.label.font // Standard editor font
        };
        EditorGUILayout.TextArea(previewContent, textStyle, GUILayout.ExpandHeight(true));

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndScrollView();
    }

    // Helper for nice headers
    private void RenderSectionHeader(string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        // Optional: Draw a line
        Rect rect = GUILayoutUtility.GetLastRect();
        rect.y += rect.height - 2;
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    private void RenderAdvancedOptions()
    {
        // 1. Smart Usings
        consolidateUsings = EditorGUILayout.Toggle(new GUIContent("Consolidate Usings", "Group all 'using' statements at the top and remove duplicates"), consolidateUsings);

        // 2. Exclusions
        enableExclusions = EditorGUILayout.Toggle(new GUIContent("Enable Exclusions", "Ignore files matching patterns"), enableExclusions);
        EditorGUI.BeginDisabledGroup(!enableExclusions);
        exclusionPatterns = EditorGUILayout.TextField("Exclude Patterns (comma sep):", exclusionPatterns);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(5);

        // 3. Cleanup
        cleanupCode = EditorGUILayout.Foldout(cleanupCode, "Code Cleanup Options", true);
        if (cleanupCode)
        {
            EditorGUI.indentLevel++;
            removeComments = EditorGUILayout.Toggle("Remove Comments", removeComments);
            removeEmptyLines = EditorGUILayout.Toggle("Remove Empty Lines", removeEmptyLines);
            removeRegions = EditorGUILayout.Toggle("Remove Regions", removeRegions);
            EditorGUI.indentLevel--;
        }

        // 4. Detailed Stats
        detailedStats = EditorGUILayout.Toggle(new GUIContent("Detailed Statistics", "Count code, blank, and comment lines separately"), detailedStats);
    }

    private void RenderEncodingSelection()
    {
        GUILayout.Label("Target Encoding:", EditorStyles.miniLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("UTF-8", GUILayout.Height(25))) selectedEncoding = Encoding.UTF8;
        if (GUILayout.Button("ANSI", GUILayout.Height(25))) selectedEncoding = Encoding.Default;
        if (GUILayout.Button("Win-1251", GUILayout.Height(25))) selectedEncoding = Encoding.GetEncoding(1251);
        GUILayout.EndHorizontal();
    }

    private void RenderSelectedPaths()
    {
        // Drag & Drop Area
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 45.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag & Drop Files/Folders Here", EditorStyles.centeredGreyMiniLabel);
        HandleDragAndDrop(dropArea);

        // File List
        var listRect = GUILayoutUtility.GetRect(0.0f, 100.0f, GUILayout.ExpandWidth(true));
        GUI.Box(listRect, ""); // Frame for list

        var listScroll = EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(100));
        // Offset slightly for the box
        GUILayout.BeginHorizontal();
        GUILayout.Space(5);
        EditorGUILayout.BeginVertical();

        for (int i = 0; i < selectedPaths.Count; i++)
        {
            GUILayout.BeginHorizontal();
            // Icon
            GUILayout.Label(selectedPaths.Count > 0 && Directory.Exists(ConvertToFullPath(selectedPaths[i])) ? "📁 " : "📄 ", GUILayout.Width(20));
            EditorGUILayout.LabelField(selectedPaths[i], EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("✖", GUILayout.Width(20)))
            {
                selectedPaths.RemoveAt(i);
                UpdateStatistics();
                EditorGUILayout.EndScrollView();
                GUIUtility.ExitGUI();
                return;
            }
            GUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        GUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void RenderActionButtons()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Selected", GUILayout.Height(25)))
        {
            AddSelectedInProject();
        }
        if (GUILayout.Button("Add Folder", GUILayout.Height(25)))
        {
            AddFolder();
        }
        if (GUILayout.Button("Clear", GUILayout.Height(25)))
        {
            selectedPaths.Clear();
            statistics.Clear();
            previewContent = "";
        }
        GUILayout.EndHorizontal();
    }

    private void RenderStatistics()
    {
        EditorGUILayout.BeginHorizontal();

        // Left Column
        EditorGUILayout.BeginVertical(GUILayout.Width(150));
        EditorGUILayout.LabelField("Files: " + statistics.TotalFiles, EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("Size: " + statistics.TotalSizeKB.ToString("F2") + " KB", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        // Right Column
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        if (detailedStats)
        {
            EditorGUILayout.LabelField($"Code: {statistics.CodeLines} | Comment: {statistics.CommentLines} | Empty: {statistics.BlankLines}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Classes: {statistics.TotalClasses} | Methods: {statistics.TotalMethods}", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField($"Lines: {statistics.TotalLines}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Classes: {statistics.TotalClasses} | Methods: {statistics.TotalMethods}", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void RenderOutputButtons()
    {
        EditorGUI.BeginDisabledGroup(selectedPaths.Count == 0);

        if (GUILayout.Button("Save Combined Scripts To File...", GUILayout.Height(30)))
        {
            SaveCombinedScripts();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(30)))
        {
            CopyCombinedScripts();
        }

        EditorGUI.EndDisabledGroup();
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated)
        {
            if (dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
        }
        else if (evt.type == EventType.DragPerform)
        {
            if (dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.AcceptDrag();
                bool added = false;

                foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                {
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(path) && !selectedPaths.Contains(path))
                    {
                        if (Directory.Exists(path) || IsCSharpFile(path))
                        {
                            selectedPaths.Add(path);
                            added = true;
                        }
                    }
                }

                foreach (string path in DragAndDrop.paths)
                {
                    if (!string.IsNullOrEmpty(path) && !selectedPaths.Contains(path))
                    {
                        string relativePath = ConvertToRelativePath(path);
                        if (!string.IsNullOrEmpty(relativePath)) selectedPaths.Add(relativePath);
                        else selectedPaths.Add(path);
                        added = true;
                    }
                }

                if (added) UpdateStatistics();
                evt.Use();
            }
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
                foreach (string file in Directory.GetFiles(fullPath, "*.cs", System.IO.SearchOption.AllDirectories))
                {
                    if (IsExcluded(file)) continue;
                    statistics.Add(processor.ProcessFile(file, detailedStats));
                }
            }
            else if (File.Exists(fullPath) && IsCSharpFile(fullPath))
            {
                if (IsExcluded(fullPath)) continue;
                statistics.Add(processor.ProcessFile(fullPath, detailedStats));
            }
        }

        Repaint();
    }

    private bool IsExcluded(string filePath)
    {
        if (!enableExclusions || string.IsNullOrEmpty(exclusionPatterns)) return false;

        string fileName = Path.GetFileName(filePath);
        var patterns = exclusionPatterns.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var pattern in patterns)
        {
            if (fileName.Contains(pattern.Trim()))
                return true;
        }
        return false;
    }

    private void GeneratePreviewContent()
    {
        if (selectedPaths.Count == 0)
        {
            previewContent = "// No files selected.";
            return;
        }

        var options = new ProcessorOptions
        {
            ConsolidateUsings = consolidateUsings,
            RemoveComments = removeComments && cleanupCode,
            RemoveEmptyLines = removeEmptyLines && cleanupCode,
            RemoveRegions = removeRegions && cleanupCode,
            IsDetailed = detailedStats,
            ExclusionCheck = IsExcluded
        };

        var processor = new ScriptProcessor();
        previewContent = processor.GenerateCombinedText(selectedPaths, statistics, selectedEncoding, options);
    }

    private void SaveCombinedScripts()
    {
        if (selectedPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "Please select files or folders first", "OK");
            return;
        }

        GeneratePreviewContent();

        string directory = Application.dataPath;
        if (selectedPaths.Count == 1 && !File.Exists(selectedPaths[0]) && Directory.Exists(ConvertToFullPath(selectedPaths[0])))
        {
            directory = ConvertToFullPath(selectedPaths[0]);
        }

        string fileName = $"CombinedScripts_{selectedEncoding.EncodingName}.txt";
        string savePath = EditorUtility.SaveFilePanel("Save Combined Scripts", directory, fileName, "txt");

        if (!string.IsNullOrEmpty(savePath))
        {
            try
            {
                File.WriteAllText(savePath, previewContent, selectedEncoding);
                EditorUtility.RevealInFinder(savePath);
                EditorUtility.DisplayDialog("Success", "Scripts combined successfully!", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error writing combined file: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Error writing file: {e.Message}", "OK");
            }
        }
    }

    private void CopyCombinedScripts()
    {
        if (selectedPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "Please select files or folders first", "OK");
            return;
        }

        GeneratePreviewContent();
        GUIUtility.systemCopyBuffer = previewContent;
        Debug.Log($"Combined {statistics.TotalFiles} scripts copied to clipboard.");
    }
    #endregion

    #region Helper Methods
    private string ConvertToFullPath(string path)
    {
        if (path.StartsWith("Assets/") || path.StartsWith("Assets\\"))
            return Path.Combine(Application.dataPath, path.Substring(7));
        return path;
    }

    private string ConvertToRelativePath(string absolutePath)
    {
        if (absolutePath.StartsWith(Application.dataPath))
        {
            return "Assets" + absolutePath.Substring(Application.dataPath.Length);
        }
        return null;
    }

    private bool IsCSharpFile(string path)
    {
        return Path.GetExtension(path).ToLower() == ".cs";
    }
    #endregion
}

/// <summary>
/// Container for processing options
/// </summary>
public class ProcessorOptions
{
    public bool ConsolidateUsings;
    public bool RemoveComments;
    public bool RemoveEmptyLines;
    public bool RemoveRegions;
    public bool IsDetailed;
    public System.Func<string, bool> ExclusionCheck;
}

/// <summary>
/// Responsible for processing and analyzing C# script files
/// </summary>
public class ScriptProcessor
{
    public FileStatistics ProcessFile(string filePath, bool detailed)
    {
        try
        {
            string content = FileReader.ReadFileWithAutoEncoding(filePath);
            FileInfo fileInfo = new FileInfo(filePath);

            var stats = new FileStatistics
            {
                FilePath = filePath,
                SizeBytes = fileInfo.Length,
                LineCount = content.Split('\n').Length,
                ClassCount = CountOccurrences(content, "class "),
                MethodCount = CountMethods(content),
                CommentCount = CountComments(content)
            };

            if (detailed)
            {
                AnalyzeLines(content, out int code, out int blank, out int comments);
                stats.CodeLines = code;
                stats.BlankLines = blank;
                stats.CommentLines = comments;
            }

            return stats;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error processing file {filePath}: {e.Message}");
            return new FileStatistics();
        }
    }

    public string GenerateCombinedText(List<string> paths, ScriptStatistics statistics, Encoding encoding, ProcessorOptions options)
    {
        var allScriptPaths = CollectScriptPaths(paths, options.ExclusionCheck);

        if (allScriptPaths.Count == 0)
        {
            return "// No .cs files found to combine (or all were excluded).";
        }

        // Process content based on options
        HashSet<string> allUsings = new HashSet<string>();
        List<FileContent> processedFiles = new List<FileContent>();

        foreach (string scriptPath in allScriptPaths)
        {
            try
            {
                string content = FileReader.ReadFileWithAutoEncoding(scriptPath);

                // 1. Cleanup Code
                if (options.RemoveComments) content = RemoveComments(content);
                if (options.RemoveRegions) content = RemoveRegions(content);

                // 2. Extract Usings if enabled
                if (options.ConsolidateUsings)
                {
                    content = ExtractUsings(content, allUsings);
                }

                // 3. Remove Empty Lines (do this after extracting usings to keep structure somewhat sane)
                if (options.RemoveEmptyLines)
                {
                    // Remove lines that are empty or only whitespace
                    content = Regex.Replace(content, @"^\s*$[\r\n]*", string.Empty, RegexOptions.Multiline);
                }

                processedFiles.Add(new FileContent { Path = scriptPath, Content = content });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error reading file {scriptPath}: {e.Message}");
            }
        }

        return BuildCombinedText(processedFiles, statistics, encoding, options.ConsolidateUsings, allUsings);
    }

    private string BuildCombinedText(List<FileContent> files, ScriptStatistics stats, Encoding encoding, bool consolidateUsings, HashSet<string> usings)
    {
        StringBuilder combinedText = new StringBuilder();

        // Header
        combinedText.AppendLine("// ===== Combined Scripts Header =====");
        combinedText.AppendLine($"// Generation Time: {System.DateTime.Now}");
        combinedText.AppendLine($"// Encoding: {encoding.EncodingName}");
        combinedText.AppendLine($"// Total Files: {files.Count}");
        combinedText.AppendLine("// =================================");
        combinedText.AppendLine();

        // Consolidated Usings
        if (consolidateUsings && usings.Count > 0)
        {
            combinedText.AppendLine("// ===== Consolidated Usings =====");
            var sortedUsings = usings.OrderBy(u => u).ToList();
            foreach (var u in sortedUsings)
            {
                combinedText.AppendLine(u);
            }
            combinedText.AppendLine("// =================================");
            combinedText.AppendLine();
        }

        // Content
        for (int i = 0; i < files.Count; i++)
        {
            combinedText.AppendLine($"//==== File {i + 1} of {files.Count}: {files[i].Path} ====");
            combinedText.AppendLine(files[i].Content);
            combinedText.AppendLine();
        }

        // Statistics
        combinedText.AppendLine();
        combinedText.AppendLine("// ============ Statistics =============");
        combinedText.AppendLine($"// Total Files: {stats.TotalFiles}");
        combinedText.AppendLine($"// Total Size: {stats.TotalSizeKB:F2} KB");

        if (stats.CodeLines > 0)
        {
            combinedText.AppendLine($"// Code Lines: {stats.CodeLines}");
            combinedText.AppendLine($"// Comment Lines: {stats.CommentLines}");
            combinedText.AppendLine($"// Blank Lines: {stats.BlankLines}");
        }
        else
        {
            combinedText.AppendLine($"// Total Lines: {stats.TotalLines}");
        }

        combinedText.AppendLine($"// Classes: {stats.TotalClasses}");
        combinedText.AppendLine($"// Methods: {stats.TotalMethods}");
        combinedText.AppendLine($"// Comments (Blocks): {stats.TotalComments}");
        combinedText.AppendLine("// =====================================");

        return combinedText.ToString();
    }

    private class FileContent { public string Path; public string Content; }

    #region Helper Methods
    private string RemoveComments(string content)
    {
        content = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline);
        content = Regex.Replace(content, @"/\*[\s\S]*?\*/", "", RegexOptions.Multiline);
        return content;
    }

    private string RemoveRegions(string content)
    {
        content = Regex.Replace(content, @"#region\s.*", "", RegexOptions.Multiline);
        content = Regex.Replace(content, @"#endregion", "", RegexOptions.Multiline);
        return content;
    }

    private string ExtractUsings(string content, HashSet<string> usingsSet)
    {
        StringBuilder sb = new StringBuilder();
        bool inHeader = true;
        bool hasNamespace = false;

        using (var reader = new StringReader(content))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith("using ") && trimmed.EndsWith(";"))
                {
                    usingsSet.Add(trimmed);
                    if (!inHeader) sb.AppendLine(line);
                    continue;
                }

                if (trimmed.StartsWith("namespace "))
                {
                    hasNamespace = true;
                }

                if (inHeader && !string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("//") && !trimmed.StartsWith("using ") && !trimmed.StartsWith("namespace"))
                {
                    inHeader = false;
                }

                if (!inHeader || trimmed.StartsWith("//") || (hasNamespace && trimmed.StartsWith("namespace")))
                {
                    sb.AppendLine(line);
                }
                else if (inHeader && string.IsNullOrEmpty(trimmed))
                {
                    // Remove empty lines in header
                }
            }
        }
        return sb.ToString();
    }

    private void AnalyzeLines(string content, out int code, out int blank, out int comments)
    {
        code = 0; blank = 0; comments = 0;
        var lines = content.Split('\n');
        bool inBlockComment = false;

        foreach (var line in lines)
        {
            string trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                blank++;
                continue;
            }

            if (trimmed.StartsWith("/*") || trimmed.Contains("/*")) inBlockComment = true;
            if (trimmed.Contains("*/")) inBlockComment = false;

            if (inBlockComment || trimmed.StartsWith("//") || trimmed.StartsWith("/*"))
            {
                comments++;
                continue;
            }

            code++;
        }
    }

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

    private List<string> CollectScriptPaths(List<string> paths, System.Func<string, bool> exclusionCheck)
    {
        var allScriptPaths = new List<string>();

        foreach (string path in paths)
        {
            string fullPath = path.StartsWith("Assets/") || path.StartsWith("Assets\\") ?
                Path.Combine(Application.dataPath, path.Substring(7)) :
                path;

            if (Directory.Exists(fullPath))
            {
                allScriptPaths.AddRange(Directory.GetFiles(fullPath, "*.cs", System.IO.SearchOption.AllDirectories)
                    .Where(p => !exclusionCheck(p)));
            }
            else if (File.Exists(fullPath) && Path.GetExtension(fullPath).ToLower() == ".cs")
            {
                if (!exclusionCheck(fullPath)) allScriptPaths.Add(fullPath);
            }
        }

        return allScriptPaths.Distinct().ToList();
    }
    #endregion
}

/// <summary>
/// Static class for handling file reading operations with automatic encoding detection
/// </summary>
public static class FileReader
{
    public static string ReadFileWithAutoEncoding(string path)
    {
        byte[] fileBytes = File.ReadAllBytes(path);

        if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
            return Encoding.UTF8.GetString(fileBytes, 3, fileBytes.Length - 3);
        if (fileBytes.Length >= 2 && fileBytes[0] == 0xFF && fileBytes[1] == 0xFE)
            return Encoding.Unicode.GetString(fileBytes, 2, fileBytes.Length - 2);
        if (fileBytes.Length >= 2 && fileBytes[0] == 0xFE && fileBytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(fileBytes, 2, fileBytes.Length - 2);

        Encoding[] encodingsToTry = { Encoding.UTF8, Encoding.GetEncoding(1251), Encoding.Default };
        foreach (var encoding in encodingsToTry)
        {
            try
            {
                string content = encoding.GetString(fileBytes);
                if (ContainsMeaningfulContent(content)) return content;
            }
            catch { }
        }
        return Encoding.UTF8.GetString(fileBytes);
    }

    private static bool ContainsMeaningfulContent(string content)
    {
        return content.Any(c => c >= 'A' && c <= 'z') ||
               content.Any(c => c >= 'А' && c <= 'я');
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

    // Detailed stats
    public int CodeLines;
    public int BlankLines;
    public int CommentLines;
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
    public int CodeLines { get; private set; }
    public int BlankLines { get; private set; }
    public int CommentLines { get; private set; }

    public void Clear()
    {
        TotalFiles = 0;
        TotalSizeBytes = 0;
        TotalLines = 0;
        TotalClasses = 0;
        TotalMethods = 0;
        TotalComments = 0;
        CodeLines = 0;
        BlankLines = 0;
        CommentLines = 0;
    }

    public void Add(FileStatistics fileStats)
    {
        TotalFiles++;
        TotalSizeBytes += fileStats.SizeBytes;
        TotalLines += fileStats.LineCount;
        TotalClasses += fileStats.ClassCount;
        TotalMethods += fileStats.MethodCount;
        TotalComments += fileStats.CommentCount;

        CodeLines += fileStats.CodeLines;
        BlankLines += fileStats.BlankLines;
        CommentLines += fileStats.CommentLines;
    }
}