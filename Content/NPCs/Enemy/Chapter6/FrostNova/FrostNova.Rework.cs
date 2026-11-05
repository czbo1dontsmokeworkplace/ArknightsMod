using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Common.Networking;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class FrostNova
{
    private static int nextEncounterToken;
    private int encounterToken, combatTimer, attackSerial, supportTimer;
    private Vector2 attackAnchor, attackAim, dashDirection;
    private bool attackInitialized;
    public int EncounterToken => encounterToken;
    public bool AirbornePhase => FNStage == 2f;
    public bool CombatHazardsActive => (FNStage == 1f || FNStage == 2f) && escapetimer == 0;
    internal int CombatPattern => FrostNovaCombatRules.Pattern(AirbornePhase ? 2 : 1, attackSerial);
    internal int CombatTimer => combatTimer;

    private void RunReworkedAI()
    {
        if (encounterToken == 0 && Main.netMode != NetmodeID.MultiplayerClient) {
            encounterToken = ++nextEncounterToken;
            if (encounterToken <= 0) encounterToken = nextEncounterToken = 1;
            NPC.netUpdate = true;
        }
        NPC.TargetClosest(false);
        Player player = Main.player[NPC.target];
        bool valid = player.active && !player.dead && !player.ghost && NPC.Distance(player.Center) < 10000;
        if (!valid && !FNDeathStart) {
            escapetimer++;
            NPC.damage = 0;
            NPC.dontTakeDamage = true;
            NPC.velocity *= .94f;
            DisableDistanceRing();
            if (escapetimer >= 300 && Main.netMode != NetmodeID.MultiplayerClient) {
                RetireEncounter(); NPC.active = false; NPC.netUpdate = true;
            }
            return;
        }
        escapetimer = 0;
        NPC.timeLeft = Math.Max(NPC.timeLeft, 600);
        NPC.direction = NPC.spriteDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
        if (!Main.dedServ) {
            Music = MusicLoader.GetMusicSlot(Mod, FNDeathStart ? "Sounds/Music/FrostnovaDeath" :
                FNStage >= 1.5f ? "Sounds/Music/FrostnovaStage2" : "Sounds/Music/FrostnovaStage1");
        }
        if (FNDeathStart) { TickFinalDeath(); return; }
        if (FNRevivalStart) { TickRevival(); return; }
        if (!FNBirthEnd) {
            BirthTimer++;
            NPC.damage = 0; NPC.dontTakeDamage = true;
            NPC.Opacity = MathHelper.Clamp(BirthTimer / 100f, 0, 1);
            NPC.velocity.X *= .85f;
            if (BirthTimer == 30) SpawnAppearance();
            if (BirthTimer >= FrostNovaCombatRules.BirthDuration) {
                FNStage = 1; FNBirthEnd = true; NPC.dontTakeDamage = false;
                ResetCombat(); NPC.netUpdate = true;
            }
            UpdateDistanceRing(player);
            return;
        }
        if (AirbornePhase) {
            ReBirthTimer++;
            NPC.noGravity = true; NPC.noTileCollide = true;
            DisableDistanceRing();
            if (ReBirthTimer <= FrostNovaCombatRules.PhaseTwoEntrance) {
                NPC.dontTakeDamage = true; NPC.damage = 0;
                MoveTo(player.Center + new Vector2(-NPC.direction * 320, -260), 7.5f, .055f);
                if (ReBirthTimer == 1) {
                    SpawnHazard(NPC.Center, Vector2.Zero, ModContent.ProjectileType<FNInvincibleEffect>(), 0, 1800, -1);
                    SpawnHazard(NPC.Center, Vector2.Zero, ModContent.ProjectileType<FNInvincibleEffect>(), 0, 1800, 1);
                }
                if (ReBirthTimer == 30) {
                    FrostNovaVisuals.Cast(NPC.Center, 2, 1.4f);
                    SummonStorms(player.Center, 1000);
                }
                if (ReBirthTimer == FrostNovaCombatRules.PhaseTwoEntrance) {
                    NPC.dontTakeDamage = false; ResetCombat(); NPC.netUpdate = true;
                }
                return;
            }
        }
        else {
            NPC.noGravity = false; NPC.noTileCollide = false;
            UpdateDistanceRing(player);
        }
        NPC.dontTakeDamage = false;
        // The caster's body is safe outside the explicitly warned sweeping attack.
        NPC.damage = 0;
        int phase = AirbornePhase ? 2 : 1, pattern = CombatPattern;
        if (!attackInitialized) InitializeAttack(player);
        if (!attackInitialized) return; // Wait for the authority's locked formation anchor.
        bool recovery = FrostNovaCombatRules.InRecovery(phase, pattern, combatTimer);
        FNOnATK = !recovery && pattern == 0;
        FNATKTimer = FNOnATK ? 156 + combatTimer % 110 : 0;
        FNOnSkill = !recovery && pattern != 0;
        FNSkillTimer = FNOnSkill ? 480 + combatTimer : 0;
        if (recovery) {
            if (AirbornePhase) MoveTo(attackAnchor, 6, .055f);
            else NPC.velocity.X *= .85f;
        }
        else {
            MoveForAttack(player, pattern);
            DoAttack(player, pattern);
        }
        if (++supportTimer >= (AirbornePhase ? 1080 : 1260)) {
            supportTimer = 0;
            if (AirbornePhase) SummonAltar(player);
            else SummonSnowSupport(player);
        }
        combatTimer++;
        if (combatTimer >= FrostNovaCombatRules.AttackDuration(phase, pattern)) {
            attackSerial++; combatTimer = 0; attackInitialized = false;
            FNATKFrameInitialized = FNSkillFrameInitialized = false;
            NPC.netUpdate = true;
        }
    }

    private void ResetCombat() {
        combatTimer = attackSerial = 0; attackInitialized = false;
        FNOnATK = FNOnSkill = FNATKFrameInitialized = FNSkillFrameInitialized = false;
        FNATKTimer = FNSkillTimer = 0;
    }
    private void InitializeAttack(Player player) {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        attackInitialized = true;
        float side = attackSerial % 2 == 0 ? -1 : 1;
        attackAim = player.Center;
        attackAnchor = player.Center + new Vector2(side * (AirbornePhase ? 350 : 240), AirbornePhase ? -230 : -24);
        dashDirection = (attackAim - attackAnchor).SafeNormalize(Vector2.UnitX);
        NPC.netUpdate = true;
    }
    private void MoveTo(Vector2 point, float speed, float smoothing) {
        Vector2 desired = (point - NPC.Center) * .045f;
        if (desired.Length() > speed) desired = desired.SafeNormalize(Vector2.Zero) * speed;
        NPC.velocity = Vector2.Lerp(NPC.velocity, desired, smoothing);
    }
    private void MoveForAttack(Player player, int pattern) {
        if (AirbornePhase) {
            if (pattern == 6 && combatTimer >= 30 && combatTimer < 72) {
                NPC.velocity = Vector2.Zero; // The warned corridor starts at this exact locked origin.
                return;
            }
            if (pattern == 6 && combatTimer >= 72 && combatTimer < 114) {
                NPC.velocity = dashDirection * FrostNovaCombatRules.DashSpeed;
                NPC.damage = FrostNovaCombatRules.ShotDamage(2, true);
                return;
            }
            Vector2 drift = new Vector2(MathF.Sin(combatTimer / 65f) * 70, MathF.Sin(combatTimer / 40f) * 25);
            // Follow at range between patterns; once the warning starts the attack anchor stays fixed.
            MoveTo(attackAnchor + drift, pattern == 6 ? 9 : FrostNovaCombatRules.FlightSpeed, .065f);
            if (NPC.Distance(player.Center) > 1500 && combatTimer < 45) {
                attackAnchor = player.Center + new Vector2(-NPC.direction * 350, -230);
            }
        }
        else {
            float delta = attackAnchor.X - NPC.Center.X;
            float desired = MathHelper.Clamp(delta * .025f, -3.6f, 3.6f);
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, combatTimer < 70 ? desired : desired * .25f, .11f);
            if (NPC.collideX && NPC.velocity.Y == 0) NPC.velocity.Y = -7.5f;
            // Escape terrain traps with a visible relocation instead of cycling failed jumps.
            if (combatTimer == 30 && (NPC.Distance(player.Center) > 900 || Math.Abs(NPC.Center.Y - player.Center.Y) > 320)) {
                Vector2 at = player.Center + new Vector2(-NPC.direction * 280, -60);
                if (!Collision.SolidCollision(at - NPC.Size * .5f, NPC.width, NPC.height)) {
                    FrostNovaVisuals.Cast(NPC.Center, 0); NPC.Center = at;
                    FrostNovaVisuals.Cast(NPC.Center, 0); NPC.netUpdate = true;
                }
            }
        }
    }
    private void DoAttack(Player player, int pattern) {
        int t = combatTimer, phase = AirbornePhase ? 2 : 1;
        switch (pattern) {
            case 0:
                if (t == 48 || t == 84 || (AirbornePhase && t == 126)) {
                    Vector2 aim = (player.Center + player.velocity * 12 - NPC.Center).SafeNormalize(Vector2.UnitY);
                    FireFan(NPC.Center + new Vector2(0, -24), aim, AirbornePhase ? 5 : 3, .18f, 5.8f, 0, 36);
                    FrostNovaVisuals.Cast(NPC.Center, 0, .65f);
                }
                break;
            case 1:
                if (t == 36) {
                    SpawnIceRing(attackAim + new Vector2(-260, -210), 0, attackAim, 54);
                    if (AirbornePhase) SpawnIceRing(attackAim + new Vector2(260, -210), 0, attackAim, 88);
                }
                break;
            case 2:
                if (t == 36) SpawnIceRing(attackAim + new Vector2(0, -220), 1, attackAim, 54);
                if (AirbornePhase && t == 150) FireFan(NPC.Center, Vector2.UnitY, 5, .25f, 4.6f, 1, 48);
                break;
            case 3:
                if (t == 36) {
                    SummonSnowSupport(player);
                    if (AirbornePhase) SummonAltar(player);
                }
                if (t == 96 || t == 150) FireFan(NPC.Center, (attackAim - NPC.Center).SafeNormalize(Vector2.UnitY), 3, .28f, 4.8f, 1, 42);
                break;
            case 4:
                if (t == 40) SpawnIceRing(attackAim + new Vector2(0, -180), 3, attackAim, 60);
                if (AirbornePhase && t == 134) SpawnIceRing(attackAim + new Vector2(300 * (attackSerial % 2 == 0 ? 1 : -1), -80), 3, attackAim, 60);
                break;
            case 5:
                if (t == 36) SpawnIceRing(attackAim + new Vector2(0, -260), 2, attackAim, 60);
                break;
            case 6:
                if (t == 30) {
                    attackAnchor = NPC.Center;
                    attackAim = player.Center;
                    dashDirection = (attackAim - NPC.Center).SafeNormalize(Vector2.UnitX);
                    NPC.netUpdate = true;
                    FrostNovaVisuals.Cast(NPC.Center, 1);
                }
                if (t >= 72 && t < 114 && t % 14 == 2)
                    FireShot(NPC.Center, new Vector2(0, -1), 2, 64);
                if (t == 114) { attackAnchor = NPC.Center; NPC.velocity *= .2f; NPC.netUpdate = true; }
                break;
            case 7:
                if (t == 36) for (int i = -1; i <= 1; i++) {
                    Vector2 at = attackAim + new Vector2(i * 300, -160 + Math.Abs(i) * 90);
                    Vector2 gap = (attackAim - at).SafeNormalize(Vector2.UnitY);
                    FireShot(at, gap, 2, 72 + (i + 1) * 24);
                }
                break;
            case 8:
                if (t == 36 || t == 108) {
                    float x = attackAim.X + (t == 36 ? -240 : 240);
                    Vector2 at = new(x, attackAim.Y - 420);
                    SpawnHazard(at, Vector2.UnitY * 10, ModContent.ProjectileType<FrostNovaBlackIceSpike>(),
                        FrostNovaCombatRules.ShotDamage(phase, true), NPC.whoAmI, 60, 600);
                    FrostNovaVisuals.Cast(at, 3, .8f);
                }
                break;
        }
        if (AirbornePhase && t == 48 && attackSerial > 0 && attackSerial % 5 == 0) SummonStorms(attackAim, 900);
    }
    private int HazardCount() {
        int count = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.GetGlobalProjectile<FrostNovaOwnedProjectile>().BelongsTo(NPC.whoAmI, encounterToken) && p.hostile) count++;
        return count;
    }
    private int SpawnHazard(Vector2 position, Vector2 velocity, int type, int damage, float ai0 = 0, float ai1 = 0, float ai2 = 0) {
        if (Main.netMode == NetmodeID.MultiplayerClient || HazardCount() >= FrostNovaCombatRules.HazardCap) return Main.maxProjectiles;
        return EnemyAuthority.NewProjectile(NPC.GetSource_FromAI(), position, velocity, type, damage, 0, -1, ai0, ai1, ai2);
    }
    private void FireShot(Vector2 at, Vector2 velocity, int mode, int warning, int damage = -1) =>
        SpawnHazard(at, velocity, ModContent.ProjectileType<FrostNovaBarrage>(), damage < 0 ? FrostNovaCombatRules.ShotDamage(AirbornePhase ? 2 : 1) : damage,
            NPC.whoAmI, mode, warning);
    private void FireFan(Vector2 at, Vector2 direction, int count, float spacing, float speed, int mode, int warning) {
        for (int i = 0; i < count; i++) FireShot(at, direction.RotatedBy((i - (count - 1) * .5f) * spacing) * speed, mode, warning);
    }
    private void SpawnIceRing(Vector2 at, int kind, Vector2 aim, int warning) {
        int slot = SpawnHazard(at, Vector2.Zero, ModContent.ProjectileType<FNIceRingChoice>(),
            FrostNovaCombatRules.ShotDamage(AirbornePhase ? 2 : 1), AirbornePhase ? 2 : 1, kind);
        if (slot < Main.maxProjectiles && Main.projectile[slot].ModProjectile is FNIceRingChoice ring)
            ring.ConfigureFormation(NPC, aim, warning);
    }
    private void SummonStorms(Vector2 center, float distance) {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        int count = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.type == ModContent.ProjectileType<BlizzardStorm>() && p.GetGlobalProjectile<FrostNovaOwnedProjectile>().BelongsTo(NPC.whoAmI, encounterToken)) count++;
        if (count >= 2) return;
        for (int side = -1; side <= 1; side += 2)
            SpawnHazard(center + new Vector2(side * distance, -200), Vector2.Zero, ModContent.ProjectileType<BlizzardStorm>(), 24, side);
    }
    private int SupportCount() {
        int count = 0;
        foreach (NPC n in Main.ActiveNPCs) if (n.GetGlobalNPC<FrostNovaOwnedNPC>().BelongsTo(NPC.whoAmI, encounterToken)) count++;
        return count;
    }
    private void SummonSnowSupport(Player player) {
        if (Main.netMode == NetmodeID.MultiplayerClient || SupportCount() >= FrostNovaCombatRules.SupportCap) return;
        int[] types = { ModContent.NPCType<SnowCaster>(), ModContent.NPCType<SnowSniper>(), ModContent.NPCType<SnowSoldier>(), ModContent.NPCType<SnowHound>(), ModContent.NPCType<Oneiros>(), ModContent.NPCType<IceCleaver>() };
        for (int side = -1; side <= 1; side += 2) {
            if (SupportCount() >= FrostNovaCombatRules.SupportCap) break;
            Vector2 at = FindSurface(player.Center + new Vector2(side * 460, -80));
            EnemyAuthority.NewNPC(NPC.GetSource_FromAI(), (int)at.X, (int)at.Y - 16, types[(attackSerial + (side + 1) / 2) % types.Length]);
        }
    }
    private void SummonAltar(Player player) {
        if (Main.netMode == NetmodeID.MultiplayerClient || SupportCount() >= FrostNovaCombatRules.SupportCap || IceCrystalAltar.CountActiveAltars() >= 3) return;
        Vector2 at = player.Center + new Vector2(attackSerial % 2 == 0 ? -340 : 340, -50);
        SpawnHazard(NPC.Center, Vector2.Zero, ModContent.ProjectileType<IceAltarSummonWisp>(), 0, at.X, at.Y);
    }
    internal static Vector2 FindSurface(Vector2 from) {
        from.X = MathHelper.Clamp(from.X, 160, Main.maxTilesX * 16 - 176);
        from.Y = MathHelper.Clamp(from.Y, 160, Main.maxTilesY * 16 - 176);
        Point p = from.ToTileCoordinates();
        p.X = Math.Clamp(p.X, 10, Main.maxTilesX - 11);
        for (int y = Math.Max(10, p.Y); y < Math.Min(Main.maxTilesY - 10, p.Y + 70); y++) {
            if (WorldGen.SolidOrSlopedTile(p.X, y) || (Main.tile[p.X, y].HasUnactuatedTile && Main.tileSolidTop[Main.tile[p.X, y].TileType]))
                return new Vector2(from.X, y * 16);
        }
        return from + Vector2.UnitY * 120;
    }
    private void TickRevival() {
        DisableDistanceRing();
        NPC.damage = 0; NPC.dontTakeDamage = true; NPC.velocity.X *= .85f;
        DeathTimer++;
        NPC.life = DeathTimer <= 320 ? 1 : (int)MathHelper.Lerp(1, NPC.lifeMax, MathHelper.SmoothStep(0, 1, (DeathTimer - 320) / 180));
        if (DeathTimer == 325) SpawnHazard(NPC.Center, Vector2.Zero, ModContent.ProjectileType<FrostNovaRevival>(), 0, NPC.direction);
        if (DeathTimer == 411) SpawnHazard(NPC.Center, Vector2.Zero, ModContent.ProjectileType<FrostNovaWhiteRing>(), 0, NPC.direction);
        if (DeathTimer >= FrostNovaCombatRules.RevivalDuration) {
            NPC.life = NPC.lifeMax; FNStage = 2; FNRevival = true; FNRevivalStart = FNRevivalEnd = false;
            ReBirthTimer = 0; NPC.noGravity = NPC.noTileCollide = true;
            ResetCombat(); NPC.netUpdate = true;
        }
    }
    private void TickFinalDeath() {
        DisableDistanceRing(); DeathTimer++;
        NPC.damage = 0; NPC.dontTakeDamage = true;
        NPC.velocity *= .88f;
        if (DeathTimer % 12 == 0 && DeathTimer < 125) FrostNovaVisuals.Cast(NPC.Center, 3, .3f);
        if (DeathTimer == 130) {
            for (int i = 0; i < 14; i++) SpawnHazard(NPC.Center + new Vector2((i % 4 - 1.5f) * 12, i % 3 * 10),
                new Vector2((i % 3 - 1) * 1.8f, -.4f), ModContent.ProjectileType<FrostNovaSmoke>(), 0, 0, 85, i % 2 == 0 ? -1 : 1);
            FrostNovaVisuals.Cast(NPC.Center, 2, 1.6f);
        }
        if (DeathTimer >= 135 && Main.netMode != NetmodeID.MultiplayerClient) {
            FNEnd = true; NPC.life = 0; NPC.checkDead();
        }
    }
    private void SpawnAppearance() {
        for (int i = 0; i < 6; i++) SpawnHazard(NPC.Center + new Vector2((i - 2.5f) * 5, 5),
            new Vector2(0, -1), ModContent.ProjectileType<FrostNovaJump>(), 0, 0, 40);
        FrostNovaVisuals.Cast(NPC.Center, 0, 1.2f);
    }
    private void UpdateDistanceRing(Player player) {
        if (FNStage != 1) { DisableDistanceRing(); return; }
        FNShaderRingTimer++;
        if (NPC.Distance(player.Center) > 560) player.AddBuff(BuffID.Chilled, 15);
        if (Main.dedServ) return;
        var filter = Terraria.Graphics.Effects.Filters.Scene["FNTwistedRing"];
        if (filter == null) return;
        if (!filter.IsActive()) Terraria.Graphics.Effects.Filters.Scene.Activate("FNTwistedRing", NPC.Center);
        filter.GetShader().UseColor(.39f, .46f, .5f).UseTargetPosition(NPC.Center + new Vector2(0, 3))
            .UseIntensity(.75f).UseOpacity(Math.Min(1, FNShaderRingTimer / 60f)).UseProgress(.93f);
        ArknightsMod.FNTwistedRing?.Value.Parameters["uTime"]?.SetValue((float)Main.time / 64);
    }
    private static void DisableDistanceRing() {
        if (Main.dedServ) return;
        var filter = Terraria.Graphics.Effects.Filters.Scene["FNTwistedRing"];
        if (filter != null && filter.IsActive()) filter.Deactivate();
    }
    private void RetireEncounter() {
        foreach (Projectile p in Main.ActiveProjectiles) {
            if (p.GetGlobalProjectile<FrostNovaOwnedProjectile>().BelongsTo(NPC.whoAmI, encounterToken) && p.ModProjectile is not FrostNovaSmoke) {
                p.damage = 0; p.hostile = false; p.active = false; p.netUpdate = true;
                if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.KillProjectile, number: p.identity, number2: p.owner);
            }
        }
        foreach (NPC n in Main.ActiveNPCs) {
            if (n.GetGlobalNPC<FrostNovaOwnedNPC>().BelongsTo(NPC.whoAmI, encounterToken)) { n.active = false; n.netUpdate = true; }
        }
    }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
        FrostNovaVisuals.DrawBoss(NPC, AirbornePhase, CombatHazardsActive ? combatTimer / (float)FrostNovaCombatRules.AttackDuration(AirbornePhase ? 2 : 1, CombatPattern) : 0, CombatPattern);
        if (AirbornePhase && CombatPattern == 6 && combatTimer >= 30 && combatTimer < 72) {
            // The locked sweep uses the same axis as its warning.
            Vector2 end = attackAnchor + dashDirection * FrostNovaCombatRules.DashSpeed * 42;
            for (int i = 0; i < 12; i++) FrostNovaVisuals.DrawSigil(Vector2.Lerp(attackAnchor, end, i / 11f), 13, .25f, 4);
        }
        return true;
    }
}
