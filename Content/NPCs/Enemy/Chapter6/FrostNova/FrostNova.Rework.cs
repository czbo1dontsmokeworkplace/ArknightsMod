using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameContent;
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
        UpdateShieldTarget();
        UpdateDomainCenter();
        UpdateColdAir();
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
            NPC.noGravity = false; NPC.noTileCollide = false;
            DisableDistanceRing();
            if (ReBirthTimer <= FrostNovaCombatRules.PhaseTwoEntrance) {
                NPC.dontTakeDamage = true; NPC.damage = 0;
                NPC.velocity.X *= .85f;
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
        // All attacks are spells; the caster has no contact-damage movement.
        NPC.damage = 0;
        int phase = AirbornePhase ? 2 : 1, pattern = CombatPattern;
        if (!attackInitialized) InitializeAttack(player);
        if (!attackInitialized) return; // Wait for the authority's locked formation anchor.
        bool recovery = FrostNovaCombatRules.InRecovery(phase, pattern, combatTimer);
        FNOnATK = false;
        FNATKTimer = FNOnATK ? 156 + combatTimer % 110 : 0;
        FNOnSkill = !recovery;
        FNSkillTimer = FNOnSkill ? 480 + combatTimer : 0;
        if (recovery) {
            NPC.velocity.X *= .85f;
        }
        else if (!TickPlannedRelocation(player, pattern)) {
            MoveForAttack(player, pattern);
            DoAttack(player, pattern);
            AddCryogenicPressure(player, pattern);
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
        relocationArmed = false;
        FNOnATK = FNOnSkill = FNATKFrameInitialized = FNSkillFrameInitialized = false;
        FNATKTimer = FNSkillTimer = 0;
    }
    private void InitializeAttack(Player player) {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        attackInitialized = true;
        float side = attackSerial % 2 == 0 ? -1 : 1;
        attackAim = player.Center;
        attackAnchor = player.Center + new Vector2(side * 300, 0);
        dashDirection = (attackAim - attackAnchor).SafeNormalize(Vector2.UnitX);
        NPC.netUpdate = true;
    }
    private void MoveForAttack(Player player, int pattern) {
        NPC.noGravity = false; NPC.noTileCollide = false;
        float moveX = Math.Abs(player.Center.X - NPC.Center.X) > FrostNovaDomainRules.Radius
            ? player.Center.X - NPC.direction * 240 : attackAnchor.X;
        float delta = moveX - NPC.Center.X;
        float speed = AirbornePhase ? 1.35f : 1.1f;
        float desired = Math.Abs(delta) < 30 ? 0 : MathHelper.Clamp(delta * .012f, -speed, speed);
        // Feet stay planted while chanting. No launches, hover chase, or contact dash.
        if (combatTimer < 72 || pattern == 3) desired *= .25f;
        NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, desired, .08f);
        if (NPC.collideX) NPC.velocity.X = 0;
    }
    private void DoAttack(Player player, int pattern) {
        int t = combatTimer, phase = AirbornePhase ? 2 : 1;
        switch (pattern) {
            case 0:
                if (t == 48) SummonAltar(player);
                if (t == 144 || AirbornePhase && t == 240) {
                    Vector2 aim = (attackAim - NPC.Center).SafeNormalize(Vector2.UnitY);
                    FireFan(NPC.Center + new Vector2(0, -24), aim, 3, .28f, 5.8f, 0, 54);
                    FrostNovaVisuals.Cast(NPC.Center, 0, .85f);
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

                break;
            case 3:
                if (t == 0) { AnnounceSupport(); QuietForSupport(); }
                if (t == 36) SummonSnowSupport(player);
                // A full dedicated support window: no extra fan, altar or pressure salvo.
                break;
            case 4:
                if (t == 40) SpawnIceRing(attackAim + new Vector2(0, -180), 3, attackAim, 60);

                break;
            case 5:
                if (t == 36) SpawnIceRing(attackAim + new Vector2(0, -260), 2, attackAim, 60);
                break;
            case 6:
            case 9:
            case 10:
                if (AirbornePhase) CastRemoteMotion(pattern);
                // Phase one retains its warned local teleport. In phase two,
                // the caster walks while the spell appears above the locked aim.
                break;
            case 7:
                if (t == 36) for (int i = -1; i <= 1; i++) {
                    Vector2 at = attackAim + new Vector2(i * 300, -160 + Math.Abs(i) * 90);
                    Vector2 gap = (attackAim - at).SafeNormalize(Vector2.UnitY);
                    FireShot(at, gap, 2, 72 + (i + 1) * 24);
                }
                break;
            case 8:
                if (t == 36 || t == 156) {
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
    private void CastRemoteMotion(int pattern) {
        int t = combatTimer;
        if (pattern == 10) {
            if (t == 36 || t == 132) {
                Vector2 at = attackAim + new Vector2(t == 36 ? -180 : 180, -420);
                SpawnHazard(at, Vector2.UnitY, ModContent.ProjectileType<FrostNovaBlackIceSpike>(),
                    FrostNovaCombatRules.ShotDamage(2, true), NPC.whoAmI, 72, 600);
                FrostNovaVisuals.Cast(at, 3, 1.1f);
            }
            return;
        }
        if (t != 42 && !(pattern == 9 && t == 150)) return;
        Vector2 center = attackAim + new Vector2(t == 42 ? -260 : 260, -180);
        SpawnHazard(center, (attackAim - center).SafeNormalize(Vector2.UnitY),
            ModContent.ProjectileType<FrostNovaRitualPulse>(), FrostNovaCombatRules.ShotDamage(2), NPC.whoAmI, 1);
        FrostNovaVisuals.Cast(center, 1, 1.1f);
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
        if (!Main.dedServ && FrostNovaVisuals.Visible(at))
            SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/FNIceRingStg2Start")
                { Volume = .65f, MaxInstances = 2 }, at);
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
    private void SummonSnowSupport(Player player) => OpenSupportPortals(player);
    private void SummonAltar(Player player) {
        if (Main.netMode == NetmodeID.MultiplayerClient || PendingSupport() >= FrostNovaCombatRules.SupportCap
            || IceCrystalAltar.CountActiveAltars() >= (AirbornePhase ? 2 : 1)) return;
        Vector2 seed = NPC.Center + new Vector2(attackSerial % 2 == 0 ? -240 : 240, 0);
        if (!TryStandingCenter(seed, IceCrystalAltar.AltarNpcWidth, IceCrystalAltar.AltarNpcHeight, out Vector2 at)) return;
        // The wisp carries a destination, never a scaled flying altar sprite.
        Vector2 bottom = at + new Vector2(0, IceCrystalAltar.AltarNpcHeight * .5f);
        SpawnHazard(NPC.Center, Vector2.Zero, ModContent.ProjectileType<IceAltarSummonWisp>(), 0, bottom.X, bottom.Y);
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
        relocationArmed = false;
        DisableDistanceRing();
        NPC.damage = 0; NPC.dontTakeDamage = true; NPC.velocity.X *= .85f;
        DeathTimer++;
        TickRevivalMotion();
        NPC.life = DeathTimer <= 320 ? 1 : (int)MathHelper.Lerp(1, NPC.lifeMax, MathHelper.SmoothStep(0, 1, (DeathTimer - 320) / 180));
        if (DeathTimer == 325) SpawnHazard(NPC.Center, Vector2.Zero, ModContent.ProjectileType<FrostNovaRevival>(), 0, NPC.direction);
        if (DeathTimer == 411) SpawnHazard(NPC.Center, Vector2.Zero, ModContent.ProjectileType<FrostNovaWhiteRing>(), 0, NPC.direction);
        if (DeathTimer >= FrostNovaCombatRules.RevivalDuration) {
            NPC.life = NPC.lifeMax; FNStage = 2; FNRevival = true; FNRevivalStart = FNRevivalEnd = false;
            ReBirthTimer = 0; NPC.noGravity = NPC.noTileCollide = false;
            ResetCombat(); NPC.netUpdate = true;
        }
    }
    private void TickFinalDeath() {
        relocationArmed = false;
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
        // World-space arena and each player's exposure are owned by the domain system.
        DisableDistanceRing();
    }
    internal float DomainOpacity => Math.Min(1, FNShaderRingTimer / 60f);
    private static void DisableDistanceRing() {
        if (Main.dedServ) return;
        var filter = Terraria.Graphics.Effects.Filters.Scene["FNTwistedRing"];
        if (filter != null && filter.IsActive()) filter.Deactivate();
    }
    private void RetireEncounter() {
        foreach (Projectile p in Main.ActiveProjectiles) {
            if (p.GetGlobalProjectile<FrostNovaOwnedProjectile>().BelongsTo(NPC.whoAmI, encounterToken) && p.ModProjectile is not FrostNovaSmoke) {
                p.GetGlobalProjectile<FrostNovaOwnedProjectile>().Retire(p);
            }
        }
        foreach (NPC n in Main.ActiveNPCs) {
            if (n.GetGlobalNPC<FrostNovaOwnedNPC>().BelongsTo(NPC.whoAmI, encounterToken)) { n.active = false; n.netUpdate = true; }
        }
    }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
        DrawOrbitHalo(spriteBatch, screenPos);
        DrawRelocationWarning(spriteBatch, screenPos);
        if (CombatHazardsActive && CombatPattern == 0 && combatTimer < 48) {
            var crystal = FrostNovaVisuals.Texture("BlackIceIntro");
            Rectangle frame = crystal.Frame(1, 15, 0, Math.Clamp(combatTimer * 15 / 48, 0, 14));
            spriteBatch.Draw(crystal, NPC.Center + new Vector2(NPC.direction * 28, -18) - screenPos,
                frame, Color.White, 0, frame.Size() * .5f, 1.2f, SpriteEffects.None, 0);
        }
        FrostNovaVisuals.DrawBoss(NPC, AirbornePhase, CombatHazardsActive ? combatTimer / (float)FrostNovaCombatRules.AttackDuration(AirbornePhase ? 2 : 1, CombatPattern) : 0, CombatPattern);
        if (AirbornePhase) {
            // The enlarged target must not stretch or bottom-align the original character art.
            var texture = TextureAssets.Npc[Type].Value;
            Main.EntitySpriteDraw(texture, NPC.Center - screenPos + new Vector2(0, NPC.gfxOffY), NPC.frame,
                NPC.GetAlpha(drawColor), NPC.rotation, NPC.frame.Size() * .5f, NPC.scale,
                NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
            return false;
        }
        return true;
    }
}
