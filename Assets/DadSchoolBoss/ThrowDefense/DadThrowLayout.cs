using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Where every picture and control goes on the 1280x720 logical canvas (plan §4). Numbers only: no textures,
// no scene, so the same rules run in the window-free checks.
// Picture sizes and anchors are read from the art manifests in Resources; the street's manifest entry also
// carries the field numbers (feet line, house line, where dad stands), and DadThrowField is built from them.
// The fight reads that field, so the rules and the picture cannot be given different numbers.
public sealed class DadThrowLayout
{
    public sealed class Sprite
    {
        public string id,resource;public float width,height,stand;
        public Rect bounds;public Vector2 pivot,hand;public bool hasHand;
    }
    public enum Pose { Ready, Windup, Release }
    // Logical canvas fitted inside the device's safe area without stretching.
    public struct Fit
    {
        public float scale;public Vector2 offset;
        // Screen point (bottom-left origin, pixels) to canvas point (top-left origin, logical units).
        public Vector2 ToCanvas(Vector2 screen,float screenHeight){return (new Vector2(screen.x,screenHeight-screen.y)-offset)/scale;}
    }

    public const float W=DadThrowModel.Width,H=DadThrowModel.Height;
    // How long the release pose stays after a throw before returning to ready.
    public const float ReleaseTime=.22f;
    // Drawn sizes of the thrown things. They were 28 and 64 while dad stood 183 tall; since 2026-11-17 dad is the
    // opponents' size and the things he throws came down with him, as far as the rules allow: exactly the size of
    // their hit circles (DadThrowModel.GolfHit, ShoeHit). Drawn any smaller they would hit before they touch.
    public const float GolfDiameter=DadThrowModel.GolfHit*2,ShoeLength=DadThrowModel.ShoeHit*2,IconSize=46;

    // ── battle screen (plan §4.3). The top bar is thin and the middle of the street stays empty. ──
    public const float TopBar=86;
    // HouseHealth holds the word 대문 and five cells: the gate touches this group of challengers has left.
    public static readonly Rect StageLabel=new Rect(32,20,180,48),HouseHealth=new Rect(230,20,252,48),PauseButton=new Rect(1160,16,72,64);
    // The plan gives 140 for an icon and a number; no icon exists yet, so the words need the extra width.
    public static readonly Rect RemainingLabel=new Rect(880,20,200,48);
    public static readonly Rect GolfButton=new Rect(24,625,210,65),ShoeButton=new Rect(250,625,210,65);
    // A drag may start anywhere here; the finger does not have to pick dad up.
    public static readonly Rect AimArea=new Rect(25,100,1230,505),Hint=new Rect(470,100,340,50);
    public const int HealthCells=DadThrowStageSet.MaxHealth;
    public static Rect HealthLabel {get{return new Rect(HouseHealth.x,HouseHealth.y,62,HouseHealth.height);}}
    public static Rect HealthCell(int index){return new Rect(HouseHealth.x+72+index*36,HouseHealth.center.y-14,28,28);}
    public static Rect PauseBar(int index){Vector2 c=PauseButton.center;return new Rect(c.x-14+index*18,c.y-14,10,28);}
    public static Rect IconSlot(Rect button){return new Rect(button.x+14,button.center.y-IconSize/2,IconSize,IconSize);}
    public static Rect LabelSlot(Rect button){return Rect.MinMaxRect(button.x+14+IconSize+6,button.y,button.xMax-10,button.yMax);}

    // ── title (plan §6.2) ──
    public static readonly Rect TitleText=new Rect(290,75,700,165),StartButton=new Rect(490,600,300,72);

