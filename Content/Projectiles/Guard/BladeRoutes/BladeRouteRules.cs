using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Guard.BladeRoutes;

internal static class BladeRouteRules
{
    internal static int VanillaItem(int kind) => kind switch
    {
        0 => ItemID.Katana, 1 => ItemID.BluePhaseblade, 2 => ItemID.Cutlass,
        3 => ItemID.IronBroadsword, 4 => ItemID.BladeofGrass, _ => ItemID.NightsEdge
    };
    internal static string WeaponTexture(int kind) => kind switch
    {
        6 => "ArknightsMod/Content/Items/Weapons/Guard/Frostleaf/FrostleafAxe",
        7 => "ArknightsMod/Content/Items/Weapons/Guard/SilverAsh/SilverAshWeapon",
        _ => "Terraria/Images/Item_" + VanillaItem(kind)
    };
    internal static int HeldKind(Item item)=>item.ModItem?.Name switch
    {
        "CutterBlades"=>0,"BibeakSword"=>1,"IreneSaber"=>2,"MidnightSword"=>3,
        "AreneSword"=>4,"LapplandSwords"=>5,"FrostleafAxe"=>6,"SilverAshWeapon"=>7,_=>-1
    };
    internal static int UseTicks(int kind) => kind switch { 0 => 28, 1 => 26, 2 => 24, 3 => 34, 4 => 30, 6=>31,7=>39,_ => 28 };
    internal static float Length(int kind) => kind switch { 0 => 54, 1 => 62, 2 => 72, 3 => 54, 4 => 62,6=>68,7=>76,_ => 70 };
    internal static Color Theme(int kind) => kind switch
    {
        0 => new(255, 125, 56), 1 => new(102, 155, 255), 2 => new(255, 205, 104),
        3 => new(176, 103, 255), 4 => new(126, 235, 151),6=>new(105,218,255),7=>new(210,232,255),8=>new(220,65,80),_ => new(198, 211, 246)
    };
    // Excalibur-family half-turn and expanding close-range aura. The 1.2 / 1.0
    // scale terms are the True Excalibur variant, not the smaller Excalibur.
    internal static float Angle(float progress, int direction, float gravity) =>
        MathHelper.Pi * direction * gravity * progress + (direction < 0 ? MathHelper.Pi : 0) + MathHelper.Pi * direction * gravity;
    internal static float AuraScale(float progress, float itemScale) => (1.2f + progress) * itemScale * .66f;
    internal const int WaveLifetime = 120;
    internal const float WaveSpeed = 12f;
    internal const int LordShotInterval=60;
    internal const int SpikeLifetime=36;
    internal const int BoltDelay = 12;
    internal const int BoltLifetime = 90;
}
