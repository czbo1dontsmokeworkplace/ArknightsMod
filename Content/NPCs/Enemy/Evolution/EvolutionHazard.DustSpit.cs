using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class Evolution
{
    internal void LobBroodDust(Vector2 origin, Vector2 landing, int flight = 75)
    {
        const float gravity = .22f;
        Vector2 velocity = new((landing.X - origin.X) / flight,
            (landing.Y - origin.Y) / flight - gravity * (flight + 1) * .5f);
        Shoot(EvolutionShot.BroodSpit, origin, velocity, delay: 0, lifetime: flight + 35);
    }
}

public sealed partial class EvolutionHazard
{
    private void UpdateBroodSpit(Evolution boss)
    {
        if (Age < Delay) return;
        Projectile.velocity.Y = Math.Min(15, Projectile.velocity.Y + .22f);
        Projectile.tileCollide = Age >= Delay + 4 && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height);
        if (Main.dedServ) return;
        for (int i = 0; i < 3; i++)
        {
            Vector2 point = Projectile.Center - Projectile.velocity * (i / 3f);
            Dust dust = Dust.NewDustPerfect(point + Main.rand.NextVector2Circular(2, 2), DustID.FireworkFountain_Red,
                -Projectile.velocity * .08f, 0, boss.ThemeColor, i == 0 ? 1.25f : .85f);
            dust.noGravity = true; dust.fadeIn = .6f;
        }
        Lighting.AddLight(Projectile.Center, boss.ThemeColor.ToVector3() * .2f);
    }
    private void UpdateSpinBomb()
    {
        Projectile.velocity *= .97f;
        if (Age >= Delay - 24) Projectile.velocity = Vector2.Zero;
        if (Age != Delay || Main.dedServ) return;
        EvolutionArsenalVisuals.Burst(Projectile.Center, .75f);
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = .35f, Pitch = -.4f, MaxInstances = 3 }, Projectile.Center);
    }
    private void DrawSpinBomb()
    {
        if (Age < Delay)
        {
            float charge = Math.Clamp((Age - Delay + 24) / 24f, 0, 1);
            EvolutionProjectileVisuals.DrawBomb(Projectile.Center, Age * .1f, 31 + charge * 10, charge);
            if (Age >= Delay - 24)
                EvolutionVisuals.Ring(Projectile.Center, Parameter, 0, EvolutionVisuals.Blood * (.4f + charge * .4f), 1.5f, false);
        }
        else EvolutionArsenalVisuals.Blast(Projectile.Center, Age - Delay, Parameter);
    }
}
