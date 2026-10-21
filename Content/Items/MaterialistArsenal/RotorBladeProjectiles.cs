using System;
using System.IO;
using ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;
using ArknightsMod.Content.Projectiles.Caster.MechAccord;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.MaterialistArsenal;

public sealed class RotorBladeProjectile : ModProjectile
{
    private int targetType, visualSerial;
    private float flash;
    public override string LocalizationCategory => "Projectiles.MaterialistArsenal";
    public override string Texture => $"Terraria/Images/Item_{ItemID.LightDisc}";
    private bool Returning => Projectile.ai[0] > 0;
    private int Age => (int)Projectile.ai[1];
    private NPC Enemy => Projectile.ai[2] > 0 && Projectile.ai[2] <= Main.maxNPCs &&
        Main.npc[(int)Projectile.ai[2] - 1] is NPC npc && npc.type == targetType && npc.CanBeChasedBy() ? npc : null;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 10; ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 36; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee; Projectile.tileCollide = false;
        Projectile.ignoreWater = true; Projectile.penetrate = -1; Projectile.timeLeft = 300;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        // Remain airborne until caught, even when the owner can outrun the return speed.
        Projectile.timeLeft = 2;
        Projectile.ai[1]++;
        Projectile.rotation += .33f;
        bool authority = Projectile.owner == Main.myPlayer;
        if (authority && !Returning && (Age >= 50 || Projectile.Distance(owner.Center) > 760))
        { Projectile.ai[0] = 1; Projectile.netUpdate = true; }
        if (Returning)
        {
            Projectile.tileCollide = false;
            Vector2 delta = owner.MountedCenter - Projectile.Center;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                delta.SafeNormalize(Vector2.UnitX) * MathHelper.Clamp(12 + Age * .08f, 12, 26), .13f);
            if (authority && delta.LengthSquared() < 28 * 28)
            { Projectile.Kill(); return; }
        }
        else Projectile.velocity = Projectile.velocity.RotatedBy(.012f * owner.direction) * .993f;
        if (authority && (Age % MaterialistArsenalRules.RotorLaserInterval == 0 || Projectile.ai[2] > 0 && Enemy == null))
        {
            NPC enemy = MaterialistArsenalRules.Target(Projectile.Center, 700, owner, false,
                (int)Projectile.ai[2] - 1, ownerRange: float.PositiveInfinity, ignoreTiles: true);
            int selected = enemy?.whoAmI + 1 ?? 0;
            if (selected != (int)Projectile.ai[2] || enemy != null && enemy.type != targetType)
            { Projectile.ai[2] = selected; targetType = enemy?.type ?? 0; Projectile.netUpdate = true; }
        }
        if (authority && Age % MaterialistArsenalRules.RotorLaserInterval == 0 && Enemy is NPC target &&
            Projectile.Distance(target.Center) < 700)
        {
            int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                ModContent.ProjectileType<RotorTrackerLaser>(), (int)(Projectile.damage * .65f), Projectile.knockBack * .5f,
                Projectile.owner, Projectile.identity + 1, target.whoAmI + 1, target.type);
            if (index < Main.maxProjectiles && Main.projectile[index].ModProjectile is RotorTrackerLaser laser)
            { laser.Start = Projectile.Center; Main.projectile[index].netUpdate = true; }
            visualSerial++; flash = 1; Projectile.netUpdate = true;
        }
        flash *= .8f;
        if (!Main.dedServ)
        {
            Lighting.AddLight(Projectile.Center, .3f, .07f, .09f);
            if (Age % (MaterialistVisuals.Reduced ? 4 : 2) == 0)
            {
                // Opposed samples of a rotating lemniscate shed a compact, interlaced wake.
                float phase = Age * .16f + Projectile.identity * 2.399963f;
                float orientation = Projectile.velocity.ToRotation();
                int count = MaterialistVisuals.Reduced ? 1 : 2;
                for (int i = 0; i < count; i++)
                {
                    float angle = phase + i * MathHelper.Pi;
                    Vector2 offset = new Vector2(MathF.Cos(angle) * 21, MathF.Sin(angle * 2) * 11).RotatedBy(orientation);
                    Vector2 tangent = new Vector2(-MathF.Sin(angle) * 21, MathF.Cos(angle * 2) * 22).RotatedBy(orientation);
                    Color tint = Color.Lerp(new Color(255, 103, 135), new Color(184, 151, 255),
                        .5f + .5f * MathF.Sin(angle));
                    MaterialistVisuals.Nano(Projectile.Center + offset,
                        tangent * .035f - Projectile.velocity * .10f, tint,
                        .42f + .15f * (.5f + .5f * MathF.Cos(angle * 2)), 20);
                }
            }
        }
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(targetType); writer.Write(visualSerial); }
    public override void ReceiveExtraAI(BinaryReader reader)
    { targetType = reader.ReadInt32(); int serial = reader.ReadInt32(); if (serial != visualSerial) flash = 1; visualSerial = serial; }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Color tint = new(255, 91, 111);
        if (!MaterialistVisuals.Reduced)
            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
                if (Projectile.oldPos[i] != Vector2.Zero)
                    Main.spriteBatch.Draw(texture, Projectile.oldPos[i] + Projectile.Size * .5f - Main.screenPosition,
                        null, (tint with { A = 0 }) * ((1 - i / 10f) * .30f), Projectile.oldRot[i],
                        texture.Size() * .5f, 1.15f, SpriteEffects.None, 0);
        MaterialistTechRenderer.ChassisMaterial(texture, Projectile.Center, null, Projectile.rotation,
            new Vector2(1.15f), SpriteEffects.None, tint * .9f, 4);
        Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, .7f),
            Projectile.rotation, texture.Size() * .5f, 1.15f, SpriteEffects.None, 0);
        MaterialistVisuals.Glow(Projectile.Center, tint * (.25f + flash * .45f), new Vector2(42));
        return false;
    }
}

