using UnityEngine;

public sealed partial class DadSchoolPrototype
{
    bool settingsOpen;
    bool privacyOpen;
    Vector2 privacyScroll;
    string privacyText;
    GUIStyle privacyMeasure;
    public const string PrivacyPolicyUrl = "https://github.com/MCuffs/mandu-coporation/blob/codex/dad-war-admob-test/PrivacyPolicy.md";
    public void OpenSettings(){settingsOpen=true;privacyOpen=false;}
    void DrawSettings()
    {
        if(!settingsOpen)return;
        Box(new Rect(0,0,Width,Height),new Color(.05f,.08f,.07f,.88f));
        if (privacyOpen) { DrawPrivacyPolicy(); return; }
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
        if(Button(new Rect(54,620,430,42),"개인정보 처리방침",Color.white,true,19)) { privacyOpen=true;privacyScroll=Vector2.zero; }
        if(DadAdPrivacy.Available && Button(new Rect(54,666,430,36),"광고 개인정보 선택 변경",Color.white,true,17)) DadAdPrivacy.Show();
        if(Button(new Rect(54,710,430,44),"확인",Yellow,true,25))settingsOpen=false;
    }
    void DrawPrivacyPolicy()
    {
        if (privacyText == null)
        {
            var asset = Resources.Load<TextAsset>("DadPrivacy");
            privacyText = asset != null ? asset.text.Replace("## ", "").Replace("# ", "") : "처리방침을 불러오지 못했습니다. 문의: alstnwjd0424@naver.com";
        }
        if (privacyMeasure == null) privacyMeasure = new GUIStyle { font=body, fontSize=18, wordWrap=true };
        Panel(new Rect(28,130,484,700),Paper);
        Text(new Rect(48,150,444,60),"개인정보 처리방침",29,Ink,true,TextAnchor.MiddleCenter);
        Rect view = new Rect(54,222,432,455);
        float height = privacyMeasure.CalcHeight(new GUIContent(privacyText), 402) + 40;
        if (Event.current.type == EventType.MouseDrag && view.Contains(Event.current.mousePosition))
        { privacyScroll.y = Mathf.Clamp(privacyScroll.y - Event.current.delta.y, 0, Mathf.Max(0,height-view.height)); Event.current.Use(); }
        privacyScroll = GUI.BeginScrollView(view, privacyScroll, new Rect(0,0,402,height));
        Text(new Rect(0,0,402,height),privacyText,18,Ink,false,TextAnchor.UpperLeft);
        GUI.EndScrollView();
        if(Button(new Rect(54,692,432,45),"공개 페이지 열기",Color.white,true,20))Application.OpenURL(PrivacyPolicyUrl);
        if(Button(new Rect(54,751,432,52),"설정으로 돌아가기",Yellow,true,21))privacyOpen=false;
    }
}
