using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

[AutoloadBossHead]
public sealed partial class Isharmla : ModNPC
{
    public const string Root = "ArknightsMod/Content/NPCs/Enemy/Isharmla/Assets/";
    private static int nextEncounter;
    public int Encounter, DragonIndex = -1;
    public Vector2 Anchor, Portal, Aim;
    public int Transition, Cast, Serial, MissingPlayers;
    public bool Initialized, DragonSummoned, Victorious;
    public Vector2 PhraseAnchor, DashDirection;
    public int PhraseSide = 1, PhraseSerial = -1;
    public float Gesture, Lean;
    public bool Cleaning { get; private set; }
    public bool Mounted => Stage == TideStage.LastTide || Stage == TideStage.Covenant && Dragon is { Pattern: 3 };
    public int SpawnedShots { get; private set; }
    public int SpawnedLines { get; private set; }
    public TideStage Stage => (TideStage)Math.Max(1, (int)NPC.ai[0]);
    public int Timer => (int)NPC.ai[1];
    public int Attack => (int)NPC.ai[2];
    internal bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
    public IsharmlaDragon Dragon => DragonIndex >= 0 && DragonIndex < Main.maxNPCs && Main.npc[DragonIndex].active &&
        Main.npc[DragonIndex].ModNPC is IsharmlaDragon d && d.Encounter == Encounter ? d : null;
    public override string Texture => Root + "Human";
    public override string BossHeadTexture => Root + "Human";
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 1;
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.MustAlwaysDraw[Type] = true;
    }
    public override void SetDefaults()
    {
        NPC.width = 82; NPC.height = 92; NPC.lifeMax = 270000; NPC.defense = 65; NPC.damage = 130;
        NPC.boss = NPC.noGravity = NPC.noTileCollide = NPC.netAlways = true;
        NPC.aiStyle = -1; NPC.knockBackResist = 0; NPC.npcSlots = 12; NPC.value = Item.buyPrice(gold: 45);
        NPC.HitSound = SoundID.NPCHit5; NPC.DeathSound = SoundID.NPCDeath7;
        if (!Main.dedServ) Music = MusicID.Boss5;
    }
    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) =>
        NPC.lifeMax = (int)(NPC.lifeMax * .7f * balance * bossAdjustment);
    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry entry)
    {
        entry.Info.Add(BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Ocean);
        entry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.ArknightsMod.IsharmlaText.Bestiary"));
    }
    public override void OnSpawn(IEntitySource source)
    {
        if (!Authority) return;
        Encounter = ++nextEncounter; NPC.TargetClosest();
        Player p = Main.player[NPC.target];
        Anchor = new Vector2(MathHelper.Clamp(p.Center.X, 380, Main.maxTilesX * 16 - 380), FindSeaLevel(p));
        NPC.Center = Anchor - new Vector2(0, 82); Aim = p.Center;
        NPC.ai[0] = 1; NPC.ai[1] = -120; Initialized = true; NPC.netUpdate = true;
    }
    private static float FindSeaLevel(Player player)
    {
        int x = Math.Clamp((int)player.Center.X / 16, 10, Main.maxTilesX - 11);
        for (int y = Math.Max(15, (int)Main.worldSurface - 130); y < Math.Min(Main.maxTilesY - 20, Main.worldSurface + 100); y++)
            if (Main.tile[x, y].LiquidAmount > 150 && Main.tile[x, y + 1].LiquidAmount > 150) return y * 16;
        return player.Bottom.Y + 32;
    }
    public override bool CheckActive() => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        cooldownSlot = ImmunityCooldownID.Bosses;
        return Stage == TideStage.LastSong && Transition == 0 && Attack == 2 &&
            (Cast is >= 90 and < 113 || Cast is >= 174 and < 197);
    }
    public override bool CanHitNPC(NPC target) => false;
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        int floor = IsharmlaRules.HumanFloor(NPC.lifeMax, Stage);
        modifiers.SetMaxDamage(IsharmlaRules.HitLimit(NPC.life, floor));
    }
    public override bool CheckDead()
    {
        if (Victorious) return true;
        NPC.life = Math.Max(1, IsharmlaRules.HumanFloor(NPC.lifeMax, Stage)); NPC.netUpdate = true; return false;
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(Encounter); w.Write(DragonIndex); w.WriteVector2(Anchor); w.WriteVector2(Portal); w.WriteVector2(Aim);
        w.Write(Transition); w.Write(Cast); w.Write(Serial); w.Write(Initialized); w.Write(DragonSummoned); w.Write(Victorious);
        w.WriteVector2(PhraseAnchor); w.WriteVector2(DashDirection); w.Write(PhraseSide); w.Write(PhraseSerial);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        Encounter = r.ReadInt32(); DragonIndex = r.ReadInt32(); Anchor = r.ReadVector2(); Portal = r.ReadVector2(); Aim = r.ReadVector2();
        Transition = r.ReadInt32(); Cast = r.ReadInt32(); Serial = r.ReadInt32(); Initialized = r.ReadBoolean();
        DragonSummoned = r.ReadBoolean(); Victorious = r.ReadBoolean();
        PhraseAnchor = r.ReadVector2(); DashDirection = r.ReadVector2(); PhraseSide = r.ReadInt32(); PhraseSerial = r.ReadInt32();
    }
    public override void AI()
    {
        if (!Initialized) return;
        NPC.TargetClosest(faceTarget: false); Player p = Main.player[NPC.target];
        if (!p.active || p.dead || Vector2.DistanceSquared(p.Center, Anchor) > 11000 * 11000)
        {
            NPC.velocity = new Vector2(0, -8);
            if (Authority && ++MissingPlayers > 180) { Cleanup(true); IsharmlaWorld.Deactivate(NPC); }
            return;
        }
        MissingPlayers = 0; NPC.timeLeft = 1800; NPC.ai[1]++;
        if (Authority && Timer % 90 == 0) NPC.netUpdate = true;
        int floor = IsharmlaRules.HumanFloor(NPC.lifeMax, Stage);
        NPC.life = Math.Max(floor, NPC.life);
        NPC.dontTakeDamage = Transition > 0 || !IsharmlaRules.HumanVulnerable(Stage) || NPC.life <= floor;
        NPC.damage = Stage == TideStage.LastSong ? 160 : 130;
        NPC.rotation = 0; NPC.spriteDirection = p.Center.X >= NPC.Center.X ? 1 : -1;
        if (Transition > 0) { RunTransition(p); return; }
        switch (Stage)
        {
            case TideStage.Cantor:
                if (Timer >= 0) CastSpells(p, false);
                if (Authority && NPC.life <= floor && Timer >= 120) ChangeStage(TideStage.Leviathan, IsharmlaRules.ArrivalWarning, p);
                break;
            case TideStage.Leviathan:
                Hover(Anchor + new Vector2(430 * Side(p), -550), .04f, 14);
                if (Authority && Timer >= 180 && Dragon is { } first && first.NPC.life <= IsharmlaRules.DragonFloor(first.NPC.lifeMax, Stage))
                    ChangeStage(TideStage.Covenant, 150, p);
                break;
            case TideStage.Covenant:
                if (Dragon is { } mount)
                {
                    if (Mounted) { NPC.dontTakeDamage |= mount.Visibility < .65f; Ride(mount); }
                    CastTogether(p, mount);
                    if (Authority && Timer >= 240 && NPC.life <= floor && mount.NPC.life <= mount.NPC.lifeMax / 2)
                        ChangeStage(TideStage.LastSong, 150, p);
                }
                break;
            case TideStage.LastSong:
                CastSpells(p, true);
                if (Authority && Timer >= 180 && NPC.life <= 1) ChangeStage(TideStage.LastTide, 150, p);
                break;
            case TideStage.LastTide:
                if (Dragon is { } finalMount)
                {
                    Ride(finalMount);
                    if (finalMount.Dying == 0) CastTogether(p, finalMount);
                }
                break;
            case TideStage.Finale:
                RunFinale(p);
                if (Authority && Timer >= IsharmlaRules.FinaleTime)
                {
                    Victorious = true; Cleanup(true); NPC.dontTakeDamage = false; NPC.life = 0; NPC.checkDead(); NPC.netUpdate = true;
                }
                break;
        }
        if (!Main.dedServ && Stage != TideStage.Finale) IsharmlaVisuals.Ambient(NPC.Center, Stage, Timer);
    }
    internal int Side(Player p) => p.Center.X < Anchor.X ? -1 : 1;
    private void Ride(IsharmlaDragon dragon)
    {
        NPC.Center = dragon.NPC.Center + new Vector2(-35, -125).RotatedBy(dragon.FacingAngle);
        NPC.velocity = Vector2.Zero; Lean = MathHelper.Clamp(dragon.NPC.velocity.X * .009f, -.24f, .24f);
    }
    private void Hover(Vector2 destination, float steering, float speed)
    {
        destination.X = MathHelper.Clamp(destination.X, 120, Main.maxTilesX * 16 - 120);
        destination.Y = MathHelper.Clamp(destination.Y, 250, Main.maxTilesY * 16 - 350);
        NPC.velocity = IsharmlaMotion.Hover(NPC.velocity, destination - NPC.Center, steering, speed);
    }
    private void ChangeStage(TideStage stage, int duration, Player p)
    {
        if (!Authority) return;
        Cleanup(false); NPC.ai[0] = (int)stage; NPC.ai[1] = 0; NPC.ai[2] = 0; Cast = 0;
        Transition = duration; Serial++; Aim = IsharmlaRules.BoundedAim(p.Center); PhraseSerial = -1;
        PhraseAnchor = Aim; Gesture = 0;
        SpawnedShots = SpawnedLines = 0;
        if (stage == TideStage.Leviathan) Portal = new Vector2(p.Center.X, Math.Max(Anchor.Y + 240, p.Center.Y + 500));
        NPC.netUpdate = true;
    }
    private void RunTransition(Player p)
    {
        NPC.dontTakeDamage = true;
        if (Stage == TideStage.Leviathan)
        {
            NPC.Center = Anchor - new Vector2(0, 82); NPC.velocity = Vector2.Zero;
            if (!Main.dedServ && Transition % 90 == 0) IsharmlaPresentation.Cue(0, NPC.Center);
            // The arrival is conveyed by chanting, water effects and shaking, without a HUD countdown or ray corridor.
            if (!Main.dedServ)
            {
                IsharmlaVisuals.Shake((1 - Transition / 300f) * 5);
                IsharmlaVisuals.Transition(NPC.Center, Transition);
            }
            if (Transition == 45 && Authority)
            {
                DragonIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Portal.X, (int)Portal.Y + 680, ModContent.NPCType<IsharmlaDragon>());
                if (DragonIndex < Main.maxNPCs && Main.npc[DragonIndex].ModNPC is IsharmlaDragon dragon)
                {
                    dragon.Bind(this); DragonSummoned = true; Main.npc[DragonIndex].netUpdate = true;
                }
                NPC.netUpdate = true;
            }
        }
        else
        {
            NPC.velocity *= .92f;
            if (Stage == TideStage.LastTide && Dragon is { } mount)
                NPC.Center = Vector2.Lerp(NPC.Center, mount.NPC.Center + new Vector2(-25, -115).RotatedBy(mount.FacingAngle), .055f);
            if (Stage == TideStage.Finale) NPC.velocity = Vector2.Zero;
            else if (!Main.dedServ) IsharmlaVisuals.Transition(NPC.Center, Transition);
        }
        Transition--;
        if (Transition == 0) { NPC.ai[1] = 0; if (Authority) NPC.netUpdate = true; }
    }
    internal void DragonDefeated()
    {
        if (Authority && Stage == TideStage.LastTide) ChangeStage(TideStage.Finale, 90, Main.player[NPC.target]);
    }
    internal void Cleanup(bool removeNpcs)
    {
        if (!Authority) return;
        Cleaning = true;
        foreach (Projectile shot in Main.ActiveProjectiles)
            if (shot.ModProjectile is IsharmlaHazard h && h.OwnerIndex == NPC.whoAmI && h.Encounter == Encounter) shot.Kill();
        foreach (NPC n in Main.ActiveNPCs)
        {
            bool bound = n.ModNPC is IsharmlaSeaborn s && s.OwnerIndex == NPC.whoAmI && s.Encounter == Encounter;
            bound |= n.ModNPC is IsharmlaDrifter drifter && drifter.OwnerIndex == NPC.whoAmI && drifter.Encounter == Encounter;
            if (removeNpcs) bound |= n.ModNPC is IsharmlaDragon d && d.OwnerIndex == NPC.whoAmI && d.Encounter == Encounter ||
                n.ModNPC is IsharmlaSegment part && part.OwnerIndex == NPC.whoAmI && part.Encounter == Encounter;
            if (bound) IsharmlaWorld.Deactivate(n);
        }
        Cleaning = false;
    }
    public override void ModifyNPCLoot(NPCLoot loot)
    {
        loot.Add(ItemDropRule.BossBag(ModContent.ItemType<IsharmlaTreasureBag>()));
        var normal = new LeadingConditionRule(new Conditions.NotExpert());
        normal.OnSuccess(ItemDropRule.OneFromOptions(1, IsharmlaRewards.WeaponTypes));
        normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HeartOfTheTides>())); loot.Add(normal);
    }
    public override void OnKill() { if (Victorious) IsharmlaWorld.MarkDowned(); }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    { IsharmlaVisuals.DrawHuman(spriteBatch, this, screenPos); return false; }
}
