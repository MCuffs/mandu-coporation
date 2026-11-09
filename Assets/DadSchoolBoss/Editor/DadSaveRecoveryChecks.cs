using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class DadSaveRecoveryChecks
{
    static void Check(bool ok, string label, StringBuilder report)
    { if (!ok) throw new Exception("SAVE_RECOVERY_FAILED: " + label); report.AppendLine("PASS " + label); }
    public static string Run()
    {
        var report = new StringBuilder("Native save validation and recovery checks\n");
        string path = Path.Combine(Path.GetTempPath(), "dad-save-validation-" + Guid.NewGuid() + ".json");
        var eco = new DadEconomy();
        string healthy = JsonUtility.ToJson(new DadSave { coins = 123 });
        Action clear = () => { foreach (string suffix in new[] { "", ".bak", ".tmp" }) if (File.Exists(path + suffix)) File.Delete(path + suffix); };
        try
        {
            var fresh = new DadProgress(eco, path);
            Check(!fresh.RecoveryFailed && fresh.Save.coins == 0, "missing all files is a genuine new game", report);
            fresh.Persist(); Check(new DadProgress(eco, path).Save.version == 2, "new zero-coin game persists and reloads", report);
            string[] invalid = {
                "{}", "null", "[]", "{broken", "{\"version\":2}",
                "{\"note\":\"version coins bestStage currentStage\"}",
                "{\"metadata\":{\"version\":2,\"coins\":777,\"bestStage\":1,\"currentStage\":1}}",
                "{\"version\":2,\"coins\":-1,\"bestStage\":1,\"currentStage\":1}",
                "{\"version\":2,\"coins\":123,\"bestStage\":1,\"currentStage\":2}",
                "{\"version\":2,\"coins\":123,\"bestStage\":1,\"currentStage\":1,\"upgrades\":[-1,0,0,0]}",
                "{\"version\":2,\"coins\":123,\"bestStage\":1,\"currentStage\":1,\"stars\":[4]}"
            };
            for (int i = 0; i < invalid.Length; i++)
            {
                clear(); File.WriteAllText(path, invalid[i]); File.WriteAllText(path + ".bak", healthy);
                var p = new DadProgress(eco, path);
                Check(!p.RecoveryFailed && p.Save.coins == 123, "invalid primary selects healthy backup case " + i, report);
            }
            var recovered = new DadProgress(eco, path);
            recovered.Save.coins = 777; recovered.Persist();
            Check(new DadProgress(eco, path).Save.coins == 777, "recovered progress can be saved and reloaded", report);
            Check(File.ReadAllText(path + ".bak") == healthy, "recovery write does not back up the invalid primary over healthy backup", report);
            clear(); File.WriteAllText(path, "{}"); File.WriteAllText(path + ".bak", "[]"); File.WriteAllText(path + ".tmp", healthy);
            Check(new DadProgress(eco, path).Save.coins == 123, "valid temporary save recovers when primary and backup are invalid", report);
            clear(); File.WriteAllText(path + ".bak", healthy);
            recovered = new DadProgress(eco, path); recovered.Persist();
            Check(File.ReadAllText(path + ".bak") == healthy && new DadProgress(eco, path).Save.coins == 123, "missing primary is restored without deleting its valid backup", report);
            clear(); File.WriteAllText(path, "{}"); File.WriteAllText(path + ".bak", "[]"); File.WriteAllText(path + ".tmp", "{broken");
            var failed = new DadProgress(eco, path); bool refused = false;
            try { failed.Persist(); } catch (InvalidOperationException) { refused = true; }
            Check(failed.RecoveryFailed && refused, "all invalid copies expose failure and reject automatic saving", report);
            Check(File.ReadAllText(path) == "{}" && File.ReadAllText(path + ".bak") == "[]" && File.ReadAllText(path + ".tmp") == "{broken", "all original copies survive total recovery failure byte-for-byte", report);
            File.WriteAllText(path, healthy); failed.Load(); failed.Persist();
            Check(!failed.RecoveryFailed && failed.Save.coins == 123, "retry after repairing a file clears the recovery block", report);
            clear(); File.WriteAllText(path, "{\"version\":2,\"coins\":456,\"bestStage\":1,\"currentStage\":1}");
            var legacy = new DadProgress(eco, path);
            Check(!legacy.RecoveryFailed && legacy.Save.coins == 456 && legacy.Save.pendingSlot == -1 && legacy.Save.stars.Length == 0, "minimal legacy v2 loads with optional fields defaulted", report);
            legacy.Persist(); Check(new DadProgress(eco, path).Save.coins == 456, "minimal legacy v2 remains writable", report);
            clear(); var old = new DadSave { version = 1, coins = 321, boxesOpened = 10 };
            File.WriteAllText(path, JsonUtility.ToJson(old)); legacy = new DadProgress(eco, path);
            Check(!legacy.RecoveryFailed && legacy.Save.version == 2 && legacy.Save.coins == 321 && legacy.EquippedValue(0) > 0, "v1 gear migration preserves progress", report);
            clear(); old = new DadSave { coins = 123, upgrades = new[] { 50, 0, 0, 0 } };
            File.WriteAllText(path, JsonUtility.ToJson(old));
            Check(new DadProgress(eco, path).Save.upgrades[0] == 50, "historical over-cap purchased levels are retained", report);
            clear(); string future = "{\"version\":3,\"coins\":999,\"bestStage\":1,\"currentStage\":1}";
            File.WriteAllText(path, future); File.WriteAllText(path + ".bak", healthy);
            Check(new DadProgress(eco, path).RecoveryFailed && File.ReadAllText(path) == future, "newer schema is protected rather than downgraded to an older backup", report);
            clear(); File.WriteAllText(path, healthy); var live = new DadProgress(eco, path); live.Save.coins = double.NaN; refused = false;
            try { live.Persist(); } catch (InvalidOperationException) { refused = true; }
            Check(refused && File.ReadAllText(path) == healthy && !File.Exists(path + ".tmp"), "invalid in-memory currency cannot overwrite a valid save", report);
            File.WriteAllText("Design/dad-save-recovery-checks.txt", report.ToString());
            return report.ToString();
        }
        finally { clear(); }
    }
}
