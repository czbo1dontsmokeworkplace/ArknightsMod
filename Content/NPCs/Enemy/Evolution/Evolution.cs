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
    internal float DashWarning;
    internal EvolutionCinematicPlayback Cinema;
    internal int Phase => (int)NPC.ai[0];
    internal int Timer => (int)NPC.ai[1];
    internal int Attack => EvolutionRules.Attack(Phase, (int)NPC.ai[2], Desperate);
    internal bool Transitioning => Phase is 2 or 4;
    internal bool ArenaActive => Phase is 1 or 2;
    private int noPlayerTime, visualPulse;
    private int lastVisualPhase = -1;
    private bool lastVisualDesperate;
    public override string Texture => EvolutionVisuals.Root + "Newborn";
    public override void SetStaticDefaults()
    {
        NPCID.Sets.TrailCacheLength[Type] = 10;
        NPCID.Sets.TrailingMode[Type] = 1;
        NPCID.Sets.MPAllowedEnemies[Type] = true;
    }
    public override void SetDefaults()
    {
        NPC.width = 132; NPC.height = 158;
        NPC.lifeMax = 52000; NPC.defense = 26; NPC.damage = 82;
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
        NPC.damage = (int)(NPC.damage * .8f);
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
        Aim = player.Center;
        NPC.ai[0] = 1; NPC.ai[1] = -90; NPC.netUpdate = true;
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(Encounter); w.Write(Serial); w.Write(Desperate);
        w.WriteVector2(Arena); w.WriteVector2(Aim); w.WriteVector2(DashDirection); w.WriteVector2(Anchor);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        Encounter = r.ReadInt32(); Serial = r.ReadInt32(); Desperate = r.ReadBoolean();
        Arena = r.ReadVector2(); Aim = r.ReadVector2(); DashDirection = r.ReadVector2(); Anchor = r.ReadVector2();
    }
    public override bool CheckActive() => false;
    public override bool CanHitNPC(NPC target) => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        cooldownSlot = ImmunityCooldownID.Bosses;
        return Charging && !Transitioning && Phase != 6;
    }
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
    {
        int threshold = EvolutionRules.Threshold(NPC.lifeMax, Phase);
        if (threshold > 0) modifiers.SetMaxDamage(Math.Max(1, NPC.life - threshold));
    }
    public override bool CheckDead()
    {
        if (Phase == 6 && Timer >= 100) return true;
        if (Phase < 5) { NPC.life = Math.Max(1, EvolutionRules.Threshold(NPC.lifeMax, Phase)); return false; }
        NPC.life = 1;
        if (Main.netMode != NetmodeID.MultiplayerClient) EnterPhase(6);
        return false;
    }
    public override void AI()
    {
        if (Phase == 0) return;
        Charging = false; DashWarning = 0; NPC.damage = 0;
        NPC.dontTakeDamage = Transitioning || Phase == 6 || Timer < -36;
        if (!NPC.HasValidTarget || !Main.player[NPC.target].active || Main.player[NPC.target].dead) NPC.TargetClosest(false);
        if (!NPC.HasValidTarget || NPC.Distance(Main.player[NPC.target].Center) > 7000)
        {
            NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(0, -14), .04f);
            if (++noPlayerTime >= 180 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                ClearBrood(); ClearHazards(); NPC.active = false; NPC.netUpdate = true;
                if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
            }
            return;
        }
        noPlayerTime = 0;
        Player target = Main.player[NPC.target];
        NPC.ai[3]++;
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            if (Phase == 1 && NPC.life <= EvolutionRules.Threshold(NPC.lifeMax, 1)) EnterPhase(2);
            else if (Phase == 3 && NPC.life <= EvolutionRules.Threshold(NPC.lifeMax, 3)) EnterPhase(4);
            else if (Phase == 5 && !Desperate && NPC.life <= NPC.lifeMax * .08f)
            {
                Desperate = true; ClearBrood(); ClearHazards(); Serial++;
                NPC.ai[1] = -36; NPC.ai[2] = 0; NPC.netUpdate = true;
            }
        }
        if (Phase == 6)
        {
            NPC.velocity *= .9f;
            if (Timer >= 100 && Main.netMode != NetmodeID.MultiplayerClient) { NPC.life = 0; NPC.checkDead(); }
        }
        else if (Timer < 0) NPC.velocity *= .94f;
        else if (Transitioning) DoTransition(target);
        else if (Phase == 1) { NPC.Center = Arena; NPC.velocity = Vector2.Zero; DoNewborn(target); }
        else if (NPC.Distance(target.Center) > 1300 && Timer < 25)
        {
            MoveTo(target.Center + new Vector2(NPC.Center.X < target.Center.X ? -480 : 480, -180), 34, .07f);
            NPC.ai[1] = 0;
        }
        else if (Phase == 3) DoEvolved(target);
        else DoPerfect(target);
        NPC.rotation = MathHelper.Lerp(NPC.rotation, MathHelper.Clamp(NPC.velocity.X * .012f, -.25f, .25f), .12f);
        NPC.direction = NPC.spriteDirection = target.Center.X > NPC.Center.X ? 1 : -1;
        if (Charging) NPC.damage = EvolutionRules.ContactDamage(Phase, true);
        NPC.ai[1]++;
        if (Main.netMode == NetmodeID.Server && Timer % 60 == 0) NPC.netUpdate = true;
        if (!Main.dedServ) UpdateVisuals();
    }
    private void EnterPhase(int phase)
    {
        ClearBrood(); ClearHazards(); Serial++;
        NPC.ai[0] = phase; NPC.ai[1] = 0; NPC.ai[2] = 0;
        Anchor = NPC.Center; NPC.velocity *= .25f; NPC.dontTakeDamage = true; NPC.netUpdate = true;
    }
    private void NextAttack()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (Phase == 1) ClearBrood();
        ClearHazards(); Serial++;
        NPC.ai[1] = -(int)((Desperate ? 24 : 36) / EvolutionRules.Aggression(Phase));
        NPC.ai[2]++; Anchor = NPC.Center; NPC.netUpdate = true;
    }
    private void MoveTo(Vector2 point, float maxSpeed = 14, float inertia = .09f)
    {
        Vector2 delta = point - NPC.Center;
        float aggression = EvolutionRules.Aggression(Phase);
        NPC.velocity = Vector2.Lerp(NPC.velocity, delta.SafeNormalize(Vector2.UnitY) * Math.Min(maxSpeed * aggression, delta.Length() * .04f * aggression), inertia);
    }
    private void Hover(Player player, float angle = 0) => MoveTo(player.Center + new Vector2(MathF.Cos(angle + (int)NPC.ai[2] * 2.3f) * 480,
        -240 + MathF.Sin(angle) * 90), Phase == 5 ? 20 : 14);
    internal void Shoot(EvolutionShot kind, Vector2 origin, Vector2 velocity, float parameter = 0, int delay = 42, int lifetime = 230, bool peripheral = false)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        int count = 0;
        foreach (Projectile p in Main.ActiveProjectiles) if (p.ModProjectile is EvolutionHazard h && h.Encounter == Encounter) count++;
        if (count >= EvolutionRules.HazardCap - (peripheral ? EvolutionRules.PeripheralReserve : 0)) return;
        int damage = kind switch { EvolutionShot.Beam => 42, EvolutionShot.Rock => 44, EvolutionShot.Tentacle or EvolutionShot.Eruption => 38, EvolutionShot.CrimsonBomb => 46, EvolutionShot.DashMarker => 0, _ => 32 };
        if (Phase == 5 && damage > 0) damage += 5;
        int index = Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, velocity, ModContent.ProjectileType<EvolutionHazard>(), damage, 0,
            Main.myPlayer, (int)kind, 0, NPC.whoAmI);
        if (Main.projectile.IndexInRange(index) && Main.projectile[index].ModProjectile is EvolutionHazard hazard)
        {
            hazard.Encounter = Encounter; hazard.Serial = Serial; hazard.Target = NPC.target;
            hazard.Parameter = parameter; hazard.Delay = delay; hazard.Lifetime = lifetime; hazard.Projectile.netUpdate = true;
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
        for (int i = 0; i < count; i++) Lob(EvolutionShot.Blood, NPC.Center, player.Center + player.velocity * 20 + new Vector2((i - (count - 1) * .5f) * 100, 30), 65);
    }
    private void Beam(Player player, float offset = 0, bool predict = false, int delay = 48)
    {
        Vector2 aim = player.Center + (predict ? player.velocity * 20 : Vector2.Zero);
        Shoot(EvolutionShot.Beam, NPC.Center, (aim - NPC.Center).SafeNormalize(Vector2.UnitY).RotatedBy(offset), 1850, delay, delay + 28);
    }
    private void ClearHazards()
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is EvolutionHazard h && h.Encounter == Encounter) { p.hostile = false; p.Kill(); }
    }
    internal void ClearBrood()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.ModNPC is EvolutionBroodNPC b && b.Encounter == Encounter)
            { n.ai[3] = -35; n.damage = 0; n.dontTakeDamage = true; n.netUpdate = true; }
    }
    internal void SpawnBrood(EvolutionBrood kind, Vector2 center, int fuse = 360, float gap = 0, float side = 1,
        EvolutionBroodPreset preset = EvolutionBroodPreset.Hunter, int formation = 0)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || Desperate) return;
        int count = 0, same = 0;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.ModNPC is EvolutionBroodNPC b && b.Encounter == Encounter) { count++; if (b.Kind == kind) same++; }
        int typeCap = kind == EvolutionBrood.Abomination ? 1 : kind == EvolutionBrood.GiantSpider ? 2 : kind == EvolutionBrood.Puppet ? 3 : 6;
        if (count >= EvolutionRules.MinionCap(Phase) || same >= typeCap) return;
        int type = kind switch
        {
            EvolutionBrood.Spider => ModContent.NPCType<EvolutionSpider>(), EvolutionBrood.GiantSpider => ModContent.NPCType<EvolutionGiantSpider>(),
            EvolutionBrood.Puppet => ModContent.NPCType<EvolutionPuppet>(), EvolutionBrood.Abomination => ModContent.NPCType<EvolutionAbomination>(),
            EvolutionBrood.Bomb => ModContent.NPCType<EvolutionBomb>(), _ => ModContent.NPCType<EvolutionTumor>()
        };
        center.X = MathHelper.Clamp(center.X, 160, Main.maxTilesX * 16 - 160);
        center.Y = MathHelper.Clamp(center.Y, 160, Main.maxTilesY * 16 - 160);
        int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)center.X, (int)center.Y, type, ai0: NPC.whoAmI, ai2: side);
        if (Main.npc.IndexInRange(index) && Main.npc[index].ModNPC is EvolutionBroodNPC brood)
        {
            brood.Preset = preset; brood.Formation = formation;
            brood.Encounter = Encounter; brood.Fuse = fuse; brood.Gap = gap; brood.FlightAnchor = center; brood.NPC.Center = center; brood.NPC.netUpdate = true;
        }
    }
    public override void ModifyNPCLoot(NPCLoot loot)
    {
        loot.Add(ItemDropRule.Common(ModContent.ItemType<EvolutionOrigin>()));
        loot.Add(ItemDropRule.Common(ModContent.ItemType<EvolutionTerminus>()));
    }
    public override void OnKill() { ClearBrood(); ClearHazards(); EvolutionVisuals.Burst(NPC.Center, 2); }
    private void UpdateVisuals()
    {
        EvolutionCinematics.Update(this);
        Lighting.AddLight(NPC.Center, Phase == 5 ? .85f : .55f, .03f, .07f);
        if (lastVisualPhase != Phase)
        {
            lastVisualPhase = Phase;
            bool emerging = Phase is 3 or 5 && Cinema.ExitStyle != 0;
            visualPulse = emerging ? 0 : 45;
            // The morph already culminates in a roar before its exit; do not double-play it.
            if (!emerging)
            {
                EvolutionImpactSystem.Emit(NPC.Center, 360, Transitioning ? 3 : 4);
                EvolutionVisuals.Burst(NPC.Center, Transitioning ? 1.8f : 1f);
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
        if (Phase == 6 && Timer % 10 == 0) EvolutionVisuals.Burst(NPC.Center + Main.rand.NextVector2Circular(65, 80), 1.1f, false);
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
        float opacity = Phase == 6 ? MathHelper.Clamp(1 - Timer / 105f, 0, 1) : MathHelper.Clamp((NPC.ai[3]) / 60f, .1f, 1);
        Vector2 origin = name == "Perfect" ? new Vector2(119, 112) : name == "Evolved" ? new Vector2(95, 108) : new Vector2(85, 107);
        SpriteEffects flip = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        if (flip != SpriteEffects.None) origin.X = texture.Width - origin.X;
        Vector2 scale = new(breathe * (1 + NPC.velocity.Length() * .003f), 1 / breathe);
        scale *= 1 - charge * .13f;
        Color glow = EvolutionVisuals.Blood;
        EvolutionVisuals.Glow(NPC.Center, glow * (.3f * opacity), new Vector2(260, 320), NPC.rotation);
        if (visualPulse > 0)
            EvolutionVisuals.Ring(NPC.Center, 80 + (45 - visualPulse) * 6, 0,
                EvolutionVisuals.Core * (visualPulse / 90f), 3, false);
        for (int i = 0; i < (Phase >= 3 ? 5 : 2); i++)
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
        Color bodyColor = Color.White * (opacity * (1 - EvolutionCinematics.BodyDissolve(this)));
        if (name == "Evolved") EvolutionVisuals.DrawEvolved(spriteBatch, NPC.Center - screenPos, bodyColor, NPC.rotation, scale, flip);
        else spriteBatch.Draw(texture, NPC.Center - screenPos, null, bodyColor, NPC.rotation, origin, scale, flip, 0);
        if (DashWarning > 0) EvolutionVisuals.AimLine(NPC.Center, NPC.Center + DashDirection * (Phase == 5 ? 1660 : 1160), DashWarning);
        EvolutionCinematics.DrawRevealedBody(this, spriteBatch, screenPos, scale, flip, opacity);
        if ((!Transitioning && visualPulse > 0) || (Transitioning && Timer < 100))
        {
            Texture2D shield = EvolutionVisuals.Asset(Phase == 4 ? "ShieldPerfect" : Timer > 300 ? "ShieldCracked" : "Shield");
            float shieldScale = 250f / shield.Width * (1 + MathF.Sin(time * .16f) * .035f);
            float shieldOpacity = Transitioning ? .75f * (1 - EvolutionCinematicFrame.Smooth(35, 100, Timer)) : .75f;
            spriteBatch.Draw(shield, NPC.Center - screenPos, null, (glow with { A = 0 }) * shieldOpacity, 0, shield.Size() * .5f, shieldScale, SpriteEffects.None, 0);
            EvolutionVisuals.Glow(NPC.Center, EvolutionVisuals.Core * .2f, new Vector2(170));
        }
        return false;
    }
}