public sealed class RotorTrackerLaser : ModProjectile
{
    internal Vector2 Start;
    private Vector2 end;
    private bool sparked;
    public override string LocalizationCategory => "Projectiles.MaterialistArsenal";
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.timeLeft = 15;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(IEntitySource source)
    { Start = source is EntitySource_Parent { Entity: Projectile parent } ? parent.Center : Projectile.Center; end = Projectile.Center; }
    public override bool ShouldUpdatePosition() => false;
    private NPC Target => Projectile.ai[1] > 0 && Projectile.ai[1] <= Main.maxNPCs &&
        Main.npc[(int)Projectile.ai[1] - 1] is NPC npc && npc.active && npc.type == (int)Projectile.ai[2] ? npc : null;
    public override bool? CanDamage() => Projectile.numHits == 0 && Target != null ? null : false;
    public override bool? CanHitNPC(NPC target) => Target == target ? null : false;
    public override void AI()
    {
        if (Target is NPC target) end = target.Center;
        Projectile.Center = end;
        foreach (Projectile drone in Main.ActiveProjectiles)
            if (drone.owner == Projectile.owner && drone.identity + 1 == (int)Projectile.ai[0] && drone.ModProjectile is RotorBladeProjectile)
            { Start = drone.Center; break; }
        if (!Main.dedServ && !sparked)
        {
            sparked = true;
            SoundEngine.PlaySound(SoundID.Item33 with { Volume = .35f, Pitch = .25f, MaxInstances = 4 }, Start);
            for (int i = 0; i < 5; i++)
                MaterialistVisuals.Nano(end, Main.rand.NextVector2Circular(3, 3), new Color(255, 95, 115), .8f, 15);
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => targetHitbox.Contains(end.ToPoint());
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(Start.X); writer.Write(Start.Y); writer.Write(end.X); writer.Write(end.Y); }
    public override void ReceiveExtraAI(BinaryReader reader)
    { Start = new Vector2(reader.ReadSingle(), reader.ReadSingle()); end = new Vector2(reader.ReadSingle(), reader.ReadSingle()); }
    public override bool PreDraw(ref Color lightColor)
    {
        ClickTrackerLaser.DrawBeam(Start, end, MathF.Pow(Projectile.timeLeft / 15f, 3), 1, null, 1.25f);
        return false;
    }
}
