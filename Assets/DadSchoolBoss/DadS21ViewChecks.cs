#if UNITY_EDITOR || SLING_HEADLESS
using System;
using System.Text;
using UnityEngine;
public static class DadS21ViewChecks
{
    public static string Run()
    {
        var report=new StringBuilder();
        Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("S21_VIEW: "+name);report.AppendLine("PASS "+name);};
        check(DadBattleViewRules.FlightTilt(Vector2.right,false)==0,"rightward flight stays upright");
        check(DadBattleViewRules.FlightTilt(Vector2.left,true)==0,"leftward flight mirrors rather than turning upside down");
        check(Mathf.Abs(DadBattleViewRules.FlightTilt(new Vector2(1,-1),false)+45)<.01f,"up-right flight tilt follows travel");
        check(Mathf.Abs(DadBattleViewRules.FlightTilt(new Vector2(-1,-1),true)-45)<.01f,"up-left flight tilt follows mirrored travel");
        check(DadBattleViewRules.FlightTilt(new Vector2(0,1),false)==65,"vertical down flight cannot invert the body");
        check(DadBattleViewRules.FlightTilt(new Vector2(0,-1),false)==-65,"vertical up flight stays within the readable tilt limit");
        check(DadBattleViewRules.FacingLeft(new Vector2(-.1f,1),false)==false,"near-vertical travel preserves right facing");
        check(DadBattleViewRules.FacingLeft(new Vector2(.1f,1),true),"near-vertical travel preserves left facing");
        check(DadBattleViewRules.FacingLeft(Vector2.left,false),"clear leftward movement changes facing");
        check(!DadBattleViewRules.FacingLeft(Vector2.right,true),"clear rightward movement changes facing");
        check(DadBattleViewRules.FallenOpacity(.25f)==0&&DadBattleViewRules.FallenOpacity(0)==0,"old defeated bodies disappear before the next live crowd");
        check(DadBattleViewRules.FallenOpacity(.6f)<=.65f&&DadBattleViewRules.FallenOpacity(.4f)<DadBattleViewRules.FallenOpacity(.6f),"defeated bodies are subdued and fade monotonically");
        var feet=new Vector2(270,850);var center=DadBattleViewRules.BodyCenter(feet,.72f);
        check(center.x==feet.x&&Mathf.Abs(center.y-(850-75*.72f*1.12f))<.01f,"flight body uses the same standing half-height reference");
        check(DadBattleViewRules.HudHeight>=60,"battle HUD reserves readable mobile label and pause target space");
        check(DadBattleViewRules.PocketHeight(0)==84&&DadBattleViewRules.PocketHeight(2)==100,"held phone and newspaper have separate compact sizes");
        return report.ToString();
    }
}
#endif
