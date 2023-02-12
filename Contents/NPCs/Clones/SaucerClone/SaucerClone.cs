using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;


namespace MyMod.Contents.NPCs.Clones.SaucerClone {
    [AutoloadBossHead]
    public class SaucerClone : NPCBase {

        private enum Stages {
            firstStage,
            transition,
            secondStage
        }
        private Stages Stage {
            get => (Stages)(NPC.ai[0]);
            set => NPC.ai[0] = (float)value;
        }

        private enum States {
            // first stage
            firstStage,
            // transition
            spin,
            // second stage
            secondStage,
        };
        private States State {
            get => (States)NPC.ai[1];
            set => NPC.ai[1] = (float)value;
        }

        private ref float Timer => ref NPC.localAI[0];
        private ref float Timer2 => ref NPC.localAI[1];

        private bool Schedule(bool done = true) {
            float lifeRatio = NPC.life / (float)NPC.lifeMax;
            bool flagChange = false;
            if (Stage == Stages.firstStage) {
                if (State == States.firstStage) {
                    if (done) {
                        State = States.firstStage;
                    }
                }
                if (lifeRatio <= 0.5f) {
                    Stage = Stages.transition;
                    State = States.spin;
                    flagChange = true;
                }
            }
            else if (Stage == Stages.transition) {
                if (State == States.spin) {
                    if (done) {
                        bossHeadAppearance = 2;

                        Stage = Stages.secondStage;
                        State = States.secondStage;
                        SetAttributeSecondStage();
                        flagChange = true;
                    }
                }
            }
            else if (Stage == Stages.secondStage) {
                if (State == States.secondStage) {
                    if (done) {
                        State = States.secondStage;
                        flagChange = true;
                    }
                }
            }

            if (flagChange) {
                NPC.netUpdate = true;
            }
            return flagChange;
        }

        private void SetAttributeSecondStage() {
            NPC.damage = (int)(NPC.defDamage * 1.5f);
            NPC.defense = NPC.defDefense + 10;
            NPC.HitSound = SoundID.NPCHit4;
        }

        public override void AI() {
            // Flags used to determine constants
            bool flag90 = NPC.life < NPC.lifeMax * 0.90f;
            bool flag80 = NPC.life < NPC.lifeMax * 0.80f;
            bool flag75 = NPC.life < NPC.lifeMax * 0.75f;
            bool flag70 = NPC.life < NPC.lifeMax * 0.70f;
            bool flag60 = NPC.life < NPC.lifeMax * 0.60f;
            bool flag50 = NPC.life < NPC.lifeMax * 0.50f;
            bool flag25 = NPC.life < NPC.lifeMax * 0.25f;
            bool flag10 = NPC.life < NPC.lifeMax * 0.10f;

            // Target Player
            RetargetPlayers();
            Player player = Main.player[NPC.target];

            // Despawn Condition
            if (player.dead) {
                NPC.velocity.Y -= 0.04f;
                NPC.rotation = 0f;
                NPC.EncourageDespawn(10);
                return;
            }

            if (Stage == Stages.firstStage) {
                if (State == States.firstStage) {
                    Timer += 1;
                    if (Timer <= 600) {
                    }
                    else {
                        Timer = 0;
                        Schedule();
                    }
                    if (Schedule(false)) {
                        Timer = 0;
                    }
                }
            }
            else if (Stage == Stages.transition) {
                if (State == States.spin) {
                    Timer += 1;
                    if (Timer == 100) {
                        frameAppearance = 2;
                    }

                    if (Timer >= 200) {
                        Timer = 0;
                        Schedule();
                    }
                    if (Schedule(false)) {
                        Timer = 0;
                    }
                }
            }
            else if (Stage == Stages.secondStage) {
                if (State == States.secondStage) {
                    Timer += 1;
                    if (Timer > 300) {
                        Timer = 0;
                        Schedule();
                    }
                    if (Schedule(false)) {
                        Timer = 0;
                    }
                }
            }
        }

        private int frameAppearance = 1;
        private int bossHeadAppearance = 1;

