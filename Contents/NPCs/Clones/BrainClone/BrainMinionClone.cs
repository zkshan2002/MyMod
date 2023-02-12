using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;


namespace MyMod.Contents.NPCs.Clones.BrainClone {
    public class BrainMinionClone : NPCBase {

        private enum States {
            idle,
            invade,
        };
        private States State {
            get => (States)NPC.ai[0];
            set => NPC.ai[0] = (float)value;
        }

        private ref float Timer => ref NPC.localAI[0];
        private ref float HitCount => ref NPC.localAI[1];

        internal int ownerHandle = -1;
        private NPC owner;

        public override void AI() {

            bool Schedule(bool done = true) {
                bool flagChange = false;
                switch (State) {
                    case States.idle:
                        if (done) {
                            State = States.invade;
                            flagChange = true;
                        }
                        break;
                    case States.invade:
                        if (done) {
                            State = States.idle;
                            flagChange = true;
                        }
                        break;
                }

                if (flagChange) {
                    NPC.netUpdate = true;
                }
                return flagChange;
            }

            void Idle() {
                float speed = 8f;
                if (Vector2.Distance(NPC.Center, owner.Center) > 90f) {
                    Vector2 velTarget = ToTarget(owner.Center, speed);
                    VelocitySoftUpdate(velTarget, 1 / 16f);
                }
                else {
                    if (Utils.L1Norm(NPC.velocity) < speed) {
                        NPC.velocity *= 1.05f;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient) {
                        if ((Main.expertMode && Main.rand.NextBool(100)) ||
                     Main.rand.NextBool(200)) {
                            Schedule();
                        }
                    }
                }
                Schedule(false);
            }

            void Invade(Player player) {
                Timer += 1;
                if (Timer == 1) {
                    NPC.velocity = ToPlayer(player, 8f);
                }
                if (Main.expertMode) {
                    if (Main.getGoodWorld) {
                        FlyMomentum(player.Center, 12f, 1 / 50f);
                    }
                    else {
                        FlyMomentum(player.Center, 9f, 1 / 100f);
                    }
                }
                bool flag = false;
                if (Vector2.Distance(NPC.Center, owner.Center) <= 700f) {
                    if (NPC.justHit) {
                        HitCount += 1;
                        if (NPC.knockBackResist != 0f || HitCount == 5) {
                            flag = true;
                        }
                    }
                }
                else {
                    flag = true;
                }
                if (flag) {
                    Timer = 0;
                    HitCount = 0;
                    Schedule();
                }
                if (Schedule(false)) {
                    Timer = 0;
                    HitCount = 0;
                }
            }

            // Detect owner
            if (ownerHandle < 0 || ownerHandle >= Main.maxNPCs || Main.npc[ownerHandle].type != ModContent.NPCType<BrainClone>() || !Main.npc[ownerHandle].active) {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }
            owner = Main.npc[ownerHandle];

            // Target Player
            Player player = Main.player[owner.target];

            switch (State) {
                case States.idle:
                    Idle();
                    break;
                case States.invade:
                    Invade(player);
                    break;
            }
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Minion Clone");

            NPCDebuffImmunityData debuffData = new() {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);

            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults() {
            NPC.width = 30;
            NPC.height = 30;
            NPC.HitSound = SoundID.NPCHit9;
            NPC.DeathSound = SoundID.NPCDeath11;

            NPC.damage = 20;
            NPC.defense = 10;
            NPC.lifeMax = 100;
            NPC.knockBackResist = 0.8f;

            NPC.noGravity = true;
            NPC.noTileCollide = true;

            NPC.value = Item.buyPrice();
            NPC.npcSlots = 0f;

            NPC.aiStyle = -1;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            cooldownSlot = ImmunityCooldownID.Bosses;
            return true;
        }

        public override void FindFrame(int frameHeight) {
            LoopFrame(frameHeight, 0, Main.npcFrameCount[Type] - 1);
        }

        public override void HitEffect(int hitDirection, double damage) {
            if (Main.netMode == NetmodeID.Server) {
                return;
            }

            Vector2 dustVel;
            if (NPC.life > 0) {
                dustVel = new Vector2(hitDirection, -1f);
                for (int i = 0; i < damage / NPC.lifeMax * 50.0; i++) {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, dustVel.X, dustVel.Y);
                }
            }
            else {
                dustVel = new Vector2(hitDirection, -1f) * 2f;
                for (int i = 0; i < 150; i++) {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, dustVel.X, dustVel.Y);
                }

                int goreType402 = Mod.Find<ModGore>("Gore_402").Type;
                var entitySource = NPC.GetSource_Death();
                Gore.NewGore(entitySource, NPC.Center, NPC.velocity, goreType402);
            }
        }
    }
}
