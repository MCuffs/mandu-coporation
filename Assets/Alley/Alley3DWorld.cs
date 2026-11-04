using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SecondFistGame;

public sealed class Alley3DWorld : MonoBehaviour {
    public sealed class Puppet3D {
        public GameObject root;
        public Transform hips,torso,head,armL,armR,legL,legR,gloveL,gloveR,auraRing;
        public Renderer torsoRenderer,pantsRenderer,gloveLRenderer,gloveRRenderer,headRenderer;
        public float walkCycle,punchTimer,hitFlash,lastYaw;
        public int role,appearance;
        public bool active;
    }

    sealed class Station3D {
        public GameObject root,padFrame,padFill,equipmentRoot;
        public Transform movingPart;
        public int index;
    }

    Transform worldRoot,envRoot,actorsRoot,propsRoot,coinsRoot,cashStackRoot,gateRoot,hazardsRoot,plankGroup;
    Renderer floorMainRenderer,wallLeftRenderer,wallRightRenderer,neonLeftRenderer,neonRightRenderer;
    Light mainLight,fillLight;
    Material matSkin,matHair,matDark,matWhite,matGold,matCash,matWoodFloor,matRubberFloor,matAsphalt,matBrick,matNeonCyan,matNeonRed,matShadow;
    readonly Dictionary<Color,Material> colorMaterials=new Dictionary<Color,Material>();
    Puppet3D heroPuppet,partnerPuppet;
    readonly Dictionary<int,Puppet3D> enemyPuppets=new Dictionary<int,Puppet3D>();
    readonly List<Puppet3D> traineePuppets=new List<Puppet3D>();
    readonly List<Station3D> stationModels=new List<Station3D>();
    readonly List<GameObject> coinModels=new List<GameObject>();
    readonly List<GameObject> billModels=new List<GameObject>();
    readonly List<GameObject> hazardModels=new List<GameObject>();
    readonly List<GameObject> shockwaveRings=new List<GameObject>();
    int lastThemeKey=-99,lastGearTier=-1;
    Camera cam;
    Vector3 camCurrentPos=new Vector3(0,13.5f,-9.2f);
    Shader baseShader;
    bool initialized;

    public void Initialize(Camera mainCam){
        if(initialized)return;
        initialized=true;
        cam=mainCam;
        if(cam!=null){
            cam.orthographic=false;
            cam.fieldOfView=38f;
            cam.nearClipPlane=0.5f;
            cam.farClipPlane=100f;
            cam.backgroundColor=new Color(.07f,.09f,.13f);
            cam.transform.position=camCurrentPos;
            cam.transform.rotation=Quaternion.Euler(52f,0f,0f);
        }
        RenderSettings.ambientMode=AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.62f,.64f,.70f);

        baseShader=Shader.Find("Standard");
        if(baseShader==null)baseShader=Shader.Find("Legacy Shaders/Diffuse");
        if(baseShader==null)baseShader=Shader.Find("Sprites/Default");

        worldRoot=new GameObject("World3D").transform;
        worldRoot.SetParent(transform,false);
        envRoot=new GameObject("Environment").transform;envRoot.SetParent(worldRoot,false);
        propsRoot=new GameObject("GymProps").transform;propsRoot.SetParent(worldRoot,false);
        actorsRoot=new GameObject("Actors").transform;actorsRoot.SetParent(worldRoot,false);
        coinsRoot=new GameObject("Coins").transform;coinsRoot.SetParent(worldRoot,false);
        hazardsRoot=new GameObject("Hazards").transform;hazardsRoot.SetParent(worldRoot,false);
        cashStackRoot=new GameObject("CashStack").transform;cashStackRoot.SetParent(worldRoot,false);