        public override string BossHeadTexture => "MyMod/Contents/NPCs/Bosses/SaucerClone/BossHead";
        public override void Load() {}

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Martian Saucer Clone");
            NPCID.Sets.MPAllowedEnemies[Type] = true;

            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);

            Main.npcFrameCount[Type] = 4;
        }

        public override void SetDefaults() {
            NPC.width = 150;
            NPC.height = 80;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath14;

            NPC.damage = 1;
            NPC.defense = 0;
            NPC.lifeMax = 1000;
            NPC.knockBackResist = 0f;

            NPC.noGravity = true;
            NPC.noTileCollide = true;

            NPC.value = Item.buyPrice();
            NPC.SpawnWithHigherTime(30);
            NPC.boss = true;
            NPC.npcSlots = 5f;

            NPC.aiStyle = -1;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            cooldownSlot = ImmunityCooldownID.Bosses;
            return true;
        }

        public override void FindFrame(int frameHeight) {
            int firstFrame;
            int lastFrame;
            switch (frameAppearance) {
                case 1:
                    firstFrame = 0;
                    lastFrame = 2;
                    break;
                case 2:
                    firstFrame = 3;
                    lastFrame = 5;
                    break;
                default:
                    firstFrame = 0;
                    lastFrame = Main.npcFrameCount[Type] - 1;
                    break;
            }
            LoopFrame(frameHeight, firstFrame, lastFrame);
        }

        public override void HitEffect(int hitDirection, double damage) {
            if (Main.netMode == NetmodeID.Server) {
                return;
            }

            if (NPC.life > 0) {
                for (int i = 0; i < damage / NPC.lifeMax * 100.0; i++) {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hitDirection, -1f);
                }
            }
            else {
                for (int i = 0; i < 150; i++) {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 2 * hitDirection, -2f);
                }

                int goreType2 = Mod.Find<ModGore>("Gore_2").Type;
                int goreType7 = Mod.Find<ModGore>("Gore_7").Type;
                int goreType9 = Mod.Find<ModGore>("Gore_9").Type;
                int goreType146 = Mod.Find<ModGore>("Gore_146").Type;
                var entitySource = NPC.GetSource_Death();
                for (int i = 0; i < 2; i++) {
                    Gore.NewGore(entitySource, NPC.Center, Utils.Vector2Noise(30, 6f), goreType2);
                    Gore.NewGore(entitySource, NPC.Center, Utils.Vector2Noise(30, 6f), goreType7);
                    Gore.NewGore(entitySource, NPC.Center, Utils.Vector2Noise(30, 6f), goreType9);
                    Gore.NewGore(entitySource, NPC.Center, Utils.Vector2Noise(30, 6f), goreType146);
                }

                int dustHandle;
                for (int i = 0; i < 10; i++) {
                    dustHandle = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke);
                    Dust dust = Main.dust[dustHandle];
                    dust.alpha = 100;
                    dust.scale = 1.5f;
                    Dust dustClone = dust;
                    dustClone.velocity *= 1.4f;
                }
                for (int i = 0; i < 5; i++) {
                    dustHandle = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch);
                    Dust dust = Main.dust[dustHandle];
                    dust.alpha = 100;
                    dust.scale = 2.5f;
                    dust.noGravity = true;
                    Dust dustClone = dust;
                    dustClone.velocity *= 5f;

                    dustHandle = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch);
                    dust = Main.dust[dustHandle];
                    dust.alpha = 100;
                    dust.scale = 1.5f;
                    dustClone = dust;
                    dustClone.velocity *= 3f;
                }

                for (int i = 0; i < 4; i++) {
                    int goreHandle = Gore.NewGore(entitySource, NPC.Center, default, Main.rand.Next(61, 64));
                    Gore gore = Main.gore[goreHandle];
                    Gore goreClone = gore;
                    goreClone.velocity *= 0.4f;
                    if (i % 2 == 0) {
                        gore.velocity.X += 1f;
                    }
                    else {
                        gore.velocity.X -= 1f;
                    }
                    if (i / 2 == 0) {
                        gore.velocity.Y += 1f;
                    }
                    else {
                        gore.velocity.Y -= 1f;
                    }
                }
            }
        }
    }
}
