using System;
using System.IO;
using ArknightsMod.Content.Items.Evolution;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// ai[0]: phase, ai[1]: attack clock, ai[2]: cycle position, ai[3]: encounter clock.
[AutoloadBossHead]
public sealed partial class Evolution : ModNPC
{
    private static int encounterCounter;
    internal int Encounter, Serial;
    internal Vector2 Arena, Aim, DashDirection, Anchor;
    internal bool Desperate, Charging;
    internal int DashVariant = -1;
    internal float DashWarning;
    internal EvolutionCinematicPlayback Cinema;
    internal int Phase => (int)NPC.ai[0];
    internal int Timer => (int)NPC.ai[1];
    internal int Attack => EvolutionRules.Attack(Phase, (int)NPC.ai[2], Desperate);
    internal bool Transitioning => Phase is 2 or 4;
    internal bool ArenaActive => Phase == 1;
    internal bool CorridorActive => Phase is 2 or 3;
    internal float CorridorTop => Arena.Y - EvolutionRules.CorridorHeight * .5f;
    internal float CorridorBottom => Arena.Y + EvolutionRules.CorridorHeight * .5f;
    internal Color ThemeColor => Phase == 1 ? EvolutionVisuals.Sand : Phase == 2 ?
        Color.Lerp(EvolutionVisuals.Sand, EvolutionVisuals.Blood, EvolutionPresentationRules.MorphColor(Timer)) : EvolutionVisuals.Blood;
    internal float CorridorVisibility => Phase == 2 ? EvolutionPresentationRules.BoundaryFade(Timer) : 1;
    internal bool CorridorEnforced => Phase == 3 || Phase == 2 && Timer >= EvolutionPresentationRules.BoundaryReady;
    private int noPlayerTime, visualPulse;
    private bool PhaseInvulnerable => Transitioning || Phase == 6 || Timer < -36 || AxisHealthLocked || CrashOpeningActive;
    internal bool Intangible => NPC.dontTakeDamage || PhaseInvulnerable;
    private int lastVisualPhase = -1;
    private bool lastVisualDesperate;
    public override string Texture => EvolutionVisuals.Root + "Newborn";
    public override void SetStaticDefaults()
    {
        NPCID.Sets.TrailCacheLength[Type] = 10;
        NPCID.Sets.TrailingMode[Type] = 1;
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        // 远端起手时也必须绘制穿过玩家区域的预瞄线，不能跟随本体一起被屏外剔除。
        NPCID.Sets.MustAlwaysDraw[Type] = true;
    }
    public override void SetDefaults()
    {
        NPC.width = 132; NPC.height = 158;
        NPC.lifeMax = 52000; NPC.defense = 26; NPC.damage = EvolutionDamageCockpit.BossContactDamage(3, true);
        NPC.boss = true; NPC.noGravity = NPC.noTileCollide = true;
        NPC.aiStyle = -1; NPC.knockBackResist = 0; NPC.npcSlots = 12;
        NPC.value = Item.buyPrice(gold: 15);
        NPC.HitSound = SoundID.NPCHit8; NPC.DeathSound = SoundID.NPCDeath14;
        NPC.netAlways = true;
        if (!Main.dedServ) Music = MusicID.Boss3;
    }
    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
    {
        NPC.lifeMax = (int)(NPC.lifeMax * .7f * balance * bossAdjustment);
    }
    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry entry)
    {
        entry.Info.Add(BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface);
        entry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.ArknightsMod.EvolutionText.Bestiary"));
    }
    public override void OnSpawn(IEntitySource source)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        Encounter = ++encounterCounter;
        NPC.TargetClosest();
        Player player = Main.player[NPC.target];
        Arena = player.Center + new Vector2(0, -240);
        Arena.X = MathHelper.Clamp(Arena.X, 400, Main.maxTilesX * 16 - 400);
        Arena.Y = MathHelper.Clamp(Arena.Y, 240, Main.maxTilesY * 16 - 400);
        NPC.Center = Anchor = Arena;
        PlaceGroundNest(player);
        Aim = player.Center;
        NPC.ai[0] = 1; NPC.ai[1] = -90; NPC.netUpdate = true;
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(Encounter); w.Write(Serial); w.Write(Desperate);
        w.WriteVector2(Arena); w.WriteVector2(Aim); w.WriteVector2(DashDirection); w.WriteVector2(Anchor);
        w.WriteVector2(PatternOrigin); w.WriteVector2(PatternDrift); w.Write(PatternSerial); w.Write(PatternStart);
        WriteCrashState(w);
        WritePerfectCycle(w);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        Encounter = r.ReadInt32(); Serial = r.ReadInt32(); Desperate = r.ReadBoolean();
        Arena = r.ReadVector2(); Aim = r.ReadVector2(); DashDirection = r.ReadVector2(); Anchor = r.ReadVector2();
        PatternOrigin = r.ReadVector2(); PatternDrift = r.ReadVector2(); PatternSerial = r.ReadInt32(); PatternStart = r.ReadInt32();
        ReadCrashState(r);
        ReadPerfectCycle(r);
    }
    public override bool CheckActive() => false;
    public override bool CanHitNPC(NPC target) => false;
    // Veto the hit itself: invulnerability must never consume penetration or trigger impact effects.
    public override bool? CanBeHitByProjectile(Projectile projectile) => Intangible ? false : null;
    public override bool? CanBeHitByItem(Player player, Item item) => Intangible ? false : null;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        cooldownSlot = ImmunityCooldownID.Bosses;
        return Charging && !CorridorSlamActive && !Transitioning && Phase != 6;
    }
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        if (EnrageDamageReduction) modifiers.FinalDamage *= .7f;
        int threshold = EvolutionRules.Threshold(NPC.lifeMax, Phase);
        if (threshold > 0) modifiers.SetMaxDamage(Math.Max(1, NPC.life - threshold));
    }
    public override void UpdateLifeRegen(ref int damage)
    {
        if (!EnrageDamageReduction || NPC.lifeRegen >= 0) return;
        NPC.lifeRegen = (int)(NPC.lifeRegen * .7f);
        damage = Math.Max(1, (int)(damage * .7f));
    }
    public override bool CheckDead()
    {
        if (AxisHealthLocked || CrashOpeningActive || SpinAttackLocked) { NPC.life = Math.Max(1, NPC.life); return false; }
        if (Phase == 6)
        {
            if (Timer >= EvolutionPresentationRules.DeathDuration) return true;
            NPC.life = Math.Max(1, NPC.life);
            return false;
        }
        if (Phase < 5) { NPC.life = Math.Max(1, EvolutionRules.Threshold(NPC.lifeMax, Phase)); return false; }
        NPC.life = 1;
        if (Main.netMode != NetmodeID.MultiplayerClient) EnterPhase(6);
        return false;
    }
    public override void AI()
    {
        if (Phase == 0) return;
        Charging = false; DashWarning = 0; DashVariant = -1; NPC.damage = 0;
        NPC.dontTakeDamage = PhaseInvulnerable;
        if (!NPC.HasValidTarget || !Main.player[NPC.target].active || Main.player[NPC.target].dead) NPC.TargetClosest(false);
        if (!NPC.HasValidTarget || NPC.Distance(Main.player[NPC.target].Center) > 7000)
        {
            ReleaseCrashPassenger();
            NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(0, -14), .04f);
            if (++noPlayerTime >= 180 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                ClearBrood(); ClearHazards(); NPC.active = false; NPC.netUpdate = true;
                if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
            }
            return;
        }
        noPlayerTime = 0;
        int bodyHeight = Phase <= 2 ? 260 : 158;
        if (NPC.height != bodyHeight) { Vector2 center = NPC.Center; NPC.height = bodyHeight; NPC.Center = center; }
        NPC.noGravity = NPC.noTileCollide = Phase != 1;
        if (Phase == 1) { NPC.velocity.X = 0; Anchor = NPC.Center; }
        Player target = Main.player[NPC.target];
        NPC.ai[3]++;
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            if (Phase == 1 && NPC.life <= EvolutionRules.Threshold(NPC.lifeMax, 1)) EnterPhase(2);
            else if (Phase == 3 && !CrashOpeningActive && NPC.life <= EvolutionRules.Threshold(NPC.lifeMax, 3)) EnterPhase(4);
            else if (Phase == 5 && !Desperate && !AxisHealthLocked && !SpinAttackLocked && NPC.life <= NPC.lifeMax * .08f)
            {
                Desperate = true; ClearBrood(); ClearHazards(); Serial++;
                NPC.ai[1] = -36; NPC.ai[2] = 0; NPC.netUpdate = true;
            }
        }
        if (Phase == 6)
        {
            NPC.velocity *= .9f;
            if (Timer >= EvolutionPresentationRules.DeathDuration && Main.netMode != NetmodeID.MultiplayerClient) { NPC.life = 0; NPC.checkDead(); }
        }
        else if (Timer < 0) NPC.velocity *= .94f;
        else if (Transitioning) DoTransition(target);
        else if (Phase == 1) DoNewborn(target);
        else if (Phase == 3) DoEvolved(target);
        else DoPerfect(target);
        if (CorridorEnforced) ConstrainLowFlight();
        if (!SpinAttackLocked)
        {
            NPC.rotation = NPC.rotation.AngleLerp(MathHelper.Clamp(NPC.velocity.X * .012f, -.25f, .25f), .12f);
            NPC.direction = NPC.spriteDirection = target.Center.X > NPC.Center.X ? 1 : -1;
        }
        if (Charging && !CorridorSlamActive) NPC.damage = (int)(EvolutionRules.ContactDamage(Phase, true, Desperate) * CycleBoost);
        NPC.ai[1]++;
        if (Main.netMode == NetmodeID.Server && Timer % 60 == 0) NPC.netUpdate = true;
        if (!Main.dedServ) UpdateVisuals();
    }
    private void EnterPhase(int phase)
    {
        ReleaseCrashPassenger();
        if (phase == 3) CrashOpeningComplete = false;
        if (phase != 5) CycleEnraged = AxisEscapeRecorded = false;
        if (phase == 2)
        {
            float defaultCenter = MathHelper.Clamp(NPC.Bottom.Y + 96 - EvolutionRules.CorridorHeight * .5f,
                EvolutionRules.CorridorHeight * .5f + 32, Main.maxTilesY * 16 - EvolutionRules.CorridorHeight * .5f - 32);
            // Server selects once; both boundaries share the same bounded lift and are synced.
            Arena.Y = EvolutionCorridorPlacement.SelectCenter(defaultCenter, Main.player[NPC.target].Center.X);
        }
        ClearBrood(phase == 2); ClearHazards(); Serial++;
        NPC.ai[0] = phase; NPC.ai[1] = 0; NPC.ai[2] = 0;
        Anchor = NPC.Center; NPC.velocity *= .25f; NPC.dontTakeDamage = true; NPC.netUpdate = true;
        NPC.noGravity = NPC.noTileCollide = phase != 1;
    }
    private void NextAttack()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (Phase == 1) ClearBrood();
        ClearHazards(); Serial++;
        NPC.ai[1] = -(int)Math.Ceiling(EvolutionRules.Recovery((int)((Desperate ? 24 : 36) / EvolutionRules.Aggression(Phase))) / CycleBoost);
        NPC.ai[2]++; Anchor = NPC.Center; NPC.netUpdate = true;
    }
    private void MoveTo(Vector2 point, float maxSpeed = 14, float inertia = .09f)
    {
        Vector2 delta = point - NPC.Center;
        float aggression = EvolutionRules.Aggression(Phase) * CycleBoost;
        NPC.velocity = Vector2.Lerp(NPC.velocity, delta.SafeNormalize(Vector2.UnitY) * Math.Min(maxSpeed * aggression, delta.Length() * .04f * aggression), inertia);
    }
    private void Hover(Player player, float angle = 0) => MoveTo(PatternCenter + new Vector2(MathF.Cos(angle + (int)NPC.ai[2] * 2.3f) * 560 * CycleBoost,
        (-400 + MathF.Sin(angle) * 120) * CycleBoost), Phase == 5 ? 20 : 14);
    internal void Shoot(EvolutionShot kind, Vector2 origin, Vector2 velocity, float parameter = 0, int delay = 42, int lifetime = 230, bool peripheral = false,
        EvolutionBeamStyle beamStyle = EvolutionBeamStyle.Standard, bool blade = false, bool gentleHoming = false, bool stagedSpirit = false,
        bool quietLaunch = false)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        SpawnHazard(kind, origin, velocity, parameter, delay, lifetime, peripheral, beamStyle, blade, gentleHoming, stagedSpirit, quietLaunch);
        if (Phase != 3 || kind is EvolutionShot.CrashFan or EvolutionShot.Core or EvolutionShot.DashMarker or EvolutionShot.Ricochet) return;
        // Adjacent horizontal score rows already have a playable gap; density copies must not split it.
        if (EvolutionCorridorPressureRules.IsHorizontalLaser(kind, velocity)) return;
        if (corridorDensitySerial != Serial) { corridorDensitySerial = Serial; corridorDensityOrdinal = 0; }
        if (!EvolutionCorridorPressureRules.ExtraShot(++corridorDensityOrdinal)) return;
        Vector2 offset = EvolutionCorridorPressureRules.CompanionOffset(origin, velocity, PatternOrigin, CorridorTop, CorridorBottom);
        SpawnHazard(kind, origin + offset, velocity, parameter, delay, lifetime, peripheral, beamStyle, blade, gentleHoming, stagedSpirit, quietLaunch);
    }
    private int corridorDensitySerial = -1, corridorDensityOrdinal;
    private void SpawnHazard(EvolutionShot kind, Vector2 origin, Vector2 velocity, float parameter, int delay, int lifetime, bool peripheral,
        EvolutionBeamStyle beamStyle, bool blade, bool gentleHoming, bool stagedSpirit, bool quietLaunch)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (Phase == 3 && !EvolutionCorridorPressureRules.LaserLaneAllowed(kind, origin, velocity, PatternOrigin,
            CorridorTop, CorridorBottom, EvolutionLaserVisuals.CollisionWidth(beamStyle, true))) return;
        int count = 0;
        foreach (Projectile p in Main.ActiveProjectiles) if (p.ModProjectile is EvolutionHazard h && h.Encounter == Encounter) count++;
        if (count >= EvolutionRules.HazardCap - (peripheral ? EvolutionRules.PeripheralReserve : 0)) return;
        int damage = EvolutionDamageCockpit.SpawnDamage(kind, beamStyle, CorridorActive, CycleBoost, Main.masterMode);
        if (EvolutionRules.IsLaser(kind) && parameter > 0) parameter *= EvolutionRules.LaserReachMultiplier;
        if (Phase == 3 && kind == EvolutionShot.BroodRocket) parameter = Math.Max(1, (int)MathF.Round(parameter / EvolutionCorridorPressureRules.Speed));
        if (kind is EvolutionShot.Lance or EvolutionShot.Tentacle && parameter > 0) parameter *= EvolutionRules.LaserReachMultiplier;
        velocity = EvolutionRules.ShotVelocity(kind, velocity);
        if (Phase == 3 && kind is not (EvolutionShot.Beam or EvolutionShot.Spike or EvolutionShot.Eruption or EvolutionShot.Tentacle or EvolutionShot.CrashFan))
            velocity *= EvolutionCorridorPressureRules.Speed;
        if (kind is not (EvolutionShot.Beam or EvolutionShot.Spike or EvolutionShot.Eruption or EvolutionShot.Tentacle or EvolutionShot.CrashFan))
            velocity *= CycleBoost;
        int originalDelay = delay;
        delay = EvolutionRules.ProjectileWarning(kind, delay, Phase == 5, CycleBoost);
        if (Phase == 3 && kind == EvolutionShot.CrashFan)
            delay = EvolutionCorridorPressureRules.FanDelay(delay);
        lifetime += delay - originalDelay;
        if (kind == EvolutionShot.Ricochet && CorridorActive)
            lifetime = EvolutionRicochetRules.Lifetime(origin, velocity, delay, CorridorTop, CorridorBottom);
        if (EvolutionRules.IsLaser(kind))
        {
            int active = beamStyle is EvolutionBeamStyle.Pulse or EvolutionBeamStyle.PianoStaccato ? 6 :
                beamStyle==EvolutionBeamStyle.Piano ? EvolutionPianoRules.Active : CorridorActive?32:10;
            int fade = EvolutionPianoRules.IsStyle(beamStyle) ? EvolutionPianoRules.Fade : EvolutionLaserVisuals.FadeTicks;
            lifetime = Math.Max(lifetime, EvolutionRules.FireTime(kind, delay) + active + fade + 16);
        }
        if (EvolutionRules.IsLaser(kind)) lifetime += EvolutionRules.LaserFadeTicks / 2 - 4;
        if (beamStyle == EvolutionBeamStyle.Axis) lifetime = EvolutionRules.FireTime(kind, delay) + EvolutionRules.AxisActiveTicks + 24;
        int index = Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, velocity, ModContent.ProjectileType<EvolutionHazard>(), damage, 0,
            Main.myPlayer, (int)kind, 0, NPC.whoAmI);
        if (Main.projectile.IndexInRange(index) && Main.projectile[index].ModProjectile is EvolutionHazard hazard)
        {
            hazard.Encounter = Encounter; hazard.Serial = Serial; hazard.Target = NPC.target; hazard.ArsenalOrigin = origin;
            hazard.Parameter = parameter; hazard.Delay = delay; hazard.Lifetime = lifetime;
            if (hazard.IsBloodglowCompanion) { hazard.Projectile.hostile = false; hazard.Projectile.damage = 0; }
            hazard.PowerScale = CycleBoost * (Phase == 3 ? EvolutionCorridorPressureRules.Speed : 1); hazard.Projectile.netUpdate = true;
            hazard.BeamStyle = beamStyle; hazard.Blade = blade; hazard.GentleHoming = gentleHoming; hazard.StagedSpirit = stagedSpirit;
            hazard.QuietLaunch = quietLaunch;
            hazard.BurstAngle = (PatternCenter - origin).ToRotation() + MathHelper.Pi / 12;
            hazard.Projectile.timeLeft = Math.Max(600, lifetime + 30);
        }
    }
    internal void Lob(EvolutionShot kind, Vector2 start, Vector2 end, float flight = 70)
    {
        float gravity = kind == EvolutionShot.Rock ? .3f : .21f;
        Vector2 velocity = (end - start) / flight;
        velocity.Y -= gravity * flight * .5f;
        Shoot(kind, start, velocity, lifetime: kind == EvolutionShot.Rock ? 300 : 210);
    }
    internal void TentacleAt(Vector2 point, int delay = 48) => Shoot(EvolutionShot.Tentacle, point + new Vector2(0, 180), -Vector2.UnitY, 360, delay, delay + 44);
    private void BloodFan(Player player, int count)
    {
        for (int i = 0; i < count; i++) Lob(EvolutionShot.Blood, NPC.Center, FormationLanding(i, count), 75);
    }
    private void Beam(Player player, float offset = 0, bool predict = false, int delay = 48)
    {
        Vector2 aim = PatternCenter;
        Shoot(EvolutionShot.Beam, NPC.Center, (aim - NPC.Center).SafeNormalize(Vector2.UnitY).RotatedBy(offset), 1850, delay, delay + 28);
    }
    private void ClearHazards()
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is EvolutionHazard h && h.Encounter == Encounter) { p.hostile = false; p.Kill(); }
    }
    internal void ClearBrood(bool molting = false)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.ModNPC is EvolutionBroodNPC b && b.Encounter == Encounter)
            { b.Molting = molting; n.ai[3] = molting ? -EvolutionPresentationRules.BroodMoltDuration : -35; n.damage = 0; n.dontTakeDamage = true; n.netUpdate = true; }
    }
    internal void SpawnBrood(EvolutionBrood kind, Vector2 center, int fuse = 360, float gap = 0, float side = 1,
        EvolutionBroodPreset preset = EvolutionBroodPreset.Hunter, int formation = 0)
    {
        if (!CanSpawnBrood(kind)) return;
        int type = Phase >= 4 ? ModContent.NPCType<EvolutionSupportOrb>() : kind switch
        {
            EvolutionBrood.Spider => ModContent.NPCType<EvolutionSpider>(), EvolutionBrood.GiantSpider => ModContent.NPCType<EvolutionGiantSpider>(),
            EvolutionBrood.Puppet => ModContent.NPCType<EvolutionPuppet>(), EvolutionBrood.Abomination => ModContent.NPCType<EvolutionAbomination>(),
            EvolutionBrood.Bomb => ModContent.NPCType<EvolutionBomb>(), _ => ModContent.NPCType<EvolutionTumor>()
        };
        SpawnAllowedBrood(kind, center, fuse, gap, side, preset, formation, type);
    }
    internal bool CanSpawnBrood(EvolutionBrood kind)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || Desperate && !(Phase == 5 && kind == EvolutionBrood.Puppet)) return false;
        // 完美形态只允许独立的辅助光球类型，不能重新生出地面敌人。
        if (Phase >= 4 && kind != EvolutionBrood.Puppet) return false;
        int count = 0, same = 0;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.ModNPC is EvolutionBroodNPC b && b.Encounter == Encounter) { count++; if (b.Kind == kind) same++; }
        int typeCap = kind == EvolutionBrood.Abomination ? 1 : kind == EvolutionBrood.GiantSpider ? 2 : kind == EvolutionBrood.Puppet ? 3 : 6;
        return count < EvolutionRules.MinionCap(Phase) && same < typeCap;
    }
    private void SpawnAllowedBrood(EvolutionBrood kind, Vector2 center, int fuse, float gap, float side,
        EvolutionBroodPreset preset, int formation, int type)
    {
        center.X = MathHelper.Clamp(center.X, 160, Main.maxTilesX * 16 - 160);
        center.Y = MathHelper.Clamp(center.Y, 160, Main.maxTilesY * 16 - 160);
        if (Phase <= 3)
        {
            int width = kind == EvolutionBrood.GiantSpider ? 58 : kind == EvolutionBrood.Abomination ? 66 : 36;
            int height = EvolutionBroodNPC.GroundHeight(kind);
            if (CorridorActive) center.Y = MathHelper.Clamp(center.Y, CorridorTop + height, CorridorBottom - height * .5f - 6);
            if (Collision.SolidCollision(center - new Vector2(width, height) * .5f, width, height)) return;
        }
        int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)center.X, (int)center.Y, type, ai0: NPC.whoAmI, ai2: side);
        if (Main.npc.IndexInRange(index) && Main.npc[index].ModNPC is EvolutionBroodNPC brood)
        {
            if (Phase == 3)
            {
                brood.NPC.lifeMax = (int)(brood.NPC.lifeMax * 1.3f);
                brood.NPC.life = brood.NPC.lifeMax;
            }
            brood.Preset = preset; brood.Formation = formation;
            brood.Encounter = Encounter; brood.Fuse = fuse; brood.Gap = gap; brood.FlightAnchor = center; brood.NPC.Center = center; brood.NPC.netUpdate = true;
            brood.NPC.dontTakeDamage = true;
            brood.NPC.chaseable = !brood.SupportOnly;
            if (Phase <= 3) { brood.InitializeGroundBody(); brood.NPC.Center = center; }
        }
    }
    public override void ModifyNPCLoot(NPCLoot loot)
    {
        // All difficulties use the bag; expert/master retain vanilla per-player bag distribution.
        loot.Add(ItemDropRule.BossBag(ModContent.ItemType<EvolutionTreasureBag>()));
        loot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<EvolutionTreasureBag>()));
    }
    public override void OnKill() { ReleaseCrashPassenger(); ClearBrood(); ClearHazards(); EvolutionVisuals.Burst(NPC.Center, 2,
        density: EvolutionPresentationRules.DeathDensity, opacity: EvolutionPresentationRules.DeathOpacity, intensity: EvolutionPresentationRules.DeathIntensity); }
    private void UpdateVisuals()
    {
        UpdateCycleVisuals();
        UpdateBloodSpectacle();
        EvolutionCinematics.Update(this);
        Lighting.AddLight(NPC.Center, ThemeColor.ToVector3() * (Phase == 5 ? .95f : .65f));
        if (lastVisualPhase != Phase)
        {
            lastVisualPhase = Phase;
            bool emerging = Phase == 2 || Phase is 3 or 5 && Cinema.ExitStyle != 0;
            visualPulse = emerging || Phase == 6 ? 0 : 45;
            // The morph already culminates in a roar before its exit; do not double-play it.
            if (!emerging && Phase != 6)
            {
                EvolutionImpactSystem.Emit(NPC.Center, 360, Transitioning ? 3 : 4);
                EvolutionVisuals.Burst(NPC.Center, Transitioning ? 1.8f : 1f, color: ThemeColor);
                SoundEngine.PlaySound(SoundID.Roar with { Volume = .6f, Pitch = -.5f }, NPC.Center);
            }
        }
        if (Desperate && !lastVisualDesperate)
        {
            lastVisualDesperate = true; visualPulse = 45;
            EvolutionImpactSystem.Emit(NPC.Center, 680, 7, true, 32);
            EvolutionVisuals.Burst(NPC.Center, 2f);
        }
        if (visualPulse > 0) visualPulse--;
        if (Charging && Timer % 4 == 0) EvolutionVisuals.Burst(NPC.Center - NPC.velocity * 3, .35f, false);
        if (Phase == 6) UpdateDeathDisplay();
    }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        string name = Phase >= 5 ? "Perfect" : Phase >= 3 ? "Evolved" : "Newborn";
        Texture2D texture = EvolutionVisuals.Asset(name);
        float time = NPC.ai[3];
        int cinematicTime = EvolutionRules.CinematicTime(Phase, Timer);
        float charge = Transitioning ? new EvolutionCinematicFrame(Phase, cinematicTime, NPC.Center).Charge : 0;
        float breathingPhase = Transitioning ? cinematicTime * .065f + 14 * EvolutionCinematicFrame.Smooth(360, 568, cinematicTime) : time * (Phase >= 5 ? .13f : .065f);
        float breathe = 1 + MathF.Sin(breathingPhase) * (.025f + charge * .018f);
        float opacity = Phase == 6 ? 1 - EvolutionCinematicFrame.Smooth(70 + EvolutionPresentationRules.DeathLead, EvolutionPresentationRules.DeathDuration, Timer) : MathHelper.Clamp((NPC.ai[3]) / 60f, .1f, 1);
        Vector2 origin = name == "Perfect" ? new Vector2(119, 112) : name == "Evolved" ? new Vector2(95, 108) : new Vector2(85, 107);
        if (Phase == 1) origin.Y = texture.Height - 8;
        float liftBlend = Phase == 2 ? EvolutionCinematicFrame.Smooth(0, 180, Timer) : 1;
        if (Phase == 2) origin.Y = MathHelper.Lerp(texture.Height - 8, origin.Y, liftBlend);
        SpriteEffects flip = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        if (flip != SpriteEffects.None) origin.X = texture.Width - origin.X;
        Vector2 scale = new(breathe * (1 + NPC.velocity.Length() * .003f), 1 / breathe);
        if (Phase == 1) scale = new Vector2(1 + MathF.Sin(time * .045f) * .012f, 1);
        scale *= 1 - charge * .13f;
        Color glow = ThemeColor;
        EvolutionVisuals.Glow(NPC.Center, glow * (.3f * opacity), new Vector2(260, 320), NPC.rotation);
        if (visualPulse > 0)
            EvolutionVisuals.Ring(NPC.Center, 80 + (45 - visualPulse) * 6, 0,
                EvolutionVisuals.Core * (visualPulse / 90f), 3, false);
        for (int i = 0; i < (Phase == 1 ? 0 : Phase >= 3 ? 5 : 2); i++)
        {
            Vector2 start = NPC.Center + new Vector2((i - 2) * 17, 53).RotatedBy(NPC.rotation);
            Vector2 end = start + new Vector2(MathF.Sin(time * .045f + i) * 55 - NPC.velocity.X * 3, 90 + i * 8);
            EvolutionVisuals.Tendril(start, end, MathF.Cos(time * .04f + i) * 32, time + i * 30, 15, opacity * .75f);
        }
        if (NPC.velocity.Length() > 14)
            for (int i = NPC.oldPos.Length - 1; i >= 1; i--)
                if (NPC.oldPos[i] != Vector2.Zero)
                {
                    Color trailColor = (glow with { A = 0 }) * ((1 - i / (float)NPC.oldPos.Length) * .35f * opacity);
                    Vector2 trailPosition = NPC.oldPos[i] + NPC.Size * .5f - screenPos;
                    if (name == "Evolved") EvolutionVisuals.DrawEvolved(spriteBatch, trailPosition, trailColor, NPC.rotation, scale, flip);
                    else spriteBatch.Draw(texture, trailPosition, null, trailColor, NPC.rotation, origin, scale, flip, 0);
                }
        EvolutionCinematics.DrawBodyAura(this, texture, origin, scale, flip);
        Color bodyTint = Phase == 1 ? new Color(255, 235, 190) : Phase == 2 ?
            Color.Lerp(new Color(255, 235, 190), new Color(255, 120, 135), EvolutionPresentationRules.MorphColor(Timer)) : Color.White;
        Color bodyColor = bodyTint * (opacity * (1 - EvolutionCinematics.BodyDissolve(this)));
        DrawCycleOutline(spriteBatch, texture, NPC.Center - screenPos, origin, scale, flip, opacity);
        if (name == "Evolved") EvolutionVisuals.DrawEvolved(spriteBatch, NPC.Center - screenPos, bodyColor, NPC.rotation, scale, flip);
        else spriteBatch.Draw(texture, (Phase == 1 ? NPC.Bottom : Phase == 2 ? Vector2.Lerp(NPC.Bottom, NPC.Center, liftBlend) : NPC.Center) - screenPos, null, bodyColor, NPC.rotation, origin, scale, flip, 0);
        if (DashWarning > 0)
        {
            EvolutionVisuals.AimLine(NPC.Center, NPC.Center + DashDirection * EvolutionRules.ChargeDistance(Phase == 5, Desperate) * CycleBoost, DashWarning);
            // 本体从远处起手，变体标记放在玩家附近的预瞄线上，确保能读到。
            EvolutionVisuals.ChargeVariantWarning(Vector2.Lerp(NPC.Center, Aim, .72f), DashDirection, DashVariant, DashWarning);
        }
        DrawCrashOpening();
        DrawPianoKeyboard();
        DrawFleetWarnings();
        if (Phase == 6) DrawDeathDisplay();
        EvolutionCinematics.DrawRevealedBody(this, spriteBatch, screenPos, scale, flip, opacity);
        if ((Phase != 1 && !Transitioning && visualPulse > 0) || (Transitioning && Timer < 100) || AxisHealthLocked)
        {
            Texture2D shield = EvolutionVisuals.Asset(AxisHealthLocked || Phase == 4 ? "ShieldPerfect" : Timer > 300 ? "ShieldCracked" : "Shield");
            float shieldScale = 250f / shield.Width * (1 + MathF.Sin(time * .16f) * .035f);
            float shieldOpacity = AxisHealthLocked ? .48f : Transitioning ? .75f * (1 - EvolutionCinematicFrame.Smooth(35, 100, Timer)) : .75f;
            spriteBatch.Draw(shield, NPC.Center - screenPos, null, (glow with { A = 0 }) * shieldOpacity, 0, shield.Size() * .5f, shieldScale, SpriteEffects.None, 0);
            EvolutionVisuals.Glow(NPC.Center, EvolutionVisuals.Core * .2f, new Vector2(170));
        }
        return false;
    }
}
