using ArknightsMod.Content.Projectiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs
{
    public class MostimaRingAndWingBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<MostimaRingAndWingProj>()] < 1)
            {
                Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.position.X + player.width / 2, player.position.Y + player.height / 2, 0f, 0f, ModContent.ProjectileType<MostimaRingAndWingProj>(), 0, 0f, player.whoAmI);
            }
        }
    }
}
