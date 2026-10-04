using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.FaustAndMephisto;
using ArknightsMod.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.FaustAndMephisto;

[AutoloadBossHead]
public sealed class Faust : ModNPC
{
    // ai: partner slot + 1 / phase / shot timer / encounter token.
    internal Vector2 Arena, Aim;
    internal int Shot;
    private int supportClock, abandonedTicks;
    private Vector2 lastVisualPosition;
    private bool visualInitialized;
    private bool[] participants;
    internal bool FinalPhase => NPC.ai[1] == 1;
    internal bool Retreating => DuoEncounter.Partner(this)?.Retreating == true;
    public override string LocalizationCategory => "FaustAndMephisto.NPCs";
    public override string Texture => "Terraria/Images/NPC_22";
    public override string BossHeadTexture => "Terraria/Images/NPC_Head_Boss_4";
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 1;
        NPCID.Sets.MPAllowedEnemies[Type] = true;
        NPCID.Sets.MustAlwaysDraw[Type] = true;
    }
    public override void SetDefaults()
    {
        participants = new bool[Main.maxPlayers];
        NPC.width = 32; NPC.height = 52; NPC.aiStyle = -1;
        NPC.lifeMax = DuoRules.Life(DuoRules.FaustMasterLife, false, false);
        NPC.damage = 0; NPC.defense = 20; NPC.boss = true; NPC.dontTakeDamage = true;
        NPC.knockBackResist = 0; NPC.npcSlots = 10; NPC.netAlways = true;
        NPC.value = Item.buyPrice(gold: 4); NPC.HitSound = SoundID.NPCHit1; NPC.DeathSound = SoundID.NPCDeath1;
        if (!Main.dedServ) Music = MusicID.Boss2;
    }
    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) =>
        NPC.lifeMax = DuoRules.Life(DuoRules.FaustMasterLife, Main.expertMode, Main.masterMode, balance);
    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry entry) =>
        entry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.ArknightsMod.FaustAndMephisto.FaustBestiary"));
    public override void OnSpawn(IEntitySource source) => DuoEncounter.Initialize(NPC);
    public override bool CheckActive() => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
    public override bool CanHitNPC(NPC target) => false;
    public override void ModifyNPCLoot(NPCLoot loot) => loot.Add(ItemDropRule.Common(ModContent.ItemType<FaustCrossbow>()));
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) => modifiers.FinalDamage *= DuoRules.TurretMultiplier(DuoEncounter.Turrets(this));
    public override void OnKill()
    {
        DuoEncounter.Cleanup(this);
        if (DuoEncounter.Authority) DownedBossSystem.MarkDowned(ref DownedBossSystem.DownedFaustAndMephisto);
    }
    internal void BeginFinalPhase()
    {
        if (!DuoEncounter.Authority || FinalPhase) return;
        NPC.ai[1] = 1; NPC.ai[2] = -60; Shot = 0; supportClock = 0;
        NPC.dontTakeDamage = false; NPC.netUpdate = true;
    }
    internal void IncludeParticipant(int player) => participants[player] = true;
    private void TargetParticipant()
    {
        Vector2 anchor = DuoEncounter.Partner(this)?.NPC.Center ?? Arena;
        int chosen = Main.maxPlayers; float nearest = float.MaxValue;
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player candidate = Main.player[i];
            if (!DuoEncounter.Alive(candidate)) continue;
            if (candidate.Distance(anchor) < DuoRules.EscapeRadius) participants[i] = true;
            if (!participants[i]) continue;
            float distance = candidate.DistanceSQ(NPC.Center);
            if (distance < nearest) { chosen = i; nearest = distance; }
        }
        if (NPC.target != chosen) { NPC.target = chosen; NPC.netUpdate = true; }
    }
    public override void AI()
    {
        if (DuoEncounter.Authority) TargetParticipant();
        if (NPC.target < 0 || NPC.target >= Main.maxPlayers || !DuoEncounter.Alive(Main.player[NPC.target]))
        {
            if (DuoEncounter.Authority && ++abandonedTicks > 180) { DuoEncounter.Cleanup(this); DuoEncounter.Remove(NPC); }
            return;
        }
        abandonedTicks = 0;
        Player player = Main.player[NPC.target];
        Mephisto partner = DuoEncounter.Partner(this);
        if (DuoEncounter.Authority && !FinalPhase && partner == null) BeginFinalPhase();
        NPC.dontTakeDamage = !FinalPhase || DuoEncounter.Turrets(this) >= 10;
        NPC.velocity.X *= .8f;
        NPC.direction = NPC.spriteDirection = player.Center.X < NPC.Center.X ? -1 : 1;
        UpdateVisuals();
        if (Retreating) return;
        NPC.ai[2]++;
        int timer = (int)NPC.ai[2], interval = DuoRules.ShotInterval(FinalPhase);
        if (timer >= 0 && timer <= interval - DuoRules.HiddenAimTicks)
        {
            Aim = player.Center + player.velocity * 8;
            if (DuoEncounter.Authority && (timer % 12 == 0 || timer == interval - DuoRules.HiddenAimTicks)) NPC.netUpdate = true;
        }
        if (!Main.dedServ && timer == interval - DuoRules.HiddenAimTicks && NPC.target == Main.myPlayer)
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = .55f, Pitch = DuoRules.StrongShot(Shot) ? -.3f : .2f });
        if (!DuoEncounter.Authority) return;
        if (++supportClock % DuoRules.HostInterval == 0)
            DuoEncounter.SpawnSupport(this, ModContent.NPCType<ReunionHost>(),
                (partner?.NPC.Center ?? NPC.Center) + new Vector2(Main.rand.NextBool() ? -160 : 160, 0));
        if (FinalPhase && supportClock % DuoRules.TurretInterval == 0)
            DuoEncounter.SpawnSupport(this, ModContent.NPCType<ReunionBallista>(), NPC.Center + new Vector2(Main.rand.Next(-160, 161), 0));
        if (timer >= interval)
        {
            Vector2 velocity = (Aim - Muzzle).SafeNormalize(Vector2.UnitX) * 40;
            Projectile.NewProjectile(NPC.GetSource_FromAI(), Muzzle, velocity, ModContent.ProjectileType<FaustBolt>(),
                DuoRules.Attack * (DuoRules.StrongShot(Shot) ? 2 : 1), 0, Main.myPlayer,
                NPC.whoAmI + 1, NPC.ai[3], DuoRules.StrongShot(Shot) ? NPC.target + 1 : 0);
            Shot++; NPC.ai[2] = 0; NPC.netUpdate = true;
            if (Shot % 3 == 0) Teleport(player);
        }
    }
    internal Vector2 Muzzle => NPC.Center + new Vector2(NPC.direction * 20, -8);
    private void Teleport(Player player)
    {
        Vector2 origin = NPC.Center;
        // A retreating player cannot drag the sniper indefinitely across the world.
        Vector2 focus = player.Center;
        if (Vector2.Distance(focus, Arena) > 1200) focus = Arena + (focus - Arena).SafeNormalize(Vector2.UnitX) * 1200;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            int side = attempt == 0 ? (NPC.Center.X < player.Center.X ? 1 : -1) : (Main.rand.NextBool() ? 1 : -1);
            Vector2 point = DuoEncounter.GroundNear(focus + new Vector2(side * Main.rand.Next(860, 1160), Main.rand.Next(-120, 30)), NPC.width, NPC.height);
            if (Vector2.Distance(point, origin) < 450 || Vector2.Distance(point, player.Center) < 650) continue;
            if (Collision.SolidCollision(point - NPC.Size * .5f, NPC.width, NPC.height)) continue;
            NPC.Center = point; NPC.velocity = Vector2.Zero; break;
        }
        NPC.ai[2] = -45; NPC.netUpdate = true;
    }
    private void UpdateVisuals()
    {
        if (Main.dedServ) return;
        if (visualInitialized && Vector2.DistanceSquared(lastVisualPosition, NPC.Center) > 300 * 300)
        {
            DuoVisuals.Smoke(lastVisualPosition); DuoVisuals.Smoke(NPC.Center);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = .55f, Pitch = -.35f }, NPC.Center);
        }
        lastVisualPosition = NPC.Center; visualInitialized = true;
        if (!FinalPhase && Main.rand.NextBool(8))
            Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(20, 25), DustID.Smoke, Vector2.Zero, 150, new Color(100, 145, 122), .7f).noGravity = true;
    }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        int timer = (int)NPC.ai[2];
        if (!Retreating && timer >= 0 && DuoRules.AimVisible(timer, FinalPhase))
        {
            float fraction = timer / (float)(DuoRules.ShotInterval(FinalPhase) - DuoRules.HiddenAimTicks);
            Color color = DuoRules.StrongShot(Shot) ? Color.Lerp(Color.IndianRed, new Color(212, 100, 255), MathHelper.Clamp(fraction * 1.5f, 0, 1)) : Color.IndianRed;
            Vector2 direction = (Aim - Muzzle).SafeNormalize(Vector2.UnitX);
            DuoVisuals.Line(spriteBatch, Muzzle - screenPos, Muzzle + direction * 4000 - screenPos, color * (.35f + .35f * fraction), 1.3f);
            DuoVisuals.Ring(spriteBatch, Muzzle - screenPos, 10 + 5 * fraction, color * .8f, 20);
        }
        DuoVisuals.Person(spriteBatch, NPC, screenPos, FinalPhase ? new Color(92, 169, 135) : new Color(99, 143, 130) * .45f, true);
        if (FinalPhase && DuoEncounter.Turrets(this) > 0)
        {
            DuoVisuals.Ring(spriteBatch, NPC.Center - screenPos, 35, new Color(150, 112, 220) * .5f, 32);
            int count = DuoEncounter.Turrets(this);
            for (int i = 0; i < Math.Min(10, count); i++)
            {
                Vector2 point = NPC.Center - screenPos + (-MathHelper.PiOver2 + MathHelper.TwoPi * i / 10).ToRotationVector2() * 35;
                DuoVisuals.Line(spriteBatch, point - new Vector2(2, 0), point + new Vector2(2, 0), new Color(204, 165, 249), 4);
            }
        }
        return false;
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.WriteVector2(Arena); writer.WriteVector2(Aim); writer.Write(Shot); }
    public override void ReceiveExtraAI(BinaryReader reader) { Arena = reader.ReadVector2(); Aim = reader.ReadVector2(); Shot = reader.ReadInt32(); }
}