        SetupLights();
        SetupMaterials();
        BuildEnvironment();
        heroPuppet=CreatePuppet("Hero_MinSu",0,0);
        partnerPuppet=CreatePuppet("Partner_Fighter",1,1);
        partnerPuppet.root.SetActive(false);
    }

    void SetupLights(){
        GameObject l1=new GameObject("SunLight");
        l1.transform.SetParent(worldRoot,false);
        l1.transform.rotation=Quaternion.Euler(55f,-28f,0f);
        mainLight=l1.AddComponent<Light>();
        mainLight.type=LightType.Directional;
        mainLight.color=new Color(1f,.96f,.88f);
        mainLight.intensity=1.15f;
        mainLight.shadows=LightShadows.Soft;
        mainLight.shadowStrength=0.45f;

        GameObject l2=new GameObject("RimLight");
        l2.transform.SetParent(worldRoot,false);
        l2.transform.rotation=Quaternion.Euler(35f,145f,0f);
        fillLight=l2.AddComponent<Light>();
        fillLight.type=LightType.Directional;
        fillLight.color=new Color(.45f,.65f,.95f);
        fillLight.intensity=0.42f;
        fillLight.shadows=LightShadows.None;
    }

    Material Mat(Color c,float smoothness=0.35f,float metallic=0.05f,bool emissive=false){
        Material m;
        if(!emissive&&colorMaterials.TryGetValue(c,out m))return m;
        m=new Material(baseShader);
        m.color=c;
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smoothness);
        if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);
        if(emissive&&m.HasProperty("_EmissionColor")){
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor",c*0.85f);
        }
        if(!emissive)colorMaterials[c]=m;
        return m;
    }

    void SetupMaterials(){
        matSkin=Mat(new Color(1f,.82f,.68f),.25f);
        matHair=Mat(new Color(.14f,.15f,.18f),.3f);
        matDark=Mat(new Color(.15f,.18f,.23f),.3f);
        matWhite=Mat(new Color(.96f,.96f,.94f),.4f);
        matGold=Mat(new Color(1f,.78f,.20f),.75f,.35f,true);
        matCash=Mat(new Color(.22f,.84f,.42f),.4f,.05f);
        matWoodFloor=Mat(new Color(.82f,.57f,.32f),.45f);
        matRubberFloor=Mat(new Color(.20f,.24f,.30f),.25f);
        matAsphalt=Mat(new Color(.24f,.26f,.30f),.15f);
        matBrick=Mat(new Color(.52f,.24f,.20f),.15f);
        matNeonCyan=Mat(new Color(.20f,.88f,.95f),.8f,.1f,true);
        matNeonRed=Mat(new Color(1f,.28f,.20f),.8f,.1f,true);
        matShadow=Mat(new Color(.05f,.07f,.10f),.1f);
    }

    GameObject Part(PrimitiveType type,Transform parent,Vector3 localPos,Vector3 localScale,Material mat,Vector3 localEuler=default(Vector3),bool castShadow=true){
        GameObject g=GameObject.CreatePrimitive(type);
        g.transform.SetParent(parent,false);
        g.transform.localPosition=localPos;
        g.transform.localScale=localScale;
        if(localEuler!=Vector3.zero)g.transform.localEulerAngles=localEuler;
        Renderer r=g.GetComponent<Renderer>();
        r.sharedMaterial=mat;
        r.shadowCastingMode=castShadow?ShadowCastingMode.On:ShadowCastingMode.Off;
        r.receiveShadows=true;
        return g;
    }

    public Vector3 MapTo3D(Vector2 p,Rect arena){
        float nx=(p.x-arena.center.x)/Mathf.Max(140f,arena.width*.5f);
        float nz=(arena.center.y-p.y)/Mathf.Max(160f,arena.height*.5f);
        return new Vector3(nx*4.8f,0f,nz*6.4f);
    }

    public Vector2 WorldToGui(Vector3 worldPos,float scale){
        if(cam==null)return Vector2.zero;
        Vector3 sp=cam.WorldToScreenPoint(worldPos);
        return new Vector2(sp.x/scale,(Screen.height-sp.y)/scale);
    }

    void BuildEnvironment(){
        // Main Gym & Alley Floor slabs
        floorMainRenderer=Part(PrimitiveType.Cube,envRoot,new Vector3(0,-.25f,-0.8f),new Vector3(11.6f,0.5f,11.2f),matWoodFloor,Vector3.zero,false).GetComponent<Renderer>();
        // Wood floor plank stripes
        plankGroup=new GameObject("WoodPlanks").transform;plankGroup.SetParent(envRoot,false);
        Material darkPlank=Mat(new Color(.73f,.48f,.26f),.4f);
        for(int i=-4;i<=4;i++){
            Part(PrimitiveType.Cube,plankGroup,new Vector3(i*1.15f,0.005f,-0.8f),new Vector3(0.05f,0.01f,11.0f),darkPlank,Vector3.zero,false);
        }
        // Top Entrance Rubber Mat & Street Asphalt outside
        Part(PrimitiveType.Cube,envRoot,new Vector3(0,-.22f,5.6f),new Vector3(12.2f,0.5f,4.5f),matRubberFloor,Vector3.zero,false);
        Part(PrimitiveType.Cube,envRoot,new Vector3(0,-.20f,9.5f),new Vector3(18.0f,0.5f,6.0f),matAsphalt,Vector3.zero,false);

        // Yellow/Black Hazard Line at the Gym Gate Threshold
        for(int i=-5;i<=5;i++){
            Part(PrimitiveType.Cube,envRoot,new Vector3(i*0.95f,0.03f,4.6f),new Vector3(0.48f,0.03f,0.32f),i%2==0?matGold:matDark,Vector3.zero,false);
        }

        // Left & Right Brick Walls with Neon Bars
        wallLeftRenderer=Part(PrimitiveType.Cube,envRoot,new Vector3(-6.1f,1.8f,0.5f),new Vector3(0.7f,4.2f,15.5f),matBrick).GetComponent<Renderer>();
        wallRightRenderer=Part(PrimitiveType.Cube,envRoot,new Vector3(6.1f,1.8f,0.5f),new Vector3(0.7f,4.2f,15.5f),matBrick).GetComponent<Renderer>();
        neonLeftRenderer=Part(PrimitiveType.Cube,envRoot,new Vector3(-5.7f,2.4f,0.2f),new Vector3(0.12f,0.22f,5.2f),matNeonCyan).GetComponent<Renderer>();
        neonRightRenderer=Part(PrimitiveType.Cube,envRoot,new Vector3(5.7f,2.4f,0.2f),new Vector3(0.12f,0.22f,5.2f),matNeonRed).GetComponent<Renderer>();

        // Top Turf Gate Archway
        gateRoot=new GameObject("TurfGate").transform;gateRoot.SetParent(envRoot,false);
        gateRoot.localPosition=new Vector3(0,0,5.2f);
        Part(PrimitiveType.Cube,gateRoot,new Vector3(-2.6f,1.6f,0),new Vector3(0.45f,3.2f,0.45f),matDark);
        Part(PrimitiveType.Cube,gateRoot,new Vector3(2.6f,1.6f,0),new Vector3(0.45f,3.2f,0.45f),matDark);
        Part(PrimitiveType.Cube,gateRoot,new Vector3(0,3.1f,0),new Vector3(5.8f,0.48f,0.55f),matNeonRed);

        // Cash Safe Desk in Gym
        GameObject desk=new GameObject("CashSafeDesk");desk.transform.SetParent(propsRoot,false);
        Part(PrimitiveType.Cube,desk.transform,new Vector3(0,0.35f,0),new Vector3(1.75f,0.7f,0.95f),Mat(new Color(.16f,.26f,.22f),.4f));
        Part(PrimitiveType.Cube,desk.transform,new Vector3(0,0.72f,0),new Vector3(1.85f,0.08f,1.05f),matGold);
        cashStackRoot.localPosition=new Vector3(0,0,-2.6f);
        desk.transform.localPosition=new Vector3(-1.35f,0,-2.6f);

        // Build 5 Gym Station 3D Models
        for(int i=0;i<5;i++)stationModels.Add(CreateStation3D(i));
    }

    Station3D CreateStation3D(int index){
        Station3D s=new Station3D{index=index};
        s.root=new GameObject("GymStation_"+index);
        s.root.transform.SetParent(propsRoot,false);

        // Floor Pad (Supercent white/gold rounded drop zone)
        s.padFrame=Part(PrimitiveType.Cylinder,s.root.transform,new Vector3(0,0.03f,0),new Vector3(2.15f,0.03f,2.15f),matGold,Vector3.zero,false);
        s.padFill=Part(PrimitiveType.Cylinder,s.root.transform,new Vector3(0,0.045f,0),new Vector3(1.95f,0.03f,1.95f),matRubberFloor,Vector3.zero,false);

        s.equipmentRoot=new GameObject("EquipmentMesh");
        s.equipmentRoot.transform.SetParent(s.root.transform,false);

        if(index==0){
            // Heavy Red & Gold Punching Bag on Chrome Stand
            Material chrome=Mat(new Color(.75f,.80f,.86f),.75f,.6f);
            Part(PrimitiveType.Cylinder,s.equipmentRoot.transform,new Vector3(-0.55f,1.1f,0.35f),new Vector3(0.14f,1.1f,0.14f),chrome);
            Part(PrimitiveType.Cube,s.equipmentRoot.transform,new Vector3(-0.25f,2.15f,0.15f),new Vector3(0.75f,0.12f,0.14f),chrome);
            GameObject bagPivot=new GameObject("BagPivot");bagPivot.transform.SetParent(s.equipmentRoot.transform,false);
            bagPivot.transform.localPosition=new Vector3(0,2.1f,0);
            Part(PrimitiveType.Cylinder,bagPivot.transform,new Vector3(0,-0.85f,0),new Vector3(0.62f,0.68f,0.62f),Mat(new Color(.92f,.22f,.18f),.55f));
            Part(PrimitiveType.Cylinder,bagPivot.transform,new Vector3(0,-0.32f,0),new Vector3(0.64f,0.08f,0.64f),matGold);
            Part(PrimitiveType.Cylinder,bagPivot.transform,new Vector3(0,-1.35f,0),new Vector3(0.64f,0.08f,0.64f),matGold);
            s.movingPart=bagPivot.transform;
        }else if(index==1){
            // Blue & Gold Agility Floor Mat + Kettlebells
            Part(PrimitiveType.Cube,s.equipmentRoot.transform,new Vector3(0,0.08f,0),new Vector3(1.75f,0.08f,1.75f),Mat(new Color(.20f,.46f,.88f),.4f));
            Part(PrimitiveType.Sphere,s.equipmentRoot.transform,new Vector3(-0.55f,0.28f,-0.55f),new Vector3(0.36f,0.36f,0.36f),matGold);
            Part(PrimitiveType.Sphere,s.equipmentRoot.transform,new Vector3(0.55f,0.24f,0.55f),new Vector3(0.30f,0.30f,0.30f),matGold);
            s.movingPart=s.equipmentRoot.transform;
        }else if(index==2){
            // Champion Sparring Ring with 4 Corner Posts & Gold Ropes
            Part(PrimitiveType.Cube,s.equipmentRoot.transform,new Vector3(0,0.14f,0),new Vector3(2.1f,0.22f,2.1f),Mat(new Color(.92f,.86f,.74f),.3f));
            Vector3[] corners={new Vector3(-.92f,.55f,-.92f),new Vector3(.92f,.55f,-.92f),new Vector3(-.92f,.55f,.92f),new Vector3(.92f,.55f,.92f)};
            for(int c=0;c<4;c++){
                Part(PrimitiveType.Cylinder,s.equipmentRoot.transform,corners[c],new Vector3(0.16f,0.55f,0.16f),c%2==0?matNeonRed:Mat(new Color(.20f,.52f,.95f),.5f));
            }
            Part(PrimitiveType.Cube,s.equipmentRoot.transform,new Vector3(0,0.65f,-.92f),new Vector3(1.85f,0.05f,0.05f),matGold);
            Part(PrimitiveType.Cube,s.equipmentRoot.transform,new Vector3(0,0.65f,.92f),new Vector3(1.85f,0.05f,0.05f),matGold);
            Part(PrimitiveType.Cube,s.equipmentRoot.transform,new Vector3(-.92f,0.65f,0),new Vector3(0.05f,0.05f,1.85f),matGold);
            Part(PrimitiveType.Cube,s.equipmentRoot.transform,new Vector3(.92f,0.65f,0),new Vector3(0.05f,0.05f,1.85f),matGold);
            s.movingPart=s.equipmentRoot.transform;
        }else if(index==3){
            // Heavy Barbell Bench Press
            Part(PrimitiveType.Cube,s.equipmentRoot.transform,new Vector3(0,0.32f,0),new Vector3(0.55f,0.32f,1.35f),Mat(new Color(.88f,.20f,.18f),.5f));
            GameObject barbell=new GameObject("Barbell");barbell.transform.SetParent(s.equipmentRoot.transform,false);
            barbell.transform.localPosition=new Vector3(0,0.85f,-0.35f);
            Part(PrimitiveType.Cylinder,barbell.transform,Vector3.zero,new Vector3(0.08f,0.95f,0.08f),Mat(new Color(.8f,.85f,.9f),.7f,.6f),new Vector3(0,0,90f));
            Part(PrimitiveType.Cylinder,barbell.transform,new Vector3(-0.78f,0,0),new Vector3(0.52f,0.12f,0.52f),matGold,new Vector3(0,0,90f));
            Part(PrimitiveType.Cylinder,barbell.transform,new Vector3(0.78f,0,0),new Vector3(0.52f,0.12f,0.52f),matGold,new Vector3(0,0,90f));
            s.movingPart=barbell.transform;
        }else{
            // Golden Dragon Trophy Shrine
            Part(PrimitiveType.Cylinder,s.equipmentRoot.transform,new Vector3(0,0.35f,0),new Vector3(1.25f,0.35f,1.25f),Mat(new Color(.18f,.20f,.26f),.5f));
            GameObject orb=Part(PrimitiveType.Sphere,s.equipmentRoot.transform,new Vector3(0,1.15f,0),new Vector3(0.68f,0.68f,0.68f),matGold);
            s.movingPart=orb.transform;
        }
        return s;
    }

    Puppet3D CreatePuppet(string name,int role,int appearance){
        // role: 0=Hero, 1=Partner, 2=Enemy, 3=Boss, 4=Trainee
        Puppet3D p=new Puppet3D{role=role,appearance=appearance,active=true};
        p.root=new GameObject(name);
        p.root.transform.SetParent(actorsRoot,false);

        float scale=role==3?1.38f:role==2&&appearance==1?1.16f:role==4?0.92f:1.05f;
        p.root.transform.localScale=Vector3.one*scale;

        // Ground shadow disc
        Part(PrimitiveType.Cylinder,p.root.transform,new Vector3(0,0.02f,0),new Vector3(0.85f,0.01f,0.85f),matShadow,Vector3.zero,false);
        p.auraRing=Part(PrimitiveType.Cylinder,p.root.transform,new Vector3(0,0.035f,0),new Vector3(1.15f,0.015f,1.15f),role==0?matGold:role==1?matNeonCyan:matNeonRed,Vector3.zero,false).transform;
        p.auraRing.gameObject.SetActive(role<=1||role==3);

        p.hips=new GameObject("Hips").transform;
        p.hips.SetParent(p.root.transform,false);
        p.hips.localPosition=new Vector3(0,0.52f,0);

        Color shirtCol=role==0?new Color(.96f,.96f,.98f):role==1?new Color(.18f,.68f,.95f):role==4?new Color(.28f,.82f,.56f):role==3?new Color(.88f,.18f,.16f):appearance==0?new Color(.24f,.28f,.36f):appearance==1?new Color(.68f,.24f,.18f):new Color(.42f,.24f,.58f);
        Color pantsCol=role==0?new Color(.16f,.22f,.32f):role==1?new Color(.14f,.24f,.38f):new Color(.18f,.18f,.22f);
        Color gloveCol=role==0?new Color(1f,.26f,.16f):role==1?new Color(.18f,.78f,.98f):role==3?new Color(1f,.72f,.15f):new Color(.85f,.22f,.20f);

        // Legs
        p.legL=new GameObject("LegL").transform;p.legL.SetParent(p.hips,false);p.legL.localPosition=new Vector3(-0.16f,0,0);
        var legLM=Part(PrimitiveType.Capsule,p.legL,new Vector3(0,-0.22f,0),new Vector3(0.20f,0.24f,0.20f),Mat(pantsCol));
        p.pantsRenderer=legLM.GetComponent<Renderer>();

        p.legR=new GameObject("LegR").transform;p.legR.SetParent(p.hips,false);p.legR.localPosition=new Vector3(0.16f,0,0);
        Part(PrimitiveType.Capsule,p.legR,new Vector3(0,-0.22f,0),new Vector3(0.20f,0.24f,0.20f),Mat(pantsCol));

        // Torso
        p.torso=new GameObject("Torso").transform;p.torso.SetParent(p.hips,false);p.torso.localPosition=new Vector3(0,0.08f,0);
        var torsoObj=Part(PrimitiveType.Cube,p.torso,new Vector3(0,0.24f,0),new Vector3(0.52f,0.54f,0.34f),Mat(shirtCol,.45f));
        p.torsoRenderer=torsoObj.GetComponent<Renderer>();
        // Belt / Champion sash
        Part(PrimitiveType.Cube,p.torso,new Vector3(0,0.02f,0),new Vector3(0.55f,0.10f,0.36f),role==0||role==3?matGold:matDark);

        // Head
        p.head=new GameObject("Head").transform;p.head.SetParent(p.torso,false);p.head.localPosition=new Vector3(0,0.58f,0);
        var headObj=Part(PrimitiveType.Sphere,p.head,new Vector3(0,0.16f,0),new Vector3(0.46f,0.46f,0.46f),matSkin);
        p.headRenderer=headObj.GetComponent<Renderer>();
        // Hair / Headband
        Part(PrimitiveType.Sphere,p.head,new Vector3(0,0.26f,-0.02f),new Vector3(0.48f,0.34f,0.48f),matHair);
        if(role==0||role==3){
            Part(PrimitiveType.Cylinder,p.head,new Vector3(0,0.22f,0),new Vector3(0.49f,0.05f,0.49f),role==0?matNeonRed:matGold);
        }

        // Arms & Big Stylized Boxing Gloves (Supercent / Brawl Stars readable silhouettes)
        p.armL=new GameObject("ArmL").transform;p.armL.SetParent(p.torso,false);p.armL.localPosition=new Vector3(-0.34f,0.42f,0);
        Part(PrimitiveType.Capsule,p.armL,new Vector3(0,-0.12f,0.10f),new Vector3(0.16f,0.18f,0.16f),matSkin,new Vector3(45f,0,0));
        var gL=Part(PrimitiveType.Sphere,p.armL,new Vector3(0,-0.22f,0.28f),new Vector3(0.32f,0.30f,0.36f),Mat(gloveCol,.65f));
        p.gloveL=gL.transform;p.gloveLRenderer=gL.GetComponent<Renderer>();

        p.armR=new GameObject("ArmR").transform;p.armR.SetParent(p.torso,false);p.armR.localPosition=new Vector3(0.34f,0.42f,0);
        Part(PrimitiveType.Capsule,p.armR,new Vector3(0,-0.12f,0.10f),new Vector3(0.16f,0.18f,0.16f),matSkin,new Vector3(45f,0,0));
        var gR=Part(PrimitiveType.Sphere,p.armR,new Vector3(0,-0.22f,0.28f),new Vector3(0.32f,0.30f,0.36f),Mat(gloveCol,.65f));
        p.gloveR=gR.transform;p.gloveRRenderer=gR.GetComponent<Renderer>();

        return p;
    }

    void AnimatePuppet(Puppet3D p,Vector3 worldPos,Vector2 moveVel,Vector2 facingDir,float lift,bool attacking,int comboStep,float hitFlash,bool stunned,float dt){
        if(p==null||p.root==null)return;
        p.root.transform.position=worldPos+new Vector3(0,lift*0.03f,0);

        float speed=moveVel.magnitude;
        Vector2 lookDir=attacking||speed<8f?facingDir:moveVel.normalized;
        if(lookDir.sqrMagnitude>.001f){
            float targetYaw=Mathf.Atan2(lookDir.x,-lookDir.y)*Mathf.Rad2Deg;
            p.lastYaw=Mathf.LerpAngle(p.lastYaw,targetYaw,1f-Mathf.Exp(-18f*dt));
        }
        p.root.transform.rotation=Quaternion.Euler(0f,p.lastYaw,0f);

        if(speed>10f){
            p.walkCycle+=dt*(speed*0.085f);
            float swing=Mathf.Sin(p.walkCycle)*38f;
            float bounce=Mathf.Abs(Mathf.Cos(p.walkCycle))*0.09f;
            p.hips.localPosition=new Vector3(0,0.52f+bounce,0);
            p.legL.localRotation=Quaternion.Euler(swing,0,0);
            p.legR.localRotation=Quaternion.Euler(-swing,0,0);
            p.torso.localRotation=Quaternion.Euler(Mathf.Clamp(speed*0.06f,0,12f),Mathf.Sin(p.walkCycle)*6f,0);
        }else{
            p.walkCycle+=dt*3.2f;
            float breath=Mathf.Sin(p.walkCycle)*0.025f;
            p.hips.localPosition=new Vector3(0,0.52f+breath,0);
            p.legL.localRotation=Quaternion.Lerp(p.legL.localRotation,Quaternion.identity,1f-Mathf.Exp(-14f*dt));
            p.legR.localRotation=Quaternion.Lerp(p.legR.localRotation,Quaternion.identity,1f-Mathf.Exp(-14f*dt));
            p.torso.localRotation=Quaternion.Lerp(p.torso.localRotation,Quaternion.identity,1f-Mathf.Exp(-14f*dt));
        }

        if(attacking){
            p.punchTimer+=dt*18f;
            float thrust=Mathf.Sin(Mathf.Clamp01(p.punchTimer)*Mathf.PI);
            bool rightHand=(comboStep%2==0);
            if(comboStep==0){
                // Heavy 3rd hit spin/double smash
                p.torso.localRotation=Quaternion.Euler(16f,thrust*28f,0);
                p.armR.localPosition=new Vector3(0.34f,0.44f,thrust*0.58f);
                p.armL.localPosition=new Vector3(-0.34f,0.44f,thrust*0.42f);
                p.gloveR.localScale=Vector3.one*(0.34f+thrust*0.18f);
            }else if(rightHand){
                p.torso.localRotation=Quaternion.Euler(10f,-thrust*22f,0);
                p.armR.localPosition=new Vector3(0.34f,0.42f,thrust*0.52f);
                p.armL.localPosition=new Vector3(-0.34f,0.42f,0);
                p.gloveR.localScale=new Vector3(0.32f,0.30f,0.36f)*(1f+thrust*0.25f);
            }else{
                p.torso.localRotation=Quaternion.Euler(10f,thrust*22f,0);
                p.armL.localPosition=new Vector3(-0.34f,0.42f,thrust*0.52f);
                p.armR.localPosition=new Vector3(0.34f,0.42f,0);
                p.gloveL.localScale=new Vector3(0.32f,0.30f,0.36f)*(1f+thrust*0.25f);
            }
        }else{
            p.punchTimer=0;
            p.armL.localPosition=Vector3.Lerp(p.armL.localPosition,new Vector3(-0.34f,0.42f,0),1f-Mathf.Exp(-16f*dt));
            p.armR.localPosition=Vector3.Lerp(p.armR.localPosition,new Vector3(0.34f,0.42f,0),1f-Mathf.Exp(-16f*dt));
            p.gloveL.localScale=new Vector3(0.32f,0.30f,0.36f);
            p.gloveR.localScale=new Vector3(0.32f,0.30f,0.36f);
        }

        if(stunned||lift>1f){
            p.torso.localRotation=Quaternion.Euler(-28f,Mathf.Sin(Time.time*25f)*15f,0);
        }
    }

    public void SyncWorld(AlleyGame game,float dt){
        if(!initialized)Initialize(Camera.main);
        AlleyCombat c=game.combat;
        if(c==null)return;

        bool visible=!game.IsStoryActive();
        if(worldRoot.gameObject.activeSelf!=visible)worldRoot.gameObject.SetActive(visible);
        if(!visible)return;

        Rect arena=c.arena;
        bool inGym=game.IsInGym();
        propsRoot.gameObject.SetActive(inGym||game.IsHome());
        cashStackRoot.gameObject.SetActive(inGym||game.IsHome());
        gateRoot.gameObject.SetActive(inGym||game.IsHome());
        hazardsRoot.gameObject.SetActive(!inGym&&!game.IsHome());

        SyncEnvironmentTheme(inGym||game.IsHome(),game.CurrentStage);
        SyncHeroGearVisuals(game.CurrentGearTier);

        // Smooth 2.5D Isometric Follow Camera (Supercent style)
        Vector3 hero3D=MapTo3D(c.player,arena);
        Vector3 desiredCam=new Vector3(hero3D.x*0.42f,13.4f,hero3D.z*0.38f-9.2f);
        if(game.IsHome()){
            desiredCam=new Vector3(0f,12.5f,-8.6f);
        }
        camCurrentPos=Vector3.Lerp(camCurrentPos,desiredCam,1f-Mathf.Exp(-8f*dt));
        if(cam!=null){
            cam.transform.position=camCurrentPos;
            cam.transform.rotation=Quaternion.Euler(52f,0f,0f);
        }

        // Update Hero 3D Puppet
        AnimatePuppet(heroPuppet,hero3D,c.moveVelocity,c.direction,0f,c.punch>0||c.attackAnimation>.12f,c.comboStep,c.playerReaction,false,dt);
        if(heroPuppet.auraRing!=null){
            float auraScale=c.isFever?1.65f+Mathf.Sin(Time.time*14f)*0.15f:1.15f;
            heroPuppet.auraRing.localScale=new Vector3(auraScale,0.015f,auraScale);
        }

        // Update Partner 3D Puppet
        bool showPartner=c.hasPartner||game.gym.HasPartner;
        if(partnerPuppet.root.activeSelf!=showPartner)partnerPuppet.root.SetActive(showPartner);
        if(showPartner){
            Vector3 p3D=MapTo3D(c.partnerPos,arena);
            AnimatePuppet(partnerPuppet,p3D,c.partnerVelocity,c.partnerDirection,0f,c.partnerPunch>0,1,0f,false,dt);
        }

        if(inGym||game.IsHome()){
            SyncGym3D(game,arena,dt);
            foreach(var kv in enemyPuppets)if(kv.Value.root!=null)kv.Value.root.SetActive(false);
        }else{
            for(int i=0;i<traineePuppets.Count;i++)if(traineePuppets[i].root!=null)traineePuppets[i].root.SetActive(false);
            SyncCombatEnemies3D(c,arena,dt);
            SyncHazards3D(c,arena);
        }

        SyncCoins3D(c,arena,dt);
    }

    void SyncEnvironmentTheme(bool showGym,int stageIndex){
        int key=showGym?-1:Mathf.Clamp(stageIndex,0,3);
        if(key==lastThemeKey)return;
        lastThemeKey=key;
        if(plankGroup!=null)plankGroup.gameObject.SetActive(showGym);
        if(showGym){
            if(floorMainRenderer!=null)floorMainRenderer.sharedMaterial=matWoodFloor;
            if(wallLeftRenderer!=null)wallLeftRenderer.sharedMaterial=matBrick;
            if(wallRightRenderer!=null)wallRightRenderer.sharedMaterial=matBrick;
            if(neonLeftRenderer!=null)neonLeftRenderer.sharedMaterial=matNeonCyan;
            if(neonRightRenderer!=null)neonRightRenderer.sharedMaterial=matNeonRed;
            if(mainLight!=null)mainLight.color=new Color(1f,.96f,.88f);
            return;
        }
        if(key==0){
            // Stage 0: School Back Alley
            if(floorMainRenderer!=null)floorMainRenderer.sharedMaterial=Mat(new Color(.25f,.27f,.32f),.20f);
            if(wallLeftRenderer!=null)wallLeftRenderer.sharedMaterial=Mat(new Color(.48f,.23f,.20f),.15f);
            if(wallRightRenderer!=null)wallRightRenderer.sharedMaterial=Mat(new Color(.48f,.23f,.20f),.15f);
            if(neonLeftRenderer!=null)neonLeftRenderer.sharedMaterial=matNeonCyan;
            if(neonRightRenderer!=null)neonRightRenderer.sharedMaterial=matGold;
            if(mainLight!=null)mainLight.color=new Color(.95f,.93f,.90f);
        }else if(key==1){
            // Stage 1: Neon Night Market
            if(floorMainRenderer!=null)floorMainRenderer.sharedMaterial=Mat(new Color(.17f,.15f,.26f),.35f);
            if(wallLeftRenderer!=null)wallLeftRenderer.sharedMaterial=Mat(new Color(.22f,.17f,.34f),.25f);
            if(wallRightRenderer!=null)wallRightRenderer.sharedMaterial=Mat(new Color(.22f,.17f,.34f),.25f);
            if(neonLeftRenderer!=null)neonLeftRenderer.sharedMaterial=Mat(new Color(1f,.20f,.75f),.85f,.1f,true);
            if(neonRightRenderer!=null)neonRightRenderer.sharedMaterial=matNeonCyan;
            if(mainLight!=null)mainLight.color=new Color(.88f,.82f,1f);
        }else if(key==2){
            // Stage 2: Sunset Rooftop
            if(floorMainRenderer!=null)floorMainRenderer.sharedMaterial=Mat(new Color(.38f,.34f,.32f),.25f);
            if(wallLeftRenderer!=null)wallLeftRenderer.sharedMaterial=Mat(new Color(.30f,.27f,.26f),.20f);
            if(wallRightRenderer!=null)wallRightRenderer.sharedMaterial=Mat(new Color(.30f,.27f,.26f),.20f);
            if(neonLeftRenderer!=null)neonLeftRenderer.sharedMaterial=matGold;
            if(neonRightRenderer!=null)neonRightRenderer.sharedMaterial=matNeonRed;
            if(mainLight!=null)mainLight.color=new Color(1f,.78f,.58f);
        }else{
            // Stage 3: Harbor Logistics Warehouse
            if(floorMainRenderer!=null)floorMainRenderer.sharedMaterial=Mat(new Color(.18f,.22f,.27f),.42f,.25f);
            if(wallLeftRenderer!=null)wallLeftRenderer.sharedMaterial=Mat(new Color(.15f,.29f,.44f),.35f,.2f);
            if(wallRightRenderer!=null)wallRightRenderer.sharedMaterial=Mat(new Color(.42f,.20f,.16f),.35f,.2f);
            if(neonLeftRenderer!=null)neonLeftRenderer.sharedMaterial=matNeonRed;
            if(neonRightRenderer!=null)neonRightRenderer.sharedMaterial=matGold;
            if(mainLight!=null)mainLight.color=new Color(.92f,.95f,1f);
        }
    }

    void SyncHeroGearVisuals(int gearTier){
        if(heroPuppet==null||gearTier==lastGearTier)return;
        lastGearTier=gearTier;
        Color[] gloveColors={
            new Color(1f,.26f,.16f),
            new Color(.75f,.82f,.92f),
            new Color(1f,.58f,.12f),
            new Color(1f,.18f,.24f),
            new Color(.20f,.88f,1f),
            new Color(1f,.82f,.18f)
        };
        int t=Mathf.Clamp(gearTier,0,gloveColors.Length-1);
        Material gm=Mat(gloveColors[t],.75f,t>=1?.35f:.05f,t>=3);
        if(heroPuppet.gloveLRenderer!=null)heroPuppet.gloveLRenderer.sharedMaterial=gm;
        if(heroPuppet.gloveRRenderer!=null)heroPuppet.gloveRRenderer.sharedMaterial=gm;
    }

    void SyncHazards3D(AlleyCombat c,Rect arena){
        while(hazardModels.Count<c.hazards.Count){
            GameObject root=new GameObject("Hazard_"+hazardModels.Count);
            root.transform.SetParent(hazardsRoot,false);
            Part(PrimitiveType.Cylinder,root.transform,new Vector3(0,0.42f,0),new Vector3(0.68f,0.42f,0.68f),matNeonRed);
            Part(PrimitiveType.Cylinder,root.transform,new Vector3(0,0.42f,0),new Vector3(0.72f,0.10f,0.72f),matGold);
            hazardModels.Add(root);
        }
        for(int i=0;i<hazardModels.Count;i++){
            bool active=i<c.hazards.Count&&c.hazards[i].hp>0;
            if(hazardModels[i].activeSelf!=active)hazardModels[i].SetActive(active);
            if(!active)continue;
            AlleyHazard h=c.hazards[i];
            Vector3 pos=MapTo3D(h.pos,arena);
            hazardModels[i].transform.position=pos;
            hazardModels[i].transform.localEulerAngles=new Vector3(0,h.kind==1?Time.time*65f:0f,0);
        }
    }

    void SyncGym3D(AlleyGame game,Rect arena,float dt){
        GymWorld gym=game.gym;
        for(int i=0;i<stationModels.Count&&i<gym.stations.Count;i++){
            GymStation gs=gym.stations[i];
            Station3D sm=stationModels[i];
            Vector3 pos=MapTo3D(gym.StationPos(arena,gs),arena);
            sm.root.transform.position=pos;
            sm.equipmentRoot.SetActive(gs.unlocked);
            if(gs.unlocked){
                float pop=1f+Mathf.Sin(Mathf.Clamp01(gs.popScale)*Mathf.PI)*0.28f;
                sm.equipmentRoot.transform.localScale=Vector3.one*pop;
                if(sm.movingPart!=null){
                    if(i==0)sm.movingPart.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*9f)*10f,0,Mathf.Cos(Time.time*7f)*6f);
                    else if(i==3)sm.movingPart.localPosition=new Vector3(0,0.82f+Mathf.Sin(Time.time*6f)*0.12f,-0.35f);
                    else if(i==4)sm.movingPart.localPosition=new Vector3(0,1.15f+Mathf.Sin(Time.time*4f)*0.10f,0);
                }
            }
        }

        // Sync 3D Trainees
        while(traineePuppets.Count<gym.trainees.Count){
            traineePuppets.Add(CreatePuppet("Trainee_"+traineePuppets.Count,4,traineePuppets.Count%3));
        }
        for(int i=0;i<traineePuppets.Count;i++){
            bool active=i<gym.trainees.Count;
            if(traineePuppets[i].root.activeSelf!=active)traineePuppets[i].root.SetActive(active);
            if(!active)continue;
            GymTrainee gt=gym.trainees[i];
            GymStation gs=gym.stations[gt.stationId];
            Vector2 sPos=gym.StationPos(arena,gs)+new Vector2(gt.facingLeft?42f:-42f,12f);
            Vector3 t3D=MapTo3D(sPos,arena);
            Vector2 look=gt.facingLeft?Vector2.left:Vector2.right;
            AnimatePuppet(traineePuppets[i],t3D,Vector2.zero,look,0f,gt.punchClock<.24f,i%2+1,0f,false,dt);
        }

        // Sync 3D Green Cash Pile at Desk
        Vector3 desk3D=MapTo3D(gym.CashDeskPos(arena),arena);
        cashStackRoot.position=desk3D+new Vector3(1.15f,0,0);
        int billCount=Mathf.Clamp(gym.cashDeskPile/6,0,24);
        while(billModels.Count<billCount){
            GameObject bill=Part(PrimitiveType.Cube,cashStackRoot,Vector3.zero,new Vector3(0.38f,0.09f,0.22f),matCash,Vector3.zero,false);
            Part(PrimitiveType.Cube,bill.transform,Vector3.zero,new Vector3(0.32f,1.05f,0.35f),matGold,Vector3.zero,false);
            billModels.Add(bill);
        }
        for(int i=0;i<billModels.Count;i++){
            bool show=i<billCount;
            if(billModels[i].activeSelf!=show)billModels[i].SetActive(show);
            if(show){
                int col=i%2;int row=i/2;
                billModels[i].transform.localPosition=new Vector3(col*0.44f,0.05f+row*0.10f,0);
            }
        }
    }

    void SyncCombatEnemies3D(AlleyCombat c,Rect arena,float dt){
        HashSet<int> aliveIds=new HashSet<int>();
        foreach(Fighter e in c.enemies){
            if(e.hp<=0)continue;
            aliveIds.Add(e.id);
            Puppet3D p;
            if(!enemyPuppets.TryGetValue(e.id,out p)){
                p=CreatePuppet("Enemy_"+e.id,e.boss?3:2,e.appearance);
                enemyPuppets[e.id]=p;
            }
            if(!p.root.activeSelf)p.root.SetActive(true);
            Vector3 e3D=MapTo3D(e.pos,arena);
            Vector2 vel=e.velocity.sqrMagnitude>100f?e.velocity:e.locomotion;
            Vector2 face=e.windup>0||e.dash>0?e.aim:(c.player-e.pos).normalized;
            AnimatePuppet(p,e3D,vel,face,e.lift,e.windup>0||e.dash>0,1,e.flash,e.stun>0||e.reaction>0,dt);
        }
        foreach(var kv in enemyPuppets){
            if(!aliveIds.Contains(kv.Key)&&kv.Value.root!=null&&kv.Value.root.activeSelf){
                kv.Value.root.SetActive(false);
            }
        }
    }

    void SyncCoins3D(AlleyCombat c,Rect arena,float dt){
        while(coinModels.Count<c.coins.Count){
            GameObject coin=Part(PrimitiveType.Cylinder,coinsRoot,Vector3.zero,new Vector3(0.32f,0.05f,0.32f),matGold,new Vector3(90f,0,0),false);
            coinModels.Add(coin);
        }
        for(int i=0;i<coinModels.Count;i++){
            bool active=i<c.coins.Count;
            if(coinModels[i].activeSelf!=active)coinModels[i].SetActive(active);
            if(!active)continue;
            CoinDrop drop=c.coins[i];
            Vector3 pos=MapTo3D(drop.pos,arena)+new Vector3(0,0.22f+drop.height*0.015f,0);
            coinModels[i].transform.position=pos;
            coinModels[i].transform.localEulerAngles=new Vector3(90f,Time.time*260f+i*45f,0f);
        }
    }
}
