using System;
using ArknightsMod.Content.NPCs.Enemy.Evolution;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Evolution;

public sealed class EvolutionCompanionBuff : ModBuff
{
    public override string Texture => EvolutionVisuals.Root + "CompanionBuffIcon";
    public override void SetStaticDefaults() { Main.buffNoTimeDisplay[Type] = true; Main.buffNoSave[Type] = true; }
    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<EvolutionCompanion>()] + player.ownedProjectileCounts[ModContent.ProjectileType<EvolutionHeavyCompanion>()] > 0)
            player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}
public class EvolutionCompanion : ModProjectile
{
    protected virtual bool Heavy => false;
    public override string Texture => EvolutionVisuals.Root + (Heavy ? "Abomination" : "Excrescence");
    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = Heavy ? 19 : 3;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = Heavy ? 76 : 26; Projectile.height = Heavy ? 48 : 20;
        Projectile.friendly = true; Projectile.minion = true; Projectile.minionSlots = Heavy ? 2 : 1;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1; Projectile.netImportant = true;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = 2;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 24;
    }
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead) return;
        if (player.HasBuff<EvolutionCompanionBuff>()) Projectile.timeLeft = 2;
        Projectile.ai[1]++;
        NPC target = EvolutionWeaponShot.FindTarget(Projectile.Center, 1000, player);
        Vector2 point = target != null ? target.Center + new Vector2(Heavy ? -player.direction * 300 : MathF.Sin(Projectile.ai[1] * .07f + Projectile.identity) * 150, Heavy ? -160 : -75) :
            player.Center + new Vector2(-player.direction * (65 + Projectile.minionPos * 40), -70);
        Vector2 delta = point - Projectile.Center;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, delta.SafeNormalize(Vector2.UnitY) * Math.Min(16, delta.Length() * .07f), .14f);
        if (Projectile.Distance(player.Center) > 2000 && Projectile.owner == Main.myPlayer)
        { Projectile.Center = player.Center; Projectile.velocity = Vector2.Zero; Projectile.netUpdate = true; }
        int interval = Heavy ? 65 : 42;
        if (target != null && (int)Projectile.ai[1] % interval == 0 && Projectile.owner == Main.myPlayer)
        {
            Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, direction * (Heavy ? 1 : 12),
                ModContent.ProjectileType<EvolutionSummonShot>(), Projectile.damage, 2, Projectile.owner, Heavy ? 2 : 1);
        }
        Projectile.frame = (int)Projectile.ai[1] / 7 % Main.projFrames[Type];
        Projectile.spriteDirection = Projectile.velocity.X > 0 ? 1 : -1;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = EvolutionVisuals.Asset(Heavy ? "Abomination" : "Excrescence");
        Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
        EvolutionVisuals.Glow(Projectile.Center, EvolutionVisuals.Blood * .35f, new Vector2(Heavy ? 95 : 42));
        Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, 0, frame.Size() * .5f,
            Heavy ? .8f : 1.5f, Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        return false;
    }
}
public sealed class EvolutionHeavyCompanion : EvolutionCompanion { protected override bool Heavy => true; }
