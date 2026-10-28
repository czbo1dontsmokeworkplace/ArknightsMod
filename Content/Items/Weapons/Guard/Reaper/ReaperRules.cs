using Microsoft.Xna.Framework;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Guard.Reaper;

public enum ReaperKind { Humus, LaPluma, Entelechia }

internal static class ReaperRules
{
    internal const float SwingTicks = 54 * .8f;
    internal const float WaveSpeed = 17.28f; // Current launch speed reduced by 20%; later motion is unchanged.
    internal const float LeftScale = 1f;
    internal const float SpinStep = MathHelper.TwoPi * 2 / 50 * 1.3f;
    internal const int WispInterval = 8;
    // Deterministic retained emission per ten original pair opportunities.
    internal static int WispEmissionTenths(ReaperKind kind) => kind switch {
        ReaperKind.Humus => 3, ReaperKind.LaPluma => 7, _ => 9 };
    internal const int WispLimit = 12;
    internal const float WispDamage = .2f;
    // Previous |tangent * 6 + radial * 3|, multiplied by the requested 1.3.
    internal const float WispLaunchSpeed = 8.720665f;
    internal const float WispSeekSpeed = 18f;
    internal static int HealCap(ReaperKind kind) => 2 + (int)kind;
    // Source-size geometry keeps art and melee contact on the same native blade.
    private static Vector2 SpriteSize(ReaperKind kind) => kind switch {
        ReaperKind.Humus => new(40,40), ReaperKind.LaPluma => new(76,76), _ => new(80,84) };
    internal static Vector2 Grip(ReaperKind kind) => kind==ReaperKind.LaPluma?new(7,69):SpriteSize(kind)*new Vector2(.12f,.88f);
    internal static Vector2 Head(ReaperKind kind) => kind==ReaperKind.LaPluma?new(64,40):SpriteSize(kind)*new Vector2(.73f,.22f);
    internal static float Reach(ReaperKind kind) => Vector2.Distance(Grip(kind),Head(kind));
    internal static int Placeholder(ReaperKind kind) => kind switch { ReaperKind.Humus => ItemID.Sickle, ReaperKind.LaPluma => ItemID.IceSickle, _ => ItemID.DeathSickle };
    internal static string Art(ReaperKind kind) => kind switch {
        ReaperKind.LaPluma => "ArknightsMod/Content/Items/Weapons/Guard/Reaper/LaPlumaScythe",
        ReaperKind.Entelechia => "ArknightsMod/Content/Items/Weapons/Guard/Entelechia/EntelechiaScythe",
        _ => "Terraria/Images/Item_" + Placeholder(kind) };
    internal static Color Color(ReaperKind kind) => kind switch {
        ReaperKind.Humus => new(179, 153, 69), ReaperKind.LaPluma => new(89, 196, 205),
        ReaperKind.Entelechia => new(199, 43, 100), _ => new(218, 211, 160) };
    internal static Color Edge(ReaperKind kind) => kind switch {
        ReaperKind.Humus => new(255, 227, 144), ReaperKind.LaPluma => new(205, 250, 255),
        ReaperKind.Entelechia => new(255, 179, 208), _ => new(255, 251, 222) };
}
