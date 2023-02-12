using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;


namespace MyMod.Contents.NPCs.Clones.BrainClone {
    [AutoloadBossHead]
    public class BrainClone : NPCBase {
        private enum Stages {
            spawn,
            firstStage,
            transition,
            secondStage
        }
        private Stages Stage {
            get => (Stages)(NPC.ai[0]);
            set => NPC.ai[0] = (float)value;
        }
        private enum States {
            // spawn
            spawn,
            // first stage
            fly,
            teleport,
            // transition
            transition,
            // second stage
            softFlySecondStage,
            teleportSecondStage,
        };
        private States State {
            get => (States)NPC.ai[1];
            set => NPC.ai[1] = (float)value;
        }

        private ref float Timer => ref NPC.localAI[0];

        private ref float DespawnTimer => ref NPC.localAI[1];

        private Vector2 TeleportPos;

        private readonly int minionType = ModContent.NPCType<BrainMinionClone>();

        public override void AI() {

            bool Schedule(bool done = true) {
                bool DetectTransition() {
                    for (int npcHandle = 0; npcHandle < Main.maxNPCs; npcHandle++) {
                        NPC npc = Main.npc[npcHandle];
                        if (npc.type == minionType && npc.active) {
                            return false;
                        }
                    }
                    return true;
                }
                bool flagChange = false;
                switch (Stage) {
                    case Stages.spawn:
                        if (State == States.spawn) {
                            if (done) {
                                Stage = Stages.firstStage;
                                State = States.fly;
                                flagChange = true;
                            }
                        }
                        break;
                    case Stages.firstStage:
                        switch (State) {
                            case States.fly:
                                if (done) {
                                    State = States.teleport;
                                    flagChange = true;
                                }
                                break;
                            case States.teleport:
                                if (done) {
                                    State = States.fly;
                                    flagChange = true;
                                }
                                break;
                        }
                        if (DetectTransition()) {
                            Stage = Stages.transition;
                            State = States.transition;
                            flagChange = true;
                        }
                        break;
                    case Stages.transition:
                        if (State == States.transition) {
                            if (done) {
                                Stage = Stages.secondStage;
                                State = States.softFlySecondStage;
                                SetDefaultsSecondStage();
                                flagChange = true;
                            }
                        }
                        break;
                    case Stages.secondStage:
                        switch (State) {
                            case States.softFlySecondStage:
                                if (done) {
                                    State = States.teleportSecondStage;
                                    flagChange = true;
                                }
                                break;
                            case States.teleportSecondStage:
                                if (done) {
                                    State = States.softFlySecondStage;
                                    flagChange = true;
                                }
                                break;
                        }
                        break;
                }

                if (flagChange) {
                    NPC.netUpdate = true;
                }
                return flagChange;
            }

            void Spawn() {
                int minionCount = NPC.GetBrainOfCthuluCreepersCount();
                for (int i = 0; i < minionCount; i++) {
                    Vector2 minionPos = NPC.Center;
                    minionPos.X += Main.rand.Next(-NPC.width, NPC.width + 1);
                    minionPos.Y += Main.rand.Next(-NPC.height, NPC.height + 1);
                    Vector2 minionVel = Utils.Vector2Noise(30, 3f);
                    int minionHandle = NewNPC(minionType, minionPos, minionVel);
                    if (minionHandle != -1) {
                        NPC minion = Main.npc[minionHandle];
                        if (minion.ModNPC is BrainMinionClone modMinion) {
                            modMinion.ownerHandle = NPC.whoAmI;
                        }
                        minion.netUpdate = true;
                    }
                }

                Schedule();
            }

            void Fly(Player player) {
                float speed = 1f;
                if (Main.getGoodWorld) {
                    speed *= 3f;
                }
                FlySmooth(player.Center, speed);

                Timer += 1;
                int flyLasts = 120 + Main.rand.Next(300);
                // notice the strange distribution here
                if (Timer > flyLasts) {
                    Timer = 0;
                    Schedule();
                }
                if (Schedule(false)) {
                    Timer = 0;
                }
            }

            void Teleport(Player player) {
                float speed = 1f;
                if (Main.getGoodWorld) {
                    speed *= 3f;
                }
                FlySmooth(player.Center, speed);

                Timer += 1;
                if (Timer == 1) {
                    TeleportPos = GetTeleportPos(player);
                }
                else if (Timer < 1 + 51) {
                    NPC.alpha = ((int)Timer - 1) * 5;
                }
                else if (Timer == 1 + 51) {
                    TeleportTo(TeleportPos);
                    NPC.alpha = 255;
                    NPC.netUpdate = true;
                    NPC.netSpam = 0;
                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                }
                else if (Timer < 1 + 51 * 2) {
                    NPC.alpha = 255 - ((int)Timer - 1 - 51) * 5;
                }
                else {
                    Timer = 0;
                    NPC.alpha = 0;
                    Schedule();
                }
                if (Schedule(false)) {
                    Timer = 0;
                    NPC.alpha = 0;
                }
            }

            Vector2 GetTeleportPos(Player player) {
                for (int i = 0;; i++) {
                    Vector2Int tileCoordinates = Utils.PixelToTile(player.Center);
                    tileCoordinates += Utils.Vector2IntNoise(50);
                    Vector2 pixelCoordinates = Utils.TileToPixel(tileCoordinates);
                    bool flag = false;
                    if (i >= 100) {
                        flag = true;
                    }
                    else if (!WorldGen.SolidTile(tileCoordinates.X, tileCoordinates.Y)) {
                        if (i >= 75) {
                            flag = true;
                        }
                        else if (Collision.CanHit(pixelCoordinates, 1, 1, player.position, player.width, player.height)) {
                            flag = true;
                        }
                    }
                    if (flag) {
                        return pixelCoordinates;
                    }
                }
            }

            void Transition() {
                frameAppearance = 2;

                int goreType392 = Mod.Find<ModGore>("Gore_392").Type;
                int goreType393 = Mod.Find<ModGore>("Gore_393").Type;
                int goreType394 = Mod.Find<ModGore>("Gore_394").Type;
                int goreType395 = Mod.Find<ModGore>("Gore_395").Type;
                var entitySource = NPC.GetSource_Death();
                Gore.NewGore(entitySource, NPC.Center, Utils.Vector2Noise(30, 6f), goreType392);
                Gore.NewGore(entitySource, NPC.Center, Utils.Vector2Noise(30, 6f), goreType393);
                Gore.NewGore(entitySource, NPC.Center, Utils.Vector2Noise(30, 6f), goreType394);
                Gore.NewGore(entitySource, NPC.Center, Utils.Vector2Noise(30, 6f), goreType395);

                Vector2 dustVel;
                for (int i = 0; i < 20; i++) {
                    dustVel = Utils.Vector2Noise(30, 6f);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, dustVel.X, dustVel.Y);
                }

                SoundEngine.PlaySound(SoundID.NPCHit1, NPC.Center);
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                Schedule();
            }

