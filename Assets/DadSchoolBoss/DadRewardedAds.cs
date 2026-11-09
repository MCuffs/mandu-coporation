using System;
using UnityEngine;

public interface IDadRewardedProvider
{
    bool Ready { get; }
    // true is reserved for the SDK's earned-reward callback, never a close/impression event.
    void Show(Action<bool> completed);
}
public sealed class DadRewardedGate
{
    readonly IDadRewardedProvider provider;
    readonly Func<double> now;
    double next;
    public bool Busy { get; private set; }
    public int Earned { get; private set; }
    public DadRewardedGate(IDadRewardedProvider provider, Func<double> clock) { this.provider=provider;now=clock; }
    public bool Available { get { return !Busy && provider!=null && provider.Ready && Earned<5 && now()>=next; } }
    public bool Show(Action reward)
    {
        if (!Available) return false;
        Busy=true; bool resolved=false;
        try
        {
            provider.Show(earned =>
            {
                if (resolved) return; resolved=true;Busy=false;
                if (earned) { Earned++;next=now()+30;reward(); }
            });
        }
        catch { if(!resolved) { resolved=true;Busy=false; } return false; }
        return true;
    }
}
public static class DadRewardedAds
{
    public static DadRewardedGate Gate { get; private set; }
    public static bool Ready { get { return Gate!=null&&Gate.Available; } }
    public static bool Busy { get { return DadAdPrivacy.Busy || (Gate!=null&&Gate.Busy); } }
    public static void Bind(IDadRewardedProvider provider) { Gate=new DadRewardedGate(provider,()=>Time.realtimeSinceStartup); }
    public static bool Show(Action reward) { return Gate!=null&&Gate.Show(reward); }
}
