using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

/*
 * Clone of Eye of Cuthulhu
 * type = 4, aiStyle = 4
 * Not implemented: afterimage effect, bestiary, loot
 * Known bugs: sound effect
 */
namespace MyMod.Contents.NPCs.Clones.EOCClone {
    [AutoloadBossHead]
    public class EOCClone : NPCBase {

        private enum Stages {
            firstStage,
            transition,
            secondStage
        }
        private Stages Stage {
            get => (Stages)(NPC.ai[0]);
            set => NPC.ai[0] = (float)value;
        }

        private int Appearance() {
            if (Stage == Stages.firstStage || (Stage == Stages.transition && State == States.spin1)) {
                return 1;
            }
            else if (Stage == Stages.secondStage || (Stage == Stages.transition && State == States.spin2)) {
                return 2;
            }
            else {
                return -1;
            }
        }

        private enum States {
            // first stage
            approachAboveAndSummon,
            initCharge,
            chargeAndRecover,
            // transition
            spin1,
            spin2,
            // second stage
            approachSecondStage,
            initChargeSecondStage,
            chargeAndRecoverSecondStage,
            initSuperCharge,
            superChargeAndRecover,
            approachBelow,
        };
        private States State {
            get => (States)NPC.ai[1];
            set => NPC.ai[1] = (float)value;
        }

        private int ChargesDone {
            get => (int)NPC.ai[2];
            set => NPC.ai[2] = value;
        }
        private bool FasterSuperCharge {
            get => (NPC.ai[3] == -1f);
            set => NPC.ai[3] = value ? -1f : 0f;
        }
        private int Timer {
            get => (int)NPC.localAI[0];
            set => NPC.localAI[0] = value;
        }
        private int Timer2 {
            get => (int)NPC.localAI[1];
            set => NPC.localAI[1] = value;
        }
        private ref float SpinAngVel => ref NPC.localAI[1];

        private float RotationTargetStare(Player player) {
            Vector2 fromAbovePlayer = NPC.Bottom - (player.Center + new Vector2(0f, 59f));
            float rotationTarget = fromAbovePlayer.ToRotation() + (float)Math.PI * 0.5f;
            Utils.ClipRad(ref rotationTarget);
            return rotationTarget;
        }