            void SoftFlySecondStage(Player player) {
                FlyMomentum(player.Center, 8f, 1 / 51f);

                Timer += 1;
                if (NPC.justHit) {
                    Timer -= Main.rand.Next(5);
                }
                int flyLasts = 60 + Main.rand.Next(120);
                if (Main.netMode != NetmodeID.SinglePlayer) {
                    flyLasts += Main.rand.Next(30, 90);
                }
                // notice the strange distribution here
                if (Timer > flyLasts) {
                    Timer = 0;
                    Schedule();
                }
                if (Schedule(false)) {
                    Timer = 0;
                }
            }

            void TeleportSecondStage(Player player) {
                FlyMomentum(player.Center, 8f, 1 / 51f);

                Timer += 1;
                if (Timer == 1) {
                    TeleportPos = GetTeleportPosSecondStage(player);
                }
                else if (Timer < 1 + 255) {
                    if (Main.netMode != NetmodeID.SinglePlayer) {
                        Timer += 15 - 1;
                    }
                    else {
                        Timer += 25 - 1;
                    }
                    // notice the strange vel decay here
                    VelocityDecay(0.9f);
                    NPC.alpha = ((int)Timer - 1);
                }
                else if (Timer < 1000) {
                    Timer = 1000;
                    TeleportTo(TeleportPos);
                    NPC.alpha = 255;
                    NPC.netUpdate = true;
                    NPC.netSpam = 0;
                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                }
                else if (Timer < 1000 + 255) {
                    if (Main.netMode != NetmodeID.SinglePlayer) {
                        Timer += 15 - 1;
                    }
                    else {
                        Timer += 25 - 1;
                    }
                    NPC.alpha = 255 - ((int)Timer - 1000);
                }
                else {
                    Timer = 0;
                    NPC.alpha = 0;
                    Schedule();
                }
                if (Schedule(false)) {
                    Timer = 0;
                    NPC.alpha = 0;
                }
            }

