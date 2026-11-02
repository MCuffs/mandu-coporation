using System;

namespace SecondFistGame {
    public sealed class PrologueTimeline {
        public sealed class Beat {
            public readonly int art;public readonly string speaker,text;public readonly float duration;public readonly bool impact;
            public Beat(int art,string speaker,string text,float duration,bool impact=false){this.art=art;this.speaker=speaker;this.text=text;this.duration=duration;this.impact=impact;}
        }
        public static readonly Beat[] Beats={
            new Beat(0,"정민수","나는 팔룡회의 전국구 보스,\n정민수였다.",3.4f),
            new Beat(0,"조직원","회장님.\n이제 그 자리, 비워주셔야겠습니다.",3.2f),
            new Beat(1,"정민수","믿었던 조직이 내 뒤통수를 쳤다.\n그렇게, 나는 죽었다.",3.6f,true),
            new Beat(-1,"","모든 게 끝난 줄 알았다.",2.4f),
            new Beat(-2,"","",.6f,true),
            new Beat(2,"정민수","눈을 뜨자, 낯선 몸이 짓밟히고 있었다.\n…이건, 내 아들의 몸이었다.",3.8f,true),
            new Beat(2,"일진","야 가서 니네 부모 모셔와.",2.8f),
            new Beat(3,"엄마","그만해! 우리 애한테서 떨어져!",2.8f),
            new Beat(4,"엄마","아악…!\n제발… 우리 아들만은…!",3.1f,true),
            new Beat(5,"아들","엄마… 엄마!!\n그만하라고!!",3.1f),
            new Beat(5,"정민수","내가 버린 가족이, 이렇게 살고 있었다.\n아이의 절규가 내 안을 찢어놓았다.",3.7f),
            new Beat(5,"정민수 / 아들","니들 다 뒤졌다.",2.4f,true),
            new Beat(5,"정민수","내가 누군지 아냐!",2.2f),
            new Beat(5,"정민수","나 팔룡회 정민수다!",3.1f,true)
        };
        public int index{get;private set;}public float elapsed{get;private set;}public bool finished{get;private set;}
        bool forceReveal;
        readonly float[] minimumDurations=new float[Beats.Length];
        public float currentDuration{get{return Math.Max(current.duration,minimumDurations[index]);}}
        public void SetMinimumDuration(int beat,float seconds){if(beat<0||beat>=Beats.Length)throw new ArgumentOutOfRangeException("beat");minimumDurations[beat]=Math.Max(0,seconds);}
        public Beat current{get{return Beats[Math.Min(index,Beats.Length-1)];}}
        public bool textComplete{get{return forceReveal||elapsed*27>=current.text.Length;}}
        public string visibleText{get{return textComplete?current.text:current.text.Substring(0,Math.Min(current.text.Length,(int)(elapsed*27)));}}
        // Returns true once, exactly when the prologue finishes. No combat runs inside the timeline.
        public bool Step(float dt,bool advance=false,bool skip=false){
            if(finished)return false;
            elapsed+=Math.Max(0,Math.Min(dt,.1f));
            if(skip){finished=true;return true;}
            bool next=elapsed>=currentDuration;
            if(advance&&elapsed>.35f){if(!textComplete)forceReveal=true;else next=true;}
            if(!next)return false;
            index++;elapsed=0;forceReveal=false;
            if(index>=Beats.Length){index=Beats.Length-1;finished=true;return true;}
            return false;
        }
    }
}
