using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;


namespace MyMod.Contents.NPCs.Clones.TwinsClone {
    [AutoloadBossHead]
    public class SpazClone : NPCBase {

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
            flyToSideAndShoot,
            shortCharge10,
            // transition
            spin,
            // second stage
            flyToSideAndFireSecondStage,
            charge6SecondStage,
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
        private ref float Timer3 => ref NPC.localAI[2];
        private float SpinAngVel;

        private float RotationTargetStare(Player player) {
            Vector2 fromAbovePlayer = NPC.Bottom - (player.Center + new Vector2(0f, 59f));
            float rotationTarget = fromAbovePlayer.ToRotation() + (float)Math.PI * 0.5f;
            Utils.ClipRad(ref rotationTarget);
            return rotationTarget;
        }

        private void RotationStare(Player player, float acceleration = 0.15f) {
            RotationCorrection(RotationTargetStare(player), acceleration);
        }

        private float RotationShoot(Player player) {
            Vector2 toPlayer = player.Center - NPC.Center;
            float rotation = toPlayer.ToRotation() - (float)Math.PI * 0.5f;
            Utils.ClipRad(ref rotation);
            return rotation;
        }

        private bool Schedule(bool done = true) {
            float lifeRatio = NPC.life / (float)NPC.lifeMax;
            bool flagChange = false;
            if (Stage == Stages.firstStage) {
                if (State == States.flyToSideAndShoot) {
                    if (done) {
                        State = States.shortCharge10;
                        flagChange = true;
                    }
                }
                else if (State == States.shortCharge10) {
                    if (done) {
                        State = States.flyToSideAndShoot;
                        flagChange = true;
                    }
                }
                if (lifeRatio <= 0.4f) {
                    Stage = Stages.transition;
                    State = States.spin;
                    flagChange = true;
                }
            }
            else if (Stage == Stages.transition) {
                if (done) {
                    bossHeadAppearance = 2;

                    Stage = Stages.secondStage;
                    State = States.flyToSideAndFireSecondStage;
                    SetAttributeSecondStage();
                    flagChange = true;
                }
            }
            else if (Stage == Stages.secondStage) {
                if (State == States.flyToSideAndFireSecondStage) {
                    if (done) {
                        State = States.charge6SecondStage;
                        flagChange = true;
                    }
                }
                else if (State == States.charge6SecondStage) {
                    if (done) {
                        State = States.flyToSideAndFireSecondStage;
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
            NPC.defense = NPC.defDefense + 18;
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

            bool flagBrotherActive = false;
            NPC brother = null;
            for (int npcHandle = 0; npcHandle < Main.maxNPCs; npcHandle++) {
                NPC npc = Main.npc[npcHandle];
                if (npc.type == ModContent.NPCType<RetClone>() && npc.active) {
                    flagBrotherActive = true;
                    brother = npc;
                    break;
                }
            }

            // Despawn Condition
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

            // Emit a dust every 5 ticks
            Vector2 dustVel;
            if (Main.rand.NextBool(5)) {
                dustVel = new Vector2(NPC.velocity.X * 0.5f, 0.2f);
                Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y + NPC.height * 0.25f), NPC.width, (int)(NPC.height * 0.5f), DustID.Blood, dustVel.X, dustVel.Y);
            }

            if (Stage == Stages.firstStage) {
                if (State == States.flyToSideAndShoot) {
                    float speed = 12f;
                    float acceleration = 0.4f;
                    if (Main.getGoodWorld) {
                        speed *= 1.15f;
                        acceleration *= 1.15f;
                    }
                    int signSide = Math.Sign(NPC.Center.X - player.Center.X);
                    Vector2 target = player.Center + new Vector2(signSide * 400f, 0f);
                    FlySimple(target, speed, acceleration);
                    RotationStare(player);

                    Timer += 1;
                    if (Timer <= 600) {
                        if (!player.dead) {
                            Timer2 += 1;
                            if (Main.expertMode && flag80) {
                                Timer2 += 0.6f;
                            }
                            if (Main.getGoodWorld) {
                                Timer2 += 0.4f;
                            }
                        }
                        if (Timer2 >= 60) {
                            Timer2 = 0;
                            float shootSpeed = 12f; // ?
                            if (Main.expertMode) {
                                shootSpeed = 14f;
                            }
                            Vector2 projVel = ToPlayer(player, shootSpeed);
                            int projDamage = NPC.GetAttackDamage_ForProjectiles(25f, 22f);
                            if (Main.netMode != NetmodeID.MultiplayerClient) {
                                projVel += Utils.Vector2Noise(40, 2f);
                                Vector2 projPos = NPC.Center + projVel * 4f;
                                var entitySource = NPC.GetSource_FromAI();
                                Projectile.NewProjectile(entitySource, projPos, projVel, ProjectileID.CursedFlameHostile, projDamage, 0f, Main.myPlayer);
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
                else if (State == States.shortCharge10) {
                    Timer += 1;
                    if (Timer == 1) {
                        ForceRetargetPlayers(ref player);

                        float speed = 13f;
                        if (Main.expertMode) {
                            if (flag90) {
                                speed += 0.5f;
                                if (flag80) {
                                    speed += 0.5f;
                                    if (flag70) {
                                        speed += 0.55f;
                                        if (flag60) {
                                            speed += 0.6f;
                                            if (flag50) {
                                                speed += 0.65f;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (Main.getGoodWorld) {
                            speed *= 1.2f;
                        }
                        NPC.velocity = ToPlayer(player, speed);
                    }

                    int chargeLasts = 8;
                    if (Timer <= 1 + chargeLasts) {
                        RotationCharge();
                    }
                    else {
                        VelocityDecay(0.9f, 0.1f);
                        RotationStare(player);
                    }

                    int recoverLasts = 34;
                    if (Timer > 1 + chargeLasts + recoverLasts) {
                        Timer = 0;
                        ChargesDone += 1;
                        NPC.rotation = RotationTargetStare(player);
                        if (ChargesDone == 10) {
                            ChargesDone = 0;
                            Schedule();
                        }
                    }
                    if (Schedule(false)) {
                        Timer = 0;
                        ChargesDone = 0;
                    }
                }
            }
            else if (Stage == Stages.transition) {
                if (State == States.spin) {
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
                    NPC.rotation += SpinAngVel;
                    Utils.ClipRad(ref NPC.rotation);

                    if (Timer == accLasts) {
                        frameAppearance = 2;

                        int goreType6 = Mod.Find<ModGore>("Gore_6").Type;
                        int goreType7 = Mod.Find<ModGore>("Gore_7").Type;
                        int goreType144 = Mod.Find<ModGore>("Gore_144").Type;
                        var entitySource = NPC.GetSource_FromAI(); // ?
                        for (int i = 0; i < 2; i++) {
                            Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType6);
                            Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType7);
                            Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType144);
                        }
                        for (int i = 0; i < 20; i++) {
                            dustVel = Utils.Vector2Noise(30, 6f);
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, dustVel.X, dustVel.Y);
                        }
                        SoundEngine.PlaySound(SoundID.NPCHit1, NPC.Center); // 3
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
            }
            else if (Stage == Stages.secondStage) {
                if (State == States.flyToSideAndFireSecondStage) {
                    float speed = 4f;
                    float acceleration = 0.1f;
                    int signSide = Math.Sign(NPC.Center.X - player.Center.X);
                    Vector2 target = player.Center + new Vector2(signSide * 180f, 0f);
                    float distanceToTarget = Vector2.Distance(target, NPC.Center);
                    if (Main.expertMode) {
                        if (distanceToTarget > 300f) {
                            speed += 0.5f;
                            if (distanceToTarget > 400f) {
                                speed += 0.5f;
                                if (distanceToTarget > 500f) {
                                    speed += 0.55f;
                                    if (distanceToTarget > 600f) {
                                        speed += 0.55f;
                                        if (distanceToTarget > 700f) {
                                            speed += 0.6f;
                                            if (distanceToTarget > 800f) {
                                                speed += 0.6f;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (Main.getGoodWorld) {
                        speed *= 1.15f;
                        acceleration *= 1.15f;
                    }
                    FlySimple(target, speed, acceleration);
                    NPC.rotation = RotationShoot(player);

                    Timer += 1;
                    if (Timer <= 400) {
                        if (Collision.CanHit(NPC.position, NPC.width, NPC.height, player.position, player.width, player.height)) {
                            Timer2 += 1;
                            if (flag75) {
                                Timer2 += 1;
                                if (flag50) {
                                    Timer2 += 1;
                                    if (flag25) {
                                        Timer2 += 1;
                                        if (flag10) {
                                            Timer2 += 2;
                                        }
                                    }
                                }
                            }
                            if (Timer2 >= 8) {
                                Timer2 = 0;
                                Vector2 projVel = ToPlayer(player, 6f);
                                projVel += NPC.velocity * 0.5f;
                                int projDamage = NPC.GetAttackDamage_ForProjectiles(30f, 27f);
                                if (Main.netMode != NetmodeID.MultiplayerClient) {
                                    projVel += Utils.Vector2Noise(40, 0.4f);
                                    Vector2 projPos = NPC.Center + projVel * -1f;
                                    var entitySource = NPC.GetSource_FromAI();
                                    Projectile.NewProjectile(entitySource, projPos, projVel, ProjectileID.EyeFire, projDamage, 0f, Main.myPlayer);
                                }
                            }

                            Timer3 += 1;
                            if(Timer3 >= 22) {
                                Timer3 = 0;
                                SoundEngine.PlaySound(SoundID.Waterfall, NPC.Center);
                            }
                        }
                    }
                    else {
                        Timer = 0;
                        Timer2 = 0;
                        Timer3 = 0;
                        Schedule();
                    }
                    if (Schedule(false)) {
                        Timer = 0;
                        Timer2 = 0;
                        Timer3 = 0;
                    }
                }
                else if (State == States.charge6SecondStage) {
                    Timer += 1;
                    if (Timer == 1) {
                        SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                        ForceRetargetPlayers(ref player);

                        float speed = 14f;
                        if (Main.expertMode) {
                            speed = 16.5f;
                        }
                        NPC.velocity = ToPlayer(player, speed);
                    }

                    if (Main.expertMode) {
                        Timer += 0.5f;
                    }
                    int chargeLasts = 50;
                    if (Timer <= 1 + chargeLasts) {
                        RotationCharge();
                    }
                    else {
                        VelocityDecay(0.93f, 0.1f);
                        RotationStare(player);
                    }

                    int recoverLasts = 30;
                    if (Timer > 1 + chargeLasts + recoverLasts) {
                        Timer = 0;
                        ChargesDone += 1;
                        NPC.rotation = RotationTargetStare(player);
                        if (ChargesDone == 6) {
                            ChargesDone = 0;
                            Schedule();
                        }
                    }
                    if (Schedule(false)) {
                        Timer = 0;
                        ChargesDone = 0;
                    }
                }
            }
        }

        private int frameAppearance = 1;
        private int bossHeadAppearance = 1;

        public override string BossHeadTexture => "MyMod/Contents/NPCs/Bosses/TwinsClone/SpazBossHead1";
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

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Spazmatism Clone");
            NPCID.Sets.MPAllowedEnemies[Type] = true;

            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData {
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
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath14;

            NPC.damage = 50;
            NPC.defense = 10;
            NPC.lifeMax = 23000;
            NPC.knockBackResist = 0f;

            NPC.noGravity = true;
            NPC.noTileCollide = true;

            NPC.value = Item.buyPrice(gold: 12);
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
                    Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType2);
                    Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType7);
                    Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType9);
                    Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType146);
                }
            }
        }

    }
}
