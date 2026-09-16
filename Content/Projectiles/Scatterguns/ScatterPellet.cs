using System;
using ArknightsMod.Content.Projectiles.Phalanx;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Scatterguns;

public sealed class ScatterPellet : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private int Tier => (int)Projectile.ai[0];
    private int Mode => (int)Projectile.ai[1];
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 8;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Ranged;
        Projectile.timeLeft = 100; Projectile.extraUpdates = 1;
        Projectile.penetrate = 2; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        if (Projectile.localAI[0]++ == 0)
        {
            Projectile.penetrate = Tier == 2 ? -1 : 2;
            Projectile.ArmorPenetration = Tier == 1 ? (Mode == 1 ? 35 : 15) : 0;
        }
        Projectile.ai[2] += Projectile.velocity.Length();
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Tier == 2) Projectile.velocity.Y += .025f;
        if (Projectile.ai[2] > 820) Projectile.Kill();
        Lighting.AddLight(Projectile.Center, ScatterVisuals.ColorFor(Tier).ToVector3() * .18f);
        if (Tier == 2 && Projectile.numUpdates == 0)
        {
            Projectile.frame = ++Projectile.frameCounter / 3 % 20;
            ScatterVisuals.WaterTrail(Projectile);
        }
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // Shotguns reward being close without losing their identity to long-range sniping.
        float range = MathHelper.Clamp((Projectile.ai[2] - 120) / 480, 0, 1);
        modifiers.SourceDamage *= MathHelper.Lerp(Tier == 1 ? 1.5f : 1.2f, .65f, range);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        ScatterVisuals.Spray(Projectile.Center, -Projectile.velocity.SafeNormalize(Vector2.UnitY), Tier, Tier == 2 ? 12 : 7, 5);
        if (Tier == 2)
        {
            target.AddBuff(BuffID.Wet, 240);
            if (Mode > 0) target.AddBuff(ModContent.BuffType<HighPressureSoak>(), Mode == 2 ? 180 : 90);
            if (Mode == 2 && Projectile.owner == Main.myPlayer
                && Main.player[Projectile.owner].ownedProjectileCounts[ModContent.ProjectileType<ChalterWaterField>()] < 3)
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<ChalterWaterField>(), Math.Max(1, Projectile.damage / 5), 0,
                    Projectile.owner);
        }
        Projectile.damage = Math.Max(1, (int)(Projectile.damage * .8f));
    }
    public override void OnKill(int timeLeft) => ScatterVisuals.Spray(Projectile.Center,
        -Projectile.velocity.SafeNormalize(Vector2.UnitY), Tier, Tier == 2 ? 10 : 4, 3);
    public override bool PreDraw(ref Color lightColor)
    {
        if (Tier == 2)
        {
            DrawWater();
            return false;
        }
        Color color = ScatterVisuals.ColorFor(Tier); color.A = 0;
        Vector2 center = Projectile.Center - Main.screenPosition;
        // Short, texture-based afterimages; never connect unrelated history positions with a line.
        const float maxTrail = 48f;
        for (int i = 1; i < Math.Min(5, Projectile.oldPos.Length); i++)
        {
            Vector2 old = Projectile.oldPos[i];
            if (old == Vector2.Zero || !float.IsFinite(old.X) || !float.IsFinite(old.Y)) continue;
            Vector2 previousCenter = old + Projectile.Size / 2;
            if (Vector2.DistanceSquared(previousCenter, Projectile.Center) > maxTrail * maxTrail) continue;
            float fade = (1 - i / 5f) * .24f;
            PhalanxVisuals.Glow(previousCenter - Main.screenPosition,
                Tier == 2 ? new Vector2(12, 7) : new Vector2(6), color * fade, Projectile.rotation);
        }
        PhalanxVisuals.Glow(center, new Vector2(9, 5), color, Projectile.rotation);
        PhalanxVisuals.Glow(center, new Vector2(4), new Color(255, 255, 255, 0) * .7f);
        return false;
    }
    private void DrawWater()
    {
        Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Scatterguns/ChalterAquaBlast").Value;
        Rectangle frame = texture.Frame(1, 20, 0, Math.Clamp(Projectile.frame, 0, 19));
        // Bound each animated frame, not the full 2000px spritesheet.
        Vector2 scale = new Vector2(16, 48) / frame.Size();
        Vector2 center = Projectile.Center - Main.screenPosition;
        float rotation = Projectile.rotation + MathHelper.PiOver2;
        for (int i = 1; i < Math.Min(4, Projectile.oldPos.Length); i++)
        {
            Vector2 old = Projectile.oldPos[i];
            if (old == Vector2.Zero || !float.IsFinite(old.X) || !float.IsFinite(old.Y)) continue;
            if (Vector2.DistanceSquared(old, Projectile.position) > 48 * 48) continue;
            Main.EntitySpriteDraw(texture, old + Projectile.Size / 2 - Main.screenPosition, frame,
                Color.White * ((1 - i / 4f) * .18f), rotation, frame.Size() * .5f, scale, SpriteEffects.None);
        }
        Main.EntitySpriteDraw(texture, center, frame, Color.White * .9f, rotation,
            frame.Size() * .5f, scale, SpriteEffects.None);
    }

}

public sealed class HighPressureSoak : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Wet;
    public override void SetStaticDefaults() { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; }
    public override void Update(NPC npc, ref int buffIndex)
    {
        if (!npc.boss && npc.realLife < 0 && npc.knockBackResist > 0) npc.velocity *= .94f;
    }
}

public sealed class ChalterWaterField : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = 160; Projectile.height = 80; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged; Projectile.timeLeft = 180;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        // All fields share the same hit cooldown, preventing three overlapping fields from triple-ticking.
        Projectile.usesIDStaticNPCImmunity = true; Projectile.idStaticNPCHitCooldown = 30;
    }
    public override void AI()
    {
        Projectile.ai[0]++;
        if (!Main.dedServ && Main.rand.NextBool(3))
            ScatterVisuals.Spray(Projectile.Center + Main.rand.NextVector2Circular(65, 28), -Vector2.UnitY, 2, 2, 3);
    }
    public override bool? CanHitNPC(NPC target) => Collision.CanHitLine(Projectile.Center, 1, 1, target.Center, 1, 1) ? null : false;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    { target.AddBuff(ModContent.BuffType<HighPressureSoak>(), 90); target.AddBuff(BuffID.Wet, 180); }
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = MathHelper.Clamp(Math.Min(Projectile.ai[0], Projectile.timeLeft) / 20f, 0, 1);
        Color color = ScatterVisuals.ColorFor(2); color.A = 0;
        Vector2 center = Projectile.Center - Main.screenPosition;
        PhalanxVisuals.Glow(center, new Vector2(160, 72), color * fade * .22f);
        for (int j = 0; j < 2; j++)
        {
            float phase = (Projectile.ai[0] / 60f + j * .5f) % 1f;
            PhalanxVisuals.Shockwave(center, new Vector2(160, 72) * (.5f + phase * .5f),
                color * fade * (1 - phase) * .28f);
        }
        return false;
    }
}
