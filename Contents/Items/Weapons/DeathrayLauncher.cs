using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

using MyMod.Contents.Projectiles.Deathray;

namespace MyMod.Contents.Items.Weapons {
	public class DeathrayLauncher : ModItem {
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Deathray Launcher");
		}

		public override void SetDefaults() {
			Item.width = 78;
			Item.height = 118;
			Item.maxStack = 1;

			Item.useAnimation = 5;
			Item.useTime = 5;
			Item.useStyle = ItemUseStyleID.HoldUp;

			Item.DamageType = DamageClass.Magic;
			Item.damage = 10000;
			Item.knockBack = 0f;
			Item.noMelee = true;

            Item.shoot = ModContent.ProjectileType<MoonLordDeathray>();
			Item.shootSpeed = 1;
            Item.alpha = 255;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type,
            int damage, float knockback)
        {
            Projectile.NewProjectileDirect(source, position, velocity.SafeNormalize(-Vector2.UnitY), type, Item.damage, Item.knockBack, player.whoAmI, ai0: player.whoAmI);
            return false;
        }
    }
}