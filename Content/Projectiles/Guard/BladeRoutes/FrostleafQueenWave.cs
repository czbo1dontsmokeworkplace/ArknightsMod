using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Frostleaf;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Projectiles.Guard.BladeRoutes;

// Use the actual Ice Queen projectile (348), including native AI, alpha pulse and native drawing.
// Only explicitly bound player shots receive friendly flags; hostile Ice Queen attacks stay untouched.
public sealed class FrostleafQueenWave : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.type == ProjectileID.FrostWave;
    internal bool Bound;
    private readonly Vector2[] trail = new Vector2[8];
    private int age;

    internal static Projectile Spawn(Projectile source, Vector2 origin, Vector2 velocity, int damage)
    {
        var shot = Projectile.NewProjectileDirect(source.GetSource_FromAI(), origin, velocity,
            ProjectileID.FrostWave, damage, source.knockBack, source.owner);
        shot.GetGlobalProjectile<FrostleafQueenWave>().Bound = true;
        shot.CritChance = source.CritChance; shot.timeLeft = 360;
        Configure(shot); shot.netUpdate = true;
        return shot;
    }
    private static void Configure(Projectile projectile)
    {
        projectile.friendly = true; projectile.hostile = false; projectile.npcProj = false;
        projectile.DamageType = DamageClass.Melee; projectile.penetrate = -1;
        projectile.tileCollide = false; projectile.usesLocalNPCImmunity = true; projectile.localNPCHitCooldown = -1;
    }
    public override bool PreAI(Projectile projectile)
    {
        if (Bound) Configure(projectile);
        return true;
    }
    public override void PostAI(Projectile projectile)
    {
        if (!Bound || projectile.numUpdates != 0) return;
        age++;
        for (int i = trail.Length - 1; i > 0; i--) trail[i] = trail[i - 1];
        trail[0] = projectile.Center;
        if (!Main.dedServ && age % 3 == 0)
        {
            Dust dust = Dust.NewDustPerfect(projectile.Center - projectile.velocity.SafeNormalize(Vector2.UnitX) * 12,
                DustID.IceTorch, -projectile.velocity * .08f + Main.rand.NextVector2Circular(.5f, .5f), 130,
                new Color(124, 208, 244), .7f);
            dust.noGravity = true;
        }
    }
    public override bool CanHitPlayer(Projectile projectile, Player target) => !Bound;
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Bound && Main.player[projectile.owner].HeldItem.ModItem is FrostleafAxe)
            BladeRouteCombat.FrostleafHit(Main.player[projectile.owner], target);
    }
    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (!Bound) return true;
        Main.instance.LoadProjectile(ProjectileID.FrostWave);
        Texture2D texture = TextureAssets.Projectile[ProjectileID.FrostWave].Value;
        Color tint = projectile.GetAlpha(lightColor);
        for (int i = trail.Length - 1; i >= 2; i -= 2)
            if (trail[i] != Vector2.Zero)
                Main.EntitySpriteDraw(texture, trail[i] - Main.screenPosition, null, tint * ((1 - i / 8f) * .16f),
                    projectile.rotation, texture.Size() * .5f, projectile.scale * (1 - i * .025f), SpriteEffects.None);
        return true;
    }
    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer)
    {
        writer.Write(Bound);
        if (Bound) { writer.Write(projectile.CritChance); writer.Write(projectile.timeLeft); }
    }
    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
    {
        Bound = reader.ReadBoolean();
        if (Bound) { projectile.CritChance = reader.ReadInt32(); projectile.timeLeft = reader.ReadInt32(); Configure(projectile); }
    }
}
