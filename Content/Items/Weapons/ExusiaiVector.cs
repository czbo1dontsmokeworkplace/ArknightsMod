using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using System;
using ArknightsMod.Content.Projectiles;

namespace ArknightsMod.Content.Items.Weapons
{
    public class ExusiaiVector : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 54;
            Item.height = 28;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.reuseDelay = 15;
            Item.shootSpeed = 8f;
            Item.damage = 108;
            Item.knockBack = 5f;
            Item.shoot = ModContent.ProjectileType<ExusiaiVector_Bullet>();
            Item.DamageType = DamageClass.Ranged;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item31;
            Item.useAmmo = AmmoID.Bullet;
            Item.value = Item.sellPrice(0);
            Item.noMelee = true;
            Item.autoReuse = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {

            Projectile.NewProjectileDirect(source, position, velocity.RotatedByRandom(MathHelper.ToRadians(1)), type, damage, knockback, player.whoAmI);

            for (int j = 0; j < 2; j++)
            {
                float[] rand = {
                        Main.rand.NextFloat(-30f, 0f),
                        Main.rand.NextFloat(-15f, 15f),
                        Main.rand.NextFloat(0f, 30f)
                    };
                for (int i = 0; i < 3; i++)
                {
                    float angleMagnitude = Math.Abs(rand[i]);

                    Vector2 vel = velocity.RotatedBy(MathHelper.ToRadians(rand[i])) * (1f - angleMagnitude / 30f * 0.3f) * Main.rand.NextFloat(0.5f, 1.5f);

                    Projectile.NewProjectileDirect(
                        source,
                        position + velocity.SafeNormalize(Vector2.Zero) * 40,
                        vel,
                        ModContent.ProjectileType<Exusiai_Gun_Effect>(),
                        0, 0, player.whoAmI
                    );
                }
            }

            return false;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0, 0);
        }
    }
}
