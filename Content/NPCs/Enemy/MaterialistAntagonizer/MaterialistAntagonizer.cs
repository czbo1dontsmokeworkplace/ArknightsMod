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
    internal Vector2 Anchor, Aim, DashDirection;
    internal int Phase => (int)NPC.ai[0];
    internal FlightOrder Order => (FlightOrder)(int)NPC.ai[1];
    internal int Timer => (int)NPC.ai[2];
    internal int Cycle => (int)NPC.ai[3];
    internal bool Paused => Order is FlightOrder.Arrival or FlightOrder.Transition or FlightOrder.Death or FlightOrder.Recover;
    internal bool EscortFire => lostTargetTime == 0 && (Order is FlightOrder.Command or FlightOrder.Bombard);
    internal bool Charging => Order == FlightOrder.Ram && Timer >= 84 && Timer < 114;
    private int lostTargetTime;
    private int observedLife;
    private bool raptorDeployed;
    private int visualRevision = -1, lastVisualTimer;

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
        NPC.ai[0] = 1;
        Enter(FlightOrder.Arrival);
    }
    public override bool CheckActive() => false;
    public override bool CanHitNPC(NPC target) => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        cooldownSlot = ImmunityCooldownID.Bosses;
        return Charging;
    }
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        if (Shield > 0) modifiers.FinalDamage *= .60f;
        if (Order == FlightOrder.Recover) modifiers.FinalDamage *= 1.20f;
        int threshold = MaterialistRules.Threshold(NPC.lifeMax, Phase);
        if (threshold > 0) modifiers.SetMaxDamage(Math.Max(1, NPC.life - threshold));
    }
    private void DamageShield(int damage)
    {
        if (!Authority || Shield <= 0) return;
        Shield = Math.Max(0, Shield - damage);
        if (Shield == 0) { ClearHazards(); NPC.ai[3] = -1; Enter(FlightOrder.Recover); }
        NPC.netUpdate = true;
    }
    internal void EscortDestroyed()
    {
        if (!Authority || Paused) return;
        DamageShield(Math.Max(1, ShieldMax / 3));
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
        // Player-owned hit hooks run on the attacking client. Observe synchronized life on the authority
        // instead so direct attacks and damage-over-time drain the same finite shield in multiplayer.
        if (Authority)
        {
            if (observedLife > NPC.life) DamageShield(observedLife - NPC.life);
            observedLife = NPC.life;
        }
        NPC.damage = Charging ? (Main.masterMode ? 175 : Main.expertMode ? 150 : 105) : 0;
        NPC.dontTakeDamage = Order is FlightOrder.Arrival or FlightOrder.Transition or FlightOrder.Death;
        if (Order == FlightOrder.Death)
        {
            NPC.velocity *= .92f;
            UpdateVisuals();
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
        if (Authority && Order != FlightOrder.Transition && Phase < 3 && NPC.life <= MaterialistRules.Threshold(NPC.lifeMax, Phase))
            ChangePhase();
        Player target = Main.player[NPC.target];
        RunOrder(target);
        NPC.rotation = MathHelper.Lerp(NPC.rotation, MathHelper.Clamp(NPC.velocity.X * .011f, -.19f, .19f), .10f);
        NPC.ai[2]++;
        if (Authority && Timer % 60 == 0) NPC.netUpdate = true;
        UpdateVisuals();
    }
    private void ChangePhase()
    {
        ClearHazards(); RecallWing(); Shield = 0; ShieldMax = 0;
        NPC.ai[0]++; NPC.ai[3] = 0;
        Enter(FlightOrder.Transition);
    }
    private void Enter(FlightOrder order)
    {
        if (!Authority) return;
        NPC.ai[1] = (int)order; NPC.ai[2] = 0;
        Anchor = NPC.Center; Revision++;
        NPC.dontTakeDamage = order is FlightOrder.Arrival or FlightOrder.Transition or FlightOrder.Death;
        if (order is FlightOrder.Ram or FlightOrder.Sweep or FlightOrder.Overload or FlightOrder.Recover) ClearHazards();
        if (order == FlightOrder.Recover) Shield = 0;
        NPC.netUpdate = true;
    }
    private void NextOrder()
    {
        if (!Authority) return;
        NPC.ai[3]++;
        Enter(MaterialistRules.Order(Phase, Cycle));
    }
    private void MoveTo(Vector2 destination, float speed = 12)
    {
        destination.X = MathHelper.Clamp(destination.X, 180, Main.maxTilesX * 16 - 180);
        destination.Y = MathHelper.Clamp(destination.Y, 160, Main.maxTilesY * 16 - 200);
        Vector2 delta = destination - NPC.Center;
        NPC.velocity = Vector2.Lerp(NPC.velocity, delta.SafeNormalize(Vector2.UnitY) * Math.Min(speed, delta.Length() * .045f), .075f);
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
        w.Write(Encounter); w.Write(Shield); w.Write(ShieldMax); w.Write(Wave); w.Write(Revision); w.Write(raptorDeployed);
        w.WriteVector2(Anchor); w.WriteVector2(Aim); w.WriteVector2(DashDirection);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        Encounter = r.ReadInt32(); Shield = r.ReadInt32(); ShieldMax = r.ReadInt32(); Wave = r.ReadInt32(); Revision = r.ReadInt32(); raptorDeployed = r.ReadBoolean();
        Anchor = r.ReadVector2(); Aim = r.ReadVector2(); DashDirection = r.ReadVector2();
    }
    public override void ModifyNPCLoot(NPCLoot loot)
    {
        loot.Add(ItemDropRule.BossBag(ModContent.ItemType<MaterialistTreasureBag>()));
        var normal = new LeadingConditionRule(new Conditions.NotExpert());
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
        if (visualRevision != Revision) { visualRevision = Revision; lastVisualTimer = -1; }
        MaterialistVisuals.UpdateBoss(this, lastVisualTimer);
        lastVisualTimer = Timer;
    }
}
