using System;
using System.IO;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public static class DadCommercialChecks
{
    static void Check(bool ok,string text,StringBuilder report){if(!ok)throw new Exception("COMMERCIAL_CHECK_FAILED: "+text);report.AppendLine("PASS "+text);}
    [MenuItem("Dad School Boss/Run Presentation And Recovery Checks")]
    public static void Run()
    {
        var report=new StringBuilder("V18 presentation and save recovery checks\n");
        var path=Path.Combine(Path.GetTempPath(),"dad-recovery-"+Guid.NewGuid()+".json");GameObject obj=null;bool hadSeen=PlayerPrefs.HasKey("DadWar.SeenRoles");int seen=PlayerPrefs.GetInt("DadWar.SeenRoles",0);
        try
        {
            var eco=DadEconomy.Load();var p=new DadProgress(eco,path);p.Save.coins=123;p.Persist();p.Save.coins=456;p.Persist();
            Check(new DadProgress(eco,path).Save.coins==456,"atomic replacement retains the newest primary save",report);
            Check(File.Exists(path+".bak"),"a previous save copy is retained",report);
            File.WriteAllText(path,"{broken save");
            var recovered=new DadProgress(eco,path);Check(recovered.Save.coins==123,"corrupt primary recovers coins from backup",report);
            recovered.Save.coins=777;recovered.Persist();Check(new DadProgress(eco,path).Save.coins==777,"recovered progress can be saved again",report);
            File.Delete(path);Check(new DadProgress(eco,path).Save.coins==123,"missing primary recovers the known-good backup",report);
            File.Delete(path+".bak");File.WriteAllText(path+".tmp",JsonUtility.ToJson(recovered.Save));
            Check(new DadProgress(eco,path).Save.coins==777,"interrupted initial write recovers the completed temporary save",report);
            obj=new GameObject("Commercial checks");var view=obj.AddComponent<DadSlingshotPrototype>();
            var stage=SlingshotSim.ForMain(1,1,DadSlingshotPrototype.Arena);view.StartEmbedded(stage,null);
            view.DebugAim(new Vector2(270,700),new Vector2(270,810));view.SetPaused(true);
            Check(view.UserPaused&&!view.Aiming,"pause cancels an active pull",report);
            var update=typeof(DadSlingshotPrototype).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance);
            float elapsed=view.Sim.elapsed;Vector2 position=view.Sim.dad.pos;int shots=view.Sim.shots;
            for(int i=0;i<3;i++)update.Invoke(view,null);
            Check(view.Sim.elapsed==elapsed&&view.Sim.dad.pos==position&&view.Sim.shots==shots,"paused updates do not move the battle or fire a shot",report);
            view.SetPaused(false);Check(!view.UserPaused&&!view.Aiming&&view.Sim.shots==shots,"resume does not release the canceled shot",report);
            var focus=typeof(DadSlingshotPrototype).GetMethod("OnApplicationFocus",BindingFlags.NonPublic|BindingFlags.Instance);
            focus.Invoke(view,new object[]{false});focus.Invoke(view,new object[]{true});
            Check(view.UserPaused,"focus loss pauses and returning requires an explicit resume",report);
            view.StartEmbedded(SlingshotSim.ForMain(1,2,DadSlingshotPrototype.Arena),null);
            Check(!view.UserPaused,"a new battle clears the previous pause",report);
            typeof(DadSlingshotPrototype).GetField("lessonKind",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,2);
            elapsed=view.Sim.elapsed;update.Invoke(view,null);Check(view.TutorialOpen&&view.Sim.elapsed==elapsed,"new opponent counter lesson freezes the encounter until acknowledged",report);
            view.AcknowledgeLesson();Check(!view.TutorialOpen&&(PlayerPrefs.GetInt("DadWar.SeenRoles",0)&4)!=0,"acknowledging a counter lesson persists its seen flag and releases play",report);
            view.Sim.phase=SlingshotSim.Phase.Won;view.SetPaused(true);Check(!view.UserPaused,"finished battles cannot be trapped behind the pause dialog",report);
            File.WriteAllText("Design/dad-commercial-checks-v18.txt",report.ToString());Debug.Log("DAD_COMMERCIAL_CHECKS_OK\n"+report);
        }
        finally{if(hadSeen)PlayerPrefs.SetInt("DadWar.SeenRoles",seen);else PlayerPrefs.DeleteKey("DadWar.SeenRoles");PlayerPrefs.Save();if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);foreach(var suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
    }
    public static int PhonePreview()
    {
        var assembly=typeof(Editor).Assembly;
        var views=Resources.FindObjectsOfTypeAll(assembly.GetType("UnityEditor.GameView"));
        if(views.Length==0)throw new InvalidOperationException("No existing Game view; background verification will not open one.");
        var view=(EditorWindow)views[0];
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes=singleton.GetProperty("instance").GetValue(null,null);
        var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
        object group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});
        var groupClass=group.GetType();int count=(int)groupClass.GetMethod("GetBuiltinCount").Invoke(group,null)+(int)groupClass.GetMethod("GetCustomCount").Invoke(group,null);
        var sizeType=assembly.GetType("UnityEditor.GameViewSize");var type=assembly.GetType("UnityEditor.GameViewSizeType");
        object size=Activator.CreateInstance(sizeType,new object[]{Enum.Parse(type,"FixedResolution"),1080,1920,"Dad War Phone QA"});
        groupClass.GetMethod("AddCustomSize").Invoke(group,new[]{size});
        var selected=view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);int old=(int)selected.GetValue(view,null);selected.SetValue(view,count,null);view.Repaint();return old;
    }
}
