using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DadFunLabSetup
{
    [MenuItem("Dad School Boss/Fun Lab/Open Three Experiments")]
    public static void Open()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
        { Debug.LogWarning("Save the current scene and stop Play Mode before opening the experiments."); return; }
        EditorSceneManager.OpenScene("Assets/DadFunLab.unity");
    }
}
