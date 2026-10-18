using System;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Projectiles.BasePROJ;
using ArknightsMod.Content.Projectiles.Guard.Blaze;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

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

    internal static void DrawOutline(Texture2D texture, Vector2 hand, float angle, float reach, float spool, int mode)
    {
        BaseHeldMeleeSupport.BeginAdditive(Main.spriteBatch);
        float pulse = 0.9f + MathF.Sin((float)Main.GlobalTimeWrappedHourly * 8f) * 0.1f;
        Color edge = (mode == 2 ? new Color(172, 228, 78, 170) : new Color(112, 172, 62, 140))
            * (spool * pulse * (mode == 2 ? 0.85f : 0.6f));
        for (int i = 0; i < 8; i++)
        {
            Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * (mode == 2 ? 2.8f : 1.8f);
            DrawWeapon(texture, hand + offset, angle, reach, edge);
        }
        BaseHeldMeleeSupport.EndAdditive(Main.spriteBatch);
    }

    internal static void DrawChain(Vector2 hand, Vector2 tip, int age, float spool, int mode)
    {
        Vector2 axis = tip - hand;
        Vector2 direction = axis.SafeNormalize(Vector2.UnitX);
        Vector2 normal = new(-direction.Y, direction.X);
        float angle = direction.ToRotation();
        float phase = age * (mode == 2 ? 0.18f : mode == 3 ? 0.15f : 0.085f);
        int teeth = mode == 2 ? 14 : 8;
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
                    i % 3 == 0 ? Steel : Olive, mode == 2 ? 16f : mode == 3 ? 12f : 8f,
                    mode == 2 ? 3.2f : 2.2f, spool * (mode == 2 ? 0.85f : 0.5f));
            }
        }
        // 加长模式表现为紧贴实体锯刃的机械切割划痕。
        if (mode == 2 || mode == 3)
            BaseHeldMeleeSupport.DrawSharpTear(Vector2.Lerp(hand, tip, 0.8f), angle,
                mode == 2 ? new Color(172, 228, 78) : Olive,
                mode == 2 ? 54f : 32f, mode == 2 ? 8f : 6f, (mode == 2 ? 0.48f : 0.28f) * spool);
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

    internal static void SawParticles(Vector2 hand, Vector2 tip, int age, float spool, int mode)
    {
        if (Main.dedServ || spool < 0.4f || age % (mode is 2 or 3 ? 3 : 4) != 0) return;
        Vector2 direction = (tip - hand).SafeNormalize(Vector2.UnitX);
        Vector2 normal = new(-direction.Y, direction.X);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 position = Vector2.Lerp(hand, tip, Main.rand.NextFloat(0.48f, 0.98f))
                + normal * side * 10f;
            // 两侧锯链反向流动，火花脱离锯身后自然卷曲散开。
            Vector2 velocity = direction * (side * Main.rand.NextFloat(2.4f, 4.2f))
                + normal * side * Main.rand.NextFloat(0.6f, 1.4f);
            Color color = side < 0 ? new Color(173, 205, 81) : new Color(225, 219, 144);
            new T_Gavial_Saw_Spark_Particle(position, velocity, color,
                Main.rand.NextFloat(11f, mode == 2 ? 22f : 17f), side * 0.075f,
                Main.rand.Next(16, 24)).Spawn();
        }
    }

    internal static void SawImpactParticles(Vector2 position, Vector2 direction, int mode)
    {
        if (Main.dedServ) return;
        Vector2 axis = direction.SafeNormalize(Vector2.UnitX);
        Vector2 normal = new(-axis.Y, axis.X);
        int count = mode == 2 ? 10 : 7;
        for (int i = 0; i < count; i++)
        {
            int side = (i & 1) == 0 ? 1 : -1;
            Vector2 velocity = (-axis * Main.rand.NextFloat(1.5f, 4f)
                + normal * side * Main.rand.NextFloat(2f, 6f)).RotatedByRandom(0.4f);
            Color color = i % 3 == 0 ? new Color(231, 232, 172) : new Color(137, 192, 62);
            new T_Gavial_Saw_Spark_Particle(position + Main.rand.NextVector2Circular(4f, 4f),
                velocity, color, Main.rand.NextFloat(14f, 24f), side * 0.09f,
                Main.rand.Next(18, 27)).Spawn();
        }
        if (Main.rand.NextBool(3))
            new T_SoftSmoke_Particle(position, -axis * 0.6f - Vector2.UnitY * 0.4f,
                new Color(77, 91, 56), 14f, 20).Spawn();
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
