using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Common.Networking;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

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
        if (active == 0) FrostNovaVisuals.Cast(Projectile.Center, kind, .85f);
        if (active < 0 || active > 120 || Main.netMode == NetmodeID.MultiplayerClient) return;
        Vector2 aim = (formationAim - Projectile.Center).SafeNormalize(Vector2.UnitY);
        switch (kind) {
            case 0: // Opposed fans cross in distinct beats rather than continuously re-aiming.
                if (active % 40 == 0) {
                    for (int i = -2; i <= 2; i++) Shard(Projectile.Center, aim.RotatedBy(i * .24f) * 6.4f, 24);
                }
                break;
            case 1: // A double spiral with a stable, visible opening toward the arena center.
                if (active % 7 == 0) {
                    float rotation = active * .057f + aim.ToRotation() + .6f;
                    for (int i = 0; i < 2; i++) {
                        float angle = rotation + i * MathHelper.Pi;
                        if (!FrostNovaCombatRules.InGap(angle, aim.ToRotation(), .36f))
                            Shard(Projectile.Center, angle.ToRotationVector2() * 5.3f, 30);
                    }
                }
                break;
            case 2: // Three staggered curtains, each preserving one traversable lane.
                if (active % 36 == 0 && active <= 72) {
                    int wave = active / 36;
                    for (int i = -3; i <= 3; i++) {
                        if (i == wave - 1) continue;
                        Vector2 at = formationAim + new Vector2(i * 145, -370 - Math.Abs(i) * 10);
                        Shard(at, Vector2.UnitY * (6.5f + wave * .35f), 48);
                    }
                }
                break;
            case 3: // Offset concentric salvos share a broad corridor.
                if (active % 42 == 0 && active <= 84) {
                    float gap = aim.ToRotation();
                    int count = Projectile.ai[0] == 2 ? 18 : 14;
                    for (int i = 0; i < count; i++) {
                        float angle = FrostNovaCombatRules.FormationAngle(i, count, gap) + (active / 42 % 2) * .09f;
                        if (FrostNovaCombatRules.InGap(angle, gap, .4f)) continue;
                        Shard(Projectile.Center + angle.ToRotationVector2() * 40, angle.ToRotationVector2() * (4.6f + active * .006f), 30);
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
            shard.ConfigureShard(velocity, delay);
    }
}

public partial class BlackIceProjectile
{
    private bool formationShard;
    private Vector2 formationVelocity;
    private int shardDelay, shardAge;
    internal void ConfigureShard(Vector2 velocity, int delay) {
        formationShard = true; formationVelocity = velocity; shardDelay = delay;
        Projectile.timeLeft = 240 + delay; Projectile.velocity = Vector2.Zero;
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
            float speed = Math.Min(FrostNovaCombatRules.LegacyShardSpeed, formationVelocity.Length() + (shardAge - shardDelay) * .018f);
            Projectile.velocity = formationVelocity.SafeNormalize(Vector2.UnitY) * speed;
        }
        for (int i = oldPos.Length - 1; i > 0; i--) oldPos[i] = oldPos[i - 1];
        oldPos[0] = Projectile.Center;
        if (!Main.dedServ && shardAge % 8 == 0) {
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, -Projectile.velocity * .1f, 120, new Color(95, 195, 255), .7f);
            d.noGravity = true;
        }
    }
    internal void WriteFormationShard(BinaryWriter writer) {
        writer.Write(formationShard); writer.Write(formationVelocity.X); writer.Write(formationVelocity.Y);
        writer.Write(shardDelay); writer.Write(shardAge);
    }
    internal void ReadFormationShard(BinaryReader reader) {
        formationShard = reader.ReadBoolean(); formationVelocity = new(reader.ReadSingle(), reader.ReadSingle());
        shardDelay = reader.ReadInt32(); shardAge = reader.ReadInt32();
    }
}
