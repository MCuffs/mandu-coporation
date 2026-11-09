using System.Collections.Generic;
using UnityEngine;

// Shared UI rendering. Rasterize dynamic glyphs at their displayed density instead of enlarging a small atlas glyph.
public static class DadUi
{
    struct StyleKey:System.IEquatable<StyleKey>
    {
        public int font,size;public TextAnchor align;
        public bool Equals(StyleKey other){return font==other.font&&size==other.size&&align==other.align;}
        public override bool Equals(object other){return other is StyleKey&&Equals((StyleKey)other);}
        public override int GetHashCode(){unchecked{return (font*397^size)*31^(int)align;}}
    }
    static readonly Dictionary<StyleKey,GUIStyle> styles=new Dictionary<StyleKey,GUIStyle>();
    public static void Text(Rect rect,string text,Font font,int size,Color color,TextAnchor align)
    {
        Matrix4x4 old=GUI.matrix;
        float density=Mathf.Clamp(new Vector2(old.m00,old.m10).magnitude,1,4);
        int pixels=Mathf.CeilToInt(size*density);
        var key=new StyleKey{font=font!=null?font.GetInstanceID():0,size=pixels,align=align};
        GUIStyle style;
        if(!styles.TryGetValue(key,out style))
        {style=new GUIStyle{font=font,fontSize=pixels,alignment=align,wordWrap=true,padding=new RectOffset(),margin=new RectOffset()};styles[key]=style;}
        style.normal.textColor=color;
        GUI.matrix=old*Matrix4x4.Scale(new Vector3(1/density,1/density,1));
        GUI.Label(new Rect(rect.x*density,rect.y*density,rect.width*density,rect.height*density),text,style);
        GUI.matrix=old;
    }
    public static void Fill(Rect rect,Color color,float radius=10)
    {Color old=GUI.color;GUI.color=Color.white;GUI.DrawTexture(rect,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,color,0,radius);GUI.color=old;}
    public static void Panel(Rect rect,Color fill,Color ink)
    {Fill(rect,ink,10);Fill(new Rect(rect.x+1.5f,rect.y+1.5f,rect.width-3,rect.height-3),fill,8.5f);}
    public static bool Pressed(Rect rect)
    {return GUI.enabled&&rect.Contains(Event.current.mousePosition)&&(Input.GetMouseButton(0)||Input.touchCount>0);}
}

public static class DadPreferences
{
    const string Prefix="DadWar.Settings.";
    public static bool Music {get{return PlayerPrefs.GetInt(Prefix+"Music",1)==1;}set{Set("Music",value);}}
    public static bool Effects {get{return PlayerPrefs.GetInt(Prefix+"Effects",1)==1;}set{Set("Effects",value);}}
    public static bool Haptics {get{return PlayerPrefs.GetInt(Prefix+"Haptics",1)==1;}set{Set("Haptics",value);}}
    public static bool ReducedMotion {get{return PlayerPrefs.GetInt(Prefix+"ReducedMotion",0)==1;}set{Set("ReducedMotion",value);}}
    static void Set(string name,bool value){PlayerPrefs.SetInt(Prefix+name,value?1:0);PlayerPrefs.Save();}
    static float lastHaptic=-10;
    public static void Impact()
    {
        if(!Haptics||Time.unscaledTime-lastHaptic<.8f)return;lastHaptic=Time.unscaledTime;
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
}
