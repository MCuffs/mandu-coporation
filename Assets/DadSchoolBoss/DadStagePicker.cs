using UnityEngine;

public sealed partial class DadSchoolPrototype
{
    bool stagePicker;
    readonly System.Collections.Generic.Dictionary<int,int> rematchLimits = new System.Collections.Generic.Dictionary<int,int>();
    int RematchLimit(int stage, int rank)
    {
        int key=stage*4+rank, limit;
        if (!rematchLimits.TryGetValue(key,out limit))
        { limit=SlingshotSim.Rematch(Progress.Eco.Floor(stage),Progress.Eco.Number(stage),rank,DadSlingshotPrototype.Arena).shotLimit; rematchLimits[key]=limit; }
        return limit;
    }
    int mapFloor,selectedMapStage;
    void DrawStagePicker()
    {
        if(mapFloor==0){mapFloor=Progress.Eco.Floor(Progress.Save.currentStage);selectedMapStage=Progress.Save.currentStage;}
        int first=(mapFloor-1)*20+1;
        if(selectedMapStage<first||selectedMapStage>=first+20)selectedMapStage=first;
        Box(new Rect(0,0,Width,Height),Paper);
        Text(new Rect(24,22,352,52),"학교 정리 지도",33,Ink,true);
        if(Button(new Rect(410,26,106,48),"닫기",Color.white,true,20))stagePicker=false;
        string[] floors={"1층 복도","2층 급식실","3층 체육관"};
        for(int i=0;i<3;i++)
            if(Button(new Rect(22+i*170,90,156,54),floors[i],mapFloor==i+1?Yellow:Color.white,true,20)){mapFloor=i+1;selectedMapStage=(mapFloor-1)*20+1;}
        int stars=0;foreach(int value in Progress.Save.stars)stars+=value;
        Text(new Rect(24,154,492,32),"현재 개방 "+Progress.Eco.Label(Progress.Save.bestStage)+"   ·   모은 별 "+stars+" / "+Progress.Eco.TotalStages*3,17,Green);
        Vector2 previous=Vector2.zero;
        for(int i=0;i<20;i++)
        {
            int row=i/4,col=row%2==0?i%4:3-i%4;Vector2 center=new Vector2(80+col*126,236+row*92);
            if(i>0)Line(previous,center,4,i+first<=Progress.Save.bestStage?Green:new Color(.66f,.66f,.58f));previous=center;
        }
        for(int i=0;i<20;i++)
        {
            int stage=first+i,row=i/4,col=row%2==0?i%4:3-i%4;bool open=stage<=Progress.Save.bestStage,selected=stage==selectedMapStage;
            Rect node=new Rect(28+col*126,198+row*92,104,76);
            if(Button(node,"",selected?Yellow:Color.white,open,20))selectedMapStage=stage;
            Text(new Rect(node.x,node.y+5,node.width,30),Progress.Eco.Label(stage),21,open?Ink:Muted,true,TextAnchor.MiddleCenter);
            if(Progress.Eco.IsFloorBoss(stage))Text(new Rect(node.x,node.y+37,node.width,27),"층 보스",15,Red,true,TextAnchor.MiddleCenter);
            else for(int star=0;star<3;star++)Icon(star<Progress.Stars(stage)?"star_full":"star_empty",new Rect(node.center.x-27+star*18,node.y+42,17,17));
        }
        int picked=selectedMapStage,best=Progress.BestShots(picked),cleared=Progress.RematchRank(picked),rank=Mathf.Min(3,cleared+1);
        bool unlocked=picked<=Progress.Save.bestStage;
        Panel(new Rect(22,682,496,112),Color.white);
        Text(new Rect(40,691,460,38),Progress.Eco.Label(picked)+"  ·  "+floors[mapFloor-1],27,Ink,true);
        Text(new Rect(40,736,460,38),unlocked?(Progress.Stars(picked)+"/3 별   ·   "+(best==0?"아직 발사 기록 없음":"최소 "+best+"발")+"   ·   재대결 "+cleared+"/3"):"앞선 판을 정리하면 도전할 수 있어요",17,Green);
        if(Button(new Rect(22,818,238,76),"일반 출동",Yellow,unlocked,26)){stagePicker=false;StartRun(picked);}
        if(Button(new Rect(278,818,240,76),"재대결 "+rank+"단계",Color.white,Progress.Stars(picked)>0,24)){stagePicker=false;StartRematch(picked,rank);}
        Text(new Rect(24,904,492,38),"재대결: "+RematchLimit(picked,rank)+"발 제한 · 단계별 첫 승리 보상",16,Green,false,TextAnchor.MiddleCenter);
    }
}
