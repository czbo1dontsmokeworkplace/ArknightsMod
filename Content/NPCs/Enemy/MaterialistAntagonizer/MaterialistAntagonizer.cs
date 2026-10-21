using System;
using System.IO;
using ArknightsMod.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer : ModNPC
{
    internal const string AssetRoot = "ArknightsMod/Content/NPCs/Enemy/MaterialistAntagonizer/";
    private static int encounterSequence;
    internal static bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
    internal int Encounter, Shield, ShieldMax, Wave, Revision;
    internal int CoreExposure;
    internal bool EscortsReleased;
    internal bool FinalStand;
    // Shared phase-clear gate: phase 1/2 hold their threshold, phase 3 holds one HP.
    internal int LockedPhaseLife => Phase < 3 ? MaterialistRules.Threshold(NPC.lifeMax, Phase) : 1;
    internal int RaptorNextSlot = 8, RaptorHandoffTicks;
    internal Vector2 Anchor, Aim, DashDirection;
    internal Vector2 FormationCenter;
    internal float ExitAngle;
    internal uint ScoutMoves, ScoutOffsets;
    internal WingPattern ScoutFormation;
    internal int ScoutRevision;
    internal bool ScoutReady => Phase == 1 && Order == FlightOrder.Command && ScoutRevision > 0;
    internal ScoutMove ScoutAttack(int slot) => slot is >= 0 and < 6 ? (ScoutMove)((ScoutMoves >> (slot * 4)) & 15) : ScoutMove.None;
    internal int ScoutOffset(int slot) => (int)((ScoutOffsets >> (slot * 2)) & 3) * 4;
    internal WingPattern Pattern => ScoutReady ? ScoutFormation : MaterialistRules.Pattern(Phase, Cycle);
    internal int OrderDuration => Order == FlightOrder.Command ? MaterialistRules.PatternDuration(Pattern) : MaterialistRules.Duration(Order);
    internal int Phase => (int)NPC.ai[0];
    internal FlightOrder Order => (FlightOrder)(int)NPC.ai[1];
    internal int Timer => (int)NPC.ai[2];
    internal int Cycle => (int)NPC.ai[3];
    internal bool Paused => Order is FlightOrder.Arrival or FlightOrder.Transition or FlightOrder.Death or FlightOrder.Recover;
    internal bool EscortFire => lostTargetTime == 0 && (Order is FlightOrder.Command or FlightOrder.Bombard or FlightOrder.Ram or FlightOrder.Deploy);
    internal bool DeploymentProtection => !EscortsReleased && Order is FlightOrder.Deploy or FlightOrder.Command;
    internal bool Charging => Order == FlightOrder.Ram && Timer >= 170 && Timer < 200;
    private int lostTargetTime;
    private int observedLife;
    private int visualRevision = -1, lastVisualTimer;
    private int visualCoreExposure;

    public override void SetStaticDefaults()
    {
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.MustAlwaysDraw[Type] = true;
        NPCID.Sets.TrailCacheLength[Type] = 10;
        NPCID.Sets.TrailingMode[Type] = 1;
        NPCID.Sets.BossBestiaryPriority.Add(Type);
    }
    public override void SetDefaults()
    {
        NPC.width = 242; NPC.height = 132;
        NPC.lifeMax = MaterialistRules.Life; NPC.defense = MaterialistRules.Defence;
        NPC.damage = 0; NPC.boss = true; NPC.aiStyle = -1;
        NPC.noGravity = NPC.noTileCollide = true; NPC.knockBackResist = 0;
        NPC.npcSlots = 12; NPC.netAlways = true; NPC.lavaImmune = true;
        NPC.value = Item.buyPrice(gold: 18);
        NPC.HitSound = SoundID.NPCHit4; NPC.DeathSound = SoundID.NPCDeath14;
        NPC.BossBar = ModContent.GetInstance<MaterialistBossBar>();
        if (!Main.dedServ) Music = MusicID.Boss3;
    }
    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) =>
        NPC.lifeMax = (int)(NPC.lifeMax * .75f * balance * bossAdjustment);
    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry entry)
    {
        entry.Info.Add(BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface);
        entry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.ArknightsMod.MaterialistText.Bestiary"));
    }
    public override void OnSpawn(IEntitySource source)
    {
        if (!Authority) return;
        Encounter = ++encounterSequence;
        observedLife = NPC.life;
        NPC.TargetClosest(false);
        if (NPC.HasValidTarget) NPC.Center = Main.player[NPC.target].Center + new Vector2(0, -340);
        Anchor = NPC.Center; Aim = NPC.Center + Vector2.UnitY;
        ExitAngle = MathHelper.PiOver2;
        NPC.ai[0] = 1;
        Enter(FlightOrder.Arrival);
    }
    public override bool CheckActive() => false;
    public override bool CanHitNPC(NPC target) => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        cooldownSlot = ImmunityCooldownID.Bosses;
        return false;
    }
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        if (DeploymentProtection) modifiers.FinalDamage *= .10f;
        else
        {
            if (Shield > 0) modifiers.FinalDamage *= .60f;
            if (Order == FlightOrder.Recover || CoreExposure > 0) modifiers.FinalDamage *= 1.20f;
        }
        int threshold = MaterialistRules.Threshold(NPC.lifeMax, Phase);
        if (threshold > 0) modifiers.SetMaxDamage(Math.Max(1, NPC.life - threshold));
    }
    public override void UpdateLifeRegen(ref int damage)
    {
        if (FinalStand)
        {
            NPC.lifeRegen = NPC.lifeRegenCount = damage = 0;
            return;
        }
        if (!DeploymentProtection || NPC.lifeRegen >= 0) return;
        NPC.lifeRegen = (int)(NPC.lifeRegen * .10f);
        damage = Math.Max(1, (int)Math.Ceiling(damage * .10f));
    }
    private void DamageShield(int damage)
    {
        if (!Authority || Shield <= 0) return;
        Shield = Math.Max(0, Shield - damage);
        if (Shield == 0) CoreExposure = 180;
        NPC.netUpdate = true;
    }
    internal void EscortDestroyed()
    {
        if (!Authority || Paused) return;
        DamageShield(Math.Max(1, ShieldMax / 3));
        if (FinalStand && WingCount() == 0) { FinishFinalStand(); return; }
        if (!Paused && WingCount() == 0) { ClearHazards(); NPC.ai[3] = -1; Enter(FlightOrder.Recover); }
    }
    public override bool CheckDead()
    {
        if (Order == FlightOrder.Death && Timer >= MaterialistRules.DeathTicks) return true;
        if (Order is FlightOrder.Death or FlightOrder.Transition)
        {
            NPC.life = Math.Max(1, NPC.life);
            return false;
        }
        if (WingCount() > 0)
        {
            NPC.life = LockedPhaseLife;
            if (Authority) BeginPhaseWait();
            return false;
        }
        NPC.life = Math.Max(1, MaterialistRules.Threshold(NPC.lifeMax, Phase));
        if (Authority)
        {
            if (Phase < 3) ChangePhase();
            else { ClearHazards(); RecallWing(); Shield = 0; Enter(FlightOrder.Death); }
        }
        return false;
    }
    public override void AI()
    {
        if (Encounter == 0) return;
        UpdateLockShield();
        if (Authority && FinalStand && Order != FlightOrder.Death && WingCount() == 0) FinishFinalStand();
        if (FinalStand && Order is not (FlightOrder.Death or FlightOrder.Transition)) NPC.life = LockedPhaseLife;
        if (CoreExposure > 0) CoreExposure--;
        // Player-owned hit hooks run on the attacking client. Observe synchronized life on the authority
        // instead so direct attacks and damage-over-time drain the same finite shield in multiplayer.
        if (Authority)
        {
            if (observedLife > NPC.life) DamageShield(observedLife - NPC.life);
            observedLife = NPC.life;
        }
        NPC.damage = 0;
        if (RaptorHandoffTicks > 0) RaptorHandoffTicks--;
        NPC.dontTakeDamage = FinalStand || Order is FlightOrder.Arrival or FlightOrder.Transition or FlightOrder.Death;
        if (Order == FlightOrder.Death)
        {
            NPC.velocity *= .92f;
            UpdateVisuals();
            if (Authority && Timer % 30 == 0) NPC.netUpdate = true;
            if (Authority && Timer >= MaterialistRules.DeathTicks) { NPC.life = 0; NPC.checkDead(); }
            NPC.ai[2]++;
            return;
        }
        if (!NPC.HasValidTarget || Main.player[NPC.target].dead || NPC.Distance(Main.player[NPC.target].Center) > 4500)
            NPC.TargetClosest(false);
        if (!NPC.HasValidTarget || NPC.Distance(Main.player[NPC.target].Center) > 4500)
        {
            if (Authority && lostTargetTime == 0) { ClearHazards(); RecallWing(); }
            NPC.damage = 0; NPC.dontTakeDamage = true;
            NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(0, -18), .06f);
            if (Authority && ++lostTargetTime >= 150) { ClearHazards(); RecallWing(); Remove(NPC); }
            return;
        }
        lostTargetTime = 0;
        if (Authority && !FinalStand && Order != FlightOrder.Transition && Phase < 3 && NPC.life <= LockedPhaseLife)
        {
            if (WingCount() > 0) BeginPhaseWait();
            else ChangePhase();
        }
        Player target = Main.player[NPC.target];
        if (Authority && !EscortsReleased && Order == FlightOrder.Command &&
            MaterialistTactics.Active(Timer) && DeploymentReady())
        {
            EscortsReleased = true;
            NPC.netUpdate = true;
        }
        UpdateCombatFrame(target);
        UpdateSupport();
        RunOrder(target);
        NPC.rotation = MathHelper.Lerp(NPC.rotation, MathHelper.Clamp(NPC.velocity.X * .011f, -.19f, .19f), .10f);
        NPC.ai[2]++;
        if (Authority && Timer % 60 == 0) NPC.netUpdate = true;
        UpdateVisuals();
    }
    private void ChangePhase()
    {
        FinalStand = false;
        ClearHazards(); RecallWing(); Shield = 0; ShieldMax = 0;
        CoreExposure = 0;
        NPC.ai[0]++; NPC.ai[3] = 0;
        Enter(FlightOrder.Transition);
    }
    private void Enter(FlightOrder order)
    {
        if (!Authority) return;
        ClearHazards();
        NPC.ai[1] = (int)order; NPC.ai[2] = 0;
        if (order is FlightOrder.Arrival or FlightOrder.Transition or FlightOrder.Death or FlightOrder.Recover)
        { SupportMode = SupportAge = 0; SupportTarget = SupportSlot = -1; }
        Anchor = NPC.Center; Revision++;
        if (Phase == 1 && order == FlightOrder.Command) MaterialistScoutPlanner.Roll(this);
        if (NPC.HasValidTarget)
        {
            Player target = Main.player[NPC.target];
            if (!frameReady) UpdateCombatFrame(target);
        }
        NPC.dontTakeDamage = FinalStand || order is FlightOrder.Arrival or FlightOrder.Transition or FlightOrder.Death;
        if (order == FlightOrder.Recover) Shield = 0;
        if (order == FlightOrder.Deploy)
        {
            NPC.ai[3] = 0;
            EscortsReleased = false;
            RaptorNextSlot = 8; RaptorHandoffTicks = 0;
        }
        NPC.netUpdate = true;
    }
    private void NextOrder()
    {
        if (!Authority) return;
        NPC.ai[3]++;
        if (FinalStand) { Enter(FlightOrder.Command); return; }
        Enter(MaterialistRules.Order(Phase, Cycle));
    }
    private void FinishFinalStand()
    {
        if (Phase < 3) { ChangePhase(); return; }
        ClearHazards();
        Shield = ShieldMax = 0;
        Enter(FlightOrder.Death);
    }
    private void BeginPhaseWait()
    {
        if (!Authority) return;
        FinalStand = true;
        NPC.life = LockedPhaseLife;
        NPC.dontTakeDamage = true;
        NPC.netUpdate = true;
    }
    internal static MaterialistAntagonizer Owner(int index, int encounter) => index >= 0 && index < Main.maxNPCs &&
        Main.npc[index].active && Main.npc[index].ModNPC is MaterialistAntagonizer boss && boss.Encounter == encounter ? boss : null;
    internal static void Remove(NPC npc)
    {
        if (!Authority) return;
        npc.active = false;
        if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(Encounter); w.Write(Shield); w.Write(ShieldMax); w.Write(Wave); w.Write(Revision);
        w.WriteVector2(Anchor); w.WriteVector2(Aim); w.WriteVector2(DashDirection);
        w.WriteVector2(FormationCenter); w.Write(ExitAngle);
        w.Write(CoreExposure);
        w.WriteVector2(CoreAim); w.WriteVector2(CoreSpreadAim);
        w.Write(ScoutMoves); w.Write(ScoutOffsets); w.Write((int)ScoutFormation); w.Write(ScoutRevision);
        w.WriteVector2(FormationVelocity); w.WriteVector2(TrackingVelocity);
        w.Write(SupportMode); w.Write(SupportAge); w.Write(SupportTarget); w.Write(SupportSlot); w.WriteVector2(SupportOrigin); w.Write(SuppressedCoreRound);
        w.Write(EscortsReleased); w.Write(RaptorNextSlot); w.Write(RaptorHandoffTicks);
        w.Write(FinalStand);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        Encounter = r.ReadInt32(); Shield = r.ReadInt32(); ShieldMax = r.ReadInt32(); Wave = r.ReadInt32(); Revision = r.ReadInt32();
        Anchor = r.ReadVector2(); Aim = r.ReadVector2(); DashDirection = r.ReadVector2();
        Vector2 receivedFrame = r.ReadVector2(); float receivedExit = r.ReadSingle();
        if (!frameReady) { FormationCenter = receivedFrame; ExitAngle = receivedExit; frameReady = true; }
        else
        {
            frameCorrection = receivedFrame - FormationCenter;
            ExitAngle += MathHelper.WrapAngle(receivedExit - ExitAngle) * .2f;
        }
        CoreExposure = r.ReadInt32();
        CoreAim = r.ReadVector2(); CoreSpreadAim = r.ReadVector2();
        ScoutMoves = r.ReadUInt32(); ScoutOffsets = r.ReadUInt32(); ScoutFormation = (WingPattern)r.ReadInt32(); ScoutRevision = r.ReadInt32();
        FormationVelocity = Vector2.Lerp(FormationVelocity, r.ReadVector2(), .35f);
        TrackingVelocity = Vector2.Lerp(TrackingVelocity, r.ReadVector2(), .35f);
        SupportMode = r.ReadInt32(); SupportAge = r.ReadInt32(); SupportTarget = r.ReadInt32(); SupportSlot = r.ReadInt32(); SupportOrigin = r.ReadVector2(); SuppressedCoreRound = r.ReadInt32();
        EscortsReleased = r.ReadBoolean(); RaptorNextSlot = r.ReadInt32(); RaptorHandoffTicks = r.ReadInt32();
        FinalStand = r.ReadBoolean();
    }
    public override void ModifyNPCLoot(NPCLoot loot)
    {
        loot.Add(ItemDropRule.BossBag(ModContent.ItemType<MaterialistTreasureBag>()));
        var normal = new LeadingConditionRule(new Conditions.NotExpert());
        normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<global::ArknightsMod.Content.Items.MaterialistArsenal.FighterDispatchTerminal>(), 1));
        normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<global::ArknightsMod.Content.Items.MaterialistArsenal.MiniGatling>(), 1));
        normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<global::ArknightsMod.Content.Items.MaterialistArsenal.OrbitalLaser>(), 1));
        normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<global::ArknightsMod.Content.Items.MaterialistArsenal.RotorBladeCraft>(), 1));
        normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DroneCommandCore>()));
        normal.OnSuccess(ItemDropRule.Common(ItemID.ChlorophyteBar, 1, 12, 18));
        loot.Add(normal);
    }
    public override void BossLoot(ref int potionType) => potionType = ItemID.GreaterHealingPotion;
    public override void OnKill()
    {
        ClearHazards(); RecallWing();
        DownedBossSystem.MarkDowned(ref DownedBossSystem.DownedMaterialistAntagonizer);
    }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        MaterialistVisuals.DrawBoss(this, spriteBatch, screenPos, drawColor);
        return false;
    }
    private void UpdateVisuals()
    {
        if (Main.dedServ) return;
        if (CoreExposure > 0 && visualCoreExposure == 0)
        {
            MaterialistVisuals.Burst(NPC.Center, MaterialistVisuals.Cyan, 1.4f);
            MaterialistEffects.Pulse(NPC.Center, 260, MaterialistVisuals.Cyan, 2);
        }
        visualCoreExposure = CoreExposure;
        if (visualRevision != Revision) { visualRevision = Revision; lastVisualTimer = -1; }
        MaterialistVisuals.UpdateBoss(this, lastVisualTimer);
        lastVisualTimer = Timer;
    }
}
