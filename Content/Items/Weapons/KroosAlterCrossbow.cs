using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ArknightsMod.Content.Projectiles;

namespace ArknightsMod.Content.Items.Weapons
{
    public class KroosAlterCrossbow : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 52;
            Item.height = 32;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.reuseDelay = 15;
            Item.shootSpeed = 8f;
            Item.damage = 19;
            Item.knockBack = 3f;
            Item.shoot = ModContent.ProjectileType<KroosAlterCrossbow_Hold>();
            Item.DamageType = DamageClass.Ranged;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarityID.Green;
            Item.useAmmo = AmmoID.Arrow;
            Item.value = Item.sellPrice(0);
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.channel = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {

            Projectile.NewProjectileDirect(source, position, velocity, ModContent.ProjectileType<KroosAlterCrossbow_Hold>(), damage, knockback, player.whoAmI);

            return false;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0, 4);
        }
    }
}
