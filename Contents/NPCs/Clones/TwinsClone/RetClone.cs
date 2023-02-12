using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

/*
 * Clone of the Twins
 * type = 125/126, aiStyle = 30/31
 * Not implemented: afterimage effect, chain, bestiary, loot, chained loot
 * Known bugs: movement flaws
 */
namespace MyMod.Contents.NPCs.Clones.TwinsClone {
    [AutoloadBossHead]
    public class RetClone : NPCBase {
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
            flyToSideAboveAndShoot,
            charge4,
            // transition
            spin,
            // second stage
            flyToAboveAndShootSecondStage,
            flyToSideAndFastShootSecondStage,
        };
        private States State {
            get => (States)NPC.ai[1];
            set => NPC.ai[1] = (float)value;
        }

        private int ChargesDone {
            get => (int)NPC.ai[2];
            set => NPC.ai[2] = value;
        }

        private ref float Timer => ref NPC.localAI[0];
        private ref float Timer2 => ref NPC.localAI[1];

        private float SpinAngVel;
        protected override bool Init() {
            rotationOffset = MathHelper.Pi * 0.5f;
            return true;
        }
        public override void AI() {

            bool Schedule(bool done = true) {
                bool flagChange = false;

                switch (Stage) {
                    case Stages.firstStage:
                        switch (State) {
                            case States.flyToSideAboveAndShoot:
                                if (done) {
                                    State = States.charge4;
                                    flagChange = true;
                                }
                                break;
                            case States.charge4:
                                if (done) {
                                    ChargesDone += 1;
                                    if (ChargesDone == 4) {
                                        ChargesDone = 0;
                                        State = States.flyToSideAboveAndShoot;
                                    }
                                    flagChange = true;
                                }
                                break;
                        }
                        if (NPC.life / (float)NPC.lifeMax <= 0.4f) {
                            Stage = Stages.transition;
                            State = States.spin;
                            ChargesDone = 0;
                            flagChange = true;
                        }
                        break;
                    case Stages.transition:
                        if (State == States.spin) {
                            if (done) {
                                bossHeadAppearance = 2;

                                Stage = Stages.secondStage;
                                State = States.flyToAboveAndShootSecondStage;
                                SetDefaultsSecondStage();
                                flagChange = true;
                            }
                        }
                        break;
                    case Stages.secondStage:
                        switch (State) {
                            case States.flyToAboveAndShootSecondStage:
                                if (done) {
                                    State = States.flyToSideAndFastShootSecondStage;
                                    flagChange = true;
                                }
                                break;
                            case States.flyToSideAndFastShootSecondStage:
                                if (done) {
                                    State = States.flyToAboveAndShootSecondStage;
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

            void FlyToSideAboveAndShoot(Player player) {
                float speed = 7f;
                float acceleration = 0.1f;
                if (Main.expertMode) {
                    speed = 8.25f;
                    acceleration = 0.115f;
                }
                if (Main.getGoodWorld) {
                    speed *= 1.15f;
                    acceleration *= 1.15f;
                }
                int signSide = Math.Sign(NPC.Center.X - player.Center.X);
                Vector2 target = player.Center + new Vector2(signSide * 300f, -300f);
                FlySimple(target, speed, acceleration);
                RotationStare(player);

                Timer += 1;
                if (Timer <= 600) {
                    bool flagNear = Vector2.Distance(NPC.Center, target) < 400f && NPC.position.Y + NPC.height < player.position.Y;
                    if (flagNear) {
                        if (!player.dead) {
                            Timer2 += 1;
                            if (Main.expertMode) {
                                float lifeRatio = NPC.life / (float)NPC.lifeMax;
                                if (lifeRatio <= 0.90f) {
                                    Timer2 += 0.3f;
                                    if (lifeRatio <= 0.80f) {
                                        Timer2 += 0.3f;
                                        if (lifeRatio <= 0.70f) {
                                            Timer2 += 0.3f;
                                            if (lifeRatio <= 0.60f) {
                                                Timer2 += 0.3f;
                                            }
                                        }
                                    }
                                }
                            }
                            if (Main.getGoodWorld) {
                                Timer2 += 0.5f;
                            }
                        }
                        if (Timer2 >= 60) {
                            Timer2 = 0;
                            float shootSpeed = 9f;
                            if (Main.expertMode) {
                                shootSpeed = 10.5f;
                            }
                            Vector2 projVel = ToPlayer(player, shootSpeed);
                            int projDamage = NPC.GetAttackDamage_ForProjectiles(20f, 19f);
                            if (Main.netMode != NetmodeID.MultiplayerClient) {
                                projVel += Utils.Vector2Noise(40, 3.2f);
                                Vector2 projPos = NPC.Center + projVel * 15f;
                                var entitySource = NPC.GetSource_FromAI();
                                Projectile.NewProjectile(entitySource, projPos, projVel, ProjectileID.EyeLaser, projDamage, 0f, Main.myPlayer);
                            }
                        }
                    }
                }
                else {
                    Timer = 0;
                    Timer2 = 0;
                    Schedule();
                }
                if (Schedule(false)) {
                    Timer = 0;
                    Timer2 = 0;
                }
            }

            void Charge(Player player) {
                Timer += 1;
                if (Timer == 1) {
                    ForceRetargetPlayers(ref player);

                    float speed = 12f;
                    if (Main.expertMode) {
                        speed = 15f;
                    }
                    if (Main.getGoodWorld) {
                        speed += 2f;
                    }
                    NPC.velocity = ToPlayer(player, speed);
                }

                int chargeLasts = 25;
                if (Timer <= 1 + chargeLasts) {
                    RotationCharge();
                }
                else {
                    VelocityDecay(0.96f, 0.1f);
                    RotationStare(player);
                }

                int recoverLasts = 45;
                if (Timer > 1 + chargeLasts + recoverLasts) {
                    Timer = 0;
                    Rotation = GetRotationTargetStare(player);
                    Schedule();
                }
                if (Schedule(false)) {
                    Timer = 0;
                }
            }

            void Spin() {
                Timer += 1;
                VelocityDecay(0.98f, 0.1f);

                if (Timer == 1) {
                    SpinAngVel = 0f;
                }
                int accLasts = 100;
                int descLasts = 100;
                if (Timer <= accLasts) {
                    SpinAngVel += 0.005f;
                    if (SpinAngVel > 0.5f) {
                        SpinAngVel = 0.5f;
                    }
                }
                else if (Timer <= accLasts + descLasts) {
                    SpinAngVel -= 0.005f;
                    if (SpinAngVel < 0f) {
                        SpinAngVel = 0f;
                    }
                }
                Rotation = Utils.ClipRad(Rotation + SpinAngVel);

                Vector2 dustVel;
                if (Timer == accLasts) {
                    frameAppearance = 2;

                    int goreType6 = Mod.Find<ModGore>("Gore_6").Type;
                    int goreType7 = Mod.Find<ModGore>("Gore_7").Type;
                    int goreType143 = Mod.Find<ModGore>("Gore_143").Type;
                    var entitySource = NPC.GetSource_FromAI(); // ?
                    for (int i = 0; i < 2; i++) {
                        Gore.NewGore(entitySource, NPC.Center, Utils.Vector2Noise(30, 6f), goreType6);
                        Gore.NewGore(entitySource, NPC.Center, Utils.Vector2Noise(30, 6f), goreType7);
                        Gore.NewGore(entitySource, NPC.Center, Utils.Vector2Noise(30, 6f), goreType143);
                    }
                    for (int i = 0; i < 20; i++) {
                        dustVel = Utils.Vector2Noise(30, 6f);
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, dustVel.X, dustVel.Y);
                    }
                    SoundEngine.PlaySound(SoundID.NPCHit1, NPC.Center);
                    SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                }
                if (Timer >= accLasts + descLasts) {
                    Timer = 0;
                    Schedule();
                }
                if (Schedule(false)) {
                    Timer = 0;
                }

                // Emit a dust every tick
                dustVel = Utils.Vector2Noise(30, 6f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, dustVel.X, dustVel.Y);
            }

            void FlyToAboveAndShootSecondStage(Player player) {
                float speed = 8f;
                float acceleration = 0.15f;
                if (Main.expertMode) {
                    speed = 9.5f;
                    acceleration = 0.175f;
                }
                if (Main.getGoodWorld) {
                    speed *= 1.15f;
                    acceleration *= 1.15f;
                }
                Vector2 target = player.Center + new Vector2(0f, -300f);
                FlySimple(target, speed, acceleration);

                Rotation = GetRotationShoot(player);

                Timer += 1;
                if (Timer <= 300) {
                    Timer2 += 1;
                    float lifeRatio = NPC.life / (float)NPC.lifeMax;
                    if (lifeRatio <= 0.75f) {
                        Timer2 += 1;
                        if (lifeRatio <= 0.50f) {
                            Timer2 += 1;
                            if (lifeRatio <= 0.25f) {
                                Timer2 += 1;
                                if (lifeRatio <= 0.10f) {
                                    Timer2 += 2;
                                }
                            }
                        }
                    }
                    if (Timer2 >= 180) {
                        if (Collision.CanHit(NPC.position, NPC.width, NPC.height, player.position, player.width, player.height)) {
                            Timer2 = 0;
                            float shootSpeed = 8.5f;
                            if (Main.expertMode) {
                                shootSpeed = 10f;
                            }
                            Vector2 projVel = ToPlayer(player, shootSpeed);
                            Vector2 projPos = NPC.Center + projVel * 15f;
                            int projDamage = NPC.GetAttackDamage_ForProjectiles(25f, 23f);
                            if (Main.netMode != NetmodeID.MultiplayerClient) {
                                var entitySource = NPC.GetSource_FromAI();
                                Projectile.NewProjectile(entitySource, projPos, projVel, ProjectileID.DeathLaser, projDamage, 0f, Main.myPlayer);
                            }
                        }
                    }
                }
                else {
                    Timer = 0;
                    Timer2 = 0;
                    Schedule();
                }
                if (Schedule(false)) {
                    Timer = 0;
                    Timer2 = 0;
                }
            }

            void FlyToSideAndFastShoot(Player player) {
                float speed = 8f;
                float acceleration = 0.2f;
                if (Main.expertMode) {
                    speed = 9.5f;
                    acceleration = 0.25f;
                }
                if (Main.getGoodWorld) {
                    speed *= 1.15f;
                    acceleration *= 1.15f;
                }
                int signSide = Math.Sign(NPC.Center.X - player.Center.X);
                Vector2 target = player.Center + new Vector2(signSide * 340f, 0f);
                FlySimple(target, speed, acceleration);
                Rotation = GetRotationShoot(player);

                Timer += 1;
                if (Timer <= 180) {
                    Timer2 += 1;
                    float lifeRatio = NPC.life / (float)NPC.lifeMax;
                    if (lifeRatio <= 0.75f) {
                        Timer2 += 0.5f;
                        if (lifeRatio <= 0.50f) {
                            Timer2 += 0.75f;
                            if (lifeRatio <= 0.25f) {
                                Timer2 += 1f;
                                if (lifeRatio <= 0.10f) {
                                    Timer2 += 1.5f;
                                }
                            }
                        }
                    }
                    if (Main.expertMode) {
                        Timer2 += 1.5f;
                    }
                    if (Timer2 >= 60) {
                        if (Collision.CanHit(NPC.position, NPC.width, NPC.height, player.position, player.width, player.height)) {
                            Timer2 = 0;
                            Vector2 projVel = ToPlayer(player, 9f);
                            Vector2 projPos = NPC.Center + projVel * 15f;
                            int projDamage = NPC.GetAttackDamage_ForProjectiles(18f, 17f);
                            if (Main.netMode != NetmodeID.MultiplayerClient) {
                                var entitySource = NPC.GetSource_FromAI();
                                Projectile.NewProjectile(entitySource, projPos, projVel, ProjectileID.DeathLaser, projDamage, 0f, Main.myPlayer);
                            }
                        }
                    }
                }
                else {
                    Timer = 0;
                    Timer2 = 0;
                    Schedule();
                }
                if (Schedule(false)) {
                    Timer = 0;
                    Timer2 = 0;
                }
            }

            float GetRotationTargetStare(Player player) {
                Vector2 fromAbovePlayer = NPC.Bottom - (player.Center + new Vector2(0f, 59f));
                float rotationTarget = fromAbovePlayer.ToRotation() + (float)Math.PI * 0.5f;
                Utils.ClipRad(ref rotationTarget);
                return rotationTarget;
            }

            void RotationStare(Player player, float acceleration = 0.1f) {
                RotationCorrection(GetRotationTargetStare(player), acceleration);
            }

            float GetRotationShoot(Player player) {
                Vector2 toPlayer = player.Center - NPC.Center;
                float rotation = toPlayer.ToRotation() - (float)Math.PI * 0.5f;
                Utils.ClipRad(ref rotation);
                return rotation;
            }


            // Target player
            RetargetPlayers();
            Player player = Main.player[NPC.target];

            // Detect brother
            bool flagBrotherActive = false;
            NPC brother = null;
            for (int npcHandle = 0; npcHandle < Main.maxNPCs; npcHandle++) {
                NPC npc = Main.npc[npcHandle];
                if (npc.type == ModContent.NPCType<SpazClone>() && npc.active) {
                    flagBrotherActive = true;
                    brother = npc;
                    break;
                }
            }

            // Despawn condition
            if (Main.dayTime || player.dead) {
                NPC.velocity.Y -= 0.04f;
                RotationStare(player);
                NPC.EncourageDespawn(10);
                return;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                if (NPC.timeLeft < 10 && flagBrotherActive) {
                    NPC.DiscourageDespawn(brother.timeLeft - 1);
                }
            }

            // Attacks
            switch (State) {
                case States.flyToSideAboveAndShoot:
                    FlyToSideAboveAndShoot(player);
                    break;
                case States.charge4:
                    Charge(player);
                    break;
                case States.spin:
                    Spin();
                    break;
                case States.flyToAboveAndShootSecondStage:
                    FlyToAboveAndShootSecondStage(player);
                    break;
                case States.flyToSideAndFastShootSecondStage:
                    FlyToSideAndFastShoot(player);
                    break;
            }

            // Emit a dust every 5 ticks
            Vector2 dustVel;
            if (Main.rand.NextBool(5)) {
                dustVel = new Vector2(NPC.velocity.X * 0.5f, 0.2f);
                Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y + NPC.height * 0.25f), NPC.width, (int)(NPC.height * 0.5f), DustID.Blood, dustVel.X, dustVel.Y);
            }
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Retinazer Clone");
            NPCID.Sets.MPAllowedEnemies[Type] = true;

            NPCDebuffImmunityData debuffData = new() {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);

            Main.npcFrameCount[Type] = 6;
        }

        public override void SetDefaults() {
            NPC.width = 100;
            NPC.height = 110;

            NPC.damage = 45;
            NPC.defense = 10;
            NPC.lifeMax = 20000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;

            NPC.aiStyle = -1;
            NPC.boss = true;
            NPC.SpawnWithHigherTime(30);
            NPC.npcSlots = 5f;

            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = Item.buyPrice(gold: 12);
        }

        private void SetDefaultsSecondStage() {
            NPC.damage = (int)(NPC.defDamage * 1.5f);
            NPC.defense = NPC.defDefense + 10;
            NPC.HitSound = SoundID.NPCHit4;
        }

        private int frameAppearance = 1;
        private int bossHeadAppearance = 1;

        public override string BossHeadTexture => "MyMod/Contents/NPCs/Bosses/TwinsClone/RetBossHead1";
        private static int BossHeadSlotSecondStage = -1;
        public override void Load() {
            string texture = BossHeadTexture.Replace('1', '2');
            BossHeadSlotSecondStage = Mod.AddBossHeadTexture(texture, -1);
        }
        public override void BossHeadSlot(ref int index) {
            int slot = BossHeadSlotSecondStage;
            if (bossHeadAppearance == 2 && slot != -1) {
                index = slot;
            }
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
