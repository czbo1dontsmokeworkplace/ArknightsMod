using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.TexasAlter;

internal class TexalterSetPlayer : ArknightsArmorPlayer
{
    public bool TexalterHelmetActive;
    public bool TexalterSetActive;
    private bool reductionApplied;
    private readonly TexalterSkillSession session = new();
    private static bool HasHelmet(Player player) => player.active && !player.dead
        && player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<TexalterHead>();
    internal static bool HasSet(Player player) => HasHelmet(player)
        && player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<TexalterBody>()
        && player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<TexalterLegs>();

    public override void ResetEffects() { TexalterHelmetActive = TexalterSetActive = false; reductionApplied = false; }
    public override void PostUpdateEquips() {
        TexalterHelmetActive = HasHelmet(Player);
        TexalterSetActive = HasSet(Player);
    }
    public override void PostUpdateMiscEffects() {
        var skill = Player.GetModPlayer<WeaponPlayer>();
        bool ready = session.Ready(skill.SkillActive, skill.SkillActivationId);
        // StatModifier *= scales its multiplicative component, stacking with other attack bonuses.
        if (HasHelmet(Player) && skill.SkillActive) Player.GetDamage(DamageClass.Generic) *= 1.2f;
        if (HasSet(Player) && ready) {
            Player.endurance = 1f - (1f - Player.endurance) * .85f;
            reductionApplied = true;
        }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
        if (Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer || !HasSet(Player)
            || damageDone <= 0 || target.life > 0 || target.active || target.friendly
            || NPCID.Sets.CountsAsCritter[target.type]) return;
        if (target.realLife >= 0 && target.realLife < Main.maxNPCs) {
            NPC root = Main.npc[target.realLife];
            if (root.active && root.life > 0) return;
        }
        var skill = Player.GetModPlayer<WeaponPlayer>();
        // Consume before resetting any clock: the reset itself is not a new activation.
        if (!session.TryConsume(skill.SkillActive, skill.SkillActivationId)) return;
        ActiveSkillTimeReset.Reset(Player);
        int before = Player.statLife;
        Player.GetModPlayer<IncomingHealingPlayer>().RestoreLife(Player.statLifeMax2);
        if (Player.statLife > before) {
            Player.HealEffect(Player.statLife - before);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.PlayerLifeMana, number: Player.whoAmI);
        }
        // This frame's earlier stat update may already have included our unspent DR.
        if (reductionApplied) {
            Player.endurance = 1f - (1f - Player.endurance) / .85f;
            reductionApplied = false;
        }
    }
    public override void UpdateDead() => session.Clear();
    public override void OnEnterWorld() => session.Clear();
}
