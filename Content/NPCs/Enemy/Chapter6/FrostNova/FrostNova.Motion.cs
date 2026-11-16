using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

public partial class FrostNova
{
    internal const int ShieldTargetSize = 120;
    internal Rectangle BodyContactBox => new((int)NPC.Center.X - 20, (int)NPC.Center.Y - 29, 40, 58);
    private void UpdateShieldTarget() {
        int width = AirbornePhase ? ShieldTargetSize : 40;
        // Preserve the grounded foot line. The phase-two shield broadens the target
        // horizontally without lifting a 58-pixel character above the floor.
        if (NPC.width == width && NPC.height == 58) return;
        Vector2 bottom = NPC.Bottom;
        NPC.width = width; NPC.height = 58; NPC.Bottom = bottom;
    }
    public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
}
