using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Common.Networking;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class BlackIceIntro
{
    // The retained animation is a tell; damage belongs to the crystal it releases.
    public override bool? CanDamage() => false;
    public override bool PreDraw(ref Color lightColor) {
        if (FrostNovaVisuals.SecondPhase(Projectile)) {
            var body = FrostNovaVisuals.Texture("BlackIceIntro");
            var source = new Rectangle(0, Projectile.frame * body.Height / 15, body.Width, body.Height / 15);
            FrostNovaVisuals.Outline(body, Projectile.Center, source, Projectile.rotation, source.Size() * .5f,
                new Vector2(Projectile.scale), Projectile.Opacity, 15);
        }
        return true;
    }
}

public partial class FNIceRingChoice
{
    private bool formationConfigured;
    private Vector2 formationCenter, formationAim;
    private int formationWarning, formationAge, formationHost = -1;
    internal void ConfigureFormation(NPC host, Vector2 aim, int warning) {
        formationConfigured = true; formationHost = host.whoAmI;
        formationCenter = Projectile.Center; formationAim = aim;
        formationWarning = warning; formationAge = 0;
        Projectile.hostile = false; Projectile.timeLeft = 240; Projectile.netUpdate = true;
    }
    internal void WriteFormation(BinaryWriter writer) {
        writer.Write(formationConfigured); writer.Write(formationHost);
        writer.Write(formationWarning); writer.Write(formationAge);
        writer.Write(formationCenter.X); writer.Write(formationCenter.Y);
        writer.Write(formationAim.X); writer.Write(formationAim.Y);
    }
    internal void ReadFormation(BinaryReader reader) {
        formationConfigured = reader.ReadBoolean(); formationHost = reader.ReadInt32();
        formationWarning = reader.ReadInt32(); formationAge = reader.ReadInt32();
        formationCenter = new(reader.ReadSingle(), reader.ReadSingle());
        formationAim = new(reader.ReadSingle(), reader.ReadSingle());
    }
    private void RunFormation() {
        Projectile.hostile = false;
        if (!Main.npc.IndexInRange(formationHost) || Main.npc[formationHost].ModNPC is not FrostNova host || !host.CombatHazardsActive) {
            Projectile.Kill(); return;
        }
        formationAge++;
        timer = formationAge; // Keep the existing paired ribbon trails in step with the new cast.
        int kind = (int)Projectile.ai[1] % 4;
        int active = formationAge - formationWarning;
        float side = formationCenter.X < formationAim.X ? -1 : 1;
        Projectile.velocity = Vector2.Zero;
        Projectile.Center = formationCenter + (kind == 0 ? new Vector2(side * Math.Min(70, formationAge * .4f), -MathF.Sin(formationAge / 60f) * 30) : Vector2.Zero);
        if (active == 0) {
            FrostNovaVisuals.Cast(Projectile.Center, kind, .85f);
            if (host.AirbornePhase) FrostNovaIceBurstSystem.Emit(Projectile.Center, .75f);
        }
        if (active < 0 || active > 120 || Main.netMode == NetmodeID.MultiplayerClient) return;
        Vector2 aim = (formationAim - Projectile.Center).SafeNormalize(Vector2.UnitY);
        switch (kind) {
            case 0: // Opposed fans cross in distinct beats rather than continuously re-aiming.
                if (active % 32 == 0 && active <= 96) {
                    // Leave phase one's five rays unchanged. Phase two keeps
                    // this array's original +/-0.66 outer rays and center ray.
                    int half = host.AirbornePhase ? 1 : 2;
                    float spacing = host.AirbornePhase ? .66f : .22f;
                    for (int i = -half; i <= half; i++) Shard(Projectile.Center, aim.RotatedBy(i * spacing) * 7.3f, 24);
                }
                break;
            case 1: // A double spiral with a stable, visible opening toward the arena center.
                if (active % 6 == 0) {
                    float rotation = active * .057f + aim.ToRotation() + .6f;
                    for (int i = 0; i < 2; i++) {
                        float angle = rotation + i * MathHelper.Pi;
                        if (!FrostNovaCombatRules.InGap(angle, aim.ToRotation(), .36f))
                            Shard(Projectile.Center, angle.ToRotationVector2() * 6.6f, 30);
                    }
                }
                break;
            case 2: // Three staggered curtains, each preserving one traversable lane.
                if (active % 36 == 0 && active <= 72) {
                    int wave = active / 36;
                    for (int i = -3; i <= 3; i++) {
                        if (i == wave - 1) continue;
                        Vector2 at = formationAim + new Vector2(i * 145, -370 - Math.Abs(i) * 10);
                        Shard(at, Vector2.UnitY * (7.3f + wave * .35f), 42);
                    }
                }
                break;
            case 3: // Offset concentric salvos share a broad corridor.
                if (active % 42 == 0 && active <= 84) {
                    float gap = aim.ToRotation();
                    int count = Projectile.ai[0] == 2 ? 20 : 16;
                    for (int i = 0; i < count; i++) {
                        float angle = FrostNovaCombatRules.FormationAngle(i, count, gap) + (active / 42 % 2) * .09f;
                        if (FrostNovaCombatRules.InGap(angle, gap, .4f)) continue;
                        Shard(Projectile.Center + angle.ToRotationVector2() * 40, angle.ToRotationVector2() * (5.8f + active * .006f), 30);
                    }
                }
                break;
        }
    }
    public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor) {
        if (formationConfigured) FrostNovaVisuals.DrawSigil(Projectile.Center, 76, Math.Min(1, formationAge / 30f) * Math.Min(1, Projectile.timeLeft / 30f), 6);
        return false;
    }
    private void Shard(Vector2 at, Vector2 velocity, int delay) {
        int slot = EnemyAuthority.NewProjectile(Projectile.GetSource_FromThis(), at, Vector2.Zero,
            ModContent.ProjectileType<BlackIceProjectile>(), Projectile.damage, 0, -1);
        if (slot < Main.maxProjectiles && Main.projectile[slot].ModProjectile is BlackIceProjectile shard)
        {
            shard.ConfigureShard(velocity, delay);
            shard.SetFlightProfile(Main.npc[formationHost].ModNPC is FrostNova { AirbornePhase: true }, (int)Projectile.ai[1] % 4 == 1);
        }
    }
}

