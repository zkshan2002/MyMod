using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ModLoader;

namespace MyMod.Contents.Items.Accessories.Bonus {
	public class Defense : ModItem
	{
		public override void SetStaticDefaults() {
			Tooltip.SetDefault("Locks life, reduces received damage and grants immunity to knockback.");
		}

		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 32;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual) {
			player.statLife = player.statLifeMax2 + 1000;
			player.endurance = 1;
			player.noKnockback = true;
		}
	}
}