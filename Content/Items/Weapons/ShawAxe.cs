using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Items.Weapons
{
    public class ShawAxe : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 36;
            Item.useAnimation = 36;
            Item.axe = 55;
            Item.damage = 25;
            Item.knockBack = 4.5f;
            //Item.value = Item.buyPrice(gold: 1);
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;

            Item.attackSpeedOnlyAffectsWeaponAnimation = true;
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            base.MeleeEffects(player, hitbox);
        }
    }
}
