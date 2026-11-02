using UnityEngine;
using SecondFistGame;

public partial class AlleyGame {
    Texture2D prologueAtlas;
    PrologueTimeline prologue;
    AudioSource heartbeatSource,narrationSource;AudioClip heartbeat;AudioClip[] narration=new AudioClip[14];
    bool storyActive,storyAdvance,storySkip;
    int lastStoryBeat=-1,viewportWidth,viewportHeight;
    float battleReveal,storyImpact,shotClock,cutBlend;int lastStoryArt=-99,previousStoryArt=-99;
    void LoadPrologue(){
        prologueAtlas=Resources.Load<Texture2D>("PrologueInkScenes");
        narrationSource=gameObject.AddComponent<AudioSource>();narrationSource.volume=.9f;
        for(int i=0;i<narration.Length;i++)narration[i]=Resources.Load<AudioClip>("Voice/Prologue/"+i.ToString("00"));
        heartbeatSource=gameObject.AddComponent<AudioSource>();heartbeatSource.loop=true;heartbeatSource.volume=.22f;
        int rate=22050,n=(int)(rate*1.25f);float[] data=new float[n];
        for(int i=0;i<n;i++){float t=i/(float)rate;float a=Mathf.Exp(-t*23),b=t>.23f?Mathf.Exp(-(t-.23f)*26):0;data[i]=Mathf.Sin(t*2*Mathf.PI*56)*(a+b*.68f)*.30f;}
        heartbeat=AudioClip.Create("Heartbeat",n,1,rate,false);heartbeat.SetData(data,0);heartbeatSource.clip=heartbeat;
    }
    void BeginPrologue(){
        prologue=new PrologueTimeline();
        for(int i=0;i<narration.Length;i++)if(narration[i]!=null)prologue.SetMinimumDuration(i,narration[i].length+.55f);
        storyActive=true;storyAdvance=storySkip=false;lastStoryBeat=-1;lastStoryArt=-99;previousStoryArt=-99;shotClock=cutBlend=storyImpact=0;battleReveal=0;
        if(narrationSource!=null)narrationSource.Stop();
        if(heartbeatSource!=null){heartbeatSource.mute=source.mute;heartbeatSource.Play();}
    }
    void StartBattle(){
        storyActive=false;storyAdvance=storySkip=false;if(heartbeatSource!=null)heartbeatSource.Stop();if(narrationSource!=null)narrationSource.Stop();
        paused=false;dragging=false;stick=Vector2.zero;combat.Start();battleReveal=1.0f;modalAge=0;
        heroRendered=heroLast=combat.player;actorMotion.Clear();
    }
    void ToggleFullscreen(){
        if(Screen.fullScreen){int h=Mathf.Min(960,Display.main.systemHeight-100);Screen.SetResolution(Mathf.RoundToInt(h*.5625f),h,FullScreenMode.Windowed);}
        else Screen.SetResolution(Display.main.systemWidth,Display.main.systemHeight,FullScreenMode.FullScreenWindow);
    }
    void UpdateArenaViewport(bool reset){
        if(!reset&&viewportWidth==Screen.width&&viewportHeight==Screen.height)return;
        viewportWidth=Screen.width;viewportHeight=Screen.height;
        combat.SetArena(new Rect(54,126,ViewWidth-108,ViewHeight-226));
        if(reset)combat.player=new Vector2(combat.arena.center.x,combat.arena.center.y+42);
    }
    void UpdatePrologue(float dt){
        if(prologue==null)return;
        storyImpact=Mathf.Max(0,storyImpact-dt*2.7f);shotClock+=dt;cutBlend=Mathf.Max(0,cutBlend-dt*6);narrationSource.mute=source.mute;
        bool advance=storyAdvance||Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return);
        bool skip=storySkip||Input.GetKeyDown(KeyCode.Escape);storyAdvance=storySkip=false;
        if(prologue.Step(dt,advance,skip)){StartBattle();return;}
        if(prologue.index!=lastStoryBeat){
            lastStoryBeat=prologue.index;
            if(lastStoryArt!=prologue.current.art){previousStoryArt=lastStoryArt;lastStoryArt=prologue.current.art;shotClock=0;cutBlend=(lastStoryArt==1&&previousStoryArt==0)?1:0;}
            narrationSource.Stop();narrationSource.clip=narration[prologue.index];if(narrationSource.clip!=null)narrationSource.Play();
            if(prologue.current.impact){storyImpact=1;source.PlayOneShot(prologue.current.art==-2?specialSound:wallSound,.75f);}
            heartbeatSource.volume=narrationSource.clip!=null?.12f:prologue.index>=9?.27f:.18f;
        }
    }
    void DrawSceneArt(int panel,Rect target,float progress,float alpha=1){
        if(prologueAtlas==null){Floor();Box(0,0,ViewWidth,ViewHeight,new Color(.03f,.04f,.07f,.6f));return;}
        Rect tile=new Rect(panel%3/3f,panel<3?.5f:0,1/3f,.5f);
        float ratio=target.width/target.height;float tileAspect=prologueAtlas.width/(float)prologueAtlas.height*(tile.width/tile.height);
        float width=tile.width,height=tile.height;
        if(ratio>tileAspect)height*=tileAspect/ratio;else width*=ratio/tileAspect;
        // Holds, restrained pushes, and matched eyelines rather than a new zoom for each subtitle.
        float zoom=(panel==4?1.00f:panel==5?1.025f:1.005f)+Mathf.Clamp01(progress)*(panel==4?0:.022f);width/=zoom;height/=zoom;
        float pan=(panel==0?-.012f:panel==2?.006f:0)*Mathf.Clamp01(progress);
        Rect uv=new Rect(tile.center.x-width*.5f+pan,tile.center.y-height*.5f,width,height);
        uv.x=Mathf.Clamp(uv.x,tile.xMin,tile.xMax-uv.width);uv.y=Mathf.Clamp(uv.y,tile.yMin,tile.yMax-uv.height);
        GUI.color=new Color(1,1,1,alpha);GUI.DrawTextureWithTexCoords(target,prologueAtlas,uv);GUI.color=Color.white;
    }
    void ShadeBottom(float from,float alpha){
        float height=ViewHeight-from;
        for(int i=0;i<64;i++){float t=(i+1)/64f;Box(0,from+height*i/64f,ViewWidth,height/64+1,new Color(.025f,.025f,.045f,alpha*t*t));}
    }
    void DrawPrologue(){
        if(prologue==null)return;var beat=prologue.current;float progress=Mathf.Clamp01(shotClock/9f);
        Box(0,0,ViewWidth,ViewHeight,new Color(.015f,.02f,.035f));
        Matrix4x4 old=GUI.matrix;
        if(storyImpact>0&&beat.art>=0){Vector2 offset=new Vector2(Mathf.Sin(pulse*115),Mathf.Cos(pulse*93))*storyImpact*6;GUI.matrix=old*Matrix4x4.Translate(offset);}
        if(beat.art>=0)DrawSceneArt(beat.art,new Rect(0,0,ViewWidth,ViewHeight),progress);
        if(cutBlend>0&&previousStoryArt>=0&&beat.art>=0)DrawSceneArt(previousStoryArt,new Rect(0,0,ViewWidth,ViewHeight),1,cutBlend);
        GUI.matrix=old;
        if(beat.art==-2){Box(0,0,ViewWidth,ViewHeight,Color.white);return;}
        float entrance=prologue.index==0?1-EaseOut(prologue.elapsed/.65f):0;if(entrance>0)Box(0,0,ViewWidth,ViewHeight,new Color(.015f,.02f,.035f,entrance*.8f));
        ShadeBottom(ViewHeight*.48f,.96f);
        Text("PROLOGUE  /  팔룡회",24,22,420,25,13,new Color(Cream.r,Cream.g,Cream.b,.7f),TextAnchor.MiddleLeft,true);
        Text("건너뛰기  ›",ViewWidth-146,18,124,38,15,new Color(Cream.r,Cream.g,Cream.b,.8f),TextAnchor.MiddleRight,true);
        if(GUI.Button(new Rect(ViewWidth-150,14,134,45),GUIContent.none,GUIStyle.none))storySkip=true;
        float w=Mathf.Min(ViewWidth-54,820),x=(ViewWidth-w)*.5f;
        bool rage=prologue.index>=11;int size=rage?Mathf.RoundToInt(Mathf.Min(42,w/10f)):Mathf.RoundToInt(Mathf.Min(29,w/15f));
        float y=ViewHeight-(rage?245:237);float lift=(1-EaseOut(prologue.elapsed/.25f))*16;
        if(beat.speaker.Length>0){Box(x,y-36+lift,29,3,rage?Coral:Gold);Text(beat.speaker,x+42,y-51+lift,w-42,32,17,rage?Gold:new Color(.88f,.81f,.66f),TextAnchor.MiddleLeft,true);}
        Outlined(prologue.visibleText,x,y+lift,w,151,size,rage?Cream:new Color(.97f,.95f,.9f),TextAnchor.UpperLeft,rage);
        if(prologue.textComplete){float a=.35f+Mathf.Sin(pulse*3)*.18f;Text("클릭 / SPACE 로 계속",x,ViewHeight-49,w,25,13,new Color(1,1,1,a),TextAnchor.MiddleRight);}
        if(GUI.Button(new Rect(0,80,ViewWidth,ViewHeight-80),GUIContent.none,GUIStyle.none))storyAdvance=true;
        if(storyImpact>0&&prologue.index>=11)Box(0,0,ViewWidth,ViewHeight,new Color(1,.48f,.16f,storyImpact*.09f));
    }
    void DrawBattleReveal(){
        if(battleReveal<=0)return;
        // A short fade shares the same alley backdrop; combat is unlocked only after the dialogue ends.
        Box(0,0,ViewWidth,ViewHeight,new Color(.96f,.92f,.77f,Mathf.Clamp01((battleReveal-.55f)*1.8f)));
        float a=Mathf.Clamp01(battleReveal*2);
        Outlined("벽으로 날려! 연쇄로 끝내!",(ViewWidth-Mathf.Min(600,ViewWidth))*.5f,ViewHeight*.30f,Mathf.Min(600,ViewWidth),68,31,new Color(1,.94f,.76f,a),TextAnchor.MiddleCenter,true);
    }
    void DestroyPrologue(){if(heartbeatSource!=null)heartbeatSource.Stop();if(heartbeat!=null)Destroy(heartbeat);if(narrationSource!=null)narrationSource.Stop();}
}
