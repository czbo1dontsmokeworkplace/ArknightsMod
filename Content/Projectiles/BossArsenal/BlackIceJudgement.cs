using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Charged;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

public sealed class BlackIceJudgement : ModProjectile
{
    private const string Art = "ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/BlackIce";
    private int Age => (int)Projectile.ai[1];
    private bool struck;
    private Vector2 groundPoint;
    public override string Texture => Art;
    public override void SetStaticDefaults() { ProjectileID.Sets.TrailCacheLength[Type] = 12; ProjectileID.Sets.TrailingMode[Type] = 2; }
    public override void SetDefaults() {
        Projectile.width = Projectile.height = 26; Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 150; Projectile.netImportant = true;
    }
    public override bool? CanDamage() => Age < 42 ? false : null;
    public override void AI() {
        NPC target = Main.npc.IndexInRange((int)Projectile.ai[0]) ? Main.npc[(int)Projectile.ai[0]] : null;
        bool valid = target != null && target.CanBeChasedBy(Projectile);
        Projectile.ai[1]++;
        if (Age < 42) {
            if (!valid) { Projectile.Kill(); return; }
            // Target-relative placement intentionally follows even very fast movement before launch.
            float lane = Projectile.ai[2];
            float spread = Math.Clamp(target.width * .3f, 8, 28);
            Vector2 offset = lane == 0 ? Vector2.Zero : new Vector2((lane - 2) * spread, -(lane - 1) * 36);
            Projectile.Center = target.Top + offset - new Vector2(0, 160 + 30 * MathHelper.SmoothStep(0, 1, Age / 42f));
            Projectile.velocity = Vector2.Zero;
            Projectile.Opacity = Math.Min(1, Age / 10f);
            Projectile.rotation = -MathHelper.PiOver4;
        }
        else if (Age == 42) {
            if (!valid) { Projectile.Kill(); return; }
            Projectile.velocity = Vector2.UnitY * 38;
            Projectile.netUpdate = Projectile.owner == Main.myPlayer;
        }
        else Projectile.velocity *= Projectile.velocity.Length() < 62 ? 1.07f : 1;
        if (Age >= 42) Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver4 - MathHelper.PiOver2;
        if (!Main.dedServ && Age % 2 == 0) {
            for (int side = -1; side <= 1; side += 2) {
                Vector2 at = Projectile.Center + new Vector2(side * (8 + 3 * MathF.Sin(Age * .3f)), 0);
                Dust dust = Dust.NewDustPerfect(at, side < 0 ? DustID.Granite : DustID.PortalBolt,
                    Age < 42 ? new Vector2(side * .25f, -.8f) : -Projectile.velocity * .08f, 20,
                    side < 0 ? Color.Black : Color.White, .9f); dust.noGravity = true;
            }
        }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
        struck = true; groundPoint = target.Bottom;
        target.AddBuff(ModContent.BuffType<BlackIceFrost>(), 60);
    }
    public override void OnKill(int timeLeft) {
        if (Age < 42) return;
        if (!Main.dedServ) {
            SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNIceRingStg1") with { Volume = .7f }, Projectile.Center);
            for (int i = 0; i < 26; i++) {
                var dust = Dust.NewDustPerfect(Projectile.Center, i % 2 == 0 ? DustID.Granite : DustID.PortalBolt,
                    Main.rand.NextVector2Circular(7, 7), 0, i % 2 == 0 ? Color.Black : Color.White, 1.2f); dust.noGravity = true;
            }
        }
        if (Projectile.owner != Main.myPlayer) return;
        Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
            ModContent.ProjectileType<BlackIceFracture>(), Projectile.damage / 2, Projectile.knockBack, Projectile.owner);
        if (struck) BlackIceStain.Spawn(Projectile, groundPoint);
    }
    public override bool PreDraw(ref Color lightColor) {
        Texture2D body = ModContent.Request<Texture2D>(Art).Value;
        if (Age >= 42) for (int i = 2; i < Projectile.oldPos.Length; i += 3)
            if (Projectile.oldPos[i] != Vector2.Zero)
                Main.EntitySpriteDraw(body, Projectile.oldPos[i] + Projectile.Size * .5f - Main.screenPosition, null,
                    Color.White * (.45f * (1 - i / (float)Projectile.oldPos.Length)), Projectile.rotation, body.Size() * .5f, 1.8f, SpriteEffects.None);
        Main.EntitySpriteDraw(body, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity,
            Projectile.rotation, body.Size() * .5f, 1.8f, SpriteEffects.None);
        using var pass = new ChargedOperatorVfx.Pass(true);
        // White hairline cracks split away from the original black crystal.
        float charge = Math.Min(1, Age / 42f);
        for (int i = 0; i < 4; i++) {
            Vector2 offset = new((i % 2 * 2 - 1) * (8 + charge * 5), (i / 2 * 2 - 1) * 8);
            Vector2 at = Projectile.Center + offset;
            ChargedOperatorVfx.Line(at, at + new Vector2(3, -7), Color.White * (.35f * Projectile.Opacity), 1);
        }
        return false;
    }
}

public sealed class BlackIceFracture : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/NPCs/Enemy/Chapter6/FrostNova/BlackIceExplode";
    public override void SetStaticDefaults() => Main.projFrames[Type] = 6;
    public override void SetDefaults() {
        Projectile.width = Projectile.height = 112; Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false; Projectile.penetrate = -1; Projectile.timeLeft = 30;
        Projectile.usesIDStaticNPCImmunity = true; Projectile.idStaticNPCHitCooldown = 30;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.timeLeft >= 28 ? null : false;
    public override void AI() { Projectile.frame = Math.Min(5, (30 - Projectile.timeLeft) / 5); }
    public override bool PreDraw(ref Color lightColor) {
        Texture2D body = ModContent.Request<Texture2D>(Texture).Value;
        Rectangle frame = body.Frame(1, 6, 0, Projectile.frame);
        Main.EntitySpriteDraw(body, Projectile.Center - Main.screenPosition, frame, Color.White,
            0, frame.Size() * .5f, 1.65f, SpriteEffects.None);
        // Parallel fracture flashes share the same texture and deal no extra damage.
        for (int side = -1; side <= 1; side += 2)
            Main.EntitySpriteDraw(body, Projectile.Center + new Vector2(side * 36, -8) - Main.screenPosition, frame,
                Color.White * .4f, side * .12f, frame.Size() * .5f, 1.1f, SpriteEffects.None);
        return false;
    }
}