    // ── shop (DadThrowShop): its button and the purse on the title, the purse in the fight's top bar, and the
    // shop itself: a heading, the purse, the way out, and the shelf of nine cards three by three ──
    public static readonly Rect ShopButton=new Rect(820,606,230,60),ShopPurse=new Rect(820,668,230,30),CoinLabel=new Rect(540,20,250,48);
    public static readonly Rect ShopHeading=new Rect(40,16,300,64),ShopCoins=new Rect(640,20,380,56),ShopClose=new Rect(1070,16,170,64);
    public const int ShopColumns=3;public const float ShopCardWidth=386,ShopCardHeight=176,ShopGap=21,ShopTop=104,ShopLeft=40;
    public static Rect ShopCard(int index){return new Rect(ShopLeft+index%ShopColumns*(ShopCardWidth+ShopGap),ShopTop+index/ShopColumns*(ShopCardHeight+ShopGap),ShopCardWidth,ShopCardHeight);}
    // Inside a card: the thing's picture, its name, what it does, a cell for each time it can be bought, the price.
    public static Vector2 ShopIcon(Rect card){return new Vector2(card.x+54,card.y+56);}
    public static Rect ShopName(Rect card){return new Rect(card.x+104,card.y+12,card.width-116,40);}
    public static Rect ShopLine(Rect card){return new Rect(card.x+104,card.y+54,card.width-116,52);}
    public static Rect ShopPip(Rect card,int index){return new Rect(card.x+20+index*30,card.y+130,22,22);}
    public static Rect ShopBuy(Rect card){return new Rect(card.xMax-170,card.y+112,152,52);}

    // ── clear / defeat / pause cards (plan §6.3-6.5, sizes from the wide v2 screens) ──
    public static readonly Rect ResultCard=new Rect(650,218,460,330),PauseCard=new Rect(445,145,390,430);
    // The result pose stands left of the card. Feet point and scale come from the v2 screen (a 410-wide box for a 1254 picture).
    public static readonly Vector2 ResultFeet=new Vector2(355,590);
    public const float ResultScale=410f/1254;
    static Rect CardHeading(Rect card,float top){return new Rect(card.x,card.y+top,card.width,64);}
    // One wide button, or the left/right half of a pair, `top` below the card's upper edge. Buttons stay 72 tall.
    static Rect CardButton(Rect card,float top,int slot=0,int slots=1)
    {float inner=card.width-56,gap=16,w=(inner-gap*(slots-1))/slots;return new Rect(card.x+28+slot*(w+gap),card.y+top,w,72);}
    // Clear: a heading and two buttons side by side (the lesser choice on the left). Between them, the first
    // time a stage is cleared, one line from someone in the story.
    public static readonly Rect ClearHeading=CardHeading(ResultCard,40),ClearLeft=CardButton(ResultCard,230,0,2),ClearRight=CardButton(ResultCard,230,1,2);
    public static readonly Rect ClearWordName=new Rect(ResultCard.x+28,ResultCard.y+118,ResultCard.width-56,26),ClearWordLine=new Rect(ResultCard.x+28,ResultCard.y+146,ResultCard.width-56,70);
    // Defeat: heading, one line saying what happened, a large retry and a small way back to the title.
    public static readonly Rect DefeatHeading=CardHeading(ResultCard,32),DefeatLine=new Rect(ResultCard.x+24,ResultCard.y+98,ResultCard.width-48,36),
        DefeatRetry=CardButton(ResultCard,148),DefeatShop=new Rect(ResultCard.x+28,ResultCard.y+236,194,56),DefeatTitle=new Rect(ResultCard.xMax-222,ResultCard.y+236,194,56);
    // What the stage paid, over the top edge of the result card.
    public static readonly Rect ResultEarned=new Rect(ResultCard.x,ResultCard.y-56,ResultCard.width,48);
    // Pause: heading, resume, title. The sound switch joins when the game has sound.
    public static readonly Rect PauseHeading=CardHeading(PauseCard,88),PauseResume=CardButton(PauseCard,182),PauseTitle=CardButton(PauseCard,272);

    public readonly Sprite Street,Title,Ready,Windup,Release,Golf,Shoe,Clear,Retry;
    // Painted lines of the street picture, on the canvas, and the gate door the challengers run for.
    public readonly float SandTopY,RoadEdgeY,GateFrontX;public readonly Rect GateDoor;
    // Field numbers of the street (canvas units): feet line, house line, and where dad is planted.
    public readonly float Ground,FailLine,DadFeetX,DadFeetY,DadHeight;
    public readonly DadThrowField Field;
    // Backgrounds a stage may name: only those with measured anchors.
    public readonly List<string> Backgrounds=new List<string>();

