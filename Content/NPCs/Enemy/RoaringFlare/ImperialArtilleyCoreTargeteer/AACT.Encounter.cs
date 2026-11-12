using System;
using ArknightsMod.Content.BossBars;
using ArknightsMod.Content.Items.Material;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;

[AutoloadBossHead]
public partial class AACT : ModNPC
{
    public override string Texture => "ArknightsMod/Content/NPCs/Enemy/RoaringFlare/ImperialArtilleyCoreTargeteer/AACT";
    internal int Phase => (int)NPC.ai[0];
    internal int Clock => (int)NPC.ai[1];
    internal int Pattern => (int)NPC.ai[2] % HarbingerRules.PatternCount;
    internal float HackStrength => HarbingerRules.Hack(Phase, Pattern, Clock, NPC.ai[3] > 0);
    private Vector2 anchor;
    private bool deathAcknowledged;
    private static int nextEncounter;
    internal int EncounterToken;
    private readonly Vector2[] nativeHistory = new Vector2[30];
    internal ReadOnlySpan<Vector2> NativeHistory => nativeHistory;

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 1;
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.BossBestiaryPriority.Add(Type);
    }
    public override void SetDefaults()
    {
        NPC.width = 180; NPC.height = 80; NPC.lifeMax = 32000; NPC.defense = 42; NPC.damage = 0;
        NPC.boss = true; NPC.noGravity = NPC.noTileCollide = NPC.lavaImmune = true;
        NPC.knockBackResist = 0; NPC.aiStyle = -1; NPC.npcSlots = 10;
        NPC.HitSound = SoundID.NPCHit4; NPC.DeathSound = SoundID.NPCDeath14;
        NPCID.Sets.ImmuneToAllBuffs[Type] = true;
        NPC.BossBar = ModContent.GetInstance<AACTBossBar>();
    }
    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) =>
        NPC.lifeMax = (int)(NPC.lifeMax * .75f * balance);
    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry entry) => entry.Info.AddRange(new IBestiaryInfoElement[] {
        BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
        new FlavorTextBestiaryInfoElement(Language.GetTextValue("Mods.ArknightsMod.Bestiary.AACT")) });
    public override void ModifyNPCLoot(NPCLoot loot)
    {
        loot.Add(ItemDropRule.Common(ModContent.ItemType<IncandescentAlloyBlock>(), 2, 5, 8));
        loot.Add(ItemDropRule.Common(ModContent.ItemType<CrystallineCircuit>(), 2, 5, 8));
        loot.Add(ItemDropRule.Common(ModContent.ItemType<OptimizedDevice>(), 2, 5, 8));
    }
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
    public override void HitEffect(NPC.HitInfo hit) {
        if (Main.dedServ) return;
        int dust = (Clock % 12 / 3) switch { 0 => DustID.PurificationPowder, 1 => DustID.VilePowder, 2 => DustID.Firework_Blue, _ => DustID.Firework_Pink };
        Dust.NewDust(NPC.Center, 10, 10, dust, hit.HitDirection, 0, -1, Color.White);
    }
    public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;
    public override bool CheckDead()
    {
        if (Phase == 4 && Clock >= HarbingerRules.DeathTicks) return true;
        NPC.life = 1; NPC.dontTakeDamage = true;
        NPC.ai[0] = 4; NPC.ai[1] = 0; NPC.ai[3] = 0;
        ClearHazards(); NPC.netUpdate = true;
        return false;
    }
    private void ClearHazards()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.GetGlobalProjectile<HarbingerOwnedProjectile>().BelongsTo(this)) {
                p.damage = 0; p.hostile = false; p.active = false;
            }
            else if (p.ModProjectile is HarbingerHazard hazard && (int)p.ai[2] == NPC.whoAmI + 1) hazard.Retire();
    }
    public override void OnKill()
    {
        if (!Main.dedServ && !deathAcknowledged) { deathAcknowledged = true; HarbingerInterference.ShowError(); }
    }
    public override void AI()
    {
        bool authority = Main.netMode != NetmodeID.MultiplayerClient;
        if (EncounterToken == 0 && authority) { EncounterToken = ++nextEncounter; NPC.netUpdate = true; }
        if (!Main.dedServ) {
            for (int i = nativeHistory.Length - 1; i > 0; i--) nativeHistory[i] = nativeHistory[i - 1];
            nativeHistory[0] = NPC.Center;
        }
        if (Phase == 0 && authority) { NPC.ai[0] = 1; NPC.ai[3] = 100; NPC.netUpdate = true; }
        if (!NPC.HasValidTarget) NPC.TargetClosest(false);
        Player target = Main.player[NPC.target < Main.maxPlayers ? NPC.target : 0];
        if (!target.active || target.dead) {
            NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(0, -18), .04f);
            if (authority) { ClearHazards(); NPC.EncourageDespawn(90); }
            return;
        }
        NPC.timeLeft = 600;
        if (!Main.dedServ) Music = MusicLoader.GetMusicSlot(Mod, Phase <= 1 ? "Sounds/Music/IACTBoss3" : "Assets/OriginalMusic/AACTloop");
        if (Phase == 4) {
            NPC.ai[1]++;
            if (authority && Clock == 1) Native<AACTEndShow>(NPC.Center, 0, NPC.whoAmI + 1);
            if (authority && Clock == 145) { Native<AACTDeathdust>(NPC.Center, 0); Native<IACTScreenWave>(NPC.Center, 0); }
            if (!Main.dedServ && Clock == 120) SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Assets/Sound/ImperialArtilleyCoreTargeteer/AACTendteleport"), NPC.Center);
            NPC.velocity *= .94f;
            NPC.rotation += MathF.Sin(Clock * .4f) * .025f;
            if (!Main.dedServ && Clock >= HarbingerRules.DeathTicks - 3 && !deathAcknowledged) {
                deathAcknowledged = true; HarbingerInterference.ShowError();
                SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Assets/Sound/ImperialArtilleyCoreTargeteer/AACTendboom"), NPC.Center);
                for (int i = 0; i < 70; i++) Dust.NewDustPerfect(NPC.Center, DustID.Firework_Red,
                    Main.rand.NextVector2Circular(15, 15), Scale: 2).noGravity = true;
            }
            if (authority && Clock >= HarbingerRules.DeathTicks) { NPC.life = 0; NPC.checkDead(); NPC.netUpdate = true; }
            return;
        }
        if (authority) {
            int desired = HarbingerRules.Phase(NPC.life / (float)NPC.lifeMax);
            if (desired > Phase) {
                NPC.ai[0] = Math.Min(Phase + 1, 3); NPC.ai[1] = 0; NPC.ai[2] = 0;
                NPC.ai[3] = HarbingerRules.TransitionTicks; NPC.netUpdate = true; ClearHazards();
            }
        }
        if (NPC.ai[3] > 0) {
            if (authority && (int)NPC.ai[3] == HarbingerRules.TransitionTicks - 20) Native<IACTScreenWave>(NPC.Center, 0);
            NPC.ai[3]--; NPC.dontTakeDamage = true;
            NPC.velocity = Vector2.Lerp(NPC.velocity, (target.Center + new Vector2(0, -380) - NPC.Center) * .025f, .05f);
            if (NPC.ai[3] == 1 && !Main.dedServ) SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/AACTTeleport"), NPC.Center);
            return;
        }
        NPC.dontTakeDamage = false;
        NPC.defense = Phase == 1 ? 42 : Phase == 2 ? 32 : 22;
        NPC.ai[1]++;
        float t = Clock / 60f;
        Vector2 offset = Pattern switch {
            0 => new Vector2(MathF.Sin(t * .8f) * 440, -340),
            1 => new Vector2(MathF.Cos(t) * 510, -290 - MathF.Sin(t) * 100),
            2 => new Vector2(MathF.Sin(t * .7f) * 300, -450),
            _ => new Vector2(MathF.Cos(t * 1.1f) * 540, MathF.Sin(t * 1.1f) * 210 - 240) };
        Vector2 wanted = (target.Center + offset - NPC.Center) * .03f;
        if (wanted.Length() > 18) wanted = wanted.SafeNormalize(Vector2.UnitY) * 18;
        NPC.velocity = Vector2.Lerp(NPC.velocity, wanted, .06f);
        NPC.rotation = NPC.velocity.X * .008f;
        if (authority) {
            if (Clock == 1) { anchor = target.Center; NPC.netUpdate = true; }
            Attack(target);
            if (Clock >= HarbingerRules.CycleLength(Phase)) { NPC.ai[1] = 0; NPC.ai[2] = (Pattern + 1) % HarbingerRules.PatternCount; NPC.netUpdate = true; }
            if (Clock % 15 == 0) NPC.netUpdate = true;
        }
    }
    internal int Damage => (Main.masterMode ? 52 : Main.expertMode ? 44 : 36) + (Phase - 1) * 5;
    private void Spawn(Vector2 position, Vector2 velocity, int kind, int damage = -1, float parameter = 0)
    {
        int count = 0;
        foreach (Projectile p in Main.ActiveProjectiles) if (p.ModProjectile is HarbingerHazard) count++;
        if (count >= HarbingerRules.HazardCap) return;
        Projectile.NewProjectile(NPC.GetSource_FromAI(), position, velocity, ModContent.ProjectileType<HarbingerHazard>(),
            damage < 0 ? Damage : damage, 0, Main.myPlayer, kind, parameter, NPC.whoAmI + 1);
    }
    private void Mark(Vector2 at, bool heavy = false) {
        if (heavy) Native<ExplodeAimPro>(at);
        else Native<ExplodeAim>(at);
    }
    private void Native<T>(Vector2 at, int damage = -1, float ai0 = 0) where T : ModProjectile =>
        Projectile.NewProjectile(NPC.GetSource_FromAI(), at, Vector2.Zero, ModContent.ProjectileType<T>(),
            damage < 0 ? Damage : damage, 0, Main.myPlayer, ai0);
    private void Beam(Player player, float angle)
    {
        Vector2 axis = angle.ToRotationVector2();
        Vector2 start = player.Center + axis.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(210, 470) - axis * 1000;
        Vector2 end = start + axis * 2000;
        // Check the whole segment against EVERY player, not just an offset endpoint.
        foreach (Player p in Main.ActivePlayers) if (!p.dead && !HarbingerRules.BeamClear(start, end, p.Center)) return;
        Spawn(start, axis, 4, 0, 2000);
    }
    private void Attack(Player player)
    {
        int c = Clock, phase = Phase;
        if (c > 35 && c < 285 && c % (phase == 1 ? 65 : phase == 2 ? 44 : 28) == 0)
            Beam(player, Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi));
        switch (Pattern) {
            case 0: // Original red targeting frame -> missile -> colored blast area.
                if (c >= 45 && c <= 230 && c % (phase == 1 ? 65 : phase == 2 ? 50 : 40) == 0) {
                    Vector2 aim = player.Center + Vector2.Clamp(player.velocity * 14, new Vector2(-160), new Vector2(160));
                    Mark(aim, phase >= 2);
                }
                break;
            case 1: // Original predictive scanner; guest shells descend through its warnings.
                if (c == 30) { Native<HitboxblueCore>(player.Center, 0); Native<HitboxblueFrame>(player.Center); }
                if (c == 185) Mark(anchor + new Vector2(phase == 1 ? 380 : -380, 0), phase >= 2);
                if (c >= 50 && c <= 225 && c % (phase == 1 ? 60 : phase == 2 ? 45 : 30) == 0) {
                    int count = phase + 1;
                    for (int i = 0; i < count; i++) {
                        Vector2 from = player.Center + new Vector2((i - (count - 1) / 2f) * 210 + Main.rand.NextFloat(-45, 45), -850);
                        Spawn(from, new Vector2(Main.rand.NextFloat(-2, 2), 8), 1);
                    }
                }
                break;
            case 2: // Staggered native artillery grid. Keep a full 380px escape column.
                if (c is 55 or 115 or 175) {
                    int gap = c == 115 ? 1 : -1;
                    for (int i = -2; i <= 2; i++) if (i != gap)
                        Mark(anchor + new Vector2(i * 380, (c / 60 % 2) * 220 - 110), phase == 3);
                }
                break;
            case 3: // Original randomized quadrilateral and its two area-color shaders.
                if (c == 45 || phase >= 2 && c == 165) Native<CollapsedExplodeAim>(player.Center);
                if (c == 90) Native<IACTScreenWave>(NPC.Center, 0);
                if (phase == 3 && c is 110 or 205)
                    Spawn(player.Center + new Vector2(Main.rand.NextFloat(-650, 650), -900), new Vector2(0, 8), 1);
                break;
            case 4: // Rotating bombardment. Every position is locked before a native 60-tick warning.
                if (c is 55 or 135 or 215) {
                    float turn = c / 80f * .6f;
                    for (int i = 0; i < (phase == 1 ? 3 : 4); i++)
                        Mark(anchor + (turn + i * MathHelper.PiOver2).ToRotationVector2() * 440, phase == 3);
                    if (c == 135) Native<IACTScreenWave>(anchor, 0);
                }
                break;
            case 5: // The original mirrored core/rings/prism now also directs a second artillery origin.
                if (c == 1) {
                    Native<ISTG3InCore>(NPC.Center, 0, NPC.whoAmI + 1);
                    Native<ISTG3InRing>(NPC.Center, 0, NPC.whoAmI + 1);
                    Native<ISTG3OutRing>(NPC.Center, 0, NPC.whoAmI + 1);
                    Native<ISTG3OutPrism>(NPC.Center, 0, NPC.whoAmI + 1);
                }
                if (c is 65 or 135 or 205) Mark(2 * player.Center - NPC.Center, phase >= 2);
                if (c == 100) Native<CollapsedHitboxblueCore>(player.Center, 0);
                if (c == 195) Spawn(player.Center + new Vector2(420, -850), new Vector2(-1, 8), 1);
                break;
        }
    }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        HarbingerVisuals.DrawBoss(this, spriteBatch, screenPos);
        return false;
    }
}
