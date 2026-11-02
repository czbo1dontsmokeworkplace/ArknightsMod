using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public sealed class DreadnoughtPlayer : ModPlayer
{
    internal float Momentum;
    internal float Multiplier => 1f + Momentum;
    private int targetIndex = -1, targetType;
    private ChargedOperatorKind targetKind;
    private bool readyAnnounced;
    internal int EmbeddedCount => DreadnoughtThrownBlade.EmbeddedCount(Player, targetKind, targetIndex, targetType);
    internal float Progress => EmbeddedCount / (float)DreadnoughtThrownBlade.BladesPerTarget;
    internal float ProgressFor(ChargedOperatorKind kind) => kind == targetKind ? Progress : 0;
    private int idleTicks;
    private Item fightingItem;

    internal void Attacking(Item item)
    {
        fightingItem = item;
        idleTicks = 0;
    }

    internal void RegisterHit() => Momentum = DreadnoughtRules.Gain(Momentum);

    internal void TrackTarget(NPC target, ChargedOperatorKind kind)
    {
        if (targetIndex != target.whoAmI || targetType != target.type || targetKind != kind) readyAnnounced = false;
        targetIndex = target.whoAmI; targetType = target.type; targetKind = kind;
    }

    public override void PostUpdate()
    {
        idleTicks = System.Math.Min(DreadnoughtRules.IdleGraceTicks + 1, idleTicks + 1);
        bool switchedAway = !ReferenceEquals(Player.HeldItem, fightingItem);
        if (switchedAway || idleTicks > DreadnoughtRules.IdleGraceTicks)
            Momentum = DreadnoughtRules.Decay(Momentum, switchedAway);
        int count = EmbeddedCount;
        if (count < DreadnoughtThrownBlade.BladesPerTarget) readyAnnounced = false;
        else if (!readyAnnounced && Player.whoAmI == Main.myPlayer)
        {
            readyAnnounced = true;
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.MaxMana with { Volume = .65f }, Player.Center);
                CombatText.NewText(new Rectangle((int)Player.Center.X - 30, (int)Player.Top.Y - 46, 60, 20),
                    ChargedOperatorRules.ColorFor(targetKind), Language.GetTextValue("Mods.ArknightsMod.ChargedArsenal.RecallReady"), true);
            }
        }
    }

    public override void UpdateDead()
    {
        Momentum = 0;
        idleTicks = 0;
        fightingItem = null;
        targetIndex = -1; readyAnnounced = false;
    }
}
