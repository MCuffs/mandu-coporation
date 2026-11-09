#if UNITY_EDITOR
using UnityEngine;
// Editor-only visual audit, deliberately independent of the player's save and gameplay state.
public sealed class DadOutfitAuditView : MonoBehaviour
{
    public int page;
    readonly string[] names={"dad_idle_0","dad_idle_1","dad_pull_0","dad_flight_0","dad_flight_1","dad_bounce_0","dad_land_0","dad_hurt_0"};
    readonly string[] slots={"SHOES","GLOVES","TOP","HEAD","ITEM","TOOL"};
    DadOutfitRenderer[] renderers;DadProgress[] progress;Texture2D[] poses;
    void Awake()
    {
        renderers=new DadOutfitRenderer[18];progress=new DadProgress[18];poses=new Texture2D[8];var art=DadArt.Load();
        for(int i=0;i<18;i++){renderers[i]=new DadOutfitRenderer(art);progress[i]=new DadProgress(DadEconomy.Load(),System.IO.Path.Combine(System.IO.Path.GetTempPath(),"dad-visual-"+System.Guid.NewGuid()+".json"));}
        for(int i=0;i<8;i++)poses[i]=Resources.Load<Texture2D>((i<2||i==7?"DadV5/chars/dad/":"DadV13/chars/")+names[i]);
    }
    void OnDestroy(){if(renderers!=null)foreach(var r in renderers)r.Dispose();}
    void OnGUI()
    {
        Matrix4x4 matrix=GUI.matrix;Color color=GUI.color;int depth=GUI.depth;
        GUI.matrix=Matrix4x4.identity;GUI.depth=-10000;GUI.color=new Color(.84f,.85f,.79f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;
        bool singles=page==0;int cols=singles?6:4,rows=singles?3:2,count=singles?18:8;
        float w=Screen.width/(float)cols,h=(Screen.height-35f)/rows;
        var label=new GUIStyle(GUI.skin.label){fontSize=18,normal={textColor=Color.black}};
        GUI.Label(new Rect(12,4,Screen.width,30),singles?"V17 | Each slot alone, rows: common / rare / best":"V17 | All six slots | tier "+((page-1)%3)+" | "+(page>3?"MIRRORED":"NORMAL"),label);
        for(int i=0;i<count;i++)
        {
            var p=progress[i];int rarity=singles?i/6:(page-1)%3;for(int s=0;s<6;s++)p.Save.gearRarity[s]=singles?(s==i%6?rarity:-1):rarity;
            Texture2D source=poses[singles?0:i],body=renderers[i].Body(source,p);
            float x=(i%cols)*w,y=35+(i/cols)*h;GUI.Label(new Rect(x+10,y+3,w-12,30),singles?slots[i%6]+" / "+rarity:names[i],label);
            float size=Mathf.Min(w,h-28)*1.02f;Rect canvas=new Rect(x+(w-size)/2,y+28+(h-28-size)/2,size,size);
            Matrix4x4 old=GUI.matrix;if(page>3)GUI.matrix=old*Matrix4x4.TRS(new Vector3(2*(x+w/2),0,0),Quaternion.identity,new Vector3(-1,1,1));
            GUI.DrawTexture(canvas,body);renderers[i].Layers(source.name,canvas,p);GUI.matrix=old;
        }
        GUI.depth=depth;GUI.matrix=matrix;GUI.color=color;
    }
}
#endif
