using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using ArknightsMod.Content.Projectiles;

namespace ArknightsMod.Content.Items.Weapons
{
    public class OrchidUmbrellla : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.staff[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = Item.height = 40;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useLimitPerAnimation = 1;
            Item.shootSpeed = 8;
            Item.mana = 1;
            Item.useStyle = 1;
            Item.shoot = ModContent.ProjectileType<OrchidUmbrellla_Projectile>();
            Item.DamageType = DamageClass.Magic;

            Item.UseSound = SoundID.Item1;

            Item.SetWeaponValues(1, 0);//伤害，击退

            Item.SetShopValues(ItemRarityColor.Green2, Item.sellPrice(gold: 1));

            Item.autoReuse = true;
            Item.noMelee = true;
        }
    }
}
