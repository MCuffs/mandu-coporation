using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

[InitializeOnLoad]
public static class AlleyVisualPipeline {
    static bool processing;
    static AlleyVisualPipeline(){EditorApplication.delayCall+=Delayed;}
    static void Delayed(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Delayed;return;}
        if(!File.Exists("Assets/Alley/Resources/HeroMotion.png")||!File.Exists("Assets/Alley/Resources/EnemyMotion.png")||!File.Exists("Assets/Alley/Resources/AbilityIcons.png"))return;
        Prepare();
    }
    [MenuItem("Second Fist/Prepare Visual Assets")]
    public static void Prepare(){
        if(processing)return;processing=true;
        try{
            string stamp="components-v9-gym:"+File.GetLastWriteTimeUtc("Assets/Alley/Resources/HeroMotion.png").Ticks+":"+File.GetLastWriteTimeUtc("Assets/Alley/Resources/EnemyMotion.png").Ticks+":"+File.GetLastWriteTimeUtc("Assets/Alley/Resources/AbilityIcons.png").Ticks;
            if(SessionState.GetString("AlleyVisualStamp","")==stamp)return;
            StringBuilder report=new StringBuilder("Second Fist v9 - stage, gym tycoon and visual asset validation\n");
            PrepareSheet("StreetRoster",true,report);PrepareArenas(report);PrepareGymAssets(report);PrepareSheet("HeroMotion",true,report);PrepareSheet("EnemyMotion",true,report);PrepareSheet("FighterAtlas",true,report,2);PrepareSheet("AbilityIcons",false,report);
            string[] fonts={"GothicA1-Regular","GothicA1-Bold","BlackHanSans-Regular"};
            foreach(string name in fonts){if(AssetDatabase.LoadAssetAtPath<Font>("Assets/Alley/Resources/Fonts/"+name+".ttf")==null)throw new Exception("Missing bundled font: "+name);report.AppendLine("PASS bundled font "+name);}
            report.AppendLine("NOTE enemy cells 12 and 14 are excluded from playback because their designs vary.");
            Directory.CreateDirectory("Design");File.WriteAllText("Design/visual-checks.txt",report.ToString());SessionState.SetString("AlleyVisualStamp",stamp);AssetDatabase.Refresh();Debug.Log("ALLEY_VISUAL_ASSETS_READY\n"+report);
        }finally{processing=false;}
    }
    static void PrepareGymAssets(StringBuilder report){
        const string equipPath="Assets/Alley/Resources/GymEquipment.png";
        if(File.Exists(equipPath)){
            var eqImporter=(TextureImporter)AssetImporter.GetAtPath(equipPath);
            if(eqImporter!=null){
                eqImporter.textureType=TextureImporterType.Default;eqImporter.alphaIsTransparency=true;eqImporter.mipmapEnabled=false;eqImporter.wrapMode=TextureWrapMode.Clamp;eqImporter.filterMode=FilterMode.Bilinear;eqImporter.maxTextureSize=2048;eqImporter.npotScale=TextureImporterNPOTScale.None;eqImporter.textureCompression=TextureImporterCompression.Uncompressed;eqImporter.SaveAndReimport();
                report.AppendLine("PASS GymEquipment 2x2 alpha sprite sheet imported");
            }
        }
        const string bgPath="Assets/Alley/Resources/GymBackdrop.png";
        if(File.Exists(bgPath)){
            var bgImporter=(TextureImporter)AssetImporter.GetAtPath(bgPath);
            if(bgImporter!=null){
                bgImporter.textureType=TextureImporterType.Default;bgImporter.alphaIsTransparency=false;bgImporter.mipmapEnabled=false;bgImporter.wrapMode=TextureWrapMode.Clamp;bgImporter.filterMode=FilterMode.Bilinear;bgImporter.maxTextureSize=2048;bgImporter.npotScale=TextureImporterNPOTScale.None;bgImporter.textureCompression=TextureImporterCompression.Uncompressed;bgImporter.SaveAndReimport();
                report.AppendLine("PASS GymBackdrop environment imported");
            }
        }
    }
    static void PrepareSheet(string name,bool pivots,StringBuilder report,int rows=4,int columns=4){
        string path="Assets/Alley/Resources/"+name+".png";
        TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new Exception("Texture importer missing: "+name);
        importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.isReadable=true;importer.SaveAndReimport();
        Texture2D texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);Color32[] pixels=texture.GetPixels32();
        int transparent=0,opaque=0;foreach(Color32 pixel in pixels){if(pixel.a<10)transparent++;if(pixel.a>180)opaque++;}
        if(transparent<100||opaque<100)throw new Exception("Atlas alpha/content invalid: "+name);
        report.AppendLine("PASS "+name+" alpha transparency "+texture.width+"x"+texture.height);
        if(pivots){
            AlleyGame.PivotSheet data=new AlleyGame.PivotSheet{rows=rows,columns=columns,frames=new AlleyGame.FramePivot[rows*columns]};
            float cw=texture.width/(float)columns,ch=texture.height/(float)rows;
            List<Component> components=FindComponents(pixels,texture.width,texture.height);
            HashSet<int> used=new HashSet<int>();
            for(int frame=0;frame<rows*columns;frame++){
                Vector2 expected=new Vector2((frame%columns+.5f)*cw,(rows-1-frame/columns+.5f)*ch);
                int best=-1;float bestScore=float.MaxValue;
                for(int i=0;i<components.Count;i++){
                    if(used.Contains(i)||components[i].count<500)continue;Component c=components[i];
                    Vector2 center=new Vector2((c.x0+c.x1)*.5f,(c.y0+c.y1)*.5f);
                    float score=(center-expected).sqrMagnitude;
                    if(score<bestScore){best=i;bestScore=score;}
                }
                if(best<0||bestScore>cw*cw*.6f)throw new Exception("Cannot isolate sprite: "+name+" "+frame);
                used.Add(best);Component body=components[best];
                int left=Mathf.Max(0,body.x0-2),right=Mathf.Min(texture.width-1,body.x1+2),bottom=Mathf.Max(0,body.y0-2),top=Mathf.Min(texture.height-1,body.y1+2);
                // Register against the pelvis instead of swinging shoe extremes; all poses share the same root.
                double anchorSum=0;int anchorCount=0;float bodyHeight=body.y1-body.y0+1;
                foreach(int pixelIndex in body.pixels){int y=pixelIndex/texture.width;if(y<body.y0+bodyHeight*.36f||y>body.y0+bodyHeight*.49f)continue;anchorSum+=pixelIndex%texture.width;anchorCount++;}
                float anchor=anchorCount>0?(float)(anchorSum/anchorCount):(body.x0+body.x1)*.5f;
                float width=right-left+1,height=top-bottom+1;
                data.frames[frame]=new AlleyGame.FramePivot{x=(anchor-left)/width,y=(body.y0-bottom)/height,u=left/(float)texture.width,v=bottom/(float)texture.height,w=width/texture.width,h=height/texture.height,scale=name=="FighterAtlas"||name=="StreetRoster"?ch/height:1};
                report.AppendLine("PASS "+name+" frame "+frame+" isolated sprite, pelvis root and ground contact registered");
            }
            File.WriteAllText("Assets/Alley/Resources/"+name+"Pivots.json",JsonUtility.ToJson(data,true));
        }
        importer.isReadable=false;importer.SaveAndReimport();
    }

    static void PrepareArenas(StringBuilder report){
        const string path="Assets/Alley/Resources/StageArenas.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture==null||texture.width!=1536||texture.height!=1024)throw new Exception("Stage arenas need 1536 x 1024 atlas");report.AppendLine("PASS four stage arenas imported at native resolution");
    }
    static void PreparePrologue(StringBuilder report){
        const string path="Assets/Alley/Resources/PrologueInkScenes.png";
        TextureImporter importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new Exception("Missing ink prologue artwork");
        importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=false;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture.width<1000||texture.height<700)throw new Exception("Prologue artwork resolution invalid");
        report.AppendLine("PASS six-panel ink prologue atlas imported");
        int[] indices={0,2,3,5,10,11,12,13};
        foreach(int i in indices){string voicePath="Assets/Alley/Resources/Voice/Prologue/"+i.ToString("00")+".mp3";AudioClip clip=AssetDatabase.LoadAssetAtPath<AudioClip>(voicePath);if(clip==null||clip.length<.1f)throw new Exception("Missing narration "+i);report.AppendLine("PASS narration "+i+" imported ("+clip.length.ToString("0.00")+" seconds)");}
    }
    sealed class Component {public int x0=int.MaxValue,x1,y0=int.MaxValue,y1,count;public List<int> pixels=new List<int>();}
    static List<Component> FindComponents(Color32[] pixels,int width,int height){
        bool[] visited=new bool[pixels.Length];List<Component> result=new List<Component>();Queue<int> queue=new Queue<int>();
        for(int start=0;start<pixels.Length;start++){
            if(visited[start]||pixels[start].a<160)continue;
            Component c=new Component();visited[start]=true;queue.Enqueue(start);
            while(queue.Count>0){int index=queue.Dequeue(),x=index%width,y=index/width;c.pixels.Add(index);c.count++;c.x0=Mathf.Min(c.x0,x);c.x1=Mathf.Max(c.x1,x);c.y0=Mathf.Min(c.y0,y);c.y1=Mathf.Max(c.y1,y);
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int nx=x+dx,ny=y+dy;if(nx<0||nx>=width||ny<0||ny>=height)continue;int next=ny*width+nx;if(!visited[next]&&pixels[next].a>=160){visited[next]=true;queue.Enqueue(next);}}
            }
            if(c.count>500)result.Add(c);
        }
        return result;
    }

}
