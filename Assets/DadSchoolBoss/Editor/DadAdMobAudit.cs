using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.Build.Player;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;

public static class DadAdMobAudit
{
    public static void Batch()
    {
        var report = new StringBuilder("AdMob integration: SDK/API and Android script compilation only; no device/ad impression tested.\n");
        int exit = 1;
        try
        {
            Action<bool,string> check = (ok,label) => { if(!ok)throw new Exception(label); report.AppendLine("PASS " + label); };
            check(typeof(RewardedAd).Assembly != null && typeof(ConsentForm).Assembly != null,"real Mobile Ads and UMP API assemblies imported");
            check(DadAdMobProvider.TestRewardedId == "ca-app-pub-3940256099942544/5224354917","Android rewarded demo unit selected");
            var settings = Resources.Load<ScriptableObject>("GoogleMobileAdsSettings");
            check(settings != null,"SDK settings asset loads");
            var serialized = new SerializedObject(settings);
            check(serialized.FindProperty("adMobAndroidAppId").stringValue == "ca-app-pub-3940256099942544~3347511713","Android demo app ID configured");
            check(serialized.FindProperty("overrideDefaultGmaAndroidSdk").boolValue && serialized.FindProperty("selectedGmaAndroidSdk").intValue == 0,"standard Android SDK selected explicitly");
            check((int)PlayerSettings.Android.minSdkVersion >= 24,"minimum Android API is 24 or newer");
            check(File.ReadAllText("PrivacyPolicy.md") == Resources.Load<TextAsset>("DadPrivacy").text,"public and in-game privacy text match");
            DadAdChecks.Run(); report.AppendLine("PASS rewarded gate/persisted receipt checks (13 cases)");
            string output = "Build/AdMob/AndroidScripts"; Directory.CreateDirectory(output);
            var result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings {
                group = BuildTargetGroup.Android, target = BuildTarget.Android,
                options = ScriptCompilationOptions.DevelopmentBuild
            }, output);
            check(result.assemblies != null && result.assemblies.Count > 0,"Android player scripts compile against actual SDK");
            exit = 0;
        }
        catch(Exception e) { report.AppendLine("FAIL " + e); Debug.LogException(e); }
        finally { File.WriteAllText("Design/dad-admob-sdk-checks.txt",report.ToString()); EditorApplication.Exit(exit); }
    }
}
