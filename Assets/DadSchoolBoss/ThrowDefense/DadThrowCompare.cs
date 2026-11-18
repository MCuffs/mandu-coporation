#if UNITY_EDITOR || SLING_HEADLESS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// Number comparison for the throw game (plan §8.1 ranges, roadmap §10 order: spacing, then the shoe, then the ball).
// Run by Tools/run_dad_throw_compare.py without opening Unity. It changes copies of the rules and of the stage
// tables; the game's own numbers and files are not touched.
// Every row is a bot with perfect aim. It shows what the numbers allow, not what a person will do or enjoy.
public static class DadThrowCompare
{
    static readonly CultureInfo Plain=CultureInfo.InvariantCulture;
    // Plan §13.1 tables as rows of: seconds, people, first x, spacing.
    static readonly string[] Ids={"1-1","1-2","1-3","1-4","1-5"};
    static readonly float[][] Tables={
        new float[]{0,1,650,0, 3,3,1040,90},
        new float[]{0,4,930,55, 8,4,1300,55},
        new float[]{0,2,820,70, 3,4,1180,40, 13,2,900,70, 16,4,1260,40},
        new float[]{0,2,650,100, 1,6,1250,45, 14,2,760,100, 15,6,1330,45},
        new float[]{0,4,1000,55, 9,2,850,95, 12,6,1240,40, 24,4,1100,55, 29,6,1320,40}};
    static DadThrowStage Table(string id,DadThrowStage.Kind kind,float speed,float pace,params float[] rows)
    {
        var stage=new DadThrowStage{id=id,health=5};
        for(int i=0;i<rows.Length;i+=4)stage.groups.Add(new DadThrowStage.Group{at=rows[i]*pace,count=(int)rows[i+1],firstX=rows[i+2],spacing=rows[i+3],speed=speed,hp=1,kind=kind});
        return stage;
    }
    static readonly string[] Players={"golf at once","golf held","shoe first"};
    static readonly DadThrowBots.Policy[] Policies={DadThrowBots.Spam,DadThrowBots.Patient,DadThrowBots.ShoeFirst};
    static string Row(string what,string player,DadThrowStage stage,DadThrowBots.Run run)
    {
        return string.Format(Plain,"{0,-34} {1,-13} {2,-7} house {3}/{4}  golf {5,2}  shoe {6,2}  most {7}  closest {8,4:0}  {9,5:0.0}s",
            what,player,run.Outcome,run.end.Health,stage.health,run.golf,run.shoe,run.end.BestChain,run.closest,run.end.Clock);
    }
    static string Name(DadThrowRules r)
    {return string.Format(Plain,"bounce {0} pierce {1} shoe-dmg {2}",r.MaxBounces,r.Pierce,r.ShoeDamage);}
    // Three players on one stage under one set of rules, and what the shoe player gained over holding the golf ball.
    static void Trio(StringBuilder text,string what,DadThrowStage stage,DadThrowField field,DadThrowRules rules)
    {
        var runs=new DadThrowBots.Run[Policies.Length];
        for(int i=0;i<Policies.Length;i++){runs[i]=DadThrowBots.Play(stage,field,Policies[i],rules);text.AppendLine(Row(what,Players[i],stage,runs[i]));}
        DadThrowBots.Run held=runs[1],shoe=runs[2];
        text.AppendLine(string.Format(Plain,"{0,-34} shoe first vs golf held: house {1:+0;-0;0}, most {2:+0;-0;0}, golf {3:+0;-0;0}, all throws {4:+0;-0;0}, closest {5:+0;-0;0}, seconds {6:+0.0;-0.0;0.0}",
            "",shoe.end.Health-held.end.Health,shoe.end.BestChain-held.end.BestChain,shoe.golf-held.golf,shoe.Throws-held.Throws,shoe.closest-held.closest,shoe.end.Clock-held.end.Clock));
    }
    public static string Run()
    {
        var text=new StringBuilder();DadThrowLayout layout=DadThrowChecks.Layout();DadThrowField field=layout.Field;
        DadThrowStageSet set=DadThrowStageSet.Parse(System.IO.File.ReadAllText("Assets/DadSchoolBoss/Resources/DadThrowStages/chapter1.json"),layout.Backgrounds,field.Gate);
        DadThrowStage.Kind kind=set.kinds["normal"];var stages=new List<DadThrowStage>();
        for(int i=0;i<Ids.Length;i++)stages.Add(Table(Ids[i],kind,48,1,Tables[i]));
        // Where the game has its own table for a stage, that is the one played; the plan's stands in for the rest.
        DadThrowStage planThird=stages[2];var source=new List<string>();
        for(int i=0;i<Ids.Length;i++)
        {
            DadThrowStage real=set.stages.Find(stage=>stage.id==Ids[i]);
            if(real!=null)stages[i]=real;source.Add(Ids[i]+(real!=null?" game":" plan"));
        }
        DadThrowStage third=stages[2],fifth=stages[4];
        text.AppendLine("THROW NUMBER COMPARISON");
        text.AppendLine("Bots with perfect aim, not people. Unity was not opened. The game's numbers and files are unchanged.");
        text.AppendLine(string.Format(Plain,"Field: feet line {0:0}, house line {1:0}, throwing hand ({2:0.0}, {3:0.0}).",field.Ground,field.Gate,field.Muzzle.x,field.Muzzle.y));
        text.AppendLine("Stage tables: "+string.Join(", ",source.ToArray())+". 'plan' = the plan's table (section 13.1), used for calculation only.");
        text.AppendLine("Players: 'golf at once' throws as soon as a ball reaches anyone; 'golf held' waits for three or for the lead to come within "+DadThrowBots.Danger+" of the house line;");
        text.AppendLine("'shoe first' throws a shoe at the lead group while others are within "+DadThrowBots.ShoeReach+" behind it and the lead is at least "+DadThrowBots.ShoeSafe+" out, otherwise plays like 'golf held'.");
        text.AppendLine("'most' = most people one ball reached. 'closest' = how near anyone came to the house line (0 = someone got in).");

        text.AppendLine();text.AppendLine("A. The gentle game's numbers, as they were before the stampede ("+Name(new DadThrowRules{Pierce=5,GolfReload=.65f,ShoeReload=.9f})+"), five stages");
        foreach(DadThrowStage stage in stages)Trio(text,"stage "+stage.id+" ("+stage.Total+" people)",stage,field,new DadThrowRules{Pierce=5,GolfReload=.65f,ShoeReload=.9f});

        text.AppendLine();text.AppendLine("B. Ball bounces x pierce x shoe damage, stages 1-3 and 1-5");
        int[] bounces={2,1,0},pierces={5,3},damages={1,0};
        foreach(int bounce in bounces)foreach(int pierce in pierces)foreach(int damage in damages)
        {
            var rules=new DadThrowRules{MaxBounces=bounce,Pierce=pierce,ShoeDamage=damage,GolfReload=.65f,ShoeReload=.9f};
            Trio(text,Name(rules)+", 1-3",third,field,rules);Trio(text,Name(rules)+", 1-5",fifth,field,rules);
        }

        text.AppendLine();text.AppendLine("C. Spacing: how far the four walk in behind the front pair (1-3's first two groups; the plan's table has 434, the game's 180)");
        text.AppendLine("gap  shoe-dmg  shoes  result                       at     most one well-timed ball reaches: bounce 2 / bounce 0 (no shoe)   gap after each shoe");
        float[] gaps={434,350,250,180,120};
        foreach(float gap in gaps)foreach(int damage in damages)
        {
            DadThrowStage shape=Table("gap "+gap,kind,48,1,0,2,820,70, 3,4,746+gap,40);
            DadThrowBots.Joining far=DadThrowBots.Join(shape,field,new DadThrowRules{ShoeDamage=damage,MaxBounces=2,Pierce=5,GolfReload=.65f,ShoeReload=.9f},2,4,3.1f,6);
            DadThrowBots.Joining near=DadThrowBots.Join(shape,field,new DadThrowRules{ShoeDamage=damage,MaxBounces=0,Pierce=5,GolfReload=.65f,ShoeReload=.9f},2,4,3.1f,6);
            string result=far.joined?"joined":far.frontLeft==0?"front pair gone, never joined":"still apart";
            text.AppendLine(string.Format(Plain,"{0,-4:0} {1,-9} {2,-6} {3,-28} {4,4:0.0}s  {5}",gap,damage,far.shoes,result,far.time,
                far.joined?string.Format(Plain,"{0} of {1} / {2} of {3} ({4} / {5})",far.reach,far.inView,near.reach,near.inView,far.plain,near.plain).PadRight(66)+far.trace:"-".PadRight(66)+far.trace));
        }

        text.AppendLine();text.AppendLine("D. Shoe numbers at the ends of the plan's ranges (shoe damage 0): shoes needed to join the pair with the four");
        text.AppendLine("push  radius  slow          gap 434              gap 250");
        float[] pushes={80,105,140},radii={70,85,110};float[][] slows={new float[]{1.5f,.6f},new float[]{2.5f,.35f},new float[]{3,.3f}};
        foreach(float push in pushes)foreach(float radius in radii)foreach(float[] slow in slows)
        {
            var line=new StringBuilder(string.Format(Plain,"{0,-5:0} {1,-7:0} {2:0.0}s x{3:0.00}   ",push,radius,slow[0],slow[1]));
            foreach(float gap in new float[]{434,250})
            {
                var rules=new DadThrowRules{ShoeDamage=0,ShoePush=push,ShoeRadius=radius,SlowTime=slow[0],SlowFactor=slow[1],Pierce=5,GolfReload=.65f,ShoeReload=.9f};
                DadThrowBots.Joining j=DadThrowBots.Join(Table("gap "+gap,kind,48,1,0,2,820,70, 3,4,746+gap,40),field,rules,2,4,3.1f,6);
                line.Append((j.joined?string.Format(Plain,"{0} shoe(s), {1:0.0}s",j.shoes,j.time):"not after "+j.shoes).PadRight(21));
            }
            text.AppendLine(line.ToString().TrimEnd());
        }

        text.AppendLine();text.AppendLine("E. More pressure on 1-5 (22 people): faster walking and a schedule squeezed to half its time");
        var sets=new List<DadThrowRules>{new DadThrowRules{MaxBounces=2,ShoeDamage=1,Pierce=5,GolfReload=.65f,ShoeReload=.9f},new DadThrowRules{MaxBounces=0,ShoeDamage=0,Pierce=5,GolfReload=.65f,ShoeReload=.9f},new DadThrowRules{MaxBounces=0,Pierce=3,ShoeDamage=0,GolfReload=.65f,ShoeReload=.9f},
            new DadThrowRules{MaxBounces=0,ShoeDamage=0,GolfReload=.9f,Pierce=5,ShoeReload=.9f}};
        float[] speeds={48,65},paces={1,.5f};
        foreach(DadThrowRules rules in sets)foreach(float speed in speeds)foreach(float pace in paces)
        {
            string what=Name(rules)+(rules.GolfReload!=.65f?" golf-reload "+rules.GolfReload.ToString("0.0",Plain):"");
            text.AppendLine(string.Format(Plain,"-- {0}; walking {1:0}, schedule x{2:0.0}",what,speed,pace));
            Trio(text,"",Table("1-5",kind,speed,pace,Tables[4]),field,rules);
        }

        text.AppendLine();text.AppendLine("F. Stage 1-3: the plan's spacing (four walk in 434 behind the pair) against 180, under four sets of numbers");
        DadThrowStage close=Table("1-3 gap 180",kind,48,1,0,2,820,70, 3,4,926,40, 13,2,900,70, 16,4,1006,40);
        foreach(DadThrowRules rules in new[]{new DadThrowRules{MaxBounces=2,ShoeDamage=1,Pierce=5,GolfReload=.65f,ShoeReload=.9f},new DadThrowRules{MaxBounces=2,ShoeDamage=0,Pierce=5,GolfReload=.65f,ShoeReload=.9f},new DadThrowRules{MaxBounces=1,ShoeDamage=0,Pierce=5,GolfReload=.65f,ShoeReload=.9f},new DadThrowRules{MaxBounces=0,ShoeDamage=0,Pierce=5,GolfReload=.65f,ShoeReload=.9f}})
        {Trio(text,Name(rules)+", 434",planThird,field,rules);Trio(text,Name(rules)+", 180",close,field,rules);}

        // Stage tables under consideration (Design/dad-throw-stage-strategy-v2.md). Rows: seconds, people, first x, spacing, walking speed, health.
        text.AppendLine();text.AppendLine("G. Stage tables considered for the gentle game, with its numbers ("+Name(new DadThrowRules{Pierce=5,GolfReload=.65f,ShoeReload=.9f})+"). 'nobody throws' shows how much a stage threatens.");
        string[] names={"1-2 plan","1-4 plan","1-5 plan","1-5 packs 180 behind","2-1 runner ahead of a line","2-2 fast pair through a line","2-3 fast three catch four","2-4 tight line then loose line","2-5 two-hit dads in front"};
        float[][] tables={
            new float[]{0,4,930,55,48,1, 8,4,1300,55,48,1},
            new float[]{0,2,650,100,48,1, 1,6,1250,45,48,1, 14,2,760,100,48,1, 15,6,1330,45,48,1},
            new float[]{0,4,1000,55,48,1, 9,2,850,95,48,1, 12,6,1240,40,48,1, 24,4,1100,55,48,1, 29,6,1320,40,48,1},
            new float[]{0,4,1000,55,48,1, 9,2,850,95,48,1, 12,6,981,40,48,1, 24,4,1100,55,48,1, 29,6,1205,40,48,1},
            new float[]{0,1,900,0,65,1, 0,4,1000,45,48,1, 12,1,1000,0,65,1, 12,5,1100,45,48,1},
            new float[]{0,5,900,45,48,1, 0,2,1140,60,65,1, 13,5,1000,45,48,1, 13,3,1250,60,65,1},
            new float[]{0,4,1100,50,48,1, 2,3,1300,50,65,1, 16,4,1100,50,48,1, 18,4,1300,50,65,1},
            new float[]{0,5,950,40,48,1, 10,5,1000,110,48,1, 12,1,1300,0,65,1, 24,6,1100,40,48,1},
            new float[]{0,1,700,0,36,2, 2,5,1000,45,48,1, 14,2,900,120,36,2, 16,4,1200,45,48,1, 28,1,1000,0,36,2, 28,2,1150,60,65,1}};
        for(int i=0;i<names.Length;i++)
        {
            var stage=new DadThrowStage{id=names[i],health=5};float[] rows=tables[i];
            for(int r=0;r<rows.Length;r+=6)stage.groups.Add(new DadThrowStage.Group{at=rows[r],count=(int)rows[r+1],firstX=rows[r+2],spacing=rows[r+3],speed=rows[r+4],hp=(int)rows[r+5],kind=kind});
            DadThrowBots.Run idle=DadThrowBots.Play(stage,field,DadThrowBots.Idle);
            text.AppendLine(string.Format(Plain,"{0,-34} nobody throws: {1} at {2:0.0}s, {3} of {4} people reached the gate",names[i]+" ("+stage.Total+")",idle.Outcome,idle.end.Clock,idle.end.Escaped,stage.Total));
            Trio(text,"",stage,field,new DadThrowRules{Pierce=5,GolfReload=.65f,ShoeReload=.9f});
        }

        // Crowded tables (2026-11-17: "much harder, hectic from 1-1"). Rows: seconds, people, first x, spacing, walking speed, health.
        // Two stand-ins for a person, 30 of each per table: "steady" takes 0.45s to aim and aims well 65% of the time,
        // "hurried" takes 0.7s and aims well half the time. Both are bots; they only give a scale.
        text.AppendLine();text.AppendLine("H. Crowded tables, golf reload 0.45, shoe reload 0.70, no slow motion while aiming. 30 steady and 30 hurried bot players each.");
        string[] crowded={"1-1 rush A","1-1 rush B","1-1 rush C","1-3 rush A","1-3 rush B","1-1 flood D","1-1 flood E","1-1 flood F","1-3 flood D","1-3 flood E"};
        float[][] crowds={
            new float[]{0,5,560,60,70,1, 0,8,900,40,100,1, 2,4,1300,170,150,1, 4,8,1300,38,110,1, 6,4,1300,160,160,1, 8,10,1300,34,115,1, 10.5f,5,1300,150,165,1, 13,12,1300,32,120,1, 15,4,1300,170,170,1, 17,6,1300,60,130,1},
            new float[]{0,5,560,60,70,1, 0,8,900,40,100,1, 2,5,1300,150,160,1, 3.5f,8,1300,38,115,1, 5,5,1300,140,170,1, 7,10,1300,34,120,1, 8.5f,6,1300,130,175,1, 11,12,1300,32,125,1, 12.5f,6,1300,130,180,1, 15,8,1300,50,140,1, 16,5,1300,140,185,1},
            new float[]{0,5,560,60,70,1, 0,8,900,40,100,1, 1.5f,6,1300,140,170,1, 3,10,1300,36,120,1, 4,6,1300,130,180,1, 6,12,1300,32,125,1, 7,6,1300,120,185,1, 9.5f,12,1300,32,130,1, 10.5f,6,1300,120,190,1, 13,10,1300,45,145,1},
            new float[]{0,6,600,55,80,1, 0,10,950,36,105,1, 2,5,1300,150,165,1, 4,12,1300,32,115,1, 5.5f,5,1300,140,175,1, 8,12,1300,32,120,1, 9.5f,6,1300,130,180,1, 12,14,1300,30,125,1, 14,6,1300,130,185,1, 17,8,1300,45,140,1},
            new float[]{0,6,600,55,80,1, 0,10,950,36,105,1, 1.5f,6,1300,140,175,1, 3,12,1300,32,120,1, 4,6,1300,130,185,1, 6,13,1300,30,125,1, 7,6,1300,120,190,1, 9,13,1300,30,130,1, 10,6,1300,120,195,1, 12,6,1300,50,150,1},
            // Floods: the street is full before the first throw and a new crowd arrives every second or two.
            new float[]{0,8,520,50,60,1, 0,12,860,34,95,1, 1,6,1300,120,150,1, 2,12,1300,30,110,1, 3.5f,6,1300,120,160,1, 5,14,1300,28,115,1, 6.5f,6,1300,110,165,1, 8,14,1300,28,120,1, 9.5f,6,1300,110,170,1, 11,14,1300,28,125,1, 13,6,1300,110,175,1, 15,10,1300,40,135,1},
            new float[]{0,10,480,46,55,1, 0,14,900,30,95,1, 1,8,1300,100,150,1, 2,14,1300,28,110,1, 3,8,1300,100,165,1, 4.5f,16,1300,26,115,1, 6,8,1300,95,170,1, 7.5f,16,1300,26,120,1, 9,8,1300,95,175,1, 10.5f,16,1300,26,125,1, 12,8,1300,95,180,1, 14,14,1300,34,140,1},
            new float[]{0,10,480,46,55,1, 0,16,880,28,95,1, 1,10,1300,90,155,1, 2,16,1300,26,112,1, 3,10,1300,90,170,1, 4,16,1300,26,118,1, 5,10,1300,85,175,1, 6.5f,18,1300,24,122,1, 8,10,1300,85,180,1, 9.5f,18,1300,24,128,1, 11,10,1300,85,185,1, 13,16,1300,30,145,1},
            new float[]{0,10,500,46,65,1, 0,16,880,28,100,1, 1,10,1300,90,165,1, 2,16,1300,26,118,1, 3,10,1300,90,180,1, 4,18,1300,24,124,1, 5,10,1300,85,185,1, 6.5f,18,1300,24,128,1, 8,10,1300,85,190,1, 9.5f,18,1300,24,134,1, 11,10,1300,85,195,1, 13,14,1300,30,150,1},
            new float[]{0,12,480,42,65,1, 0,18,880,26,105,1, 1,12,1300,80,170,1, 2,18,1300,24,122,1, 3,12,1300,80,185,1, 4,18,1300,24,128,1, 5,12,1300,75,190,1, 6,18,1300,24,132,1, 7,12,1300,75,195,1, 8.5f,18,1300,24,138,1, 10,10,1300,75,200,1}};
        for(int i=0;i<crowded.Length;i++)
        {
            var stage=new DadThrowStage{id=crowded[i],health=5};float[] rows=crowds[i];
            for(int r=0;r<rows.Length;r+=6)stage.groups.Add(new DadThrowStage.Group{at=rows[r],count=(int)rows[r+1],firstX=rows[r+2],spacing=rows[r+3],speed=rows[r+4],hp=(int)rows[r+5],kind=kind});
            DadThrowBots.Run idle=DadThrowBots.Play(stage,field,DadThrowBots.Idle);
            var rules=new DadThrowRules{GolfReload=.45f,ShoeReload=.7f,Pierce=5};
            DadThrowBots.Run perfect=DadThrowBots.Play(stage,field,DadThrowBots.Spam,rules);
            text.AppendLine(string.Format(Plain,"{0} ({1} people): nobody throws -> {2} at {3:0.0}s | perfect aim {4}, gate touched {5}, {6:0.0}s, golf {7}, in view {8:0} on average (most {9})",
                crowded[i],stage.Total,idle.Outcome,idle.end.Clock,perfect.Outcome,perfect.end.Escaped,perfect.end.Clock,perfect.golf,perfect.Crowd,perfect.peak));
            float[][] players={new float[]{.45f,.65f},new float[]{.7f,.5f}};
            foreach(float[] player in players)foreach(bool shoes in new[]{false,true})
            {
                int cleared=0,touches=0,throws=0,peak=0;float seconds=0,crowd=0,lead=0;
                for(int seed=1;seed<=30;seed++)
                {
                    DadThrowBots.Run run=DadThrowBots.Play(stage,field,DadThrowBots.Sloppy(seed,shoes,player[0],player[1]),rules);
                    if(run.Outcome=="clear")cleared++;touches+=Math.Min(run.end.Escaped,stage.health);throws+=run.Throws;seconds+=run.end.Clock;
                    crowd+=run.Crowd;lead+=run.LeadX;peak=Math.Max(peak,run.peak);
                }
                text.AppendLine(string.Format(Plain,"   {0}{1}: cleared {2}/30, gate touched {3:0.0} on average, {4:0} throws, {5:0.0}s | in view {6:0} on average (most {7}), nearest person {8:0} from the left edge on average",
                    player[0]<.5f?"steady":"hurried",shoes?" with shoes":"",cleared,touches/30f,throws/30f,seconds/30f,crowd/30f,peak,lead/30f));
            }
        }
        // Stampedes (2026-11-17: "they should just come running in a mad herd"). Everyone runs; nobody strolls.
        // Each table is tried with several hands: how many people a ball goes through, and how fast the hand reloads.
        text.AppendLine();text.AppendLine("I. Stampedes: everyone runs. Each table with several hands (people a ball goes through / golf reload). 30 steady and 30 hurried bot players each, golf only.");
        string[] herds={"1-1 herd XS (80)","1-3 herd K (130)","1-1 herd S (100)","1-1 herd M (124)","1-3 herd L (160)"};
        float[][] hands={new float[]{5,.45f},new float[]{12,.45f},new float[]{12,.3f},new float[]{12,.2f},new float[]{20,.3f}};
        float[][] herdRows={
            new float[]{0,8,640,44,120,1, 0,12,1040,32,150,1, 2,12,1300,30,170,1, 4,12,1300,30,180,1, 6,12,1300,28,185,1, 8,12,1300,28,190,1, 10,12,1300,28,195,1},
            new float[]{0,10,620,40,125,1, 0,14,1040,30,155,1, 1.5f,14,1300,28,175,1, 3,16,1300,26,185,1, 4.5f,16,1300,26,190,1, 6,20,1300,24,195,1, 7.5f,20,1300,24,200,1, 9,20,1300,22,205,1},
            new float[]{0,8,640,44,120,1, 0,12,1040,32,150,1, 1.5f,12,1300,30,170,1, 3,14,1300,28,180,1, 4.5f,14,1300,28,185,1, 6,14,1300,28,190,1, 7.5f,14,1300,26,195,1, 9,12,1300,30,200,1},
            new float[]{0,10,620,40,120,1, 0,14,1040,30,150,1, 1.5f,14,1300,28,170,1, 3,16,1300,26,180,1, 4.5f,16,1300,26,185,1, 6,18,1300,24,190,1, 7.5f,18,1300,24,195,1, 9,18,1300,24,200,1},
            new float[]{0,12,600,38,130,1, 0,16,1080,28,160,1, 1.2f,16,1300,26,180,1, 2.4f,18,1300,24,190,1, 3.6f,18,1300,24,200,1, 4.8f,20,1300,22,205,1, 6,20,1300,22,210,1, 7.2f,20,1300,22,215,1, 8.4f,20,1300,22,220,1}};
        for(int i=0;i<herds.Length;i++)
        {
            var stage=new DadThrowStage{id=herds[i],health=5};float[] rows=herdRows[i];
            for(int r=0;r<rows.Length;r+=6)stage.groups.Add(new DadThrowStage.Group{at=rows[r],count=(int)rows[r+1],firstX=rows[r+2],spacing=rows[r+3],speed=rows[r+4],hp=(int)rows[r+5],kind=kind});
            DadThrowBots.Run idle=DadThrowBots.Play(stage,field,DadThrowBots.Idle);
            text.AppendLine(string.Format(Plain,"{0}: nobody throws -> {1} at {2:0.0}s",herds[i],idle.Outcome,idle.end.Clock));
            foreach(float[] hand in hands)
            {
                int pierce=(int)hand[0];var rules=new DadThrowRules{GolfReload=hand[1],ShoeReload=.7f,Pierce=pierce};
                DadThrowBots.Run perfect=DadThrowBots.Play(stage,field,DadThrowBots.Spam,rules);
                text.AppendLine(string.Format(Plain,"  through {0}, reload {7:0.00}: perfect aim {1}, gate touched {2}, {3:0.0}s, golf {4}, in view {5:0} on average (most {6})",
                    pierce,perfect.Outcome,perfect.end.Escaped,perfect.end.Clock,perfect.golf,perfect.Crowd,perfect.peak,hand[1]));
                float[][] players={new float[]{.45f,.65f},new float[]{.7f,.5f}};
                foreach(float[] player in players)
                {
                    int cleared=0,touches=0,throws=0,peak=0;float seconds=0,crowd=0,lead=0;
                    for(int seed=1;seed<=30;seed++)
                    {
                        DadThrowBots.Run run=DadThrowBots.Play(stage,field,DadThrowBots.Sloppy(seed,false,player[0],player[1]),rules);
                        if(run.Outcome=="clear")cleared++;touches+=Math.Min(run.end.Escaped,stage.health);throws+=run.Throws;seconds+=run.end.Clock;
                        crowd+=run.Crowd;lead+=run.LeadX;peak=Math.Max(peak,run.peak);
                    }
                    text.AppendLine(string.Format(Plain,"     {0}: cleared {1}/30, gate touched {2:0.0} on average, {3:0} throws, {4:0.0}s | in view {5:0} on average (most {6}), nearest person {7:0} from the left edge on average",
                        player[0]<.5f?"steady":"hurried",cleared,touches/30f,throws/30f,seconds/30f,crowd/30f,peak,lead/30f));
                }
            }
        }
        return text.ToString();
    }
}
#endif
