using UnityEngine;

// Wearable frames, separate from the inventory icon. Clear lenses draw nothing over the character's eyes.
// Coordinates follow the head's three-quarter perspective: the far lens is narrower than the near lens.
public static class DadEyewear
{
    static readonly Vector2[] Near = {new Vector2(-47,-9),new Vector2(-43,-13),new Vector2(-8,-13),new Vector2(-2,-8),new Vector2(-5,8),new Vector2(-10,13),new Vector2(-37,13),new Vector2(-43,8)};
    static readonly Vector2[] Far = {new Vector2(9,-9),new Vector2(13,-12),new Vector2(37,-12),new Vector2(42,-8),new Vector2(40,9),new Vector2(35,13),new Vector2(16,12),new Vector2(11,8)};
    static void Line(Vector2 a, Vector2 b, float width, Color color)
    {
        Matrix4x4 old=GUI.matrix; Vector2 d=b-a;
        GUI.matrix=old*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg),Vector3.one);
        GUI.color=color; GUI.DrawTexture(new Rect(-width*.15f,-width/2,d.magnitude+width*.3f,width),Texture2D.whiteTexture); GUI.matrix=old;
    }
    static void Rim(Vector2[] points,float width,Color color)
    {for(int i=0;i<points.Length;i++)Line(points[i],points[(i+1)%points.Length],width,color);}
    public static void Draw(Vector2 bridge,float angle,bool sunglasses)
    {
        Matrix4x4 old=GUI.matrix;Color tint=GUI.color;
        GUI.matrix=old*Matrix4x4.TRS(new Vector3(bridge.x,bridge.y,0),Quaternion.Euler(0,0,angle),Vector3.one);
        Color frame=sunglasses?new Color(.10f,.11f,.10f,tint.a):new Color(.25f,.19f,.15f,tint.a);
        // Clear reading lenses deliberately have no fill or painted highlights.
        if(sunglasses)
        {
            GUI.color=new Color(.18f,.19f,.18f,tint.a);
            GUI.DrawTexture(new Rect(-43,-10,37,20),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(13,-9,25,20),Texture2D.whiteTexture);
        }
        float width=sunglasses?5:3.2f;
        Line(new Vector2(-78,5),new Vector2(-62,-4),width,frame);
        Line(new Vector2(-62,-4),new Vector2(-46,-6),width,frame);
        Rim(Near,width,frame);Rim(Far,width,frame);
        Line(new Vector2(-2,-6),new Vector2(3,-8),width,frame);
        Line(new Vector2(3,-8),new Vector2(9,-6),width,frame);
        Line(new Vector2(41,-6),new Vector2(48,-10),width,frame);
        GUI.color=tint;GUI.matrix=old;
    }
}
