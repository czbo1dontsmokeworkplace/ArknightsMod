using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using ArknightsMod.Content.Projectiles;

namespace ArknightsMod.Content.Items.Weapons
{
    public class FangSpear : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.SkipsInitialUseSound[Item.type] = true;
            ItemID.Sets.Spears[Item.type] = true;
        }

        public override void SetDefaults()
        {
            // Common Properties
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(silver: 10);

            // Use Properties
            Item.useAnimation = 20;
            Item.useTime = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = SoundID.Item71;
            Item.autoReuse = true;

            // Weapon Properties
            Item.damage = 31;
            Item.knockBack = 1f;
            Item.shootSpeed = 3f;
            Item.shoot = ModContent.ProjectileType<FangSpear_Stab>();
            Item.DamageType = DamageClass.Melee;
            Item.noUseGraphic = true;
            Item.noMelee = true;
        }

        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[Item.shoot] < 1;
        }

        public override bool? UseItem(Player player)
        {
            if (!Main.dedServ && Item.UseSound.HasValue)
            {
                SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
            }

            return null;
        }
    }
}
