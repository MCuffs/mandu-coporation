using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DadDifficultySetup
{
    [MenuItem("Dad School Boss/Run Difficulty Policy Bot")]
    public static void Bot()
    {
        File.WriteAllText("Design/dad-difficulty-bot-after.txt",SlingshotChecks.RunDifficultyBot(false));
        File.WriteAllText("Design/dad-rematch-bot.txt",SlingshotChecks.RunDifficultyBot(true));
        Debug.Log("DAD_DIFFICULTY_BOT_DONE");
    }
    [MenuItem("Dad School Boss/Run Difficulty Checks")]
    public static void Run()
    {
        string report=SlingshotChecks.RunChecks()+DadDifficultyChecks.Run();
        string path=Path.Combine(Path.GetTempPath(),"dad-rematch-save-"+Guid.NewGuid()+".json");
        try
        {
            var p=new DadProgress(new DadEconomy(),path);p.Save.stars=new[]{3};p.Save.bestStage=2;p.Save.currentStage=2;p.ApplyRematch(1,1,true);p.Persist();
            var restored=new DadProgress(new DadEconomy(),path);
            if(restored.RematchRank(1)!=1||!restored.CanRematch(1,2)||restored.Save.coins!=p.Save.coins)throw new Exception("rematch save reload failed");
            report+="PASS Unity JsonUtility preserves rank, currency and unlocks across reload\n";
            var before=restored.Save.coins;restored.ApplyRematch(1,1,true);
            if(restored.Save.coins!=before)throw new Exception("first reward duplicated after reload");
            report+="PASS restarting cannot reclaim a completed rank reward\n";
            File.WriteAllText(path,"{\"version\":2,\"coins\":123,\"currentStage\":1,\"bestStage\":1}");
            restored=new DadProgress(new DadEconomy(),path);
            if(restored.RematchRank(1)!=0||restored.BestShots(1)!=0||restored.Save.coins!=123)throw new Exception("legacy save failed");
            report+="PASS legacy v2 save loads without rematch or shot arrays\n";
            File.WriteAllText("Design/dad-difficulty-checks.txt",report);Debug.Log("DAD_DIFFICULTY_OK\n"+report);
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
}