    // throwArt: Resources/DadThrowV1 (dad poses, things). wide: DadThrowScreensV2 (street, title). result: DadThrowScreensV1 (clear, retry).
    DadThrowLayout(string throwArt,string wide,string result)
    {
        var poses=Assets(throwArt,"DadThrowV1 manifest");var scenes=Assets(wide,"DadThrowScreensV2 manifest");var heroes=Assets(result,"DadThrowScreensV1 manifest");
        Ready=Read(poses,"dad_ready");Windup=Read(poses,"dad_windup");Release=Read(poses,"dad_release");
        Golf=Read(poses,"projectile_golf");Shoe=Read(poses,"projectile_shoe");
        Title=Read(scenes,"title_wide");Clear=Read(heroes,"dad_clear");Retry=Read(heroes,"dad_retry");
        Street=Read(scenes,"street_wide");
        var street=scenes["street_wide"];string owner="street_wide";float k=Background().height;
        SandTopY=DadThrowJson.Number(street,"sandTopNormalizedY",owner)*k;RoadEdgeY=DadThrowJson.Number(street,"groundNormalizedY",owner)*k;
        GateFrontX=DadThrowJson.Number(street,"gateFrontNormalizedX",owner)*W;
        object door;if(!street.TryGetValue("gateDoorPixels",out door))throw new ArgumentException(owner+" has no gate door");
        var edges=DadThrowJson.List(door,owner+" gateDoorPixels");float pixel=W/Street.width;
        if(edges.Count!=4)throw new ArgumentException(owner+" gateDoorPixels needs four numbers");
        foreach(object edge in edges)if(!(edge is double))throw new ArgumentException(owner+" gateDoorPixels needs four numbers");
        GateDoor=Rect.MinMaxRect((float)(double)edges[0]*pixel,(float)(double)edges[1]*pixel,(float)(double)edges[2]*pixel,(float)(double)edges[3]*pixel);
        object value;if(!street.TryGetValue("field",out value))throw new ArgumentException(owner+" has no field numbers");
        var field=DadThrowJson.Map(value,owner+" field");owner+=" field";
        Ground=DadThrowJson.Number(field,"feetLineY",owner);FailLine=DadThrowJson.Number(field,"failLineX",owner);
        DadFeetX=DadThrowJson.Number(field,"dadFeetX",owner);DadFeetY=DadThrowJson.Number(field,"dadFeetY",owner);DadHeight=DadThrowJson.Number(field,"dadHeight",owner);
        if(!Release.hasHand||!Windup.hasHand)throw new ArgumentException("dad_release and dad_windup need handPixels");
        if(DadHeight<=0)throw new ArgumentException(owner+" needs a dad height above zero");
        Field=new DadThrowField(Ground,FailLine,ReleaseHand);
        Backgrounds.Add(Street.resource);
    }
    public static DadThrowLayout Parse(string throwArt,string wide,string result){return new DadThrowLayout(throwArt,wide,result);}

    static Dictionary<string,Dictionary<string,object>> Assets(string json,string owner)
    {
        var found=new Dictionary<string,Dictionary<string,object>>();
        var root=DadThrowJson.Map(DadThrowJson.Parse(json),owner);object list;
        if(!root.TryGetValue("assets",out list))throw new ArgumentException(owner+" has no assets");
        foreach(object item in DadThrowJson.List(list,owner+" assets"))
        {var map=DadThrowJson.Map(item,owner+" asset");found[DadThrowJson.Text(map,"id",owner+" asset")]=map;}
        return found;
    }
    static Vector2 Point(Dictionary<string,object> map,string key,string owner)
    {var p=DadThrowJson.Map(map[key],owner+" "+key);return new Vector2(DadThrowJson.Number(p,"x",owner+" "+key),DadThrowJson.Number(p,"y",owner+" "+key));}
    static Sprite Read(Dictionary<string,Dictionary<string,object>> assets,string id)
    {
        Dictionary<string,object> map;if(!assets.TryGetValue(id,out map))throw new ArgumentException("manifest has no asset "+id);
        var s=new Sprite{id=id,resource=DadThrowJson.Text(map,"resource",id),width=DadThrowJson.Number(map,"width",id),height=DadThrowJson.Number(map,"height",id)};
        if(s.width<=0||s.height<=0)throw new ArgumentException(id+" has no size");
        // Opaque part of the picture. A full-frame background has none listed and uses its whole size.
        s.bounds=new Rect(0,0,s.width,s.height);
        if(DadThrowJson.Has(map,"bounds"))
        {
            var b=DadThrowJson.List(map["bounds"],id+" bounds");
            if(b.Count!=4)throw new ArgumentException(id+" bounds needs four numbers");
            float[] edge=new float[4];for(int i=0;i<4;i++){if(!(b[i] is double))throw new ArgumentException(id+" bounds needs four numbers");edge[i]=(float)(double)b[i];}
            s.bounds=Rect.MinMaxRect(edge[0],edge[1],edge[2],edge[3]);
        }
        // Feet: the listed pivot, or the bottom middle of the opaque part when the manifest gives none.
        s.pivot=DadThrowJson.Has(map,"pivotPixels")?Point(map,"pivotPixels",id):new Vector2(s.bounds.center.x,s.bounds.yMax);
        s.hasHand=DadThrowJson.Has(map,"handPixels");if(s.hasHand)s.hand=Point(map,"handPixels",id);
        s.stand=DadThrowJson.NumberOr(map,"standPixels",s.bounds.height,id);
        return s;
    }

