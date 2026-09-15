using System;
using ArknightsMod.Content.Projectiles.BasePROJ;
using ArknightsMod.Content.Projectiles.Guard.Blaze;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.Guard.Gavial;

internal static class GavialVisuals
{
    internal static readonly Color Olive = new(92, 111, 54);
    private static readonly Color Steel = new(181, 183, 132);
    internal static SoundStyle MotorSound => BlazeVisuals.ButcherMotorSound;

    internal struct CutMark
    {
        internal Vector2 Position;
        internal float Angle, Scale;
        internal int Time;
    }

    internal static void DrawWeapon(Texture2D texture, Vector2 hand, float angle, float reach, Color light)
    {
        // 原版屠夫电锯暂代；贴图锚点集中在这里，日后换图不影响碰撞长度。
        // 已核对原版 68×26 横向贴图：握点约 (11,15)，锯尖约 (66,13)。
        var profile = new WeaponSpriteProfile(new Vector2(11f / 68f, 15f / 26f), new Vector2(66f / 68f, 13f / 26f),
            0f, reach, 42f);
        Vector2 axis = (profile.TipAnchor - profile.GripAnchor) * texture.Size();
        float scale = reach / Math.Max(1f, axis.Length());
        BaseHeldMeleeSupport.DrawHeld(texture, hand, angle, MathF.Cos(angle) < 0f,
            profile, scale, Color.Lerp(light, light.MultiplyRGBA(new Color(165, 179, 137)), 0.32f));
    }

    internal static void DrawChain(Vector2 hand, Vector2 tip, int age, float spool, int mode)
    {
        Vector2 axis = tip - hand;
        Vector2 direction = axis.SafeNormalize(Vector2.UnitX);
        Vector2 normal = new(-direction.Y, direction.X);
        float angle = direction.ToRotation();
        float phase = age * (mode == 3 ? 0.15f : 0.085f);
        int teeth = mode == 2 ? 11 : 8;
        // 短而分离的齿尖沿两侧反向运动；低亮度军绿金属边，不铺整条发光带。
        for (int i = 0; i < teeth; i++)
        {
            float u = (i + phase) / teeth;
            u -= MathF.Floor(u);
            for (int side = -1; side <= 1; side += 2)
            {
                float along = side < 0 ? 1f - u : u;
                Vector2 point = Vector2.Lerp(hand, tip, 0.3f + along * 0.67f) + normal * side * 9f;
                BaseHeldMeleeSupport.DrawSharpTear(point, angle + side * 0.3f,
                    i % 3 == 0 ? Steel : Olive, mode == 3 ? 12f : 8f, 2.2f, spool * 0.5f);
            }
        }
        // 加长模式表现为紧贴实体锯刃的机械切割划痕。
        if (mode == 2 || mode == 3)
            BaseHeldMeleeSupport.DrawSharpTear(Vector2.Lerp(hand, tip, 0.8f), angle,
                Olive, mode == 2 ? 44f : 32f, 6f, 0.28f * spool);
    }

    internal static void DrawCut(CutMark mark)
    {
        if (mark.Time <= 0) return;
        float fade = mark.Time / 12f;
        for (int i = -1; i <= 1; i++)
        {
            Vector2 offset = (mark.Angle + MathHelper.PiOver2).ToRotationVector2() * (i * 7f);
            BaseHeldMeleeSupport.DrawSharpTear(mark.Position + offset, mark.Angle,
                Olive, (48f - Math.Abs(i) * 9f) * mark.Scale, 5f * fade, fade * 0.85f);
        }
        BaseHeldMeleeSupport.DrawSharpTear(mark.Position, mark.Angle, Steel,
            32f * mark.Scale, 1.8f, fade * 0.65f);
    }

    internal static void Chips(Vector2 position, Vector2 direction, int count, bool impact)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = direction.RotatedByRandom(0.9f) * Main.rand.NextFloat(2f, impact ? 7f : 3.5f);
            var dust = Dust.NewDustPerfect(position, DustID.Iron, velocity, 70,
                i % 3 == 0 ? Steel : Olive, Main.rand.NextFloat(0.65f, 1.1f));
            dust.noLight = true;
            dust.noGravity = false;
        }
        if (impact)
        {
            var smoke = Dust.NewDustPerfect(position, DustID.Smoke, direction * 0.8f - Vector2.UnitY,
                150, new Color(69, 75, 55), 0.75f);
            smoke.noLight = true;
        }
    }

    internal static void Activate(Player player, int mode)
    {
        if (Main.dedServ) return;
        SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/SkillActive1") { Volume = 0.6f }, player.Center);
        SoundEngine.PlaySound(MotorSound with { Volume = 0.65f, Pitch = mode == 3 ? 0.35f : -0.28f }, player.Center);
        Chips(player.MountedCenter, new Vector2(player.direction, -0.2f), 14, true);
        Shake(player, 2.8f);
    }

    internal static void Shake(Player player, float strength)
    {
        if (!Main.dedServ && player.whoAmI == Main.myPlayer)
            player.GetModPlayer<BlazeImpactShakePlayer>().Add(4, strength);
    }
}
