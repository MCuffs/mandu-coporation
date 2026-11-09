using System;
using System.IO;
using System.Text;
public static class DadAdChecks
{
    sealed class Provider : IDadRewardedProvider
    {
        public bool Ready {get;set;} = true;
        public Action<bool> Pending;
        public void Show(Action<bool> callback){Pending=callback;}
    }
    static void Check(bool ok,string name,StringBuilder report){if(!ok)throw new Exception(name);report.AppendLine("PASS "+name);}
    public static void Run()
    {
        var report=new StringBuilder("Rewarded-ad gate and persisted bonus checks; no live SDK\n");
        var provider=new Provider();double time=0;var gate=new DadRewardedGate(provider,()=>time);int rewards=0;
        Check(gate.Show(()=>rewards++)&&!gate.Show(()=>rewards++),"second request is rejected while an ad is active",report);
        provider.Pending(false);Check(rewards==0&&!gate.Busy,"cancel pays nothing and releases the gate",report);
        gate.Show(()=>rewards++);provider.Pending(true);provider.Pending(true);
        Check(rewards==1&&gate.Earned==1,"duplicate completion cannot award twice",report);
        Check(!gate.Available,"successful ad starts cooldown",report);time=31;Check(gate.Available,"cooldown expires",report);
        provider.Ready=false;Check(!gate.Available,"not-ready provider hides opt-in",report);provider.Ready=true;
        for(int i=0;i<4;i++){time+=31;gate.Show(()=>rewards++);provider.Pending(true);}
        time+=31;Check(!gate.Available&&gate.Earned==5,"session cap prevents unlimited ad requests",report);
        Check(!new DadRewardedGate(null,()=>0).Available,"no SDK means no ad offer",report);
        string path=Path.Combine(Path.GetTempPath(),"dad-ad-check-"+Guid.NewGuid()+".json");
        try
        {
            var p=new DadProgress(new DadEconomy(),path);p.PrepareAdBonus(new StageReward{stage=1,won=true,coins=100});
            Check(p.Save.adBonusCoins==0,"first three campaign stages have no rewarded offer",report);
            p.PrepareAdBonus(new StageReward{stage=4,won=true,coins=100});string token=p.Save.adBonusToken;p.Persist();
            var loaded=new DadProgress(new DadEconomy(),path);
            Check(!loaded.GrantAdBonus("stale"),"stale completion cannot grant the current receipt",report);
            Check(loaded.GrantAdBonus(token)&&loaded.Save.coins==100,"earned receipt grants exactly the offered bonus",report);loaded.Persist();
            var again=new DadProgress(new DadEconomy(),path);
            Check(!again.GrantAdBonus(token)&&again.Save.coins==100,"persisted consumed receipt cannot be claimed again",report);
            again.PrepareAdBonus(new StageReward{stage=4,won=false,coins=100});Check(again.Save.adBonusCoins==0,"loss never creates an offer",report);
        }
        finally{foreach(string suffix in new[]{"",".bak",".tmp"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
        File.WriteAllText("Design/dad-ad-flow-checks.txt",report.ToString());
    }
}
