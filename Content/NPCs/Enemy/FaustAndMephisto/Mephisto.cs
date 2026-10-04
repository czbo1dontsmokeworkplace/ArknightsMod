using System.Linq;
using ArknightsMod.Content.Items.Weapons.FaustAndMephisto;
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
public sealed class Mephisto : ModNPC
{
    internal bool Retreating => NPC.ai[2] == 1;
    public override string LocalizationCategory => "FaustAndMephisto.NPCs";
    public override string Texture => "Terraria/Images/NPC_22";
    public override string BossHeadTexture => "Terraria/Images/NPC_Head_Boss_14";
    public override void SetStaticDefaults() { Main.npcFrameCount[Type] = 1; NPCID.Sets.MPAllowedEnemies[Type] = true; }
    public override void SetDefaults()
    {
        NPC.width = 32; NPC.height = 52; NPC.aiStyle = -1;
        NPC.lifeMax = DuoRules.Life(DuoRules.MephistoMasterLife, false, false);
        NPC.damage = 0; NPC.defense = 10; NPC.boss = true; NPC.knockBackResist = 0;
        NPC.npcSlots = 10; NPC.netAlways = true; NPC.value = Item.buyPrice(gold: 3);
        NPC.HitSound = SoundID.NPCHit1; NPC.DeathSound = SoundID.NPCDeath6;
        if (!Main.dedServ) Music = MusicID.Boss2;
    }
    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) =>
        NPC.lifeMax = DuoRules.Life(DuoRules.MephistoMasterLife, Main.expertMode, Main.masterMode, balance);
    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry entry) =>
        entry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.ArknightsMod.FaustAndMephisto.MephistoBestiary"));
    public override void OnSpawn(IEntitySource source) => DuoEncounter.Initialize(NPC);
    public override bool CheckActive() => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
    public override bool CanHitNPC(NPC target) => false;
    public override void ModifyNPCLoot(NPCLoot loot) => loot.Add(ItemDropRule.Common(ModContent.ItemType<MephistoCenser>()));
    public override bool CheckDead()
    {
        NPC.life = 1; NPC.dontTakeDamage = true;
        if (DuoEncounter.Authority && !Retreating) { NPC.ai[2] = 1; NPC.ai[1] = 0; NPC.netUpdate = true; }
        return false;
    }
    public override void AI()
    {
        Faust faust = DuoEncounter.Owner(NPC.ai[0], NPC.ai[3]);
        if (faust == null) { DuoEncounter.Remove(NPC); return; }
        if (Retreating)
        {
            NPC.velocity.X *= .8f;
            if (++NPC.ai[1] >= DuoRules.RetreatTicks && DuoEncounter.Authority)
            {
                // A retreat counts as this boss's defeat; run its normal drop rules exactly once.
                NPC.NPCLoot();
                faust.Arena = NPC.Center; faust.BeginFinalPhase(); DuoEncounter.Remove(NPC);
            }
            if (!Main.dedServ && (int)NPC.ai[1] == DuoRules.RetreatTicks - 1) DuoVisuals.Smoke(NPC.Center, 26);
            return;
        }
        NPC.TargetClosest();
        if (NPC.target >= Main.maxPlayers || !DuoEncounter.Alive(Main.player[NPC.target])) return;
        DuoEncounter.Walk(NPC, Main.player[NPC.target].Center, 1.05f);
        if (++NPC.ai[1] < DuoRules.HealInterval) return;
        NPC.ai[1] = 0;
        if (!Main.dedServ)
        {
            DuoVisuals.Puff(NPC.Center - new Vector2(0, 20), Color.LightGray, 10);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = .45f, Pitch = -.3f }, NPC.Center);
        }
        if (!DuoEncounter.Authority) return;
        // Distinct targets; wounded allies are prioritised by health fraction, then distance.
        foreach (NPC ally in Main.npc.Take(Main.maxNPCs).Where(n => DuoEncounter.HealTarget(n, NPC))
            .OrderBy(n => n.life / (float)n.lifeMax).ThenBy(n => n.DistanceSQ(NPC.Center)).Take(3))
            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center - new Vector2(0, 16),
                (ally.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 5,
                ModContent.ProjectileType<MephistoMedicine>(), 0, 0, Main.myPlayer,
                NPC.ai[0], NPC.ai[3], ally.whoAmI + 1);
        NPC.netUpdate = true;
    }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        float opacity = Retreating ? 1 - NPC.ai[1] / DuoRules.RetreatTicks : 1;
        DuoVisuals.Person(spriteBatch, NPC, screenPos, new Color(225, 229, 211) * opacity, false);
        float charge = Retreating ? NPC.ai[1] / DuoRules.RetreatTicks : MathHelper.Clamp((NPC.ai[1] - 135) / 45, 0, 1);
        if (charge > 0) DuoVisuals.Ring(spriteBatch, NPC.Center - screenPos, 18 + charge * 22,
            (Retreating ? new Color(132, 72, 158) : Color.LightGray) * (.5f * opacity), 32);
        return false;
    }
}