    // ── screen fit (plan §4.2): the canvas keeps its shape inside the safe area; what is left over is letterbox ──
    public static Fit FitTo(Rect safeArea,float screenHeight)
    {
        float scale=Mathf.Max(.01f,Mathf.Min(safeArea.width/W,safeArea.height/H));
        return new Fit{scale=scale,offset=new Vector2(safeArea.x+(safeArea.width-W*scale)/2,screenHeight-safeArea.yMax+(safeArea.height-H*scale)/2)};
    }

    // ── battle ──
    // Pictures are fitted to the canvas width and never stretched.
    public Rect Background(){return new Rect(0,0,W,W*Street.height/Street.width);}
    public Rect TitleArt(){return new Rect(0,0,W,W*Title.height/Title.width);}

    public Sprite Dad(Pose pose){return pose==Pose.Windup?Windup:pose==Pose.Release?Release:Ready;}
    public static Pose PoseFor(bool aiming,float sinceThrow){return aiming?Pose.Windup:sinceThrow>0?Pose.Release:Pose.Ready;}
    float DadScale(Sprite s){return DadHeight/s.stand;}
    // Full picture rectangle that puts the pose's foot pivot on the shared foot point: dad is planted in every pose.
    public Rect DadRect(Sprite s){float k=DadScale(s);return new Rect(DadFeetX-s.pivot.x*k,DadFeetY-s.pivot.y*k,s.width*k,s.height*k);}
    public Vector2 DadPoint(Sprite s,Vector2 pixel){float k=DadScale(s);return new Vector2(DadFeetX+(pixel.x-s.pivot.x)*k,DadFeetY+(pixel.y-s.pivot.y)*k);}
    // Opaque part of the pose on the canvas, for edge and overlap checks.
    public Rect DadBody(Sprite s)
    {Vector2 a=DadPoint(s,new Vector2(s.bounds.xMin,s.bounds.yMin)),b=DadPoint(s,new Vector2(s.bounds.xMax,s.bounds.yMax));return Rect.MinMaxRect(a.x,a.y,b.x,b.y);}
    // Where the thrown thing leaves the hand, and where it is held while pulling back.
    public Vector2 ReleaseHand {get{return DadPoint(Release,Release.hand);}}
    public Vector2 WindupHand {get{return DadPoint(Windup,Windup.hand);}}

    // A thing drawn `size` wide (its opaque part), centred on `center`. Rotate around `center`.
    public Rect ItemRect(Sprite s,Vector2 center,float size)
    {float k=size/s.bounds.width;return new Rect(center.x-s.pivot.x*k,center.y-s.pivot.y*k,s.width*k,s.height*k);}
    public Rect GolfRect(Vector2 center){return ItemRect(Golf,center,GolfDiameter);}
    public Rect ShoeRect(Vector2 center){return ItemRect(Shoe,center,ShoeLength);}

