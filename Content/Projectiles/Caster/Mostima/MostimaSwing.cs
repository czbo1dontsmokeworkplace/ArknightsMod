using System;
using ArknightsMod.Content.Items.Weapons.Caster.Mostima;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

public sealed class MostimaSwing : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Caster/Mostima/MostimaStaff";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.timeLeft = 36;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.CCed || owner.noItems || owner.HeldItem.ModItem is not MostimaStaff)
        { Projectile.Kill(); return; }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        owner.ChangeDir(direction.X < 0 ? -1 : 1);
        Projectile.Center = owner.MountedCenter;
        float t = (36 - Projectile.timeLeft) / 35f;
        // Deliberate windup, fast sweep through the aim, then recovery.
        float sweep = t < .25f ? -.95f - .25f * t / .25f
            : MathHelper.Lerp(-1.2f, 1.35f, 1 - MathF.Pow(1 - MathHelper.Clamp((t - .25f) / .55f, 0, 1), 2));
        Projectile.rotation = direction.ToRotation() + sweep * owner.direction;
        owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        if (Projectile.timeLeft == 21)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter,
                    direction, ModContent.ProjectileType<MostimaChronoWave>(), Projectile.damage,
                    Projectile.knockBack, owner.whoAmI);
                if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = Projectile.CritChance;
            }
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = .62f, Pitch = -.22f }, owner.Center);
                MostimaVisuals.Particles(owner.Center + direction * 55, 18, 7);
            }
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        MostimaFocus.DrawStaff(Projectile.Center, Projectile.rotation, lightColor, 1.35f);
        using var pass = MostimaVisuals.Begin();
        float fade = MathF.Sin((36 - Projectile.timeLeft) / 36f * MathHelper.Pi);
        for (int i = 2; i >= 0; i--)
            MostimaVisuals.Sprite("ChronoCrescentV2", Projectile.Center, new Vector2(190f - i * 17f),
                (i == 0 ? MostimaVisuals.Blue : MostimaVisuals.Violet) * fade * (i == 0 ? .65f : .25f),
                Projectile.rotation - i * .20f * Main.player[Projectile.owner].direction);
        return false;
    }
}
