using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Earthshaker;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Earthshaker;

public sealed class EarthshakerStagger : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_32";
    public override void SetStaticDefaults() { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; }
    public override void Update(NPC npc, ref int buffIndex) { if (!npc.boss && npc.knockBackResist > 0 && npc.realLife < 0) npc.velocity *= .75f; }
}
