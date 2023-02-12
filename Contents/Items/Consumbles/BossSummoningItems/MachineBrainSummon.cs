using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameContent.Creative;
using Terraria.ModLoader;
using MyMod.Contents.NPCs.Bosses.MachineBrain;

namespace MyMod.Contents.Items.Consumbles.BossSummoningItems {
	public class MachineBrainSummon : ModItem {
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Boss Summon Item");
			Tooltip.SetDefault("Summons Machine Brain");

			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 3;
			ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
		}

		public override void SetDefaults() {
			Item.width = 24;
			Item.height = 32;
			Item.maxStack = 1;
			Item.value = 100;
			Item.rare = ItemRarityID.Blue;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = false;
		}

		private readonly int bossType = ModContent.NPCType<MachineBrain>();

		public override bool CanUseItem(Player player) {
			return !NPC.AnyNPCs(bossType);
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer) {
				SoundEngine.PlaySound(SoundID.Roar, player.position);

				if (Main.netMode != NetmodeID.MultiplayerClient) {
					NPC.SpawnBoss((int)player.Center.X, (int)player.Center.Y, bossType, player.whoAmI);
				}
				else {
					NetMessage.SendData(MessageID.SpawnBoss, number: player.whoAmI, number2: bossType);
				}
			}
			return true;
		}
		
		public override void AddRecipes() {
			CreateRecipe()
				.Register();
		}
	}
}