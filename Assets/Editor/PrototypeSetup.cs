using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class PrototypeSetup {
    static PrototypeSetup() { EditorApplication.delayCall += Setup; }
    static void Setup() {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall+=Setup; return; }
        const string path="Assets/Scenes/FirstFight.unity";
        if(System.IO.File.Exists(path)) return;
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera").AddComponent<Camera>(); camera.tag="MainCamera"; camera.orthographic=true; camera.backgroundColor=new Color(.025f,.03f,.05f); camera.clearFlags=CameraClearFlags.SolidColor; camera.gameObject.AddComponent<AudioListener>();
        new GameObject("Second Fist").AddComponent<SecondFist>();
        EditorSceneManager.SaveScene(scene,path);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};
        PlayerSettings.productName="Second Fist"; PlayerSettings.companyName="SecondFistStudio"; PlayerSettings.defaultScreenWidth=540; PlayerSettings.defaultScreenHeight=960; PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
        AssetDatabase.SaveAssets(); Debug.Log("SECOND_FIST_READY: FirstFight scene saved");
    }
}
