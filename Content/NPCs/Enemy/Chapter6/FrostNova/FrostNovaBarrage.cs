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
    private bool phaseTwo;
    internal bool IsSecondPhase => phaseTwo;
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
        Projectile.extraUpdates = FrostNovaCombatRules.BarrageUpdates - 1;
        Projectile.timeLeft = 300 * FrostNovaCombatRules.BarrageUpdates;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void OnSpawn(IEntitySource source)
    {
        if (Mode != 2) Projectile.velocity *= 1.15f;
        int slot = (int)Projectile.ai[0];
        if (slot >= 0 && slot < Main.maxNPCs && Main.npc[slot].ModNPC is FrostNova frost)
        { encounterToken = frost.EncounterToken; phaseTwo = frost.AirbornePhase; }
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
        writer.Write(phaseTwo);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        encounterToken = reader.ReadInt32();
        age = reader.ReadUInt16();
        phaseTwo = reader.ReadBoolean();
    }
    // Velocity remains real-tick speed in snapshots. Substeps integrate it below.
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Mode != 2 && Projectile.ai[2] <= 0 && HostActive();

    public override void AI()
    {
        if (Projectile.numUpdates == 0) TickRealTime();
        if (Projectile.active && Mode != 2 && CanDamage() == true) {
            Vector2 step = Projectile.velocity / Projectile.MaxUpdates;
            // Self-integrated motion needs an explicit tile sweep per substep.
            Vector2 clipped = Collision.TileCollision(Projectile.position, step, Projectile.width, Projectile.height);
            Projectile.position += clipped;
            if (Vector2.DistanceSquared(step, clipped) > .0001f) {
                Projectile.damage = 0; Projectile.hostile = false; Projectile.Kill();
            }
        }
    }
    private void TickRealTime()
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
        if (speed < .1f) Projectile.velocity = -Vector2.UnitY * (4.5f * 1.15f);
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
            float factor = phaseTwo && Mode == 1 ? FrostNovaFlightRules.CurvedFactor(age) : 1.01f;
            Projectile.velocity = direction * (age == 1 ? speed : speed * factor);
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
            for (int i=-2;i<=2;i++)
                Projectile.NewProjectile(Projectile.GetSource_FromAI(),Projectile.Center,
                    (axis+i*.14f).ToRotationVector2()*7.2f,Type,Projectile.damage,0,Main.myPlayer,Projectile.ai[0],0,18);
        }
        FrostNovaVisuals.Cast(Projectile.Center, 0, 1.2f);
        if (phaseTwo) FrostNovaIceBurstSystem.Emit(Projectile.Center, 1.1f);
        if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item27 with { Volume = .48f, Pitch = -.15f }, Projectile.Center);
        Projectile.GetGlobalProjectile<FrostNovaOwnedProjectile>().Retire(Projectile);
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
        => FrostNovaChill.Apply(target, 120, 24);

    public override void OnKill(int timeLeft)
    {
        if (!cancelled && !detonated) FrostNovaVisuals.Shatter(Projectile.Center, Projectile.velocity);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        FrostNovaProjectileArt.Draw(Projectile, Projectile.GetGlobalProjectile<FrostNovaOwnedProjectile>().Visibility);
        return false;
    }

}
