#if UNITY_EDITOR || SLING_HEADLESS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// Window-free checks for the hand-made stages, the screen flow and the aim rules, plus bot runs of each stage.
// The bots are arithmetic, not people: they show what the numbers allow, never whether a stage is fun or readable.
public static class DadThrowStageChecks
{
    const DadThrowModel.Weapon Golf=DadThrowModel.Weapon.Golf,Shoe=DadThrowModel.Weapon.Shoe;
    const DadThrowModel.Phase Playing=DadThrowModel.Phase.Playing,Cleared=DadThrowModel.Phase.WaveClear,Lost=DadThrowModel.Phase.Lost;
    const string Kinds="\"enemies\":{\"normal\":{\"resource\":\"x/walk\",\"frames\":2,\"facesLeft\":true,\"canvas\":768,\"pivotX\":384,\"pivotY\":720,\"standPixels\":560,\"heights\":[102]}}";
    const string OneGroup="{\"at\":0,\"count\":2,\"firstX\":800,\"spacing\":50,\"enemy\":\"normal\",\"speed\":48,\"hp\":1}";
    static string StageJson(string id="1-1",string background="bg",string health="5",string groups=OneGroup,string extra="")
    {return "{\"id\":\""+id+"\",\"backgroundKey\":\""+background+"\",\"health\":"+health+",\"baseReward\":12"+extra+",\"groups\":["+groups+"]}";}
    static string FileJson(string stages){return "{"+Kinds+",\"stages\":["+stages+"]}";}
    static readonly string[] Known={"bg"};
    static DadThrowStageSet Read(string json){return DadThrowStageSet.Parse(json,Known,215);}
    static bool Refused(string json){try{Read(json);}catch(ArgumentException){return true;}return false;}
    static string Group(string change,string to){return OneGroup.Replace(change,to);}