public partial class BlackIceProjectile
{
    private bool formationShard;
    private Vector2 formationVelocity;
    private int shardDelay, shardAge;
    private bool phaseTwoShard, brakingShard;
    internal void SetFlightProfile(bool secondPhase, bool braking) { phaseTwoShard = secondPhase; brakingShard = braking && secondPhase; }
    internal void ConfigureShard(Vector2 velocity, int delay) {
        formationShard = true; formationVelocity = velocity; shardDelay = delay;
        Projectile.timeLeft = 240 + delay; Projectile.velocity = Vector2.Zero;
        Projectile.tileCollide = false;
        Projectile.rotation = velocity.ToRotation() + MathHelper.PiOver2; Projectile.netUpdate = true;
    }
    private void RunFormationShard() {
        shardAge++;
        Projectile.Opacity = Math.Min(1, shardAge / 15f);
        Projectile.rotation = formationVelocity.ToRotation() + MathHelper.PiOver2;
        Projectile.frame = Math.Min(5, shardAge * 5 / Math.Max(1, shardDelay));
        if (shardAge < shardDelay) { Projectile.velocity = Vector2.Zero; Projectile.hostile = false; }
        else {
            Projectile.frame = 5; Projectile.hostile = true;
            Projectile.tileCollide = true;
            float speed = brakingShard ? FrostNovaFlightRules.SpiralSpeed(formationVelocity.Length(), shardAge - shardDelay)
                : formationVelocity.Length() * 1.15f * MathF.Pow(1.01f, shardAge - shardDelay);
            Projectile.velocity = formationVelocity.SafeNormalize(Vector2.UnitY) * speed;
        }
        for (int i = oldPos.Length - 1; i > 0; i--) oldPos[i] = oldPos[i - 1];
        oldPos[0] = Projectile.Center;
        if (!Main.dedServ && shardAge % 8 == 0) {
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, -Projectile.velocity * .1f, 120, new Color(95, 195, 255), .7f);
            d.noGravity = true;
        }
        if (!Main.dedServ && shardAge >= shardDelay && shardAge % 3 == 0)
            FrostNovaVisuals.CrystalWake(Projectile.Center, formationVelocity.ToRotation() + MathHelper.PiOver2, shardAge);
    }
    internal void WriteFormationShard(BinaryWriter writer) {
        writer.Write(formationShard); writer.Write(formationVelocity.X); writer.Write(formationVelocity.Y);
        writer.Write(shardDelay); writer.Write(shardAge);
        writer.Write(phaseTwoShard); writer.Write(brakingShard);
    }
    internal void ReadFormationShard(BinaryReader reader) {
        formationShard = reader.ReadBoolean(); formationVelocity = new(reader.ReadSingle(), reader.ReadSingle());
        shardDelay = reader.ReadInt32(); shardAge = reader.ReadInt32();
        phaseTwoShard = reader.ReadBoolean(); brakingShard = reader.ReadBoolean();
    }
}
