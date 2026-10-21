using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

public sealed class CrusherEmberNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    private readonly byte[] marks = new byte[Main.maxPlayers];
    private readonly int[] activations = new int[Main.maxPlayers];
    internal void Mark(int owner, int activation, int level) { if ((uint)owner < marks.Length) { marks[owner] = (byte)level; activations[owner] = activation; } }
    public override void UpdateLifeRegen(NPC npc, ref int damage)
    {
        int strongest = 0;
        for (int i = 0; i < marks.Length; i++) {
            if (marks[i] == 0) continue;
            var state = Main.player[i].GetModPlayer<CrusherPlayer>();
            if (!Main.player[i].active || state.Kind != CrusherKind.Hoederer || state.ActiveMode != 3 || state.Activation != activations[i]) { marks[i] = 0; continue; }
            strongest = System.Math.Max(strongest, (int)CrusherRules.Level(marks[i], 20, 40));
        }
        if (strongest == 0) return;
        if (npc.lifeRegen > 0) npc.lifeRegen = 0;
        npc.lifeRegen -= strongest * 2; damage = System.Math.Max(damage, strongest / 5);
    }
}