    // ── clear / defeat ──
    public Rect HeroRect(Sprite s){return new Rect(ResultFeet.x-s.pivot.x*ResultScale,ResultFeet.y-s.pivot.y*ResultScale,s.width*ResultScale,s.height*ResultScale);}
    public Rect HeroBody(Sprite s)
    {Rect r=HeroRect(s);return new Rect(r.x+s.bounds.x*ResultScale,r.y+s.bounds.y*ResultScale,s.bounds.width*ResultScale,s.bounds.height*ResultScale);}

    // One line per rectangle; the offline preview reads these so its picture uses the game's own numbers.
    public string Describe()
    {
        var r=new System.Text.StringBuilder();
        Action<string,Rect> rect=(name,v)=>r.AppendLine(string.Format(CultureInfo.InvariantCulture,"LAYOUT {0} {1:0.##} {2:0.##} {3:0.##} {4:0.##}",name,v.x,v.y,v.width,v.height));
        Action<string,Vector2> point=(name,v)=>r.AppendLine(string.Format(CultureInfo.InvariantCulture,"LAYOUT {0} {1:0.##} {2:0.##} 0 0",name,v.x,v.y));
        rect("background",Background());rect("dad_ready",DadRect(Ready));rect("dad_windup",DadRect(Windup));rect("dad_release",DadRect(Release));
        point("release_hand",ReleaseHand);point("windup_hand",WindupHand);point("muzzle",Field.Muzzle);
        rect("golf_at_muzzle",GolfRect(Field.Muzzle));rect("shoe_at_muzzle",ShoeRect(Field.Muzzle));
        rect("golf_in_hand",GolfRect(WindupHand));rect("shoe_in_hand",ShoeRect(WindupHand));
        rect("golf_button",GolfButton);rect("shoe_button",ShoeButton);rect("pause_button",PauseButton);
        rect("pause_bar_0",PauseBar(0));rect("pause_bar_1",PauseBar(1));
        rect("golf_icon",ItemRect(Golf,IconSlot(GolfButton).center,IconSize));rect("shoe_icon",ItemRect(Shoe,IconSlot(ShoeButton).center,IconSize));
        rect("golf_label",LabelSlot(GolfButton));rect("shoe_label",LabelSlot(ShoeButton));
        rect("top_bar",new Rect(0,0,W,TopBar));rect("stage_label",StageLabel);rect("health_label",HealthLabel);
        for(int i=0;i<HealthCells;i++)rect("health_cell_"+i,HealthCell(i));
        rect("remaining_label",RemainingLabel);rect("hint",Hint);rect("aim_area",AimArea);
        rect("title_art",TitleArt());rect("title_text",TitleText);rect("start_button",StartButton);
        rect("shop_button",ShopButton);rect("shop_purse",ShopPurse);rect("coin_label",CoinLabel);rect("result_earned",ResultEarned);rect("defeat_shop",DefeatShop);
        rect("shop_heading",ShopHeading);rect("shop_coins",ShopCoins);rect("shop_close",ShopClose);
        for(int i=0;i<DadThrowShop.Cards.Length;i++)rect("shop_card_"+i,ShopCard(i));
        rect("result_card",ResultCard);rect("pause_card",PauseCard);rect("hero_clear",HeroRect(Clear));rect("hero_retry",HeroRect(Retry));
        rect("clear_heading",ClearHeading);rect("clear_left",ClearLeft);rect("clear_right",ClearRight);rect("clear_word_name",ClearWordName);rect("clear_word_line",ClearWordLine);
        rect("gate_door",GateDoor);rect("story_dialogue",DadThrowStory.Dialogue);rect("story_name",DadThrowStory.DialogueName);rect("story_line",DadThrowStory.DialogueLine);rect("story_skip",DadThrowStory.Skip);
        rect("story_rival",DadThrowStory.Rival);rect("story_prop",DadThrowStory.Prop);
        rect("defeat_heading",DefeatHeading);rect("defeat_line",DefeatLine);rect("defeat_retry",DefeatRetry);rect("defeat_title",DefeatTitle);
        rect("pause_heading",PauseHeading);rect("pause_resume",PauseResume);rect("pause_title",PauseTitle);
        point("gate",new Vector2(Field.Gate,Field.Ground));point("painted_gate",new Vector2(GateFrontX,Field.Ground));
        point("road_edge",new Vector2(0,RoadEdgeY));point("sand_top",new Vector2(0,SandTopY));
        return r.ToString();
    }
}
