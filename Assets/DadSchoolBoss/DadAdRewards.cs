using System;
public sealed partial class DadProgress
{
    public void PrepareAdBonus(StageReward reward)
    {
        Save.adBonusToken="";Save.adBonusCoins=0;
        if(reward==null||!reward.won||reward.challengeLevel>0||reward.stage<4||reward.coins<=0)return;
        Save.adBonusToken=Guid.NewGuid().ToString("N");Save.adBonusCoins=reward.coins;
    }
    public bool GrantAdBonus(string token)
    {
        if(string.IsNullOrEmpty(token)||token!=Save.adBonusToken||Save.adBonusCoins<=0)return false;
        Save.coins+=Save.adBonusCoins;Save.adBonusCoins=0;Save.adBonusToken="";return true;
    }
}