    static string Line(string label,DadThrowStage stage,DadThrowBots.Run run)
    {
        DadThrowModel m=run.end;
        return string.Format(CultureInfo.InvariantCulture,"STAGE {0} {1}: {2} at {3:0.0}s, house {4}/{5}, golf {6}, shoe {7}, most people one ball reached {8}, got in {9}",
            stage.id,label,run.Outcome,m.Clock,m.Health,stage.health,run.golf,run.shoe,m.BestChain,m.Escaped);
    }
    public static string Run(DadThrowLayout layout,Action<bool,string> check)
    {
        var notes=new StringBuilder();DadThrowField field=layout.Field,test=DadThrowChecks.Test;

        // ── stage file ──
        DadThrowStageSet set=DadThrowStageSet.Parse(System.IO.File.ReadAllText("Assets/DadSchoolBoss/Resources/DadThrowStages/chapter1.json"),layout.Backgrounds,field.Gate);
        DadThrowStage first=set.stages[0],third=set.stages[1];
        check(set.stages.Count==2&&first.id=="1-1"&&third.id=="1-3"&&first.Total==100&&third.Total==160&&first.health==5&&third.health==5,
            "chapter 1 file holds stage 1-1 with 100 people and 1-3 with 160, both on five gate touches");
        bool plain=true;foreach(DadThrowStage stage in set.stages)foreach(DadThrowStage.Group g in stage.groups)plain&=g.hp==1&&g.speed>=120&&g.speed<=240&&g.kind.id=="normal";
        check(plain,"chapter 1 uses one kind of opponent with one health, and nobody strolls: only the placing and the running pace change");
        int opening=0;float widest=0,lastAt=0;
        for(int i=0;i<first.groups.Count;i++){if(first.groups[i].at<=0)opening+=first.groups[i].count;if(i>0)widest=Mathf.Max(widest,first.groups[i].at-first.groups[i-1].at);lastAt=first.groups[i].at;}
        check(opening>=20&&widest<=2&&lastAt<=10,"1-1 is a stampede from its first second: twenty or more already running, a new pack at least every two seconds, everyone on the street within ten");
        DadThrowStage.Kind normal=set.kinds["normal"];Rect placed=normal.Place(700,field.Ground,normal.Height(1));float scale=normal.Height(1)/normal.stand;
        check(Mathf.Abs(placed.x+normal.pivotX*scale-700)<.01f&&Mathf.Abs(placed.y+normal.pivotY*scale-field.Ground)<.01f,"an opponent's picture is placed with its feet on its position and on the feet line");
        check(normal.facesLeft&&!normal.Mirror,"opponents are painted facing left, the way they walk, so their picture is not mirrored");
        bool sizes=true;for(int id=1;id<=12;id++)sizes&=normal.Height(id)>=100&&normal.Height(id)<=115;
        check(sizes,"opponents are drawn between 100 and 115 tall");
        float tallest=0;foreach(float height in normal.heights)tallest=Mathf.Max(tallest,height);
        check(layout.DadHeight>=tallest&&layout.DadHeight<=tallest*1.1f,"everyone on the street is about one size: dad is no shorter than the tallest opponent and at most a tenth taller");
        check(Read(FileJson(StageJson())).stages[0].Total==2,"a well-formed stage file is accepted");
        check(Refused(FileJson(StageJson()+","+StageJson())),"a stage id listed twice is refused");
        check(Refused(FileJson(StageJson(groups:Group("\"at\":0","\"at\":-1")))),"a group before time zero is refused");
        check(Refused(FileJson(StageJson(groups:Group("\"hp\":1","\"hp\":0")))),"a group with no health is refused");
        check(Refused(FileJson(StageJson(groups:Group("\"enemy\":\"normal\"","\"enemy\":\"ghost\"")))),"an opponent the file does not define is refused");
        check(Refused(FileJson(StageJson(background:"nowhere"))),"a background without measured anchors is refused");
        check(Refused(FileJson(StageJson(groups:Group("\"count\":2","\"count\":"+(DadThrowStageSet.MaxTotal+1))))),"more people than the ceiling is refused");
        check(Refused(FileJson(StageJson(groups:Group("\"firstX\":800","\"firstX\":215")))),"a group that starts at or behind the house line is refused");
        check(Refused(FileJson(StageJson(groups:Group("\"count\":2","\"count\":0"))))&&Refused(FileJson(StageJson(groups:Group("\"speed\":48","\"speed\":0"))))
            &&Refused(FileJson(StageJson(groups:Group("\"spacing\":50","\"spacing\":0"))))&&Refused(FileJson(StageJson(groups:""))),
            "an empty group, a group that does not walk, people stacked on one spot and a stage with no groups are refused");
        check(Refused(FileJson(StageJson(health:"0")))&&Refused(FileJson(StageJson(health:"6")))&&Refused(FileJson(StageJson(extra:",\"hint\":\"dance\"")))&&Refused(FileJson("")),
            "house health outside one to five, an unknown hint and a file with no stages are refused");
        check(Refused("{"+Kinds+","+Kinds+",\"stages\":["+StageJson()+"]}")&&Refused("{\"stages\":["),"a repeated key or a cut-off file is refused rather than half read");
        DadThrowStage shuffled=Read(FileJson(StageJson(groups:Group("\"at\":0","\"at\":9")+","+Group("\"at\":0","\"at\":2")))).stages[0];
        check(shuffled.groups[0].at==2&&shuffled.groups[1].at==9,"groups are walked in arrival order whatever order they were written in");

        // ── a stage is its table and nothing else ──
        var m=new DadThrowModel(field,first);m.Tick(1/120f);
        check(m.Health==5&&m.Total==first.Total&&m.Spawned==opening&&m.enemies.Count==opening&&m.Remaining==first.Total
            &&Mathf.Abs(m.enemies[0].x-first.groups[0].firstX)<2&&m.enemies[0].speed==first.groups[0].speed,
            "1-1 opens with its first crowd already on the street and the rest still to come");
        // The rule checks below stand on a small table of their own, so retuning the game's stages cannot bend them.
        var lesson=new DadThrowStage{id="lesson",health=5};
        lesson.groups.Add(new DadThrowStage.Group{at=0,count=1,firstX=650,speed=48,hp=1,kind=normal});
        lesson.groups.Add(new DadThrowStage.Group{at=3,count=3,firstX=1040,spacing=90,speed=48,hp=1,kind=normal});
        m=new DadThrowModel(field,lesson);m.Tick(1/120f);
        DadThrowChecks.Advance(m,3.1f);
        check(m.Spawned==4&&m.enemies.Count==4&&Mathf.Abs(m.enemies[0].x-(650-48*m.Clock))<1&&Mathf.Abs(m.enemies[3].x-m.enemies[1].x-180)<.01f,
            "the second group walks in at its time, spaced as written, while the first keeps walking");
        // Where a flat throw comes down, wherever the art puts the hand: someone standing just short of it is hit.
        var flat=new List<Vector2>();new DadThrowModel(field,lesson).Predict(Golf,new Vector2(150,60),flat);float reach=flat[flat.Count-1].x-5;
        m=new DadThrowModel(field,lesson);m.Tick(1/120f);m.enemies[0].hp=1;m.enemies[0].x=reach;
        m.Fire(Golf,new Vector2(150,60));DadThrowChecks.Advance(m,1);
        check(m.Killed==1&&m.enemies.Count==0&&m.phase==Playing&&m.Remaining==3,"an empty street is not a clear while people are still on the schedule");
        m=new DadThrowModel(field,lesson);
        for(int i=0;i<120*60&&m.phase==Playing;i++){m.Tick(1/120f);for(int j=m.enemies.Count-1;j>=0;j--)m.enemies[j].x=Mathf.Min(m.enemies[j].x,reach);if(m.CanFire)m.Fire(Golf,new Vector2(150,60));}
        check(m.phase==Cleared&&m.Killed==4&&m.Spawned==4&&m.Remaining==0&&m.Health==5,"the stage clears when everyone on its table has been dealt with, and never spawns more than the table");
        check(m.Coins==0&&!m.Upgrade()&&!m.NextWave()&&m.Level==0,"a stage neither pays nor grows through the wave prototype's economy");
        m=LastOneIn(test);
        check(m.phase==Lost&&m.Health==0&&m.Remaining==0,"the last person walking in at one health is a defeat, not a clear");
        m=new DadThrowModel(field,third);DadThrowChecks.Advance(m,60);
        check(m.phase==Lost&&m.Spawned<=third.Total,"leaving 1-3 alone ends in defeat");
        m=new DadThrowModel(field,first);DadThrowChecks.Advance(m,6);
        check(m.phase==Lost,"1-1 gives no time to look around: six seconds without a throw and the gate has been touched five times");

        // ── throw rules the plan lists (§19.1) ──
        m=new DadThrowModel(test);
        check(!m.Fire(Golf,new Vector2(17,0))&&m.Reload==0&&m.projectiles.Count==0&&m.Fire(Golf,new Vector2(18,0)),"a pull of 17 throws nothing and costs no reload; 18 throws");
        m=new DadThrowModel(test,DadThrowStage.Empty());var line=new List<DadThrowModel.Enemy>();
        for(int i=0;i<6;i++)line.Add(m.AddEnemy(500+i*20,10,.0001f));
        m.projectiles.Add(new DadThrowModel.Projectile{weapon=Golf,pos=new Vector2(300,535),vel=new Vector2(60000,0),remaining=5});m.Tick(1/120f);
        bool nearest=line[5].hp==10;for(int i=0;i<5;i++)nearest&=line[i].hp==9;
        check(nearest,"a ball that crosses six people in one step hits the five nearest along its flight, not the last five listed");
        m=new DadThrowModel(test,DadThrowStage.Empty());
        DadThrowModel.Enemy struck=m.AddEnemy(600,10,.0001f),in84=m.AddEnemy(684,10,.0001f),at85=m.AddEnemy(685,10,.0001f),out86=m.AddEnemy(686,10,.0001f),left84=m.AddEnemy(516,10,.0001f),left85=m.AddEnemy(515,10,.0001f);
        m.projectiles.Add(new DadThrowModel.Projectile{weapon=Shoe,pos=new Vector2(600,485),vel=new Vector2(0,600),remaining=1});m.Tick(1/120f);
        check(struck.x==705&&in84.x==789&&left84.x==621&&at85.x==685&&out86.x==686&&left85.x==515,"a shoe pushes people closer than 85 to where it lands, and nobody at 85 or beyond");
        check(struck.hp==10&&in84.hp==10&&left84.hp==10&&struck.slow==2.5f&&in84.slow==2.5f&&at85.slow==0,"a shoe hurts nobody: the person it lands on and those pushed with them are slowed, and still there to be hit");
        m.projectiles.Add(new DadThrowModel.Projectile{weapon=Shoe,pos=new Vector2(705,485),vel=new Vector2(0,600),remaining=1});m.Tick(1/120f);
        check(struck.slow==2.5f&&struck.hp==10,"a second shoe renews the slow to 2.5 seconds instead of adding to it");
        m=new DadThrowModel(test,DadThrowStage.Empty()){Rules=new DadThrowRules{ShoeDamage=1}};struck=m.AddEnemy(600,1,.0001f);in84=m.AddEnemy(684,10,.0001f);
        m.projectiles.Add(new DadThrowModel.Projectile{weapon=Shoe,pos=new Vector2(600,485),vel=new Vector2(0,600),remaining=1});m.Tick(1/120f);
        check(m.Killed==1&&in84.x==789&&in84.slow==2.5f,"in a comparison run where the shoe hurts, the people around a target it takes out are still pushed");
        DadThrowRules rules=new DadThrowModel(test).Rules;
        check(rules.MaxBounces==0&&rules.ShoeDamage==0&&rules.Pierce==12&&rules.GolfReload==.3f&&rules.ShoeReload==.7f&&rules.ShoeRadius==85&&rules.ShoePush==105&&rules.SlowTime==2.5f&&rules.SlowFactor==.35f&&rules.AimPace==1,
            "the game's throw numbers: a ball ends at its first landing and goes through twelve, a shoe pushes 105 within 85 without hurting, hands reload in 0.3 and 0.7 seconds, and the street keeps full speed while the player aims");
        m=new DadThrowModel(test);m.Fire(Golf,new Vector2(150,50));var dots=new List<Vector2>();m.Predict(Golf,new Vector2(150,50),dots);Vector2 last=m.projectiles[0].pos;
        for(int i=0;i<120&&m.projectiles.Count>0;i++){last=m.projectiles[0].pos;m.Tick(1/120f);}
        check(m.projectiles.Count==0&&Vector2.Distance(last,dots[dots.Count-1])<12,"a golf ball ends at its first landing, where the aim dots end");
        var path=new List<Vector2>();m=new DadThrowModel(test);m.Predict(Golf,new Vector2(150,-40),path);
        bool flight=path.Count>3&&Mathf.Abs(path[path.Count-1].y-(test.Ground-11))<.001f;for(int i=0;i<path.Count-1;i++)flight&=path[i].y<test.Ground-11;
        check(flight,"the aim dots run from the hand to the first landing and stop there");
        float[] frames={1/60f,1/30f,1/10f};var seen=new List<DadThrowModel>();
        foreach(float frame in frames)
        {
            var s=new DadThrowModel(test,DadThrowStage.Empty());s.AddEnemy(430,1,48);s.AddEnemy(460,1,48);s.AddEnemy(490,1,48);s.AddEnemy(300,1,48);
            s.Fire(Golf,new Vector2(150,50));for(int i=0;i<Mathf.RoundToInt(1.5f/frame);i++)s.Tick(frame);seen.Add(s);
        }
        bool same=true;foreach(DadThrowModel s in seen)same&=s.Killed==seen[0].Killed&&s.enemies.Count==seen[0].enemies.Count&&s.enemies.Count==1&&Mathf.Abs(s.enemies[0].x-seen[0].enemies[0].x)<.5f;
        check(same&&seen[0].Killed==3,"the same throw gives the same result at 60, 30 and 10 frames a second");

        // ── screen flow (§5) ──
        var flow=new DadThrowFlow(set.stages,field);
        check(flow.Now==DadThrowFlow.Screen.Boot&&!flow.Start()&&flow.Model==null&&flow.BootAlpha==0,"the game opens on the logo and nothing can be started behind it");
        flow.Tick(.6f);check(flow.BootAlpha==1&&flow.Now==DadThrowFlow.Screen.Boot,"the logo is fully shown mid-way");
        flow.Tick(.6f);check(flow.Now==DadThrowFlow.Screen.Title&&!flow.Continuing,"after 1.2 seconds the title is up, offering a new game");
        check(!flow.Pause()&&!flow.Resume()&&!flow.Retry()&&!flow.Next()&&!flow.ToTitle(),"the title accepts nothing but start");
        check(flow.Start()&&flow.Now==DadThrowFlow.Screen.Playing&&flow.Stage.id=="1-1"&&flow.Runs==1&&!flow.Start()&&flow.Runs==1,"start opens 1-1 once; a second tap starts nothing more");
        flow.Tick(1);float clock=flow.Model.Clock;int standing=flow.Model.enemies.Count;
        check(flow.Pause()&&flow.Now==DadThrowFlow.Screen.Paused,"pause stops a live fight");
        flow.Tick(5);check(flow.Model.Clock==clock&&flow.Model.enemies.Count==standing&&standing>0,"nothing moves while paused");
        check(flow.Resume()&&flow.Now==DadThrowFlow.Screen.Playing&&!flow.Resume(),"resume returns to the same fight");
        flow.Interrupted();check(flow.Now==DadThrowFlow.Screen.Paused,"leaving the app pauses a live fight");
        check(flow.Back()&&flow.Now==DadThrowFlow.Screen.Title&&flow.Model==null,"back from the pause goes to the title and drops the stage in progress");
        flow.Start();check(flow.Back()&&flow.Now==DadThrowFlow.Screen.Paused,"back during a fight pauses it");
        flow.Resume();var bot=new DadThrowBots.Run();
        for(int i=0;i<120*120&&flow.Now==DadThrowFlow.Screen.Playing;i++){DadThrowBots.Spam(flow.Model,bot);flow.Tick(1/120f);}
        check(flow.Now==DadThrowFlow.Screen.Clear&&flow.Furthest==1&&flow.HasNext,"clearing 1-1 opens the next stage");
        flow.Tick(5);flow.Interrupted();check(flow.Now==DadThrowFlow.Screen.Clear&&flow.Model.projectiles.Count==0,"a result stays a result: nothing keeps flying behind the card, and time or leaving the app does not restart the fight");
        int runs=flow.Runs;
        check(flow.Next()&&flow.Stage.id=="1-3"&&flow.Runs==runs+1&&flow.Model.Health==5&&flow.Model.Clock==0&&!flow.Next(),"next opens 1-3 clean, and only once");
        for(int i=0;i<120*60&&flow.Now==DadThrowFlow.Screen.Playing;i++)flow.Tick(1/120f);
        check(flow.Now==DadThrowFlow.Screen.Defeat&&flow.Furthest==1&&!flow.Next(),"letting everyone in ends in defeat, takes back nothing already opened and offers no next stage");
        runs=flow.Runs;
        check(flow.Retry()&&flow.Now==DadThrowFlow.Screen.Playing&&flow.Stage.id=="1-3"&&flow.Runs==runs+1&&flow.Model.Health==5&&flow.Model.Clock==0&&flow.Model.enemies.Count==0
            &&flow.Model.projectiles.Count==0&&flow.Model.Reload==0&&flow.Model.Killed==0&&flow.Model.Escaped==0,
            "retry starts the same stage clean: full house health, empty street, nothing in the air, no reload");
        flow.Tick(2);flow.Pause();flow.ToTitle();
        check(flow.Continuing&&flow.Start()&&flow.Stage.id=="1-3"&&flow.Model.Clock==0,"the title offers to continue, and continuing starts the furthest opened stage from its beginning");
        for(int i=0;i<120*240&&flow.Now==DadThrowFlow.Screen.Playing;i++){DadThrowBots.Spam(flow.Model,bot);flow.Tick(1/120f);}
        check(flow.Now==DadThrowFlow.Screen.Clear&&!flow.HasNext&&!flow.Next()&&flow.Furthest==1,"after the last stage there is no next stage to step into");
        check(flow.Retry()&&flow.Stage.id=="1-3"&&flow.Now==DadThrowFlow.Screen.Playing,"the last stage can be played again from its clear card");

        // ── aim (§7) ──
        var aim=new DadThrowAim();Vector2 street=new Vector2(700,300),pull;
        check(!aim.Begin(1,DadThrowLayout.GolfButton.center,true)&&!aim.Begin(1,DadThrowLayout.PauseButton.center,true)&&!aim.Begin(1,street,false),
            "an aim cannot start on a button, nor while the hand is not ready");
        check(aim.Begin(1,street,true)&&aim.Active&&!aim.Begin(2,street,true),"the first finger down on the street owns the aim");
        aim.Move(1,new Vector2(600,330));
        check(!aim.Move(2,new Vector2(100,100))&&!aim.End(2,new Vector2(100,100),out pull)&&aim.Active&&aim.Now==new Vector2(600,330),"another finger can neither move the aim nor let it go");
        check(aim.End(1,new Vector2(580,340),out pull)&&!aim.Active&&pull==new Vector2(120,-40),"the owner letting go hands back the pull once");
        aim.Begin(1,street,true);aim.Move(1,new Vector2(560,330));aim.Cancel();
        check(!aim.End(1,new Vector2(560,330),out pull)&&pull==Vector2.zero,"after a pause, lost focus or a turn of the screen, lifting the finger throws nothing");

        // ── opening, ending and what is remembered (story v2) ──
        const DadThrowFlow.Screen Intro=DadThrowFlow.Screen.Intro,Fight=DadThrowFlow.Screen.Playing,Won=DadThrowFlow.Screen.Clear,Closing=DadThrowFlow.Screen.Ending;
        string tale=System.IO.File.ReadAllText("Assets/DadSchoolBoss/Resources/DadThrowStoryV1/story.json");
        DadThrowStory story=DadThrowStory.Parse(tale);
        check(story.opening.Count==7&&Mathf.Abs(story.OpeningSeconds-24)<.001f&&story.ending.Count==4&&Mathf.Abs(story.EndingSeconds-12)<.001f,
            "the opening is seven cuts over 24 seconds and the ending four cuts over 12");
        DadThrowStory.Cut lastCut=story.opening[story.opening.Count-1];bool onlyLast=lastCut.battle&&lastCut.actors.Count==0,kidsInside=true,said=true;
        var everyCut=new List<DadThrowStory.Cut>(story.opening);everyCut.AddRange(story.ending);
        foreach(DadThrowStory.Cut cut in everyCut)
        {
            if(cut!=lastCut)onlyLast&=!cut.battle;
            if(cut.battle||cut.backdrop=="street")kidsInside&=!cut.actors.Contains("mansu")&&!cut.actors.Contains("cheolsu");
            said&=cut.name.Length>0&&cut.line.Length>0&&cut.seconds>=2;
        }
        check(onlyLast,"only the last cut of the opening is the fight's own camera, with nobody but dad in it");
        check(kidsInside,"the children stand indoors only, never on the street where the contest is");
        check(said,"every cut has a speaker, a line and at least two seconds to read it");
        check(DadThrowStory.At(story.opening,0)==story.opening[0]&&DadThrowStory.At(story.opening,3.99f)==story.opening[0]&&DadThrowStory.At(story.opening,4)==story.opening[1]
            &&DadThrowStory.At(story.opening,23.9f)==lastCut&&DadThrowStory.At(story.opening,99)==lastCut,"each cut shows for its own seconds, and the last stays once the time has run out");
        Rect screen=new Rect(0,0,DadThrowModel.Width,DadThrowModel.Height);Func<Rect,bool> shown=r=>r.xMin>=0&&r.yMin>=0&&r.xMax<=screen.xMax&&r.yMax<=screen.yMax;
        bool placed2=shown(DadThrowStory.Dialogue)&&shown(DadThrowStory.Skip)&&shown(DadThrowStory.Rival)&&shown(DadThrowStory.Prop)&&DadThrowStory.Skip.height>=64
            &&!DadThrowStory.Skip.Overlaps(DadThrowStory.Dialogue)&&DadThrowStory.DialogueName.yMax<=DadThrowStory.DialogueLine.yMin&&DadThrowStory.DialogueLine.yMax<=DadThrowStory.Dialogue.yMax;
        foreach(DadThrowStory.Cut cut in everyCut)foreach(string who in cut.actors)
        {Rect r=story.ActorRect(story.actors[who],cut);placed2&=r.yMax<=screen.yMax&&r.yMin>=0&&r.xMax>100&&r.xMin<screen.xMax-100;}
        check(placed2,"the dialogue box, the skip button and every cutout sit on the screen; skip is at least 64 tall and clear of the dialogue");
        Func<string,bool> badTale=text=>{try{DadThrowStory.Parse(text);}catch(ArgumentException){return true;}return false;};
        check(badTale(tale.Replace("\"actors\": [\"chilsuk\", \"mansu\"]","\"actors\": [\"chilsuk\", \"nobody\"]"))&&badTale(tale.Replace("\"backdrop\": \"street\"","\"backdrop\": \"moon\""))
            &&badTale(tale.Replace("\"seconds\": 4","\"seconds\": 0"))&&badTale("{}"),"a story file with an unknown person, an unknown backdrop, a cut of no length or nothing in it is refused");

        var memory=new DadThrowSave();var told=new DadThrowFlow(set.stages,field,story,memory,null);told.Tick(2);
        check(told.Start()&&told.Now==Intro&&told.Model==null&&told.Cut==story.opening[0]&&told.Stage.id=="1-1","a new player's first start plays the opening, not the fight");
        told.Tick(5);
        check(told.Cut==story.opening[1]&&!told.Pause()&&!told.Start()&&!told.Next()&&!told.Retry()&&!told.ToTitle()&&told.Now==Intro,"the opening moves on by itself and accepts nothing but skip");
        told.Interrupted();check(told.Now==Intro&&told.StoryClock==5,"leaving the app during the opening changes nothing: its clock only runs while the game does");
        told.Tick(20);
        check(told.Now==Fight&&told.Stage.id=="1-1"&&told.Runs==1&&memory.introSeen&&told.WaitingForAim,"when the opening runs out the first stage starts once, and the opening is remembered as seen");
        told.Tick(3);told.Tick(3);
        check(told.Model.enemies.Count==opening&&told.Model.Spawned==opening&&told.Model.Clock<.02f&&told.Model.Health==5,"after the opening the first crowd stands in view and nothing moves until the first pull");
        told.AimStarted();told.Tick(.2f);
        check(!told.WaitingForAim&&told.Model.Clock>.1f,"the first pull lets the street move");
        var skipped=new DadThrowFlow(set.stages,field,story,new DadThrowSave(),null);skipped.Tick(2);skipped.Start();skipped.Tick(1);
        check(skipped.Skip()&&skipped.Now==Fight&&skipped.Runs==1&&!skipped.Skip()&&skipped.Runs==1&&skipped.WaitingForAim&&skipped.Save.introSeen,
            "skip starts the same first stage exactly once however often it is pressed");
        var backed=new DadThrowFlow(set.stages,field,story,new DadThrowSave(),null);backed.Tick(2);backed.Start();
        check(backed.Back()&&backed.Now==Fight&&backed.Runs==1,"the back key during the opening skips it");
        var again=new DadThrowFlow(set.stages,field,story,memory,null);again.Tick(2);
        check(again.Start()&&again.Now==Fight&&!again.WaitingForAim&&again.Runs==1,"a player who has seen the opening goes straight to the fight");

        var robot=new DadThrowBots.Run();
        Action finish=()=>{for(int i=0;i<120*240&&told.Now==Fight;i++){DadThrowBots.Spam(told.Model,robot);told.Tick(1/120f);}};
        finish();
        check(told.Now==Won&&told.Word!=null&&told.Word.name=="지호 아빠"&&memory.cleared.Contains("1-1")&&memory.furthest==1,"the first clear of 1-1 brings one line from the story and is remembered");
        told.Retry();finish();
        check(told.Now==Won&&told.Word==null&&memory.cleared.Count==1,"a stage cleared before brings no line a second time");
        told.Next();finish();
        check(told.Now==Closing&&told.Cut==story.ending[0]&&memory.cleared.Contains("1-3")&&!told.Next()&&!told.Retry(),"the first clear of the chapter's last stage plays the ending before the clear card");
        told.Tick(12.1f);
        check(told.Now==Won&&told.Word==null&&!told.HasNext,"the ending leads to the clear card, which adds no second line");
        told.Retry();finish();
        check(told.Now==Won&&told.Cut==null,"the ending plays once: clearing the last stage again goes straight to the card");
        var closing=new DadThrowFlow(set.stages,field,story,new DadThrowSave{introSeen=true,furthest=1},null);closing.Tick(2);closing.Start();
        check(closing.Stage.id=="1-3","a remembered player continues at the furthest stage they opened");
        for(int i=0;i<120*240&&closing.Now==Fight;i++){DadThrowBots.Spam(closing.Model,robot);closing.Tick(1/120f);}
        check(closing.Now==Closing&&closing.Skip()&&closing.Now==Won&&!closing.Skip(),"skip on the ending goes to the clear card once");
        check(new DadThrowFlow(set.stages,field,story,new DadThrowSave{furthest=9},null).Furthest==set.stages.Count-1,"a save that points past the stages this build has is held to the last one");

        var kept=new DadThrowSave{introSeen=true,furthest=1};kept.cleared.Add("1-1");DadThrowSave back=DadThrowSave.Parse(kept.ToJson());
        check(back.introSeen&&back.furthest==1&&back.cleared.Count==1&&back.cleared[0]=="1-1","what is remembered survives being written and read");
        Func<string,bool> badSave=text=>{try{DadThrowSave.Parse(text);}catch(ArgumentException){return true;}return false;};
        check(badSave("{}")&&badSave(kept.ToJson().Replace("\"schemaVersion\":1","\"schemaVersion\":2"))&&badSave("not a save"),"an empty, foreign or unreadable save is refused rather than guessed at");
        string folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"dad-throw-check-"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(folder);
        try
        {
            string file=System.IO.Path.Combine(folder,"save.json"),problem;
            DadThrowSave none=DadThrowSave.Load(file,out problem);
            check(problem==null&&!none.introSeen&&none.cleared.Count==0,"no save file means a new player, with nothing to report");
            new DadThrowSave{introSeen=true}.Write(file);kept.Write(file);
            DadThrowSave read=DadThrowSave.Load(file,out problem);
            check(problem==null&&read.furthest==1&&System.IO.File.Exists(file+".bak")&&!System.IO.File.Exists(file+".tmp"),"a save is written beside the file and swapped in, keeping the one before as a backup");
            System.IO.File.WriteAllText(file,"{broken");read=DadThrowSave.Load(file,out problem);
            check(problem!=null&&read.introSeen&&read.furthest==0,"a broken save falls back to the backup and says so");
            System.IO.File.WriteAllText(file+".bak","{broken");read=DadThrowSave.Load(file,out problem);
            check(problem!=null&&!read.introSeen,"when the backup is broken too the player starts new and the reason is reported");
            var written=new DadThrowFlow(set.stages,field,story,new DadThrowSave(),System.IO.Path.Combine(folder,"flow.json"));written.Tick(2);written.Start();written.Skip();
            check(DadThrowSave.Load(System.IO.Path.Combine(folder,"flow.json"),out problem).introSeen&&written.SaveProblem==null,"finishing the opening is saved to disk at once");
            var blocked=new DadThrowFlow(set.stages,field,story,new DadThrowSave(),System.IO.Path.Combine(folder,"missing-folder","save.json"));blocked.Tick(2);blocked.Start();blocked.Skip();
            check(blocked.Now==Fight&&blocked.SaveProblem!=null,"a save that cannot be written is reported and play goes on");
        }
        finally{System.IO.Directory.Delete(folder,true);}

        // ── what the numbers allow: bot runs (not people) ──
        foreach(DadThrowStage stage in set.stages)
        {
            DadThrowBots.Run idleRun=DadThrowBots.Play(stage,field,DadThrowBots.Idle),eagerRun=DadThrowBots.Play(stage,field,DadThrowBots.Spam),
                patientRun=DadThrowBots.Play(stage,field,DadThrowBots.Patient),shoeRun=DadThrowBots.Play(stage,field,DadThrowBots.ShoeFirst);
            notes.AppendLine(Line("no throws",stage,idleRun));notes.AppendLine(Line("golf thrown at once",stage,eagerRun));
            notes.AppendLine(Line("golf held for three",stage,patientRun));notes.AppendLine(Line("shoe on the lead, then golf",stage,shoeRun));
            DadThrowModel idle=idleRun.end,eager=eagerRun.end;
            // Fewer people than house health cannot break the house: such a stage ends in a clear whatever the player does.
            bool harmless=stage.Total<stage.health;
            check(harmless?idle.phase==Cleared&&idle.Health==stage.health-stage.Total:idle.phase==Lost,
                stage.id+(harmless?" cannot be lost: its "+stage.Total+" people are fewer than the house health, so it clears even if nothing is thrown":" is lost if nothing is thrown"));
            check(eager.phase==Cleared,stage.id+" can be cleared with golf balls alone by a bot that never misses");
            // Thirty rough stand-ins for a person each: a scale for how much the table asks, nothing more.
            foreach(bool hurried in new[]{false,true})
            {
                int won=0,touched=0;
                for(int seed=1;seed<=30;seed++)
                {
                    DadThrowBots.Run run=DadThrowBots.Play(stage,field,DadThrowBots.Sloppy(seed,false,hurried?.7f:.45f,hurried?.5f:.65f));
                    if(run.Outcome=="clear")won++;touched+=Math.Min(run.end.Escaped,stage.health);
                }
                notes.AppendLine(string.Format(CultureInfo.InvariantCulture,"NOTE {0} {1} bot players (aim takes {2}s, good {3}% of the time): cleared {4} of 30, gate touched {5:0.0} times on average",
                    stage.id,hurried?"hurried":"steady",hurried?"0.7":"0.45",hurried?50:65,won,touched/30f));
            }
        }
        return notes.ToString();
    }
    // One person, one house health: nobody stops them.
    static DadThrowModel LastOneIn(DadThrowField field)
    {
        var stage=new DadThrowStage{id="last",health=1};
        stage.groups.Add(new DadThrowStage.Group{at=0,count=1,firstX=field.Gate+10,speed=48,hp=1});
        var m=new DadThrowModel(field,stage);DadThrowChecks.Advance(m,1);return m;
    }
}
#endif
