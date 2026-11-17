#if UNITY_EDITOR || SLING_HEADLESS
using System;
using System.Collections.Generic;
using UnityEngine;

// Scripted players for window-free runs. They aim by trying each candidate throw on a copy of the street,
// so they never miss and never hesitate. A run shows what the numbers allow; it says nothing about what a
// person sees, understands or enjoys.
public static class DadThrowBots
{
    const DadThrowModel.Weapon Golf=DadThrowModel.Weapon.Golf,Shoe=DadThrowModel.Weapon.Shoe;
    const DadThrowModel.Phase Playing=DadThrowModel.Phase.Playing;
    // How near the house line the lead may come before a waiting player throws anyway, how far out a shoe is
    // still worth throwing, and how far behind followers may be for a shoe to be tried at all.
    public const float Danger=220,ShoeSafe=300,ShoeReach=520;

    public sealed class Run
    {
        public DadThrowModel end;public int golf,shoe;public float closest=float.MaxValue;internal int wait,think=-1;
        // How busy the street looked: people in view at once (most, and on average) and how far out the nearest stood on average.
        public int peak;internal int ticks,leadTicks;internal double crowd,lead;
        public float Crowd {get{return ticks>0?(float)(crowd/ticks):0;}} public float LeadX {get{return leadTicks>0?(float)(lead/leadTicks):0;}}
        public string Outcome {get{return end.phase==DadThrowModel.Phase.WaveClear?"clear":end.phase==DadThrowModel.Phase.Lost?"defeat":"unfinished";}}
        public int Throws {get{return golf+shoe;}}
    }
    public delegate void Policy(DadThrowModel m,Run run);
    public struct Aim { public Vector2 pull;public int hits;public bool any; }

