#if UNITY_EDITOR || SLING_HEADLESS
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
public static class DadThrowChecks
{
    // Rule checks stand on fixed numbers of their own, so moving the art never breaks or bends a rule check.
    // The numbers the game really uses come from the manifests and are checked against the pictures further down.
    public static readonly DadThrowField Test=new DadThrowField(570,215,new Vector2(243,440));
    public static void Advance(DadThrowModel m,float time){for(int i=0;i<(int)(time*120);i++)m.Tick(1/120f);}
    static string Art(string folder){return System.IO.File.ReadAllText("Assets/DadSchoolBoss/Resources/"+folder+"/manifest.json");}
    public static DadThrowLayout Layout(){return DadThrowLayout.Parse(Art("DadThrowV1"),Art("DadThrowScreensV2"),Art("DadThrowScreensV1"));}
    public static string Run()
    {
        var r=new StringBuilder();Action<bool,string> check=(yes,label)=>{if(!yes)throw new Exception("THROW: "+label);r.AppendLine("PASS "+label);};
        var m=new DadThrowModel(Test);check(m.Wave==1&&m.Health==5&&m.CanFire,"throw prototype starts isolated and ready");
        check(!m.Fire(DadThrowModel.Weapon.Golf,new Vector2(5,0)),"short taps do not fire");
        check(m.Fire(DadThrowModel.Weapon.Golf,new Vector2(150,50)),"golf pull launches a projectile");
        check(!m.Fire(DadThrowModel.Weapon.Golf,new Vector2(150,50)),"reload prevents rapid-fire spam");
        m=new DadThrowModel(Test);m.AddEnemy(400);m.AddEnemy(430);m.AddEnemy(460);
        m.Fire(DadThrowModel.Weapon.Golf,new Vector2(150,50));Advance(m,.55f);
        check(m.Killed>=3&&m.BestChain>=3,"one golf ball pierces a line of three enemies");
        m=new DadThrowModel(Test);var tough=m.AddEnemy(430,10);m.Fire(DadThrowModel.Weapon.Golf,new Vector2(150,50));Advance(m,.6f);
        check(tough.hp==9,"a golf ball cannot damage the same target twice");
        m=new DadThrowModel(Test);var front=m.AddEnemy(400,10);var follower=m.AddEnemy(460,10);
        m.Fire(DadThrowModel.Weapon.Shoe,new Vector2(150,50));Advance(m,.4f);
        check(front.x>470&&follower.x>530&&front.slow>0&&follower.slow>0,"shoe pushes and slows a leading group to compress the line");
        check(m.projectiles.Count==0,"shoe is spent on its first group impact");
        check(DadThrowModel.SweptHit(new Vector2(200,535),new Vector2(700,535),new Vector2(440,535),25),"fast swept golf segment cannot tunnel through a target");
        check(!DadThrowModel.SweptHit(new Vector2(200,300),new Vector2(700,300),new Vector2(440,535),25),"a high arc does not hit ground enemies by x alone");
        m=new DadThrowModel(Test);m.Fire(DadThrowModel.Weapon.Golf,new Vector2(150,50));Advance(m,.1f);
        var path=new List<Vector2>();m.Predict(DadThrowModel.Weapon.Golf,new Vector2(150,50),path);
        check(Vector2.Distance(m.projectiles[0].pos,path[4])<.02f,"prediction and projectile share the fixed-step free-flight trajectory");
        m=new DadThrowModel(Test);m.AddEnemy(155);m.Tick(.01f);
        check(m.Health==4&&m.Escaped==1&&m.Remaining==m.Total-1,"breach loses one health and updates remaining count once");
        for(int i=0;i<4;i++)m.AddEnemy(155);m.Tick(.01f);
        check(m.phase==DadThrowModel.Phase.Lost&&m.Health==0,"five breaches end the wave and clamp at zero health");
        check(!m.Fire(DadThrowModel.Weapon.Golf,new Vector2(150,0))&&!m.NextWave(),"loss cannot launch or skip to the next wave");
        var harder=new DadThrowModel(Test,12);check(harder.Total>new DadThrowModel(Test).Total&&harder.Interval<new DadThrowModel(Test).Interval,"wave population and arrival rate escalate");
        check(new DadThrowModel(Test,999).Total<=84&&new DadThrowModel(Test,999).Interval>=.22f,"difficulty tuning has finite population and spawn-rate caps");
        m=new DadThrowModel(Test);check(!m.Upgrade()&&!m.NextWave(),"cannot buy growth or skip a live wave");
        // Complete a whole wave by taking down each spawned foe, isolating completion/economy from aim skill.
        for(int frame=0;frame<3000&&m.phase==DadThrowModel.Phase.Playing;frame++)
        {
            m.Tick(1/120f);
            foreach(var e in m.enemies){e.x=430;e.hp=1;}
            if(m.CanFire)m.Fire(DadThrowModel.Weapon.Golf,new Vector2(150,50));
        }
        check(m.phase==DadThrowModel.Phase.WaveClear&&m.Killed==m.Total,"wave completes after all scheduled enemies are resolved");
        int coins=m.Coins;check(coins==m.Killed*2+12,"kill and wave bonuses pay once");m.Tick(1);check(m.Coins==coins,"clear state cannot duplicate wave rewards");
        check(m.Upgrade()&&m.Level==1&&m.Coins==coins-30,"single upgrade spends coins between waves");
        check(m.NextWave()&&m.Wave==2&&m.Remaining==m.Total&&m.Coins==coins-30,"next wave resets combat counts and preserves earned growth");
        m=new DadThrowModel(Test);
        for(int i=0;i<120*90&&m.phase==DadThrowModel.Phase.Playing;i++)
        {if(m.CanFire)m.Fire(DadThrowModel.Weapon.Golf,new Vector2(150,50));m.Tick(1/120f);}
        check(m.phase==DadThrowModel.Phase.WaveClear,"first wave has a winning golf-only path without moving enemies in the fixture");
        m=new DadThrowModel(Test);Advance(m,90);
        check(m.phase==DadThrowModel.Phase.Lost,"ignoring the advancing crowd eventually loses instead of stalling forever");

        // ── Art placement on the wide v2 street. Numbers only: this proves where pictures are put, not how they look on a device. ──
        DadThrowLayout layout=Layout();DadThrowField field=layout.Field;
        Rect canvas=new Rect(0,0,DadThrowModel.Width,DadThrowModel.Height);
        Func<Rect,Rect,bool> inside=(inner,outer)=>inner.xMin>=outer.xMin&&inner.xMax<=outer.xMax&&inner.yMin>=outer.yMin&&inner.yMax<=outer.yMax;
        Rect street=layout.Background();
        check(street.x==0&&street.y==0&&street.width==DadThrowModel.Width&&street.height>=DadThrowModel.Height&&street.height-DadThrowModel.Height<2,"street art covers the canvas at its own aspect ratio, not stretched");
        check(layout.Street.resource=="DadThrowScreensV2/street_wide"&&layout.Title.resource=="DadThrowScreensV2/title_wide"&&layout.Backgrounds.Contains(layout.Street.resource),
            "battle and title use the wide v2 pictures, and only a measured street can be named by a stage");
        check(layout.SandTopY<layout.RoadEdgeY&&field.Ground>layout.SandTopY+20&&field.Ground<layout.RoadEdgeY-20&&layout.DadFeetY>layout.SandTopY+20&&layout.DadFeetY<layout.RoadEdgeY-20,
            "feet line and dad's feet lie on the painted sand strip between the wall and the road");
        check(field.Ground==layout.Ground&&field.Gate==layout.FailLine&&Vector2.Distance(field.Muzzle,layout.ReleaseHand)<.001f,
            "the fight's ground, house line and throwing point are the layout's own numbers, not a second copy");
        Rect readyBody=layout.DadBody(layout.Ready);
        // Gate-touch contest (story v2 §3): the line that costs a touch is the painted gate, not a mark in front of dad.
        check(field.Gate>layout.GateFrontX&&field.Gate<=layout.GateFrontX+30,"a challenger is counted when the front of their body reaches the painted gate post");
        check(layout.GateDoor.xMax<=layout.GateFrontX&&layout.GateDoor.width>20&&layout.GateDoor.yMax<field.Ground&&layout.GateDoor.yMax>field.Ground-40,
            "the gate door they run for stands on the street's ground line, just left of its post");
        check(field.Gate<field.Muzzle.x&&field.Muzzle.x-field.Gate<=100,"once past dad's hand a challenger cannot be stopped: that last run to the gate is under 100");
        Vector2 feet=new Vector2(layout.DadFeetX,layout.DadFeetY);bool planted=true,sameHeight=true,onCanvas=true;
        foreach(DadThrowLayout.Pose pose in Enum.GetValues(typeof(DadThrowLayout.Pose)))
        {
            DadThrowLayout.Sprite s=layout.Dad(pose);Rect seen=layout.DadBody(s);
            planted&=Vector2.Distance(layout.DadPoint(s,s.pivot),feet)<.01f;
            sameHeight&=Mathf.Abs(feet.y-seen.yMin-layout.DadHeight)<.5f;
            onCanvas&=seen.xMin>=0&&seen.yMin>=DadThrowLayout.TopBar&&seen.xMax<=DadThrowModel.Width;
        }
        check(planted,"ready, wind-up and release poses share one foot point");
        check(sameHeight,"every pose is drawn at the same standing height");
        check(onCanvas,"no pose is cut by the screen edge or hidden under the top bar");
        bool gateSeen=true;foreach(DadThrowLayout.Pose pose in Enum.GetValues(typeof(DadThrowLayout.Pose)))gateSeen&=layout.DadBody(layout.Dad(pose)).xMin>=layout.GateDoor.xMax;
        check(gateSeen,"dad stands beside the gate in every pose and never covers the door the challengers must touch");
        m=new DadThrowModel(field);m.Fire(DadThrowModel.Weapon.Golf,new Vector2(150,50));
        check(layout.Release.hasHand&&Vector2.Distance(m.projectiles[0].pos,layout.ReleaseHand)<.001f,"a thrown thing starts in the painted throwing hand");
        check(layout.Windup.hasHand&&layout.WindupHand.x<field.Muzzle.x&&layout.WindupHand.x>=20&&layout.WindupHand.y>DadThrowLayout.TopBar+20,"the held item is drawn in the raised hand, behind the release point and on screen");
        check(DadThrowLayout.PoseFor(true,0)==DadThrowLayout.Pose.Windup&&DadThrowLayout.PoseFor(true,.2f)==DadThrowLayout.Pose.Windup
            &&DadThrowLayout.PoseFor(false,.1f)==DadThrowLayout.Pose.Release&&DadThrowLayout.PoseFor(false,0)==DadThrowLayout.Pose.Ready,
            "dad winds up while aiming, follows through after a throw, then returns to ready");
        Func<DadThrowLayout.Sprite,Rect,Rect> opaque=(s,drawn)=>{float k=drawn.width/s.width;return new Rect(drawn.x+s.bounds.x*k,drawn.y+s.bounds.y*k,s.bounds.width*k,s.bounds.height*k);};
        Vector2 spot=new Vector2(500,300);Rect golfSeen=opaque(layout.Golf,layout.GolfRect(spot)),shoeSeen=opaque(layout.Shoe,layout.ShoeRect(spot));
        check(Vector2.Distance(golfSeen.center,spot)<1.5f&&Mathf.Abs(golfSeen.width-DadThrowLayout.GolfDiameter)<.01f&&Mathf.Abs(golfSeen.width-golfSeen.height)<1.5f,"golf ball picture is centred on the projectile, round and drawn at the stated size");
        check(Vector2.Distance(shoeSeen.center,spot)<1.5f&&Mathf.Abs(shoeSeen.width-DadThrowLayout.ShoeLength)<.01f,"shoe picture is centred on the projectile so it spins in place");
        check(DadThrowLayout.GolfDiameter>=DadThrowModel.GolfHit*2&&DadThrowLayout.GolfDiameter<=DadThrowModel.GolfHit*3&&DadThrowLayout.ShoeLength>=DadThrowModel.ShoeHit*2&&DadThrowLayout.ShoeLength<=DadThrowModel.ShoeHit*3,
            "a thrown thing is drawn no smaller than its hit circle and at most half as large again");
        check(DadThrowLayout.GolfDiameter<=layout.DadHeight*.2f&&DadThrowLayout.ShoeLength<=layout.DadHeight*.4f,"the ball and the shoe are in scale with dad: at most a fifth and two fifths of his height");
        check(!DadThrowLayout.GolfButton.Overlaps(DadThrowLayout.ShoeButton)&&DadThrowLayout.GolfButton.yMin>layout.RoadEdgeY&&inside(DadThrowLayout.GolfButton,canvas)&&inside(DadThrowLayout.ShoeButton,canvas),
            "weapon buttons sit on the road below the play field without overlapping");
        Rect golfIcon=opaque(layout.Golf,layout.ItemRect(layout.Golf,DadThrowLayout.IconSlot(DadThrowLayout.GolfButton).center,DadThrowLayout.IconSize));
        Rect shoeIcon=opaque(layout.Shoe,layout.ItemRect(layout.Shoe,DadThrowLayout.IconSlot(DadThrowLayout.ShoeButton).center,DadThrowLayout.IconSize));
        check(inside(golfIcon,DadThrowLayout.GolfButton)&&inside(shoeIcon,DadThrowLayout.ShoeButton)
            &&!golfIcon.Overlaps(DadThrowLayout.LabelSlot(DadThrowLayout.GolfButton))&&!shoeIcon.Overlaps(DadThrowLayout.LabelSlot(DadThrowLayout.ShoeButton)),"weapon pictures fit inside their buttons beside the label");

        // Top bar: thin, everything inside it, nothing touching.
        Rect bar=new Rect(0,0,DadThrowModel.Width,DadThrowLayout.TopBar-3);
        Rect[] hud={DadThrowLayout.StageLabel,DadThrowLayout.HouseHealth,DadThrowLayout.RemainingLabel,DadThrowLayout.PauseButton};
        bool hudFits=DadThrowLayout.TopBar<=90,cellsFit=true;
        for(int i=0;i<hud.Length;i++){hudFits&=inside(hud[i],bar);for(int j=i+1;j<hud.Length;j++)hudFits&=!hud[i].Overlaps(hud[j]);}
        for(int i=0;i<DadThrowLayout.HealthCells;i++)
        {
            Rect cell=DadThrowLayout.HealthCell(i);cellsFit&=inside(cell,DadThrowLayout.HouseHealth)&&!cell.Overlaps(DadThrowLayout.HealthLabel);
            if(i>0)cellsFit&=cell.xMin>DadThrowLayout.HealthCell(i-1).xMax;
        }
        check(hudFits,"stage, house health, people left and pause all sit inside the thin top bar without touching");
        check(cellsFit&&DadThrowLayout.HealthCells==DadThrowStageSet.MaxHealth,"house health is five separate cells beside its label");
        check(inside(DadThrowLayout.PauseBar(0),DadThrowLayout.PauseButton)&&inside(DadThrowLayout.PauseBar(1),DadThrowLayout.PauseButton)&&DadThrowLayout.PauseButton.width>=64&&DadThrowLayout.PauseButton.height>=64,
            "the pause mark is drawn inside a button at least 64 wide and tall");
        check(DadThrowLayout.AimArea.yMin>=DadThrowLayout.TopBar&&DadThrowLayout.AimArea.yMax<DadThrowLayout.GolfButton.yMin&&inside(DadThrowLayout.AimArea,canvas)
            &&!DadThrowLayout.AimArea.Overlaps(DadThrowLayout.PauseButton)&&inside(DadThrowLayout.Hint,DadThrowLayout.AimArea),
            "a drag can start anywhere on the open street and never on a button");

        // Title and cards.
        check(layout.TitleArt().height>=DadThrowModel.Height&&inside(DadThrowLayout.TitleText,canvas)&&inside(DadThrowLayout.StartButton,canvas)&&!DadThrowLayout.TitleText.Overlaps(DadThrowLayout.StartButton)&&DadThrowLayout.StartButton.height>=72,
            "title art covers the screen; lettering and a 72-tall start button stay on screen and apart");
        Rect card=DadThrowLayout.ResultCard,pauseCard=DadThrowLayout.PauseCard;
        check(inside(card,canvas)&&inside(DadThrowLayout.ClearHeading,card)&&inside(DadThrowLayout.ClearLeft,card)&&inside(DadThrowLayout.ClearRight,card)
            &&!DadThrowLayout.ClearLeft.Overlaps(DadThrowLayout.ClearRight)&&DadThrowLayout.ClearLeft.height>=72
            &&inside(DadThrowLayout.ClearWordName,card)&&inside(DadThrowLayout.ClearWordLine,card)&&DadThrowLayout.ClearHeading.yMax<=DadThrowLayout.ClearWordName.yMin
            &&DadThrowLayout.ClearWordName.yMax<=DadThrowLayout.ClearWordLine.yMin&&DadThrowLayout.ClearWordLine.yMax<=DadThrowLayout.ClearLeft.yMin,
            "clear card stacks its heading, one line from the story and two full-height buttons that do not touch");
        check(inside(DadThrowLayout.DefeatHeading,card)&&inside(DadThrowLayout.DefeatLine,card)&&inside(DadThrowLayout.DefeatRetry,card)&&inside(DadThrowLayout.DefeatTitle,card)
            &&DadThrowLayout.DefeatHeading.yMax<=DadThrowLayout.DefeatLine.yMin&&DadThrowLayout.DefeatLine.yMax<=DadThrowLayout.DefeatRetry.yMin&&DadThrowLayout.DefeatRetry.yMax<=DadThrowLayout.DefeatTitle.yMin
            &&DadThrowLayout.DefeatRetry.height>=72&&DadThrowLayout.DefeatRetry.width>DadThrowLayout.DefeatTitle.width*1.5f&&DadThrowLayout.DefeatTitle.height>=56,
            "defeat card stacks heading, one line, a large retry and a smaller way back to the title");
        check(inside(pauseCard,canvas)&&inside(DadThrowLayout.PauseHeading,pauseCard)&&inside(DadThrowLayout.PauseResume,pauseCard)&&inside(DadThrowLayout.PauseTitle,pauseCard)
            &&DadThrowLayout.PauseHeading.yMax<=DadThrowLayout.PauseResume.yMin&&DadThrowLayout.PauseResume.yMax<=DadThrowLayout.PauseTitle.yMin,
            "pause card stacks its heading, resume and title without overlap");
        Rect clearBody=layout.HeroBody(layout.Clear),retryBody=layout.HeroBody(layout.Retry);
        check(inside(clearBody,canvas)&&inside(retryBody,canvas)&&clearBody.xMax<card.xMin-40&&retryBody.xMax<card.xMin-40
            &&Mathf.Abs(clearBody.yMax-DadThrowLayout.ResultFeet.y)<.5f&&Mathf.Abs(retryBody.yMax-DadThrowLayout.ResultFeet.y)<.5f,
            "the clear and retry poses stand on one spot, whole, to the left of the card");

        // Screen fit: the canvas keeps its shape, stays inside the safe area, and a finger maps back to the same canvas point.
        Func<Rect,float,float,bool> fits=(safe,width,height)=>
        {
            DadThrowLayout.Fit f=DadThrowLayout.FitTo(safe,height);
            // Canvas corners in screen pixels, top-left origin.
            float left=f.offset.x,top=f.offset.y,right=left+DadThrowModel.Width*f.scale,bottom=top+DadThrowModel.Height*f.scale;
            bool within=left>=safe.x-.01f&&right<=safe.xMax+.01f&&top>=height-safe.yMax-.01f&&bottom<=height-safe.y+.01f;
            bool tight=Mathf.Abs((right-left)-safe.width)<.01f||Mathf.Abs((bottom-top)-safe.height)<.01f;
            Vector2 point=new Vector2(640,360),screen=new Vector2(left+point.x*f.scale,height-(top+point.y*f.scale));
            return within&&tight&&Vector2.Distance(f.ToCanvas(screen,height),point)<.01f;
        };
        check(fits(new Rect(0,0,1920,1080),1920,1080)&&DadThrowLayout.FitTo(new Rect(0,0,1920,1080),1080).scale==1.5f,"a 16:9 screen is filled exactly");
        check(fits(new Rect(0,0,2400,1080),2400,1080)&&fits(new Rect(110,0,2290,1080),2400,1080)&&fits(new Rect(0,0,2290,1080),2400,1080)&&fits(new Rect(0,63,2400,1017),2400,1080),
            "a longer screen keeps the canvas whole inside the safe area with the notch on either side or a bar below");
        check(DadThrowLayout.FitTo(new Rect(0,0,2400,1080),1080).scale==DadThrowLayout.FitTo(new Rect(0,0,1920,1080),1080).scale,
            "a longer screen shows no more of the street than 16:9: people are not seen earlier");
        check(fits(new Rect(0,0,2048,1536),2048,1536)&&fits(new Rect(0,0,1080,2400),1080,2400),"a squarer or upright screen still fits the whole canvas");

        bool refused=false;try{DadThrowLayout.Parse("{}","{}","{}");}catch(ArgumentException){refused=true;}
        check(refused,"an unreadable manifest is refused by name instead of placing pictures at zero");
        r.Append(DadThrowStageChecks.Run(layout,check));
        r.Append(DadThrowGrowthChecks.Run(layout,check));
        r.Append(DadThrowDailyChecks.Run(layout,check));
        r.Append(layout.Describe());
        return r.ToString();
    }
}
#endif
