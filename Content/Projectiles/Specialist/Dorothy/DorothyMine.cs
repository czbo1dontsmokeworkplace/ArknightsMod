using System;
using ArknightsMod.Content.Buffs.ArmorSets;
using ArknightsMod.Content.Items.Armor.Specialist.Dorothy;
using ArknightsMod.Content.Items.Armor.Specialist.Mortis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Specialist.Dorothy;

public sealed class DorothyMine : ModProjectile
{
    public const int Lifetime = 20 * 60;
    internal bool Doubled => Projectile.ai[0] == 1f;
    public override string Texture => "ArknightsMod/Content/Projectiles/Specialist/Dorothy/DorothyMine";
    public override void SetDefaults() {
        Projectile.width = 40;
        Projectile.height = 32;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI() {
        Projectile.velocity = Vector2.Zero;
        Player owner = Main.player[Projectile.owner];
        if (!DorothySetPlayer.HasSet(owner)) { Projectile.Kill(); return; }
        Lighting.AddLight(Projectile.Center, Doubled ? new Vector3(1.2f, .03f, .02f) : new Vector3(.35f, .25f, .05f));
        if (Projectile.owner != Main.myPlayer || MortisSetPlayer.BlocksActions(owner)) return;
        foreach (NPC npc in Main.ActiveNPCs) {
            if (!Projectile.Hitbox.Intersects(npc.Hitbox) || !CanTrigger(owner, npc)) continue;
            int damage = Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Summon).ApplyTo(30f));
            if (Doubled) damage *= 2;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<DorothyMineExplosion>(), damage, 0, Projectile.owner, Doubled ? 1f : 0f);
            Projectile.Kill();
            break;
        }
    }
    private bool CanTrigger(Player owner, NPC npc) {
        if (npc.dontTakeDamage || npc.life <= 0) return false;
        if (owner.dontHurtCritters && NPCID.Sets.CountsAsCritter[npc.type]) return false;
        bool? permission = CombinedHooks.CanHitNPCWithProj(Projectile, npc);
        if (permission.HasValue) return permission.Value;
        if (npc.friendly && npc.catchItem <= 0
            && npc.type != NPCID.TargetDummy
            && !(npc.type == NPCID.Guide && owner.killGuide)
            && !(npc.type == NPCID.Clothier && owner.killClothier)) return false;
        return true;
    }
    public override bool PreDraw(ref Color lightColor) {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Vector2 position = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() * .5f;
        if (Doubled) {
            // Alpha-zero color adds a bright red halo without changing batch state.
            for (int ring = 1; ring <= 2; ring++) {
                Color glow = new Color(255, 15, 10, 0) * (ring == 1 ? .7f : .3f);
                for (int i = 0; i < 8; i++) {
                    Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * ring * 2f;
                    Main.EntitySpriteDraw(texture, position + offset, null, glow, 0,
                        origin, 1f, SpriteEffects.None, 0);
                }
            }
        }
        Main.EntitySpriteDraw(texture, position, null, lightColor, 0,
            origin, 1f, SpriteEffects.None, 0);
        return false;
    }
}

public sealed class DorothyMineExplosion : ModProjectile
{
    public const float Radius = 48f;
    private bool Doubled => Projectile.ai[0] == 1f;
    public override string Texture => "ArknightsMod/Assets/null";
    public override void SetDefaults() {
        Projectile.width = Projectile.height = (int)Radius * 2;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.timeLeft = 2;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.timeLeft == 2;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        targetHitbox.Distance(Projectile.Center) <= Radius;
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.DisableCrit();
    public override bool CanHitPvp(Player target) => false;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
        if (Doubled && !target.friendly) target.AddBuff(ModContent.BuffType<IndigoBindDebuff>(), 3 * 60);
    }
    public override void OnSpawn(Terraria.DataStructures.IEntitySource source) {
        if (Main.dedServ) return;
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = .5f }, Projectile.Center);
        Color color = Doubled ? new Color(255, 150, 150) : new Color(255, 215, 100);
        for (int i = 0; i < 32; i++) {
            Vector2 direction = (MathHelper.TwoPi * i / 32f).ToRotationVector2();
            Dust dust = Dust.NewDustPerfect(Projectile.Center + direction * Radius, DustID.Enchanted_Gold,
                direction * 2f, 80, color, 1.2f);
            dust.noGravity = true;
        }
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
