using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.FaustAndMephisto;

public abstract class ReunionSupport : ModNPC
{
    // ai: Faust slot + 1 / encounter token / local attack clock / reserved.
    protected abstract int MasterLife { get; }
    protected abstract int MasterDamage { get; }
    protected abstract Color Tint { get; }
    public override string LocalizationCategory => "FaustAndMephisto.NPCs";
    public override string Texture => "Terraria/Images/NPC_22";
    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 1;
        if (this is ReunionBallista) NPCID.Sets.MustAlwaysDraw[Type] = true;
    }
    public override void SetDefaults()
    {
        NPC.width = 34; NPC.height = 50; NPC.aiStyle = -1;
        NPC.lifeMax = DuoRules.Life(MasterLife, false, false);
        NPC.damage = MasterDamage / 2; NPC.defense = 10; NPC.knockBackResist = .25f;
        NPC.npcSlots = 1; NPC.HitSound = SoundID.NPCHit1; NPC.DeathSound = SoundID.NPCDeath1;
        NPC.value = 0;
    }
    public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
    {
        NPC.lifeMax = DuoRules.Life(MasterLife, Main.expertMode, Main.masterMode);
        NPC.damage = DuoRules.Life(MasterDamage, Main.expertMode, Main.masterMode);
    }
    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry entry) =>
        entry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.ArknightsMod.FaustAndMephisto.SupportBestiary"));
    public override bool CheckActive() => false;
    public override bool CanHitNPC(NPC target) => false;
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => this is not ReunionBallista && this is not ReunionCaster;
    public override void AI()
    {
        Faust f = DuoEncounter.Owner(NPC.ai[0], NPC.ai[1]);
        if (f == null) { DuoEncounter.Remove(NPC); return; }
        NPC.TargetClosest();
        if (NPC.target >= Main.maxPlayers || !DuoEncounter.Alive(Main.player[NPC.target])) return;
        Player player = Main.player[NPC.target];
        NPC.direction = NPC.spriteDirection = player.Center.X < NPC.Center.X ? -1 : 1;
        NPC.ai[2]++;
        if (this is ReunionBallista)
        {
            // No gravity: the turret remains at the exact location at which it was placed.
            NPC.noGravity = true; NPC.velocity = Vector2.Zero; NPC.knockBackResist = 0;
            if (NPC.ai[2] >= DuoRules.TurretShotInterval)
            {
                NPC.ai[2] = 0;
                if (DuoEncounter.Authority)
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center - new Vector2(0, 14),
                        (player.Center + player.velocity * 5 - NPC.Center).SafeNormalize(Vector2.UnitX) * 32,
                        ModContent.ProjectileType<BallistaBolt>(), DuoRules.Attack / 2, 0, Main.myPlayer, NPC.ai[0], NPC.ai[1]);
            }
        }
        else if (this is ReunionCaster)
        {
            if (NPC.Distance(player.Center) > 430) DuoEncounter.Walk(NPC, player.Center, .95f);
            else NPC.velocity.X *= .8f;
            if (NPC.ai[2] >= 180)
            {
                NPC.ai[2] = 0;
                if (DuoEncounter.Authority)
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center - new Vector2(0, 16),
                        (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * 7,
                        ModContent.ProjectileType<ReunionArts>(), DuoRules.Attack * 2 / 5, 0, Main.myPlayer, NPC.ai[0], NPC.ai[1]);
            }
        }
        else DuoEncounter.Walk(NPC, player.Center, this is ReunionHost ? 2.5f : 1.25f);
        if (DuoEncounter.Authority && (int)NPC.ai[2] % 90 == 0) NPC.netUpdate = true;
    }
    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (this is ReunionBallista)
        {
            Vector2 center = NPC.Center - screenPos;
            DuoVisuals.Line(spriteBatch, center + new Vector2(-18, 20), center + new Vector2(18, 20), Tint, 7);
            DuoVisuals.Line(spriteBatch, center + new Vector2(-15, 19), center + new Vector2(0, -14), Tint, 5);
            DuoVisuals.Line(spriteBatch, center + new Vector2(15, 19), center + new Vector2(0, -14), Tint, 5);
            Vector2 aim = NPC.target < Main.maxPlayers ? (Main.player[NPC.target].Center - NPC.Center).SafeNormalize(Vector2.UnitX) : Vector2.UnitX;
            DuoVisuals.Line(spriteBatch, center - aim * 16, center + aim * 27, Color.LightGray, 4);
            Vector2 wing = aim.RotatedBy(MathHelper.PiOver2) * 20;
            DuoVisuals.Line(spriteBatch, center - wing, center + wing, Tint, 4);
            if (NPC.ai[2] > DuoRules.TurretShotInterval - 45)
                DuoVisuals.Line(spriteBatch, center, center + aim * 1100, Color.IndianRed * .45f, 1);
        }
        else
        {
            DuoVisuals.Person(spriteBatch, NPC, screenPos, Tint, false, this is ReunionCrusher ? 1.2f : 1);
            if (this is ReunionCaster && NPC.ai[2] > 135)
                DuoVisuals.Ring(spriteBatch, NPC.Center - new Vector2(0, 30) - screenPos, 9, Color.PaleVioletRed * .7f, 16);
        }
        return false;
    }
}
public sealed class ReunionCrusher : ReunionSupport
{
    protected override int MasterLife => 1400;
    protected override int MasterDamage => 120;
    protected override Color Tint => new(139, 128, 116);
    public override void SetDefaults() { base.SetDefaults(); NPC.defense = 18; NPC.width = 42; NPC.knockBackResist = .1f; }
}
public sealed class ReunionCaster : ReunionSupport
{
    protected override int MasterLife => 700;
    protected override int MasterDamage => 0;
    protected override Color Tint => new(161, 127, 171);
}
public sealed class ReunionHost : ReunionSupport
{
    protected override int MasterLife => 900;
    protected override int MasterDamage => 100;
    protected override Color Tint => new(183, 114, 114);
    public override void UpdateLifeRegen(ref int damage)
    {
        Mephisto m = DuoEncounter.Partner(DuoEncounter.Owner(NPC.ai[0], NPC.ai[1]));
        NPC.lifeRegen += m != null && !m.Retreating ? 24 : 12; // 6 HP/s, doubled while Mephisto is present.
    }
}
public sealed class ReunionBallista : ReunionSupport
{
    protected override int MasterLife => 650;
    protected override int MasterDamage => 0;
    protected override Color Tint => new(158, 139, 178);
    public override void SetDefaults() { base.SetDefaults(); NPC.defense = 8; NPC.height = 40; NPC.knockBackResist = 0; NPC.noGravity = true; }
}
