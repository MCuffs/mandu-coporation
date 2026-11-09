using System;
using UnityEngine;

public sealed partial class DadProgress
{
    public int RematchRank(int stage) { return stage >= 1 && stage <= Save.rematchRanks.Length ? Save.rematchRanks[stage-1] : 0; }
    public bool CanRematch(int stage, int rank)
    {
        return stage >= 1 && stage <= Eco.TotalStages && Stars(stage) > 0 && rank >= 1 && rank <= 3 && rank <= RematchRank(stage) + 1;
    }
    // Failures and completed ranks pay nothing: no consolation farming, no campaign unlocks or stars overwritten.
    public StageReward ApplyRematch(int stage, int rank, bool won)
    {
        var r = new StageReward { stage=stage, won=won, replay=true, challengeLevel=rank, stars=won?rank:0 };
        if (!CanRematch(stage,rank)) { r.won=false; r.stars=0; return r; }
        if (won && rank > RematchRank(stage))
        {
            if (Save.rematchRanks.Length < stage) Array.Resize(ref Save.rematchRanks,stage);
            Save.rematchRanks[stage-1]=rank;
            r.coins=Eco.BaseCoins(stage)*(1+.5*rank);r.boxes=rank;
            Save.coins+=r.coins;Save.lunchboxes+=r.boxes;
            r.bonusLine="재대결 "+rank+"단계 첫 승리! · 단계 보상 지급";
        }
        else r.bonusLine=won?"이미 받은 단계 보상 · 다음 단계나 기록에 도전":"재대결 실패 · 보상과 진행 기록은 유지됩니다";
        return r;
    }
}
