using System;
using System.IO;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

// ai[0]: FrostNova slot. ai[1]: straight / gentle curve / stationary mine.
// ai[2]: telegraph ticks. Velocity always stores the initial locked firing axis;
// a mine leaves a permanent eighty-degree opening centered on that axis.
public sealed class FrostNovaBarrage : ModProjectile
{
    public override string Texture => FrostNovaVisuals.Assets + "FNIceCrystal";
    private int encounterToken = -1;
    private int age;
    private bool detonated;
    private bool cancelled;
    private bool validatedHost;
    private bool hostReady;
    private int hostWaitTicks;
    private int Mode => Math.Clamp((int)Projectile.ai[1], 0, 2);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 22;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }

    public override void SetDefaults()
    {
        Projectile.width = 12;
        Projectile.height = 12;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 300;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void OnSpawn(IEntitySource source)
    {
        int slot = (int)Projectile.ai[0];
        if (slot >= 0 && slot < Main.maxNPCs && Main.npc[slot].ModNPC is FrostNova frost)
            encounterToken = frost.EncounterToken;
        if (Mode == 2 && Projectile.ai[2] <= 0) Projectile.ai[2] = 60;
        Projectile.ai[2] = Math.Clamp(Projectile.ai[2], 0, 180);
    }

    private bool HostActive()
    {
        int slot = (int)Projectile.ai[0];
        return slot >= 0 && slot < Main.maxNPCs && Main.npc[slot].active
            && Main.npc[slot].ModNPC is FrostNova frost
            && frost.EncounterToken == encounterToken && frost.CombatHazardsActive;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(encounterToken);
        writer.Write((ushort)Math.Clamp(age, 0, ushort.MaxValue));
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        encounterToken = reader.ReadInt32();
        age = reader.ReadUInt16();
    }
    public override bool ShouldUpdatePosition() => hostReady && Mode != 2 && Projectile.ai[2] <= 0;
    public override bool? CanDamage() => Mode != 2 && Projectile.ai[2] <= 0 && HostActive();

    public override void AI()
    {
        hostReady = HostActive();
        if (!hostReady)
        {
            // A projectile packet can precede the NPC's extra state on a new
            // client. Freeze harmlessly until that first host token arrives.
            // Once validated, a revival/death/token change retires it at once.
            if (Main.netMode == NetmodeID.MultiplayerClient && !validatedHost && ++hostWaitTicks < 90)
            {
                Projectile.hostile = false;
                return;
            }
            cancelled = true;
            Projectile.hostile = false;
            Projectile.Kill();
            return;
        }
        validatedHost = true;
        if (!float.IsFinite(Projectile.velocity.X) || !float.IsFinite(Projectile.velocity.Y))
        {
            cancelled = true;
            Projectile.Kill();
            return;
        }
        if (Projectile.ai[2] > 0)
        {
            Projectile.ai[2]--;
            Projectile.hostile = false;
            if (Projectile.ai[2] <= 0)
            {
                Projectile.netUpdate = Main.netMode != NetmodeID.MultiplayerClient;
                if (Mode != 2) FrostNovaVisuals.Cast(Projectile.Center, 3, .55f);
            }
            else return;
        }

        if (Mode == 2)
        {
            Detonate();
            return;
        }
        age++;
        Projectile.hostile = true;
        float speed = Projectile.velocity.Length();
        if (speed < .1f) Projectile.velocity = -Vector2.UnitY * 4.5f;
        else
        {
            Vector2 direction = Projectile.velocity / speed;
            if (Mode == 1 && age <= 70)
            {
                // A fixed, soft bend with no player lookup, then a straight exit.
                // Deterministic identity parity is the same on every peer.
                float turn = ((Projectile.identity & 1) == 0 ? 1 : -1) * .007f
                    * MathF.Sin(MathHelper.Pi * Math.Clamp(age / 70f, 0, 1));
                direction = direction.RotatedBy(turn);
            }
            Projectile.velocity = direction * Math.Min(8.8f, speed + (Mode == 0 ? .055f : .035f));
        }
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        Lighting.AddLight(Projectile.Center, FrostNovaVisuals.Ice.ToVector3() * .28f);
        if (age % 7 == 0 && FrostNovaVisuals.Visible(Projectile.Center) && ParticleManager.activeParticles.Count < 760)
            new FrostNovaFrostMote(Projectile.Center, -Projectile.velocity * .13f + Main.rand.NextVector2Circular(.5f, .5f),
                age % 14 == 0 ? 1 : 0, 22, .7f, false).Spawn();
    }

    private void Detonate()
    {
        if (detonated) return;
        detonated = true;
        // Nine angular slots with the forward one absent give a visible escape
        // wedge. Children have no tracking and do not inherit mine behavior.
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            float axis = Projectile.velocity.SafeNormalize(Vector2.UnitY).ToRotation();
            for (int i = 1; i < 9; i++)
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center,
                    (axis + i * MathHelper.TwoPi / 9).ToRotationVector2() * 5.6f,
                    Type, Projectile.damage, 0, Main.myPlayer, Projectile.ai[0], 0, 0);
        }
        FrostNovaVisuals.Cast(Projectile.Center, 0, 1.2f);
        if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item27 with { Volume = .48f, Pitch = -.15f }, Projectile.Center);
        Projectile.Kill();
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
        => FrostNovaChill.Apply(target, 120, 24);

    public override void OnKill(int timeLeft)
    {
        if (!cancelled && !detonated) FrostNovaVisuals.Cast(Projectile.Center, 3, .55f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (!FrostNovaVisuals.Visible(Projectile.Center)) return false;
        Vector2 axis = Projectile.velocity.SafeNormalize(Vector2.UnitY);
        if (Mode == 2)
        {
            DrawMine(axis);
            return false;
        }
        if (Projectile.ai[2] > 0)
        {
            float warning = .22f + .5f * (1 - Math.Clamp(Projectile.ai[2] / 60f, 0, 1));
            FrostNovaVisuals.Line(Projectile.Center + axis * 16, Projectile.Center + axis * 174,
                FrostNovaVisuals.Ice * (warning * .45f), 1.2f);
            FrostNovaVisuals.DrawShard(Projectile, warning);
            return false;
        }
        float fade = Math.Min(1, Projectile.timeLeft / 20f);
        FrostNovaVisuals.DrawShardTrail(Projectile, fade * .8f);
        FrostNovaVisuals.DrawShard(Projectile, fade);
        return false;
    }

    private void DrawMine(Vector2 axis)
    {
        float charge = 1 - Math.Clamp(Projectile.ai[2] / 60f, 0, 1);
        float radius = 36 + (1 - charge) * 18;
        float angle = axis.ToRotation();
        FrostNovaVisuals.Glow(Projectile.Center, new Vector2(65), FrostNovaVisuals.Ice, .13f + charge * .12f);
        // Preview the exact outgoing directions. The opening never rotates after
        // spawn; its two pale rails distinguish it from the blue attack rays.
        for (int i = 1; i < 9; i++)
        {
            Vector2 ray = (angle + i * MathHelper.TwoPi / 9).ToRotationVector2();
            FrostNovaVisuals.Line(Projectile.Center + ray * 20, Projectile.Center + ray * (70 + charge * 20),
                FrostNovaVisuals.Ice * (.2f + charge * .32f));
            FrostNovaVisuals.Sprite(FrostNovaVisuals.Texture("FNIceCrystal"),
                Projectile.Center + ray * radius, new Vector2(5, 9), Color.White * (.45f + charge * .45f), ray.ToRotation() + MathHelper.PiOver2);
        }
        for (int i = 1; i < 41; i++)
        {
            // Exclude +/-40 degrees around the escape axis from the warning arc.
            float from = angle + MathHelper.ToRadians(40) + i * MathHelper.ToRadians(280) / 41;
            float to = from + MathHelper.ToRadians(280) / 41;
            FrostNovaVisuals.Line(Projectile.Center + from.ToRotationVector2() * radius,
                Projectile.Center + to.ToRotationVector2() * radius,
                FrostNovaVisuals.Ice * (.32f + charge * .4f), 1.25f);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 rail = (angle + side * MathHelper.ToRadians(30)).ToRotationVector2();
            FrostNovaVisuals.Line(Projectile.Center + rail * 23, Projectile.Center + rail * 66,
                FrostNovaVisuals.Pale * (.4f + charge * .2f));
        }
        FrostNovaVisuals.Sprite(FrostNovaVisuals.Texture("FNIceCrystal"),
            Projectile.Center, new Vector2(19, 28) * (1 + charge * .1f), Color.White * .9f,
            Main.GlobalTimeWrappedHourly * .5f);
    }
}