        public override void AI() {
            // Flags
            bool flag65 = NPC.life < NPC.lifeMax * 0.65f;
            bool flag50 = NPC.life < NPC.lifeMax * 0.50f;
            bool flag35 = NPC.life < NPC.lifeMax * 0.35f;
            bool flag12 = NPC.life < NPC.lifeMax * 0.12f;
            bool flag04 = NPC.life < NPC.lifeMax * 0.04f;

            // Target Player
            RetargetPlayers();
            Player player = Main.player[NPC.target];

            // Despawn Condition
            if (Main.dayTime || player.dead) {
                NPC.velocity.Y -= 0.04f;
                NPC.EncourageDespawn(10);
                return;
            }

            // Emit a dust every 5 ticks
            Vector2 dustVel;
            if (Main.rand.NextBool(5)) {
                dustVel = new Vector2(NPC.velocity.X * 0.5f, 0.2f);
                Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y + NPC.height * 0.25f), NPC.width, (int)(NPC.height * 0.5f), DustID.Blood, dustVel.X, dustVel.Y);
            }
            if (Stage == Stages.firstStage) {
                if (State == States.approachAboveAndSummon) {
                    float angVel = 0.02f;
                    if (Main.expertMode) {
                        angVel *= 1.5f;
                    }
                    float rotationTarget = RotationTargetStare(player);
                    RotationCorrection(rotationTarget, angVel);

                    float speed = 5f;
                    float acceleration = 0.04f;
                    if (Main.expertMode) {
                        speed = 7f;
                        acceleration = 0.15f;
                    }
                    if (Main.getGoodWorld) {
                        speed += 1f;
                        acceleration += 0.05f;
                    }
                    Vector2 target = player.Center + new Vector2(0f, -200f);
                    FlySimple(target, speed, acceleration);

                    Timer += 1;
                    float stateLasts = 600f;
                    if (Main.expertMode) {
                        stateLasts *= 0.35f;
                    }
                    if (Timer <= stateLasts) {
                        float distanceToTarget = Vector2.Distance(target, NPC.Center);
                        bool flagNear = distanceToTarget < 500f && (Main.expertMode || NPC.position.Y + NPC.height < player.position.Y);
                        if (flagNear) {
                            if (!player.dead) {
                                Timer2 += 1;
                            }
                            float summonLasts = 110f;
                            if (Main.expertMode) {
                                summonLasts *= 0.4f;
                            }
                            if (Main.getGoodWorld) {
                                summonLasts *= 0.8f;
                            }
                            if (Timer2 >= summonLasts) {
                                Timer2 = 0;
                                NPC.rotation = rotationTarget;

                                float minionSpeed = 5f;
                                if (Main.expertMode) {
                                    minionSpeed = 6f;
                                }
                                Vector2 toPlayer = player.Center - NPC.Center;
                                toPlayer.Normalize();
                                Vector2 minionVel = toPlayer * minionSpeed;
                                Vector2 minionPos = NPC.Center + minionVel * 10f;

                                int minionHandle = NewNPC(NPCID.ServantofCthulhu, minionPos, minionVel);
                                NPC minion = Main.npc[minionHandle];

                                dustVel = minionVel * 0.4f;
                                for (int i = 0; i < 10; i++) {
                                    Dust.NewDust(minionPos, minion.width, minion.height, DustID.Blood, dustVel.X, dustVel.Y);
                                }
                                SoundEngine.PlaySound(SoundID.NPCHit1, minionPos); // 3
                            }
                        }
                    }
                    else {
                        Timer = 0;
                        State = States.initCharge;
                        Timer2 = 0;
                        NPC.netUpdate = true;
                    }
                }
                else if (State == States.initCharge) {
                    ForceRetargetPlayers(ref player);

                    float speed = 6f;
                    if (Main.expertMode) {
                        speed = 7f;
                    }
                    if (Main.getGoodWorld) {
                        speed += 1f;
                    }
                    Vector2 toPlayer = player.Center - NPC.Center;
                    toPlayer.Normalize();
                    NPC.velocity = toPlayer * speed;

                    State = States.chargeAndRecover;
                    NPC.netUpdate = true;
                    if (NPC.netSpam > 10) {
                        NPC.netSpam = 10;
                    }
                }
                else if (State == States.chargeAndRecover) {
                    int chargeLasts = 40;
                    Timer += 1;
                    if(Timer <= chargeLasts) {
                        RotationCharge();
                    }
                    else {
                        float angVel = 0.05f;
                        if (Main.expertMode) {
                            angVel *= 1.5f;
                        }
                        float rotationTarget = RotationTargetStare(player);
                        RotationCorrection(rotationTarget, angVel);

                        float decay = 0.98f;
                        if (Main.expertMode) {
                            decay *= 0.985f;
                        }
                        if (Main.getGoodWorld) {
                            decay *= 0.99f;
                        }
                        VelocityDecay(decay, 0.1f);
                    }

                    int recoverLasts = 110;
                    if (Main.expertMode) {
                        recoverLasts = 60;
                    }
                    if (Main.getGoodWorld) {
                        recoverLasts -= 15;
                    }
                    if (Timer > chargeLasts + recoverLasts) {
                        Timer = 0;
                        ChargesDone += 1;
                        NPC.rotation = RotationTargetStare(player);
                        if (ChargesDone == 3) {
                            ChargesDone = 0;
                            State = States.approachAboveAndSummon;
                        }
                        else {
                            State = States.initCharge;
                        }
                    }
                }

                // Detect Transition
                bool flag = flag50;
                if (Main.expertMode) {
                    flag = flag65;
                }
                if (flag) {
                    Stage = Stages.transition;
                    State = States.spin1;
                    Timer = 0;
                    Timer2 = 0;
                    ChargesDone = 0;
                    NPC.netUpdate = true;
                    if (NPC.netSpam > 10) {
                        NPC.netSpam = 10;
                    }
                }
            }
            else if (Stage == Stages.transition) {
                // Stay still
                VelocityDecay(0.98f, 0.1f);

                // Spin
                if (State == States.spin1) {
                    SpinAngVel += 0.005f;
                    if (SpinAngVel > 0.5f) {
                        SpinAngVel = 0.5f;
                    }
                }
                else if (State == States.spin2){
                    SpinAngVel -= 0.005f;
                    if (SpinAngVel < 0f) {
                        SpinAngVel = 0f;
                    }
                }
                NPC.rotation += SpinAngVel;
                Utils.ClipRad(ref NPC.rotation);

                Timer += 1;
                // In expert mode, summon a minion(with random vel) every 20 ticks
                if (Main.expertMode && Timer % 20 == 0) {
                    float minionSpeed = 5f;
                    Vector2 minionVel = Utils.Vector2Noise(200, minionSpeed);
                    Vector2 minionPos = NPC.Center + minionVel * 10f;

                    int minionHandle = NewNPC(NPCID.ServantofCthulhu, minionPos, minionVel);
                    NPC minion = Main.npc[minionHandle];

                    dustVel = minionVel * 0.4f;
                    for (int i = 0; i < 10; i++) {
                        Dust.NewDust(minionPos, minion.width, minion.height, DustID.Blood, dustVel.X, dustVel.Y);
                    }
                }
                
                // Spin speed increases for 100 ticks, then decreases for 100 ticks
                if (Timer >= 100f) {
                    Timer = 0;
                    if (State == States.spin1) {
                        State = States.spin2;

                        SoundEngine.PlaySound(SoundID.NPCHit1, NPC.position); // 3

                        int goreType6 = Mod.Find<ModGore>("Gore_6").Type;
                        int goreType7 = Mod.Find<ModGore>("Gore_7").Type;
                        int goreType8 = Mod.Find<ModGore>("Gore_8").Type;
                        var entitySource = NPC.GetSource_Death(); // ?
                        for (int i = 0; i < 2; i++) {
                            Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType6);
                            Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType7);
                            Gore.NewGore(entitySource, NPC.position, Utils.Vector2Noise(30, 6f), goreType8);
                        }
                        for (int i = 0; i < 20; i++) {
                            dustVel = Utils.Vector2Noise(30, 6f);
                            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, dustVel.X, dustVel.Y);
                        }
                        SoundEngine.PlaySound(SoundID.Roar, NPC.position);
                    }
                    else if (State == States.spin2) {
                        Stage = Stages.secondStage;
                        State = States.approachSecondStage;
                        SpinAngVel = 0f;
                    }
                }

                // Emit a dust every tick
                dustVel = Utils.Vector2Noise(30, 6f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, dustVel.X, dustVel.Y);
            }
            else if (Stage == Stages.secondStage) {
                // Set attributes
                NPC.defense = 0;
                int normalDamage = 23;
                int expertDamage = 18;
                if (Main.expertMode) {
                    if (flag12) {
                        NPC.defense = -15;
                    }
                    if (flag04) {
                        expertDamage = 20;
                        NPC.defense = -30;
                    }
                }
                NPC.damage = NPC.GetAttackDamage_LerpBetweenFinalValues(normalDamage, expertDamage);
                NPC.damage = NPC.GetAttackDamage_ScaledByStrength(NPC.damage);

                /*
                Approach
                    (skip)flag04Expert -> super charge inf
                    (skip)flag12Expert -> approach below
                    flag35Expert -> super charge 5
                    default -> charge 3
                Charge
                    flag50Expert -> super charge 2-4
                    default -> approach
                Super charge
                    flag04Expert -> super charge inf
                    flag12Expert -> approach below
                    default -> approach
                Approach below
                    (skip)flag04Expert -> super charge inf
                    default -> super charge 5-8(first fast)
                 */

                if (State == States.approachSecondStage) {
                    float angVel = 0.05f;
                    if (Main.expertMode) {
                        angVel *= 1.5f;
                    }
                    float rotationTarget = RotationTargetStare(player);
                    RotationCorrection(rotationTarget, angVel);

                    float speed = 6f;
                    float acceleration = 0.07f;
                    Vector2 target = player.position + new Vector2(0f, -120f);
                    float distanceToTarget = Vector2.Distance(target, NPC.Center);
                    if (Main.expertMode) {
                        if (distanceToTarget > 400f) {
                            speed += 1f;
                            acceleration += 0.05f;
                            if (distanceToTarget > 600f) {
                                speed += 1f;
                                acceleration += 0.05f;
                                if (distanceToTarget > 800f) {
                                    speed += 1f;
                                    acceleration += 0.05f;
                                }
                            }
                        }
                    }
                    if (Main.getGoodWorld) {
                        speed += 1f;
                        acceleration += 0.1f;
                    }
                    FlySimple(target, speed, acceleration);

                    Timer += 1;
                    if (Timer >= 200) {
                        Timer = 0;
                        if (Main.expertMode && flag35) {
                            State = States.initSuperCharge;
                        }
                        else {
                            State = States.initChargeSecondStage;
                        }
                        NPC.netUpdate = true;
                    }

                    // Skips
                    if (Main.expertMode) {
                        if (flag04) {
                            State = States.initSuperCharge;
                            Timer = 0;
                            ChargesDone = -1000;
                        }
                        else if (flag12) {
                            State = States.approachBelow;
                            Timer = 0;
                        }
                    }
                }
                else if (State == States.initChargeSecondStage) {
                    SoundEngine.PlaySound(SoundID.ForceRoar, NPC.position);

                    ForceRetargetPlayers(ref player);

                    float speed = 6.8f;
                    if (Main.expertMode && ChargesDone == 1) {
                        speed *= 1.15f;
                    }
                    if (Main.expertMode && ChargesDone == 2) {
                        speed *= 1.3f;
                    }
                    if (Main.getGoodWorld) {
                        speed *= 1.2f;
                    }
                    Vector2 toPlayer = player.Center - NPC.Center;
                    toPlayer.Normalize();
                    NPC.velocity = toPlayer * speed;

                    State = States.chargeAndRecoverSecondStage;
                    NPC.netUpdate = true;
                    if (NPC.netSpam > 10) {
                        NPC.netSpam = 10;
                    }
                }
                else if (State == States.chargeAndRecoverSecondStage) {
                    int chargeLasts = 40;
                    if (Main.expertMode) {
                        chargeLasts = 50;
                    }

                    Timer += 1;
                    if (Timer <= chargeLasts) {
                        RotationCharge();
                    }
                    else {
                        float angVel = 0.08f;
                        if (Main.expertMode) {
                            angVel *= 1.5f;
                        }
                        float rotationTarget = RotationTargetStare(player);
                        RotationCorrection(rotationTarget, angVel);

                        float decay = 0.97f;
                        if (Main.expertMode) {
                            decay *= 0.98f;
                        }
                        VelocityDecay(decay, 0.1f);
                    }

                    int recoverLasts = 90;
                    if (Main.expertMode) {
                        recoverLasts = 40;
                    }
                    if (Timer > chargeLasts + recoverLasts) {
                        Timer = 0;
                        ChargesDone += 1;
                        NPC.rotation = RotationTargetStare(player);
                        if (ChargesDone == 3) {
                            ChargesDone = 0;
                            if (Main.expertMode && flag50) {
                                if (Main.netMode != NetmodeID.MultiplayerClient) {
                                    State = States.initSuperCharge;
                                    ChargesDone = Main.rand.Next(1, 4);
                                }
                            }
                            else {
                                State = States.approachSecondStage;
                            }
                            NPC.netUpdate = true;
                            if (NPC.netSpam > 10) {
                                NPC.netSpam = 10;
                            }
                        }
                        else {
                            State = States.initChargeSecondStage;
                        }
                    }
                }
                else if (State == States.initSuperCharge) {
                    // ?
                    if (ChargesDone == 4 && Main.expertMode && flag12 && NPC.Center.Y > player.Center.Y) {
                        ChargesDone = 0;
                        State = States.approachSecondStage;
                        NPC.netUpdate = true;
                        if (NPC.netSpam > 10) {
                            NPC.netSpam = 10;
                        }
                    }
                    else {
                        if (Main.netMode != NetmodeID.MultiplayerClient) {
                            ForceRetargetPlayers(ref player);

                            float speed = 20f;
                            float postJudgeTime = 10f; // Bug here
                            if (Main.expertMode && flag04) {
                                postJudgeTime *= 2f;
                            }
                            else if (FasterSuperCharge) {
                                FasterSuperCharge = false;
                                postJudgeTime *= 4f;
                                speed *= 1.3f;
                            }
                            Vector2 target = player.position + new Vector2(-player.velocity.X * postJudgeTime, -player.velocity.Y * postJudgeTime / 4);
                            Vector2 toTarget = target - NPC.Center;
                            toTarget = Vector2.Multiply(toTarget, Utils.Vector2Noise(10, 0.1f) + Vector2.One);
                            if (Main.expertMode && flag04) {
                                toTarget = Vector2.Multiply(toTarget, Utils.Vector2Noise(10, 0.1f) + Vector2.One);
                            }
                            float distanceToTarget = toTarget.Length();
                            toTarget.Normalize();
                            NPC.velocity = toTarget * speed;
                            NPC.velocity += Utils.Vector2Noise(20, 2f);

                            if (Main.expertMode && flag04) {
                                NPC.velocity += Utils.Vector2Noise(50, 5f);
                                float velX = Math.Abs(NPC.velocity.X);
                                float velY = Math.Abs(NPC.velocity.Y);
                                if (NPC.Center.X > player.Center.X) {
                                    velY *= -1f;
                                }
                                if (NPC.Center.Y > player.Center.Y) {
                                    velX *= -1f;
                                }
                                NPC.velocity.X += velY;
                                NPC.velocity.Y += velX;
                                NPC.velocity.Normalize();
                                NPC.velocity *= speed;
                                NPC.velocity += Utils.Vector2Noise(20, 2f);
                            }
                            else if (distanceToTarget < 100f) {
                                if (Math.Abs(NPC.velocity.X) > Math.Abs(NPC.velocity.Y)) {
                                    float velX = Math.Abs(NPC.velocity.X);
                                    float velY = Math.Abs(NPC.velocity.Y);
                                    if (NPC.Center.X > player.Center.X) {
                                        velY *= -1f;
                                    }
                                    if (NPC.Center.Y > player.Center.Y) {
                                        velX *= -1f;
                                    }
                                    NPC.velocity.X = velY;
                                    NPC.velocity.Y = velX;
                                }
                            }
                            else if (Math.Abs(NPC.velocity.X) > Math.Abs(NPC.velocity.Y)) {
                                float velX = (Math.Abs(NPC.velocity.X) + Math.Abs(NPC.velocity.Y)) / 2f;
                                float velY = velX;
                                if (NPC.Center.X > player.Center.X) {
                                    velY *= -1f;
                                }
                                if (NPC.Center.Y > player.Center.Y) {
                                    velX *= -1f;
                                }
                                NPC.velocity.X = velY;
                                NPC.velocity.Y = velX;
                            }
                            
                            State = States.superChargeAndRecover;
                            NPC.netUpdate = true;
                            if (NPC.netSpam > 10) {
                                NPC.netSpam = 10;
                            }
                        }
                    }
                }
                else if (State == States.superChargeAndRecover) {
                    if (Timer == 0) {
                        SoundEngine.PlaySound(SoundID.ForceRoar, NPC.position);
                    }

                    int superChargeLasts = 20;
                    if (Main.expertMode && flag04) {
                        superChargeLasts = 10;
                    }
                    Timer += 1;
                    // If too close when charge ends, continue to charge some ticks
                    if (Timer == superChargeLasts && Vector2.Distance(NPC.Center, player.Center) < 200f) {
                        Timer -= 1;
                    }

                    if(Timer <= superChargeLasts) {
                        RotationCharge();
                    }
                    else {
                        float angVel = 0.15f;
                        if (Main.expertMode) {
                            angVel *= 1.5f;
                        }
                        float rotationTarget = RotationTargetStare(player);
                        RotationCorrection(rotationTarget, angVel);

                        VelocityDecay(0.95f, 0.1f);
                    }

                    const int recoverLasts = 13;
                    if (Timer > superChargeLasts + recoverLasts) {
                        Timer = 0;
                        ChargesDone += 1;
                        NPC.rotation = RotationTargetStare(player);
                        if (ChargesDone >= 5) {
                            ChargesDone = 0;
                            State = States.approachSecondStage;
                        }
                        else {
                            State = States.initSuperCharge;
                        }
                        NPC.netUpdate = true;
                        if (NPC.netSpam > 10) {
                            NPC.netSpam = 10;
                        }
                    }
                }
                else if (State == States.approachBelow) {
                    float angVel = 0.05f;
                    if (Main.expertMode) {
                        angVel *= 1.5f;
                    }
                    float rotationTarget = RotationTargetStare(player);
                    RotationCorrection(rotationTarget, angVel);

                    float speed = 9f;
                    float acceleration = 0.3f;
                    Vector2 target = player.position + new Vector2(0f, 600f);
                    FlySimple(target, speed, acceleration);

                    Timer += 1;
                    // Followed by faster and more super charge
                    if (Timer > 70) {
                        Timer = 0;
                        State = States.initSuperCharge;
                        FasterSuperCharge = true;
                        ChargesDone = Main.rand.Next(-3, 1);
                        NPC.netUpdate = true;
                    }
                    // Skip
                    if (Main.expertMode && flag04) {
                        State = States.initSuperCharge;
                        Timer = 0;
                        FasterSuperCharge = false;
                    }
                }
            }
        }

        public override string BossHeadTexture => "MyMod/Contents/NPCs/Bosses/EOCClone/BossHead1";
        private static int BossHeadSlotSecondStage = -1;
        public override void Load() {
            string texture = BossHeadTexture.Replace('1', '2');
            BossHeadSlotSecondStage = Mod.AddBossHeadTexture(texture, -1);
        }
        public override void BossHeadSlot(ref int index) {
            int slot = BossHeadSlotSecondStage;
            if (Appearance() == 2 && slot != -1) {
                index = slot;
            }
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Eye of Cuthulhu Clone");
            NPCID.Sets.MPAllowedEnemies[Type] = true;

            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Poisoned,
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);

            Main.npcFrameCount[Type] = 6;

            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers(0) {
                CustomTexturePath = "MyMod/Contents/NPCs/Bosses/EOCClone/Bestiary",
                PortraitScale = 0.6f,
                PortraitPositionYOverride = 0f,
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
        }

        public override void SetDefaults() {
            NPC.width = 110;
            NPC.height = 166;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;

            NPC.damage = 0;
            NPC.defense = 10;
            NPC.lifeMax = 100000;
            NPC.knockBackResist = 0f;

            NPC.noGravity = true;
            NPC.noTileCollide = true;

            NPC.value = Item.buyPrice(gold: 5);
            NPC.SpawnWithHigherTime(30);
            NPC.boss = true;
            NPC.npcSlots = 10f;

            NPC.aiStyle = -1;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
            bestiaryEntry.Info.AddRange(new List<IBestiaryInfoElement> {
                new MoonLordPortraitBackgroundProviderBestiaryInfoElement(),
                new FlavorTextBestiaryInfoElement("Eye of Cuthulhu Clone.")
            });
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) { }

        public override void OnKill() { }

        public override void BossLoot(ref string name, ref int potionType) { }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            cooldownSlot = ImmunityCooldownID.Bosses;
            return true;
        }

        public override void FindFrame(int frameHeight) {
            int firstFrame;
            int lastFrame;

            switch (Appearance()) {
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
            if (NPC.frame.Y < firstFrame * frameHeight) {
                NPC.frame.Y = firstFrame * frameHeight;
            }

            NPC.frameCounter += 1f;
            const int FrameLasts = 10;
            if (NPC.frameCounter >= FrameLasts) {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y > lastFrame * frameHeight) {
                    NPC.frame.Y = firstFrame * frameHeight;
                }
            }
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
