using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MyMod.Contents.Items.Weapons {
	public class ClickWeapon : ModItem {
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Cursor's Curse");
			Tooltip.SetDefault("Click where you feel like dealing damage.");
		}

		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 26;
			Item.maxStack = 1;

			Item.useAnimation = 5;
			Item.useTime = 5;
			Item.useStyle = ItemUseStyleID.HiddenAnimation;

			Item.DamageType = DamageClass.Magic;
			Item.damage = 1000;
			Item.knockBack = 0f;
			Item.noMelee = true;
		}
		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer) {
				int projectileHandle = Projectile.NewProjectile(Item.GetSource_FromThis(), Vector2.Zero, Vector2.Zero, ModContent.ProjectileType<ClickWeaponProjectile>(), Item.damage, Item.knockBack, Main.myPlayer);
				if (projectileHandle < Main.maxProjectiles) {
					Projectile projectile = Main.projectile[projectileHandle];
					projectile.position = Main.screenPosition + new Vector2(Main.mouseX, Main.mouseY) - new Vector2(projectile.width, projectile.height) * 0.5f;
				}
			}
			return true;
		}
	}

	public class ClickWeaponProjectile : ModProjectile {
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Click");
		}

		public override void SetDefaults() {
			Projectile.width = 54;
			Projectile.height = 54;
			Projectile.aiStyle = 0;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.ignoreWater = true;
			Projectile.light = 1f;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 30;
			Projectile.penetrate = -1;
		}
		private int Timer = 0;
		public override void AI() {
			Timer += 1;
			if (Timer < 10) {
				Projectile.alpha = 255 - Timer / 10 * 255;
			}
			else if (Timer > 20) {
				Projectile.alpha = (Timer - 20) / 10 * 255;
			}
			Player player = Main.player[Main.myPlayer];
			Utils.AddLightLineTile(player.Center, Projectile.Center, 15, Color.White);
		}
	}
}