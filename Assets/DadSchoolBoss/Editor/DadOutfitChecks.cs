using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DadOutfitChecks
{
    static void Check(bool ok,string name,StringBuilder report) { if(!ok)throw new Exception("OUTFIT_CHECK_FAILED: "+name);report.AppendLine("PASS "+name); }
    [MenuItem("Dad School Boss/Run Outfit Checks")]
    public static void Run()
    {
        var report=new StringBuilder("Equipment appearance and persistence checks\n");
        string path=Path.Combine(Path.GetTempPath(),"dad-outfit-check-"+Guid.NewGuid()+".json");
        var p=new DadProgress(DadEconomy.Load(),path);
        var renderer=new DadOutfitRenderer(DadArt.Load());
        try
        {
            Check(renderer.HasWear,"wear atlas and all nine UV entries load",report);
            Texture2D body=Resources.Load<Texture2D>("DadV5/chars/dad/dad_idle_0");
            Check(renderer.Body(body,p)==body&&renderer.CachedBodies==0,"empty wardrobe keeps original body without cosmetic allocation",report);
            p.Save.gearRarity[2]=1;p.Save.gearValue[2]=.04f;
            Texture2D cream=renderer.Body(body,p);
            Check(cream!=body&&cream.width>=768,"equipped tank top uses a dedicated full-resolution sprite",report);
            Check(renderer.Body(body,p)==cream,"same pose and equipment reuse the texture instead of allocating every frame",report);
            p.Save.gearRarity[2]=2;
            Check(renderer.Body(body,p)!=cream,"fishing vest uses a distinct sprite rather than recoloring the tank top",report);
            p.Save.gearRarity[2]=-1;
            Check(renderer.Body(body,p)==body&&renderer.CachedBodies==0,"unequipping restores the original look",report);
            var hat=Resources.Load<Texture2D>("DadV17/hat");
            Check(hat!=null&&Resources.Load<TextAsset>("DadV17/hat_uv")!=null,"dedicated wearable hat and its trim metadata load",report);
            var props=Resources.Load<TextAsset>("DadV17/prop_uv");
            Check(props!=null&&JsonUtility.FromJson<DadOutfitRenderer.Cuts>(props.text).parts.Length==7,"all held object trim metadata load without icon aspect stretching",report);
            DadOutfitRenderer.Pose pose;bool poses=true;
            foreach(string name in new[]{"dad_idle_0","dad_idle_1","dad_pull_0","dad_flight_0","dad_flight_1","dad_bounce_0","dad_land_0","dad_hurt_0"})
                poses &= DadOutfitRenderer.TryPose(name,out pose);
            Check(poses&&!DadOutfitRenderer.TryPose("gum_walk_0",out pose),"all eight current dad poses have anchors and foes do not",report);
            for(int slot=0;slot<6;slot++){p.Save.gearRarity[slot]=slot%3;p.Save.gearValue[slot]=.04f;}
            p.Persist();var restored=new DadProgress(DadEconomy.Load(),path);
            bool kept=true;for(int slot=0;slot<6;slot++)kept &= restored.Save.gearRarity[slot]==slot%3;
            Check(kept,"mixed equipment appearance selection survives Unity save reload",report);
            Texture2D masked=renderer.Body(body,restored);
            Check(masked!=body&&renderer.CachedBodies==1,"new shoes use body masking instead of stacking over the baked slippers",report);
            bool variants=true;foreach(string poseName in new[]{"dad_idle_0","dad_idle_1","dad_pull_0","dad_flight_0","dad_flight_1","dad_bounce_0","dad_land_0","dad_hurt_0"})
                for(int rarity=1;rarity<=2;rarity++){var t=Resources.Load<Texture2D>("DadV17/tops/"+rarity+"/"+poseName);variants &= t!=null&&t.width>=768;}
            Check(variants,"all sixteen pose-specific tank top and fishing vest sprites exist at full resolution",report);
            p.Save.gearRarity[0]=-1;p.Save.gearRarity[2]=-1;p.Save.gearRarity[1]=0;
            Check(renderer.Body(body,p)!=body&&renderer.Body(body,p).width==768,"gloves alone replace the original hand region at source resolution",report);
            var uv=JsonUtility.FromJson<DadOutfitRenderer.Cuts>(Resources.Load<TextAsset>("DadV17/wear_uv").text);
            Check(uv.parts[0].w<.20f&&uv.parts[6].h<.20f,"wearable UVs exclude the large transparent cell margins",report);
            File.WriteAllText("Design/dad-outfit-checks-v17.txt",report.ToString());Debug.Log("DAD_OUTFIT_CHECKS_OK\n"+report);
        }
        finally{renderer.Dispose();if(File.Exists(path))File.Delete(path);}
    }
}
