using UnityEngine;

// Presentation only: never feeds sprite size or pose back into collision/launch simulation.
public static class DadBattleViewRules
{
    public const float CharacterScale = 1.12f;
    public const float HudHeight = 64;
    public static float FlightTilt(Vector2 direction,bool facingLeft)
    {
        float angle=Mathf.Atan2(direction.y,Mathf.Abs(direction.x))*Mathf.Rad2Deg;
        return Mathf.Clamp(facingLeft?-angle:angle,-65,65);
    }
    public static bool FacingLeft(Vector2 direction,bool previous)
    {return Mathf.Abs(direction.x)<.18f?previous:direction.x<0;}
    public static Vector2 BodyCenter(Vector2 feet,float simulationScale)
    {return feet-new Vector2(0,75*simulationScale*CharacterScale);}
    public static float FallenOpacity(float life)
    {return .65f*Mathf.Clamp01((life-.25f)/.35f);}
    public static float PocketHeight(int rarity) {return rarity==2?100:84;}
}