            Vector2 GetTeleportPosSecondStage(Player player) {
                for (int i = 0; ; i++) {
                    Vector2Int tileCoordinates = Utils.PixelToTile(player.Center);
                    tileCoordinates.X += (Main.rand.Next(2) * 2 - 1) * Main.rand.Next(7, 13);
                    tileCoordinates.Y += (Main.rand.Next(2) * 2 - 1) * Main.rand.Next(7, 13);
                    Vector2 pixelCoordinates = Utils.TileToPixel(tileCoordinates);
                    if (i >= 100 || !WorldGen.SolidTile(tileCoordinates.X, tileCoordinates.Y)) {
                        return pixelCoordinates;
                    }
                }
            }


            // Target player
            RetargetPlayers();
            Player player = Main.player[NPC.target];
            ForceRetargetPlayers(ref player);

            // Despawn condition
            if (Utils.L1Distance(NPC.Center, player.Center) > 6000f) {
                NPC.active = false;
                NPC.life = 0;
                if (Main.netMode == NetmodeID.Server) {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                }
                return;
            }
            if (player.dead || !player.ZoneCrimson) {
                if(DespawnTimer < 120) {
                    DespawnTimer += 1;
                }
                if (DespawnTimer > 60) {
                    NPC.velocity.Y += (DespawnTimer - 60) * 0.25f;
                }
            }
            else {
                if (DespawnTimer > 0) {
                    DespawnTimer -= 1;
                }
            }

            switch (State) {
                case States.spawn:
                    Spawn();
                    break;
                case States.fly:
                    Fly(player);
                    break;
                case States.teleport:
                    Teleport(player);
                    break;
                case States.transition:
                    Transition();
                    break;
                case States.softFlySecondStage:
                    SoftFlySecondStage(player);
                    break;
                case States.teleportSecondStage:
                    TeleportSecondStage(player);
                    break;
            }
        }

        private void SetDefaultsSecondStage() {
            NPC.dontTakeDamage = false;
        }

        private int frameAppearance = 1;

        public override string BossHeadTexture => "MyMod/Contents/NPCs/Bosses/BrainClone/BossHead";

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Brain of Cuthulhu Clone");
            NPCID.Sets.MPAllowedEnemies[Type] = true;

            NPCDebuffImmunityData debuffData = new() {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);

            Main.npcFrameCount[Type] = 8;
        }

        public override void SetDefaults() {
            NPC.width = 160;
            NPC.height = 110;
            NPC.HitSound = SoundID.NPCHit9;
            NPC.DeathSound = SoundID.NPCDeath11;

            NPC.dontTakeDamage = true;
            NPC.damage = 30;
            NPC.defense = 14;
            NPC.lifeMax = 1000;
            NPC.knockBackResist = 0.5f;

            NPC.noGravity = true;
            NPC.noTileCollide = true;

            NPC.value = Item.buyPrice(gold: 5);
            NPC.SpawnWithHigherTime(30);
            NPC.boss = true;
            NPC.npcSlots = 6f;

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
                    lastFrame = 3;
                    break;
                case 2:
                    firstFrame = 4;
                    lastFrame = 7;
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

            Vector2 dustVel;
            if (NPC.life > 0) {
                dustVel = new Vector2(hitDirection, -1f);
                for (int i = 0; i < damage / NPC.lifeMax * 100.0; i++) {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, dustVel.X, dustVel.Y);
                }
            }
            else {
                dustVel = new Vector2(hitDirection, -1f) * 2f;
                for (int i = 0; i < 150; i++) {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, dustVel.X, dustVel.Y);
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
                
                for(int i = 0; i < 4; i++) {
                    int goreHandle = Gore.NewGore(entitySource, NPC.Center, default, Main.rand.Next(61, 64));
                    Gore gore = Main.gore[goreHandle];
                    Gore goreClone = gore;
                    goreClone.velocity *= 0.4f;
                    if(i % 2 == 0) {
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
