using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MyMod.Contents.NPCs.Clones.EOCClone {
    public class EyeMinionClone : NPCBase {
        public override void AI() {
            RetargetPlayers();
            Player player = Main.player[NPC.target];

            const float speed = 5f;
            const float acceleration = 0.03f;

            Vector2 velTarget;
            if (Main.dayTime || player.dead) {
                NPC.velocity.Y += acceleration * -2f;
                NPC.EncourageDespawn(10);
            }
            else {
                Vector2 toTarget = player.Center - NPC.Center;
                float distanceToTarget = toTarget.Length();
                if(distanceToTarget > 0f) {
                    toTarget.Normalize();
                    velTarget = toTarget * speed;
                }
                else {
                    velTarget = new Vector2();
                }
                VelocityCorrection(velTarget, acceleration);
            }


            RotationCharge();

            if (((NPC.velocity.X > 0f && NPC.oldVelocity.X < 0f) || (NPC.velocity.X < 0f && NPC.oldVelocity.X > 0f) || (NPC.velocity.Y > 0f && NPC.oldVelocity.Y < 0f) || (NPC.velocity.Y < 0f && NPC.oldVelocity.Y > 0f)) && !NPC.justHit) {
                NPC.netUpdate = true;
            }
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Servent of Cuthulhu Clone");

            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData {
                SpecificallyImmuneTo = new int[] {
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);

            Main.npcFrameCount[Type] = 2;
        }

        public override void SetDefaults() {
            NPC.width = 20;
            NPC.height = 20;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;

            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = 100;
            NPC.knockBackResist = 0f;

            NPC.noGravity = true;
            NPC.noTileCollide = true;

            NPC.npcSlots = 0f;

            NPC.aiStyle = -1;
        }

        public override void FindFrame(int frameHeight) {
            LoopFrame(frameHeight, 0, 1);
        }

        public override void HitEffect(int hitDirection, double damage) {
            if (Main.netMode == NetmodeID.Server) {
                return;
            }

            if (NPC.life > 0) {
                Vector2 dustVel = new Vector2(hitDirection, -1f);
                for (int i = 0; i < damage / NPC.lifeMax * 100.0; i++) {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, dustVel.X, dustVel.Y);
                }
            }
            else {
                Vector2 dustVel = new Vector2(hitDirection * 2f, -2f);
                for (int i = 0; i < 150; i++) {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, dustVel.X, dustVel.Y);
                }

                int goreType2 = Mod.Find<ModGore>("Gore_2").Type;
                int goreType7 = Mod.Find<ModGore>("Gore_7").Type;
                int goreType9 = Mod.Find<ModGore>("Gore_9").Type;
                int goreType10 = Mod.Find<ModGore>("Gore_10").Type;
                var entitySource = NPC.GetSource_Death();
                for (int i = 0; i < 2; i++) {
                    Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType2);
                    Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType7);
                    Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType9);
                    Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType10);

                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                }
            }
        }

    }
}
