using System;
using System.Collections.Generic;
using UnityEngine;

// Equipment shares the exact sprite transform. Coordinates below are on the established 768x768 character canvas.
// No floating inventory badges: gloves/shoes sit on limbs, glasses on eyes, props at hands.
public sealed class DadOutfitRenderer : IDisposable
{
    [Serializable] public sealed class Cut { public float x,y,w,h; public Rect UV { get { return new Rect(x,y,w,h); } } }
    [Serializable] public sealed class Cuts { public Cut[] parts; }
    readonly DadArt art;
    readonly Texture2D wear;
    readonly Cuts cuts,propCuts,hatCuts;readonly Texture2D hat;
    readonly Dictionary<string,Texture2D> outfits=new Dictionary<string,Texture2D>();
    readonly Dictionary<int,Texture2D> coats=new Dictionary<int,Texture2D>();
    int coatVariant=-9;
    public DadOutfitRenderer(DadArt art)
    {
        this.art=art;wear=Resources.Load<Texture2D>("DadV14/wear_parts");
        TextAsset meta=Resources.Load<TextAsset>("DadV17/wear_uv");
        cuts=meta!=null?JsonUtility.FromJson<Cuts>(meta.text):null;
        hat=Resources.Load<Texture2D>("DadV17/hat");hatCuts=JsonUtility.FromJson<Cuts>(Resources.Load<TextAsset>("DadV17/hat_uv").text);
        var props=Resources.Load<TextAsset>("DadV17/prop_uv");propCuts=props!=null?JsonUtility.FromJson<Cuts>(props.text):null;
    }
    public bool HasWear { get { return wear!=null&&cuts!=null&&cuts.parts!=null&&cuts.parts.Length==9; } }
    public int CachedBodies { get { return coats.Count; } }
    public void Dispose() { foreach(var texture in coats.Values)if(texture!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(texture);else UnityEngine.Object.DestroyImmediate(texture);}coats.Clear();foreach(var texture in fingers.Values)if(texture!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(texture);else UnityEngine.Object.DestroyImmediate(texture);}fingers.Clear(); }
    public Texture2D Body(Texture2D source,DadProgress progress)
    {
        if(source==null)return source;
        Pose pose;if(!TryPose(source.name,out pose))return source;
        int variant=progress!=null&&progress.HasGear(2)?progress.Save.gearRarity[2]:-1;
        bool shoes=HasWear&&progress!=null&&progress.HasGear(0), hands=HasWear&&progress!=null&&progress.HasGear(1);
        int style=(variant+1)*4+(shoes?1:0)+(hands?2:0);
        if(style!=coatVariant){Dispose();coatVariant=style;}
        Texture2D clothed=Outfit(source,variant);
        if(!shoes&&!hands)return clothed;
        Texture2D result;int key=source.GetInstanceID();if(coats.TryGetValue(key,out result)&&result!=null)return result;
        const int size=768; // retain the source's full detail in every equipped state
        RenderTexture old=RenderTexture.active,rt=RenderTexture.GetTemporary(size,size,0,RenderTextureFormat.ARGB32);
        try
        {
            Graphics.Blit(clothed,rt);RenderTexture.active=rt;
            result=new Texture2D(size,size,TextureFormat.RGBA32,false);result.name=source.name+"_wear_"+variant;
            result.ReadPixels(new Rect(0,0,size,size),0,0);Color32[] pixels=result.GetPixels32();
            for(int i=0;i<pixels.Length;i++)
            {
                Color32 original=pixels[i];if(original.a==0)continue;
                Vector2 point=new Vector2(i%size,size-1-i/size);
                bool skin=original.r>80&&original.r>original.g*1.12f&&original.g>original.b*1.1f;
                bool trousers=original.b>original.r*1.05f&&original.b>original.g*1.02f;
                // Keep the ankle skin. Only the baked shoe is replaced; no broad cuts into the trousers.
                bool foot=shoes&&!skin&&!trousers&&(FootArea(point,pose.shoeA,pose.shoeAngleA)||FootArea(point,pose.shoeB,pose.shoeAngleB));
                bool hand=hands&&(HandArea(point,pose.handA,pose.handAngleA,pose.fists,1)||HandArea(point,pose.handB,pose.handAngleB,pose.fists,pose.handScaleB));
                if(foot||hand)pixels[i]=new Color32(0,0,0,0);
            }
            result.SetPixels32(pixels);result.Apply(false,true);result.wrapMode=TextureWrapMode.Clamp;result.filterMode=FilterMode.Bilinear;
            coats[key]=result;return result;
        }
        finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);}
    }
    Texture2D Outfit(Texture2D source,int variant)
    {
        if(variant<1)return source;
        string key=variant+"/"+source.name;Texture2D texture;
        if(!outfits.TryGetValue(key,out texture)){texture=Resources.Load<Texture2D>("DadV17/tops/"+key);outfits[key]=texture;}
        return texture!=null?texture:source;
    }
    static Vector2 Local(Vector2 point,Vector2 center,float angle)
    {Vector2 d=point-center;float r=-angle*Mathf.Deg2Rad;return new Vector2(d.x*Mathf.Cos(r)-d.y*Mathf.Sin(r),d.x*Mathf.Sin(r)+d.y*Mathf.Cos(r));}
    static bool HandArea(Vector2 point,Vector2 center,float angle,bool fist,float scale)
    {
        Vector2 d=Local(point,center,angle);
        float rx=(fist?41:39)*scale,ry=(fist?34:46)*scale;
        return d.x*d.x/(rx*rx)+d.y*d.y/(ry*ry)<1;
    }
    static bool FootArea(Vector2 p,Vector2 center,float angle)
    {
        Vector2 d=p-center;float r=-angle*Mathf.Deg2Rad;
        Vector2 local=new Vector2(d.x*Mathf.Cos(r)-d.y*Mathf.Sin(r),d.x*Mathf.Sin(r)+d.y*Mathf.Cos(r));
        return Mathf.Abs(local.x)<62&&Mathf.Abs(local.y)<24;
    }
    public struct Pose
    {
        public Vector2 eyes,hat,handA,handB,shoeA,shoeB,pocket;
        public float headAngle,handAngleA,handAngleB,shoeAngleA,shoeAngleB,pocketAngle;
        public bool fists;public float handScaleB;
    }
    public static bool TryPose(string name,out Pose p)
    {
        p=new Pose{handScaleB=.80f,eyes=new Vector2(480,266),hat=new Vector2(421,202),handA=new Vector2(340,545),handB=new Vector2(470,540),shoeA=new Vector2(332,695),shoeB=new Vector2(441,693),pocket=new Vector2(314,418)};
        if(name=="dad_idle_0")return true;
        if(name=="dad_idle_1"){p.eyes=new Vector2(468,287);p.handA=new Vector2(332,558);p.handB=new Vector2(465,552);return true;}
        if(name=="dad_pull_0") {p.eyes=new Vector2(412,285);p.hat=new Vector2(375,208);p.handA=new Vector2(348,449);p.handB=new Vector2(470,420);p.shoeA=new Vector2(360,695);p.shoeB=new Vector2(504,695);p.pocket=new Vector2(291,469);p.fists=true;p.handAngleB=-75;return true;}
        if(name=="dad_flight_0") {p.handScaleB=1;p.eyes=new Vector2(563,338);p.hat=new Vector2(521,262);p.handA=new Vector2(267,461);p.handB=new Vector2(701,403);p.shoeA=new Vector2(74,441);p.shoeB=new Vector2(95,499);p.shoeAngleA=p.shoeAngleB=125;p.pocket=new Vector2(340,394);p.pocketAngle=-35;p.fists=true;p.handAngleA=125;return true;}
        if(name=="dad_flight_1") {p.handScaleB=1;p.eyes=new Vector2(547,297);p.hat=new Vector2(504,213);p.handA=new Vector2(319,416);p.handB=new Vector2(690,355);p.shoeA=new Vector2(77,504);p.shoeB=new Vector2(533,550);p.shoeAngleA=75;p.shoeAngleB=-12;p.pocket=new Vector2(365,386);p.pocketAngle=-40;p.fists=true;p.handAngleA=75;return true;}
        if(name=="dad_land_0") {p.handScaleB=1;p.eyes=new Vector2(533,344);p.hat=new Vector2(478,246);p.handA=new Vector2(201,504);p.handB=new Vector2(568,638);p.shoeA=new Vector2(213,696);p.shoeB=new Vector2(459,699);p.shoeAngleA=12;p.handAngleA=35;p.handAngleB=-25;p.pocket=new Vector2(319,477);p.pocketAngle=25;return true;}
        if(name=="dad_bounce_0") {p.handScaleB=1;p.eyes=new Vector2(384,262);p.headAngle=-11;p.hat=new Vector2(352,191);p.handA=new Vector2(372,427);p.handB=new Vector2(445,408);p.shoeA=new Vector2(423,669);p.shoeB=new Vector2(510,617);p.shoeAngleA=-28;p.shoeAngleB=-35;p.pocket=new Vector2(246,478);p.pocketAngle=10;p.fists=true;p.handAngleA=-30;p.handAngleB=-55;return true;}
        if(name=="dad_hurt_0") {p.handScaleB=.95f;p.eyes=new Vector2(267,240);p.hat=new Vector2(214,190);p.headAngle=-18;p.handA=new Vector2(336,432);p.handB=new Vector2(449,355);p.handAngleA=-75;p.handAngleB=-60;p.shoeA=new Vector2(296,695);p.shoeB=new Vector2(449,697);p.pocket=new Vector2(274,489);p.pocketAngle=-15;return true;}
        return false; // unsupported archived auto-punch poses are never given guessed attachment points
    }
    static void Sprite(Texture2D texture,Rect uv,Vector2 center,float width,float height,float angle=0,bool mirror=false)
    {
        if(texture==null)return;
        Matrix4x4 old=GUI.matrix;
        GUI.matrix=old*Matrix4x4.TRS(new Vector3(center.x,center.y,0),Quaternion.Euler(0,0,angle),new Vector3(mirror?-1:1,1,1));
        GUI.DrawTextureWithTexCoords(new Rect(-width/2,-height/2,width,height),texture,uv);GUI.matrix=old;
    }
    void Part(int index,Vector2 center,float w,float h,float angle=0,bool mirror=false)
    {if(HasWear)Sprite(wear,cuts.parts[index].UV,center,w,h,angle,mirror);}
    void Grip(int slot,int rarity,Vector2 hand,float wristAngle)
    {
        Texture2D texture=art.Gear(DadProgress.ItemIcon(slot,rarity));
        if(texture==null||propCuts==null)return;
        int index=(slot==4?0:3)+rarity;Rect uv=propCuts.parts[index].UV;
        // Preserve the actual object's aspect ratio, use its physical grip rather than image center.
        float height=slot==4?(rarity==2?115:110):(rarity==0?180:170);
        float width=height*texture.width*uv.width/(texture.height*uv.height);
        Vector2 grip;float angle;
        if(slot==4){grip=rarity==2?new Vector2(.55f,.85f):new Vector2(.50f,.98f);angle=rarity==2?-55:-10;}
        else{grip=rarity==0?new Vector2(.20f,.90f):rarity==1?new Vector2(.73f,.25f):new Vector2(.85f,.12f);angle=rarity==0?0:-35;}
        Matrix4x4 old=GUI.matrix;
        GUI.matrix=old*Matrix4x4.TRS(new Vector3(hand.x,hand.y,0),Quaternion.Euler(0,0,angle+wristAngle*(slot==4?.5f:.15f)),Vector3.one);
        GUI.DrawTextureWithTexCoords(new Rect(-grip.x*width,-grip.y*height,width,height),texture,uv);
        GUI.matrix=old;
    }
    // Restore just original skin/ink in the hand area above the held object, not a rectangle of trousers.
    readonly Dictionary<string,Texture2D> fingers=new Dictionary<string,Texture2D>();
    Texture2D Hands(string sourceName)
    {
        Texture2D result;if(fingers.TryGetValue(sourceName,out result))return result;
        Texture2D source=Resources.Load<Texture2D>((sourceName=="dad_idle_0"||sourceName=="dad_idle_1"||sourceName=="dad_hurt_0"?"DadV5/chars/dad/":"DadV13/chars/")+sourceName);
        Pose p;if(source==null||!TryPose(sourceName,out p))return null;
        var old=RenderTexture.active;var rt=RenderTexture.GetTemporary(768,768,0,RenderTextureFormat.ARGB32);
        try
        {
            Graphics.Blit(source,rt);RenderTexture.active=rt;result=new Texture2D(768,768,TextureFormat.RGBA32,false);
            result.ReadPixels(new Rect(0,0,768,768),0,0);var pixels=result.GetPixels32();
            for(int i=0;i<pixels.Length;i++)
            {
                Vector2 at=new Vector2(i%768,767-i/768);Color32 c=pixels[i];
                bool skin=c.r>80&&c.r>c.g*1.12f&&c.g>c.b*1.1f;
                // Only a narrow finger strip covers the handle; the rest of the object stays readable.
                Vector2 a=Local(at,p.handA,p.handAngleA),b=Local(at,p.handB,p.handAngleB);
                bool strip=(Mathf.Abs(a.x)<23&&Mathf.Abs(a.y)<12)||(Mathf.Abs(b.x)<18&&Mathf.Abs(b.y)<10);
                if(!strip||!skin)pixels[i]=new Color32(0,0,0,0);
            }
            result.SetPixels32(pixels);result.Apply(false,true);fingers[sourceName]=result;return result;
        }
        finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);}
    }
    public void Layers(string sourceName,Rect canvas,DadProgress progress)
    {
        Pose p;if(progress==null||!TryPose(sourceName,out p))return;
        Matrix4x4 old=GUI.matrix;
        GUI.matrix=old*Matrix4x4.TRS(new Vector3(canvas.x,canvas.y,0),Quaternion.identity,new Vector3(canvas.width/768f,canvas.height/768f,1));
        var save=progress.Save;

        // Held objects draw before gloves, so cuffs/fingers visibly close over them.
        if(progress.HasGear(4))Grip(4,save.gearRarity[4],p.handA,p.handAngleA);
        if(progress.HasGear(5))Grip(5,save.gearRarity[5],p.handB,p.handAngleB);
        if(progress.HasGear(0))
        {
            int rarity=save.gearRarity[0];float h=rarity==2?91:60;
            Part(6+rarity,p.shoeA+new Vector2(0,rarity==2?-19:0),116,h,p.shoeAngleA);
            Part(6+rarity,p.shoeB+new Vector2(0,rarity==2?-19:0),116,h,p.shoeAngleB);
        }
        if(progress.HasGear(1))
        {
            int rarity=save.gearRarity[1];bool gripA=p.fists||progress.HasGear(4),gripB=p.fists||progress.HasGear(5);
            float w=p.fists?98:86,h=p.fists?82:112;
            Part((gripA?3:0)+rarity,p.handA,gripA?98:w,gripA?82:h,p.handAngleA+(!p.fists&&gripA?90:0));
            Part((gripB?3:0)+rarity,p.handB,(gripB?98:w)*p.handScaleB,(gripB?82:h)*p.handScaleB,p.handAngleB+(!p.fists&&gripB?90:0),!gripB);
        }
        if(!progress.HasGear(1)&&(progress.HasGear(4)||progress.HasGear(5)))GUI.DrawTexture(new Rect(0,0,768,768),Hands(sourceName));
        if(progress.HasGear(3))
        {
            int rarity=save.gearRarity[3];
            if(rarity==2)
            {
                // The crown follows the real skull span, rather than a tiny centered inventory badge.
                Rect uv=hatCuts.parts[0].UV;
                float height=218*hat.height*uv.height/(hat.width*uv.width);
                Sprite(hat,uv,p.hat+new Vector2(18,-12),218,height,p.headAngle);
            }
            else DadEyewear.Draw(p.eyes,p.headAngle,rarity==1);
        }
        GUI.matrix=old;
    }
}
