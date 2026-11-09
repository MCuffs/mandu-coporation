using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Native checks only in an explicitly invoked batch editor; never opens a window or enters Play Mode.
public static class DadReleaseAudit
{
    public static void Batch()
    {
        var report = new StringBuilder("Native Unity release audit; no interactive rendering/device verification.\n");
        report.AppendLine("Android application ID: " + PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
        report.AppendLine("Android backend: " + PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android));
        report.AppendLine("Android architectures: " + PlayerSettings.Android.targetArchitectures);
        report.AppendLine("Android target SDK setting: " + PlayerSettings.Android.targetSdkVersion);
        int failures = 0;
        Action<string, Action> run = (name, action) =>
        { try { action(); report.AppendLine("PASS " + name); } catch (Exception e) { failures++; report.AppendLine("FAIL " + name + ": " + e); } };
        run("prototype assets", DadPrototypeSetup.RunChecks);
        run("economy and stage rules", DadPrototypeSetup.RunPolishChecks);
        run("equipment native loading", DadOutfitChecks.Run);
        run("difficulty and JSON reload", DadDifficultySetup.Run);
        run("presentation and backup recovery", DadCommercialChecks.Run);
        run("save validation and legacy recovery", () => DadSaveRecoveryChecks.Run());
        run("new content assets resolve in native Unity", () =>
        {
            foreach (string name in new[] { "lunch_cart", "rebound_mat" })
                if (Resources.Load<Texture2D>("DadContentV2/" + name) == null) throw new Exception("Missing " + name);
            if (Resources.Load<Font>("Fonts/GothicA1-Regular") == null) throw new Exception("Missing Korean font");
        });
        run("offline privacy policy matches public document and contact", () =>
        {
            var policy = Resources.Load<TextAsset>("DadPrivacy");
            if (policy == null || policy.text != File.ReadAllText("PrivacyPolicy.md")) throw new Exception("Privacy document copy is missing or out of sync");
            if (!policy.text.Contains("파랑버섯 스튜디오") || !policy.text.Contains("alstnwjd0424@naver.com")) throw new Exception("Missing confirmed privacy contact");
        });
        run("empty primary does not shadow valid backup", () =>
        {
            string path = Path.Combine(Path.GetTempPath(), "dad-release-" + Guid.NewGuid() + ".json");
            try
            {
                var p = new DadProgress(new DadEconomy(), path); p.Save.coins = 123; p.Persist();
                File.Copy(path, path + ".bak"); File.WriteAllText(path, "{}");
                var recovered = new DadProgress(new DadEconomy(), path);
                if (recovered.Save.coins != 123) throw new Exception("Empty JSON primary accepted instead of valid backup; coins=" + recovered.Save.coins);
            }
            finally { foreach (string suffix in new[] { "", ".bak", ".tmp" }) if (File.Exists(path + suffix)) File.Delete(path + suffix); }
        });
        File.WriteAllText("Design/dad-save-fix-native-audit.txt", report.ToString());
        Debug.Log(report.ToString());
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }
}
