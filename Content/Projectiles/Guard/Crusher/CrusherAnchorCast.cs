using System;
using Terraria.DataStructures;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

public sealed class CrusherAnchorCast : ModProjectile
{
    public override string Texture => CrusherRules.Art(CrusherKind.Ulpianus);
    private bool ended;
    private int hits;
    private int pulls,samples;
    private readonly Vector2[] history=new Vector2[18];
    private readonly bool[] feedbackHits=new bool[Main.maxNPCs];
    public override void OnSpawn(IEntitySource source)=>CrusherAnchorVisuals.Launch(Projectile);
    private readonly bool[] effectHits = new bool[Main.maxNPCs];
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 42; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = true; Projectile.ignoreWater = true; Projectile.timeLeft = 30;
        Projectile.penetrate = -1; Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        Player p = Main.player[Projectile.owner];
        if (!p.active || p.dead || p.HeldItem.ModItem is not UlpianusAnchor || p.selectedItem != (int)Projectile.ai[1]) { ended = true; Projectile.Kill(); return; }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if(samples>0&&Vector2.DistanceSquared(history[0],Projectile.Center)>650*650)samples=0;
        for(int i=Math.Min(samples,history.Length-1);i>0;i--)history[i]=history[i-1];
        history[0]=Projectile.Center;samples=Math.Min(samples+1,history.Length);
        if(Projectile.timeLeft%3==0)CrusherAnchorVisuals.Flight(Projectile);
        if(!Main.dedServ)Lighting.AddLight(Projectile.Center,CrusherRules.Tint(CrusherKind.Ulpianus).ToVector3()*.45f);
        if (Projectile.ai[0] == 1 && p.whoAmI == Main.myPlayer) {
            if (p.GetModPlayer<CrusherPlayer>().ActiveMode != 3) { ended = true; Projectile.Kill(); return; }
            foreach (NPC n in Main.ActiveNPCs) {
                if (!n.CanBeChasedBy(this)) continue;
                float collision = 0;
                if (Collision.CheckAABBvLineCollision(n.Hitbox.TopLeft(), n.Hitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity, 42, ref collision)) { Finish(); return; }
            }
        }
    }
    public override bool? CanHitNPC(NPC target) => Projectile.ai[0] == 1 || hits >= 2 ? false : null;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hits++;
        if (Projectile.ai[0] == 0) {
            CrusherCombatNet.Hit(Projectile, target);
            if(!feedbackHits[target.whoAmI]&&hits<=2){
                feedbackHits[target.whoAmI]=true;
                Feedback(target.Hitbox.ClosestPointInRect(Projectile.Center),hits==1?1.25f:.85f);
            }
        }
        else Finish();
    }
    internal void ApplyPull(NPC target)
    {
        if (Projectile.ai[0] != 0 || effectHits[target.whoAmI] || pulls >= 2) return;
        effectHits[target.whoAmI] = true; pulls++;
        if (!target.boss && target.realLife < 0 && target.knockBackResist > 0) {
            target.velocity += (Main.player[Projectile.owner].Center - target.Center).SafeNormalize(Vector2.Zero) * 10 * target.knockBackResist;
            target.netUpdate = true;
        }
    }
    public override bool OnTileCollide(Vector2 oldVelocity) { Finish(true); return false; }
    public override void OnKill(int timeLeft) { if (!ended) Finish(); }
    private void Feedback(Vector2 point,float strength)
    {
        if(Projectile.owner==Main.myPlayer)Projectile.NewProjectile(Projectile.GetSource_FromThis(),point,
            Projectile.velocity.SafeNormalize(Vector2.UnitX),ModContent.ProjectileType<CrusherAnchorFeedback>(),0,0,Projectile.owner,strength);
    }
    private void Finish(bool terrain=false)
    {
        if (ended) return; ended = true;
        if (Projectile.owner == Main.myPlayer && Projectile.ai[0] == 1) {
            Player p = Main.player[Projectile.owner];
            // Find an unoccupied nearby player rectangle; never teleport inside a wall.
            for (int row = 0; row < 7; row++) {
                Vector2 at = Projectile.Center - p.Size * .5f - new Vector2(0, row * 16);
                if (p.GetModPlayer<CrusherPlayer>().TryTeleport(at)) break;
            }
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<CrusherAnchorCrash>(), Projectile.damage, 12, Projectile.owner);
        }
        if(Projectile.ai[0]==0)Feedback(Projectile.Center,terrain?.95f:.45f);
        Projectile.Kill();
    }
    public override bool PreDraw(ref Color lightColor)
    {
        CrusherAnchorVisuals.DrawFlight(Projectile,history,samples);
        return false;
    }
}

public sealed class CrusherAnchorCrash : ModProjectile
{
    private readonly bool[] effectHits = new bool[Main.maxNPCs];
    public override string Texture => "Terraria/Images/Projectile_0";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 260; Projectile.friendly = true; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Melee; Projectile.timeLeft = 2; Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Vector2.Distance(targetHitbox.ClosestPointInRect(Projectile.Center), Projectile.Center) <= 130;
    public override void AI()
    {
        if (Projectile.localAI[0]++ == 0 && Main.myPlayer == Projectile.owner)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.UnitY, ModContent.ProjectileType<CrusherImpact>(), 0, 0, Projectile.owner, (int)CrusherKind.Ulpianus, 1.6f);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => CrusherCombatNet.Hit(Projectile, target);
    internal void ApplyStun(NPC target)
    {
        if (effectHits[target.whoAmI]) return;
        effectHits[target.whoAmI] = true;
        if (!target.boss && target.realLife < 0 && target.knockBackResist > 0) OperatorStunNPC.TryApply(target, 300);
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
