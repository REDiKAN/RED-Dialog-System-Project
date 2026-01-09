using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;

/// <summary>
/// Обработчик автоматического сохранения всех открытых окон Dialogue Graph при закрытии Unity
/// </summary>
[InitializeOnLoad]
public static class AutoSaveHandler
{
    static AutoSaveHandler()
    {
        // Подписываемся на событие закрытия редактора Unity
        EditorApplication.quitting += OnUnityQuitting;
    }

    private static void OnUnityQuitting()
    {
        try
        {
            // AUTO-SAVE: Загружаем настройки диалоговой системы
            DialogueSettingsData settings = LoadDialogueSettings();
            if (settings == null || !settings.General.AutoSaveOnUnityClose)
                return; // Если настройка выключена - выходим

            Debug.Log("Auto-saving all open Dialogue Graph windows...");

            // AUTO-SAVE: Находим все открытые окна DialogueGraph
            var graphWindows = Resources.FindObjectsOfTypeAll<DialogueGraph>()
                .Where(window => window != null && window.graphView != null)
                .ToList();

            if (graphWindows.Count == 0)
                return; // Нет открытых окон для сохранения

            // AUTO-SAVE: Сохраняем каждое окно
            int savedCount = 0;
            foreach (var window in graphWindows)
            {
                if (window == null) continue;

                try
                {
                    // AUTO-SAVE: Получаем текущий загруженный контейнер из окна
                    var container = window.GetCurrentLoadedContainer();

                    // AUTO-SAVE: Пропускаем окна без загруженного контейнера
                    if (container == null)
                        continue;

                    // AUTO-SAVE: Используем существующий метод сохранения
                    var saveUtility = GraphSaveUtility.GetInstance(window.graphView);
                    saveUtility.SaveGraphToExistingContainer(container);

                    savedCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error auto-saving dialogue graph: {e.Message}");
                }
            }

            // AUTO-SAVE: Физически сохраняем ассеты в проекте
            if (savedCount > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"Auto-saved {savedCount} dialogue graph windows");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Critical error in AutoSaveHandler: {e.Message}");
        }
    }

    /// <summary>
    /// Загружает настройки диалоговой системы
    /// </summary>
    private static DialogueSettingsData LoadDialogueSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:DialogueSettingsData");
        if (guids.Length == 0)
            return null;

        // Берем первый найденный файл настроек
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<DialogueSettingsData>(path);
    }
}
