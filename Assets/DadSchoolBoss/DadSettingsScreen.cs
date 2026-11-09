using UnityEngine;

public sealed partial class DadSchoolPrototype
{
    bool settingsOpen;
    public void OpenSettings(){settingsOpen=true;}
    void DrawSettings()
    {
        if(!settingsOpen)return;
        Box(new Rect(0,0,Width,Height),new Color(.05f,.08f,.07f,.88f));
        Panel(new Rect(28,190,484,580),Paper);
        Text(new Rect(52,212,430,54),"아빠 잠깐 쉬는 중",34,Ink,true);
        Text(new Rect(52,267,430,30),"내 취향대로 편하게",18,Muted);
        string[] labels={"배경 음악","효과음","진동","화면 흔들림 줄이기"};
        bool[] values={DadPreferences.Music,DadPreferences.Effects,DadPreferences.Haptics,DadPreferences.ReducedMotion};
        for(int i=0;i<4;i++)
        {
            float y=322+i*76;Text(new Rect(54,y,286,54),labels[i],23,Ink);
            if(Button(new Rect(358,y,126,54),values[i]?"켜짐":"꺼짐",values[i]?Yellow:Color.white,true,21))
            {bool value=!values[i];if(i==0)DadPreferences.Music=value;else if(i==1)DadPreferences.Effects=value;else if(i==2)DadPreferences.Haptics=value;else DadPreferences.ReducedMotion=value;}
        }
        if(Button(new Rect(54,662,430,68),"확인",Yellow,true,25))settingsOpen=false;
    }
}
