using System;
using ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.MaterialistArsenal;

// One cast owns its captured cursor position, ten-tick charge and three owner-spawned beams.
public sealed class OrbitalBurstMarker : ModProjectile
{
    public override string LocalizationCategory => "Projectiles.MaterialistArsenal";
    public override string Texture => "Terraria/Images/MagicPixel";
    private int Age => (int)Projectile.ai[0];
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4; Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.timeLeft = MaterialistArsenalRules.OrbitalWarning +
            (MaterialistArsenalRules.OrbitalBurstCount - 1) * MaterialistArsenalRules.OrbitalBurstSpacing + 12;
        Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        Projectile.ai[0]++;
        int index = (int)Projectile.ai[1];
        int fireAt = MaterialistArsenalRules.OrbitalWarning + index * MaterialistArsenalRules.OrbitalBurstSpacing;
        if (Projectile.owner == Main.myPlayer && index < MaterialistArsenalRules.OrbitalBurstCount && Age >= fireAt)
        {
            Vector2 mark = Projectile.Center + new Vector2(Main.rand.Next(-32, 33), 0);
            mark.X = MathHelper.Clamp(mark.X, 32, Main.maxTilesX * 16 - 32);
            Vector2 origin = new(mark.X, MathHelper.Clamp(mark.Y - 850, 32, Main.maxTilesY * 16 - 48));
            while (origin.Y < mark.Y && Collision.SolidCollision(origin - new Vector2(2), 4, 4)) origin.Y += 16;
            if (!Collision.SolidCollision(origin - new Vector2(2), 4, 4))
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, Vector2.Zero,
                    ModContent.ProjectileType<OrbitalPurpleLaser>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            Projectile.ai[1] = index + 1;
            Projectile.netUpdate = true;
        }
        if (Main.dedServ) return;
        if (Age == 1)
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = .32f, Pitch = .3f, MaxInstances = 3 }, Projectile.Center);
        if (Age <= MaterialistArsenalRules.OrbitalWarning)
        {
            float charge = Age / (float)MaterialistArsenalRules.OrbitalWarning;
            int count = MaterialistVisuals.Reduced ? 2 : 5;
            for (int i = 0; i < count; i++)
            {
                int side = (Age + i) % 4;
                float along = Main.rand.NextFloat(-1, 1), radius = 14 + 20 * (1 - charge);
                Vector2 offset = side switch
                {
                    0 => new Vector2(-radius, along * radius), 1 => new Vector2(radius, along * radius),
                    2 => new Vector2(along * radius, -radius), _ => new Vector2(along * radius, radius)
                };
                MaterialistVisuals.Nano(Projectile.Center + offset, -offset * .13f,
                    i % 3 == 0 ? Color.White : MaterialistVisuals.Violet, .9f + charge * .3f, 14);
            }
        }
        Lighting.AddLight(Projectile.Center, .25f, .09f, .4f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float charge = MathHelper.Clamp(Age / (float)MaterialistArsenalRules.OrbitalWarning, 0, 1);
        float fade = Age <= MaterialistArsenalRules.OrbitalWarning ? 1 : MathHelper.Clamp((Projectile.timeLeft - 1) / 15f, 0, 1);
        MaterialistVisuals.Brackets(Projectile.Center, new Vector2(52 - charge * 22), MaterialistVisuals.Violet, fade * .8f);
        MaterialistVisuals.Glow(Projectile.Center, MaterialistVisuals.Violet * ((.2f + charge * .35f) * fade), new Vector2(48, 22));
        return false;
    }
}
