using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;


namespace MyMod.Contents.NPCs.Bosses.MachineBrain {
    public class TestNPC : NPCBase {

        protected override bool Init() {
            spriteRotationDeg = 90;
            return true;
        }

        private int Timer = 0;

        public override void AI() {

            if (!inited) {
                inited = true;
                if (!Init()) {
                    Kill();
                    return;
                }
            }

            Timer += 1;
            Rotation = MathHelper.ToRadians(Timer);

            Utils.DustBox(NPC.Hitbox);

        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Test NPC");

            NPCDebuffImmunityData debuffData = new() {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);

            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults() {
            NPC.width = 200;
            NPC.height = 200;

            NPC.damage = 1;
            NPC.defense = 0;
            NPC.lifeMax = 1000;
            NPC.knockBackResist = 0f;

            NPC.noGravity = true;
            NPC.noTileCollide = true;

            NPC.value = Item.buyPrice();
            NPC.npcSlots = 0f;

            NPC.aiStyle = -1;
        }

        public override void FindFrame(int frameHeight) {
            LoopFrame(frameHeight, 0, Main.npcFrameCount[Type] - 1, 10);
        }
    }
}