    // Candidate pulls, as (strength, degrees): positive degrees pull downward for a flat throw, negative lob it.
    // Full strength from flat to high puts first landings about every 50-90 along the street; the weaker ones
    // cover the stretch just in front of dad.
    static readonly float[] Candidates={150,16, 150,12, 150,8, 150,4, 150,0, 150,-4, 150,-8, 150,-12, 150,-16, 150,-20, 150,-24, 150,-28,
        100,24, 100,10, 60,35, 60,20, 32,45};
    public static readonly Vector2[] Pulls=Build();
    static Vector2[] Build()
    {
        var pulls=new Vector2[Candidates.Length/2];
        for(int i=0;i<pulls.Length;i++){float a=Candidates[i*2+1]*Mathf.Deg2Rad;pulls[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Candidates[i*2];}
        return pulls;
    }
    // The people in view, copied onto an empty street with the same rules, to try a throw without touching the fight.
    static DadThrowModel Copy(DadThrowModel m,List<DadThrowModel.Enemy> twins)
    {
        var s=new DadThrowModel(m.Field,DadThrowStage.Empty()){Rules=m.Rules};twins.Clear();
        foreach(DadThrowModel.Enemy e in m.enemies)
        {
            if(e.x>DadThrowModel.Width){twins.Add(null);continue;}
            DadThrowModel.Enemy c=s.AddEnemy(e.x,e.hp,e.speed);c.slow=e.slow;twins.Add(c);
        }
        return s;
    }
    static void Fly(DadThrowModel s){for(int i=0;i<360&&s.projectiles.Count>0&&s.phase==Playing;i++)s.Tick(1/120f);}
    public static DadThrowModel.Enemy Lead(DadThrowModel m)
    {DadThrowModel.Enemy lead=null;foreach(DadThrowModel.Enemy e in m.enemies)if(e.x<=DadThrowModel.Width&&(lead==null||e.x<lead.x))lead=e;return lead;}
    static readonly List<DadThrowModel.Enemy> twins=new List<DadThrowModel.Enemy>();
    // The golf throw that reaches the most people (`most`), and the best one among those that take the lead (`onLead`).
    public static void BestGolf(DadThrowModel m,DadThrowModel.Enemy lead,out Aim most,out Aim onLead)
    {
        most=new Aim();onLead=new Aim();int leadAt=m.enemies.IndexOf(lead);
        foreach(Vector2 candidate in Pulls)
        {
            DadThrowModel s=Copy(m,twins);if(!s.Fire(Golf,candidate))continue;
            Fly(s);int hits=s.BestChain;
            if(hits>most.hits){most.pull=candidate;most.hits=hits;most.any=true;}
            bool took=leadAt>=0&&twins[leadAt]!=null&&twins[leadAt].hp<lead.hp;
            if(took&&hits>onLead.hits){onLead.pull=candidate;onLead.hits=hits;onLead.any=true;}
        }
    }
    static readonly List<float> before=new List<float>();
    // The shoe throw that moves the most people back, the lead among them. `hits` is how many it moved or hurt.
    public static Aim BestShoe(DadThrowModel m,DadThrowModel.Enemy lead)
    {
        Aim best=new Aim();int leadAt=m.enemies.IndexOf(lead);
        foreach(Vector2 candidate in Pulls)
        {
            DadThrowModel s=Copy(m,twins);if(!s.Fire(Shoe,candidate))continue;
            before.Clear();foreach(DadThrowModel.Enemy t in twins)before.Add(t!=null?t.x:0);
            Fly(s);int moved=0;bool leadMoved=false;
            for(int i=0;i<twins.Count;i++)
            {
                // Thrown back further than they could have walked forward, or hurt.
                if(twins[i]==null||!(twins[i].x>before[i]+20||twins[i].hp<m.enemies[i].hp))continue;
                moved++;leadMoved|=i==leadAt;
            }
            if(leadMoved&&moved>best.hits){best.pull=candidate;best.hits=moved;best.any=true;}
        }
        return best;
    }
    // How far the nearest person outside the lead's group is behind it. False when everyone in view is one group.
    public static bool GapBehind(DadThrowModel m,DadThrowModel.Enemy lead,out float gap)
    {
        float back=lead.x,next=float.MaxValue;bool grew=true;
        while(grew)
        {
            grew=false;next=float.MaxValue;
            foreach(DadThrowModel.Enemy e in m.enemies)
            {
                if(e.x<=back)continue;
                if(e.x-back<m.Rules.ShoeRadius){back=e.x;grew=true;}else next=Mathf.Min(next,e.x);
            }
        }
        gap=next-back;return next<float.MaxValue;
    }

    // A bot thinks when the hand is ready, and not again for `ticks` if it decided to wait.
    static bool Ready(DadThrowModel m,Run run){if(!m.CanFire)return false;if(run.wait>0){run.wait--;return false;}return true;}
    static void Golfed(DadThrowModel m,Run run,Aim aim){if(m.Fire(Golf,aim.pull))run.golf++;}
    // Throws a golf ball the moment one can reach anybody.
    public static void Spam(DadThrowModel m,Run run)
    {
        if(!Ready(m,run))return;DadThrowModel.Enemy lead=Lead(m);if(lead==null){run.wait=6;return;}
        Aim most,onLead;BestGolf(m,lead,out most,out onLead);
        if(most.any)Golfed(m,run,most);else run.wait=6;
    }
    static void Hold(DadThrowModel m,Run run,DadThrowModel.Enemy lead)
    {
        Aim most,onLead;BestGolf(m,lead,out most,out onLead);
        bool danger=lead.x-m.Field.Gate<Danger;
        if(most.hits>=Math.Min(3,m.Rules.Pierce))Golfed(m,run,most);
        else if(danger&&onLead.any)Golfed(m,run,onLead);
        else if(danger&&most.any)Golfed(m,run,most);
        else run.wait=12;
    }
    // Holds the golf ball until it can reach three people, or until the lead is close to the house.
    public static void Patient(DadThrowModel m,Run run)
    {
        if(!Ready(m,run))return;DadThrowModel.Enemy lead=Lead(m);if(lead==null){run.wait=6;return;}
        Hold(m,run,lead);
    }
    // Throws a shoe at the lead group while others are still behind it and the house is not in danger; otherwise plays like Patient.
    public static void ShoeFirst(DadThrowModel m,Run run)
    {
        if(!Ready(m,run))return;DadThrowModel.Enemy lead=Lead(m);if(lead==null){run.wait=6;return;}
        float gap;
        if(lead.x-m.Field.Gate>=ShoeSafe&&GapBehind(m,lead,out gap)&&gap<=ShoeReach)
        {
            Aim shoe=BestShoe(m,lead);
            if(shoe.any&&m.Fire(Shoe,shoe.pull)){run.shoe++;return;}
        }
        Hold(m,run,lead);
    }
    public static void Idle(DadThrowModel m,Run run){}
    // A rough stand-in for a person: takes `think` seconds to aim once the hand is ready, and aims well only
    // `good` of the time; otherwise the pull is a guess. With `shoes`, a crowd close to the gate gets a shoe first.
    // It is still a bot. It only gives a scale for how much a table asks of someone who is not perfect.
    public static Policy Sloppy(int seed,bool shoes,float think=.45f,float good=.65f)
    {
        var random=new System.Random(seed);
        return (m,run)=>
        {
            if(!m.CanFire){run.think=-1;return;}
            if(run.think<0)run.think=(int)(think*120);
            if(run.think>0){run.think--;return;}
            DadThrowModel.Enemy lead=Lead(m);if(lead==null)return;
            bool aimed=random.NextDouble()<good;
            if(shoes&&aimed&&lead.x-m.Field.Gate<ShoeSafe)
            {
                int crowd=0;foreach(DadThrowModel.Enemy e in m.enemies)if(e.x-lead.x<m.Rules.ShoeRadius&&e.slow<=0)crowd++;
                Aim shoe=crowd>=3?BestShoe(m,lead):new Aim();
                if(shoe.any&&m.Fire(Shoe,shoe.pull)){run.shoe++;run.think=-1;return;}
            }
            Vector2 pull=Pulls[random.Next(Pulls.Length)];
            if(aimed)
            {
                Aim most,onLead;BestGolf(m,lead,out most,out onLead);if(!most.any)return;
                pull=lead.x-m.Field.Gate<Danger&&onLead.any?onLead.pull:most.pull;
            }
            if(m.Fire(Golf,pull)){run.golf++;run.think=-1;}
        };
    }

    public sealed class Joining { public int shoes,frontLeft,reach,inView,plain;public bool joined;public float startGap,endGap,time;public string trace; }
    // People count as one group once the gap between them is no more than this.
    public const float JoinGap=85;
    // The most people one golf ball could reach at any moment while the street walks in untouched, up to the
    // point where the lead is `Danger` from the house line. A waiting player's best single throw.
    public static int BestBallWhileWalking(DadThrowModel m)
    {
        int best=0;
        for(int i=0;i<120*40&&m.phase==Playing;i++)
        {
            DadThrowModel.Enemy lead=Lead(m);
            if(lead!=null&&lead.x-m.Field.Gate<Danger)break;
            if(i%12==0&&lead!=null){Aim most,onLead;BestGolf(m,lead,out most,out onLead);best=Math.Max(best,most.hits);}
            m.Tick(1/120f);
        }
        return best;
    }
    // The first `front` people of a stage and the `behind` people after them, both on the street at `from` seconds:
    // shoes are thrown at the front group until it stands with those behind, is gone, or `limit` shoes are spent.
    // Then the street is left to walk in, to see the most one well-timed golf ball can reach (`reach`), against
    // the same wait with no shoe thrown at all (`plain`).
    public static Joining Join(DadThrowStage stage,DadThrowField field,DadThrowRules rules,int front,int behind,float from,int limit=4)
    {
        var m=new DadThrowModel(field,stage);if(rules!=null)m.Rules=rules;
        for(int i=0;i<(int)(from*120);i++)m.Tick(1/120f);
        var j=new Joining();var trace=new System.Text.StringBuilder();
        var untouched=new DadThrowModel(field,stage);if(rules!=null)untouched.Rules=rules;
        for(int i=0;i<(int)(from*120);i++)untouched.Tick(1/120f);
        j.plain=BestBallWhileWalking(untouched);
        for(int i=0;i<120*30&&m.phase==Playing;i++)
        {
            if(m.CanFire&&m.projectiles.Count==0)
            {
                float frontBack=float.MinValue,behindLead=float.MaxValue;j.frontLeft=0;DadThrowModel.Enemy lead=null;
                foreach(DadThrowModel.Enemy e in m.enemies)
                {
                    if(e.id<=front){j.frontLeft++;frontBack=Mathf.Max(frontBack,e.x);if(lead==null||e.x<lead.x)lead=e;}
                    else if(e.id<=front+behind)behindLead=Mathf.Min(behindLead,e.x);
                }
                j.time=m.Clock;
                if(j.frontLeft==0){trace.Append(" gone");break;}
                j.endGap=behindLead-frontBack;if(j.shoes==0)j.startGap=j.endGap;
                trace.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture," {0:0}",j.endGap));
                if(j.endGap<=JoinGap){j.joined=true;j.inView=m.enemies.Count;j.reach=BestBallWhileWalking(m);break;}
                if(j.shoes>=limit)break;
                Aim shoe=BestShoe(m,lead);if(!shoe.any||!m.Fire(Shoe,shoe.pull)){trace.Append(" (no shoe reaches)");break;}
                j.shoes++;
            }
            m.Tick(1/120f);
        }
        j.trace=trace.ToString().Trim();return j;
    }

    public static Run Play(DadThrowStage stage,DadThrowField field,Policy policy,DadThrowRules rules=null)
    {
        var m=new DadThrowModel(field,stage);if(rules!=null)m.Rules=rules;var run=new Run{end=m};
        for(int i=0;i<120*300&&m.phase==Playing;i++)
        {
            policy(m,run);m.Tick(1/120f);
            int inView=0;float nearest=float.MaxValue;
            foreach(DadThrowModel.Enemy e in m.enemies){run.closest=Mathf.Min(run.closest,e.x-field.Gate);if(e.x<=DadThrowModel.Width){inView++;nearest=Mathf.Min(nearest,e.x);}}
            run.ticks++;run.crowd+=inView;run.peak=Math.Max(run.peak,inView);if(inView>0){run.leadTicks++;run.lead+=nearest;}
        }
        // Someone who walked in was, at that moment, at the line.
        if(m.Escaped>0)run.closest=0;
        return run;
    }
}
#endif
