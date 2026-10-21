using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed class TideSpine : ModProjectile
{
    public override string Texture => Isharmla.Root + "Head";
    public override void SetDefaults()
    {
        Projectile.CloneDefaults(ProjectileID.Sunfury); AIType = ProjectileID.Sunfury;
        Projectile.width = 52; Projectile.height = 46; Projectile.DamageType = DamageClass.Melee;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 16;
    }
    public override void PostAI()
    {
        if (!Main.dedServ && Projectile.velocity.LengthSquared() > 25 && Main.GameUpdateCount % 4 == 0)
            TidalParticles.Emit(TideParticle.Wake, Projectile.Center, -Projectile.velocity * .08f, new Vector2(19, 5),
                IsharmlaVisuals.Cyan, 28, Projectile.velocity.ToRotation());
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    { if (!Main.dedServ) TidalParticles.Burst(Projectile.Center, 14); }
    public override bool PreDraw(ref Color lightColor)
    {
        Player owner = Main.player[Projectile.owner]; Vector2 delta = owner.MountedCenter - Projectile.Center;
        float length = delta.Length(); Vector2 direction = delta.SafeNormalize(Vector2.UnitX);
        Texture2D body = IsharmlaVisuals.Art("Body"), head = IsharmlaVisuals.Art("Head");
        for (float d = 38; d < length; d += 24)
            Main.spriteBatch.Draw(body, Projectile.Center + direction * d - Main.screenPosition, null, lightColor,
                direction.ToRotation() - IsharmlaRules.BodyArtAngle, new Vector2(154, 155), .12f, SpriteEffects.None, 0);
        Main.spriteBatch.Draw(head, Projectile.Center - Main.screenPosition, null, Color.White,
            (-direction).ToRotation() - IsharmlaRules.HeadArtAngle, new Vector2(220, 220), .19f, SpriteEffects.None, 0);
        if (length > 60) IsharmlaVisuals.DrawWaterMaterial(owner.MountedCenter, Projectile.Center, 15, IsharmlaVisuals.Cyan * .25f);
        return false;
    }
}

public sealed class TideBolt : ModProjectile
{
    private Vector2 origin, launch;
    private int age;
    public override string Texture => "ArknightsMod/Assets/null";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20; Projectile.friendly = true; Projectile.penetrate = 4;
        Projectile.tileCollide = true; Projectile.ignoreWater = true; Projectile.timeLeft = 110;
        Projectile.DamageType = DamageClass.Ranged; Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 18;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (age == 0) { origin = Projectile.Center; launch = Projectile.velocity; }
        age++;
        Projectile.DamageType = Projectile.ai[0] == 1 ? DamageClass.Magic : Projectile.ai[0] == 2 ? DamageClass.Summon : DamageClass.Ranged;
        Vector2 axis = launch.SafeNormalize(Vector2.UnitX), perpendicular = axis.RotatedBy(MathHelper.PiOver2);
        float spiral = Projectile.ai[0] == 1 ? MathF.Sin(age * .23f + Projectile.ai[1]) * Math.Min(35, age * 2) : 0;
        Projectile.Center = origin + launch * age + perpendicular * spiral;
        Projectile.rotation = axis.ToRotation();
        if (!Main.dedServ && age % 3 == 0)
            TidalParticles.Emit(Projectile.ai[0] == 1 ? TideParticle.Prism : TideParticle.Droplet,
                Projectile.Center, -axis * .8f, new Vector2(Projectile.ai[0] == 0 ? 5 : 3), IsharmlaVisuals.Cyan, 30);
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(age); writer.WriteVector2(origin); writer.WriteVector2(launch); }
    public override void ReceiveExtraAI(BinaryReader reader) { age = reader.ReadInt32(); origin = reader.ReadVector2(); launch = reader.ReadVector2(); }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
            Projectile.Center - launch.SafeNormalize(Vector2.UnitX) * (Projectile.ai[0] == 0 ? 90 : 28), Projectile.ai[0] == 0 ? 22 : 14, ref point);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 tail = Projectile.Center - launch.SafeNormalize(Vector2.UnitX) * (Projectile.ai[0] == 0 ? 100 : 38);
        IsharmlaVisuals.Line(tail, Projectile.Center, IsharmlaVisuals.Cyan * .65f, Projectile.ai[0] == 0 ? 17 : 9);
        IsharmlaVisuals.DrawWaterMaterial(tail, Projectile.Center, Projectile.ai[0] == 0 ? 34 : 20, IsharmlaVisuals.Cyan * .8f);
        IsharmlaVisuals.Orb(Projectile.Center, new Vector2(11), Color.White * .75f); return false;
    }
    public override void OnKill(int timeLeft) { if (!Main.dedServ) TidalParticles.Burst(Projectile.Center, Projectile.ai[0] == 0 ? 14 : 6); }
}

public sealed class TidebornBuff : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.BabySlime;
    public override void SetStaticDefaults() { Main.buffNoSave[Type] = true; Main.buffNoTimeDisplay[Type] = true; }
    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<TidebornMinion>()] > 0) player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}

public sealed class TidebornMinion : ModProjectile
{
    public override string Texture => Isharmla.Root + "SeabornAtlas";
    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true; ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true; ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = 40; Projectile.height = 48; Projectile.friendly = true; Projectile.minion = true;
        Projectile.minionSlots = 1; Projectile.DamageType = DamageClass.Summon; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.timeLeft = 18000;
    }
    public override bool MinionContactDamage() => false;
    public override void AI()
    {
        Player p = Main.player[Projectile.owner];
        if (!p.active || p.dead) { p.ClearBuff(ModContent.BuffType<TidebornBuff>()); Projectile.Kill(); return; }
        if (p.HasBuff(ModContent.BuffType<TidebornBuff>())) Projectile.timeLeft = 2;
        Projectile.ai[0]++;
        NPC target = null;
        if (p.HasMinionAttackTargetNPC)
        {
            NPC forced = Main.npc[p.MinionAttackTargetNPC];
            if (forced.CanBeChasedBy() && Vector2.DistanceSquared(forced.Center, p.Center) < 1800 * 1800) target = forced;
        }
        float best = 1100 * 1100;
        if (target == null) foreach (NPC n in Main.ActiveNPCs)
        {
            float distance = Vector2.DistanceSquared(n.Center, Projectile.Center);
            if (n.CanBeChasedBy() && distance < best) { target = n; best = distance; }
        }
        Vector2 destination = target?.Center + new Vector2(MathF.Sin(Projectile.identity) * 230, -190) ??
            p.Center + new Vector2(-p.direction * (70 + Projectile.minionPos * 45), -110 + MathF.Sin(Projectile.ai[0] * .035f + Projectile.identity) * 20);
        Vector2 desired = (destination - Projectile.Center) * .04f;
        if (desired.Length() > 17) desired = desired.SafeNormalize(Vector2.UnitY) * 17;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, .08f);
        if (Vector2.DistanceSquared(Projectile.Center, p.Center) > 2200 * 2200 && Projectile.owner == Main.myPlayer)
        { Projectile.Center = p.Center; Projectile.netUpdate = true; }
        if (target != null && Projectile.ai[0] % 42 == 0 && Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 16,
                ModContent.ProjectileType<TideBolt>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 2);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Rectangle frame = IsharmlaSeaborn.Sprite;
        Main.spriteBatch.Draw(IsharmlaVisuals.Art("SeabornAtlas"), Projectile.Center - Main.screenPosition, frame, Color.White,
            Projectile.velocity.X * .02f, frame.Size() * .5f, .15f, SpriteEffects.None, 0); return false;
    }
}
