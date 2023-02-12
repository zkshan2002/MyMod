using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.GameContent;
using Terraria.ModLoader;
using ReLogic.Content;
using System.Linq;

using MyMod.Contents.Projectiles;

namespace MyMod.Contents.NPCs.Bosses.MachineBrain {

    internal struct Offset {
        public float theta, rou, phi;
        public bool stare;
        public Offset(float theta, float rou, float phi) {
            this.theta = theta;
            this.rou = rou;
            this.phi = phi;
            stare = false;
        }
        public Offset(float theta, float rou) {
            this.theta = theta;
            this.rou = rou;
            phi = 0;
            stare = true;
        }
        public void SetStare() {
            phi = 0;
            stare = true;
        }
        public void UnsetStare(float phi) {
            this.phi = phi;
            stare = false;
        }
    }

    public class MachineBrainGuard : NPCBase {
        private enum AllState {
            ride,
            invade,
            free,
        };
        private AllState State {
            get => (AllState)NPC.ai[0];
            set => NPC.ai[0] = (float)value;
        }
        private enum AllInvadeState {
            none,
            invade,
            retreat,
            charge,
            laserFork,
            deathrayZap,
            superCharge,
            laserSpin,
            deathrayStorm,
        }
        private AllInvadeState InvadeState {
            get => (AllInvadeState)NPC.ai[1];
            set => NPC.ai[1] = (float)value;
        }

        internal bool IsRiding => State == AllState.ride;
        internal bool IsRetreating => InvadeState == AllInvadeState.retreat;

        private static readonly AllInvadeState[] AllInvadeAttack = {
            AllInvadeState.charge,
            AllInvadeState.laserFork,
            AllInvadeState.deathrayZap,
        };
        private static readonly AllInvadeState[] AllInvadeAttackSuper = {
            AllInvadeState.superCharge,
            AllInvadeState.laserSpin,
            AllInvadeState.deathrayStorm,
        };
        private AllInvadeState InvadeAttack;

        private enum AllFreeState {
            follow,
            teleport,
            charge,
        }
        private AllFreeState FreeState {
            get => (AllFreeState)NPC.ai[1];
            set => NPC.ai[1] = (float)value;
        }

        private int Timer {
            get => (int)NPC.ai[2];
            set => NPC.ai[2] = value;
        }

        private int ownerHandle = -1;
        private MachineBrain owner;
        internal void SetOwner(int handle) {
            ownerHandle = handle;
        }
        protected override bool Init() {
            if (!base.Init()) {
                return false;
            }

            offsetHandler = new AsyncLerper<Offset>(offset);
            State = AllState.ride;
            InvadeState = AllInvadeState.none;

            spriteRotationDeg = 180;
            rotCorrectionFactor = 2;

            SetNoCollideDamage(time: 1);

            return true;
        }

        // Attacks
        private bool hasCollideDamage = true;
        private void SetCollideDamage(int time = 5) {
            if (hasCollideDamage) {
                return;
            }
            hasCollideDamage = true;
            NPC.damage = owner.GetDamage("GuardContact");
            SetMaxAlpha(1, time);
        }
        private void SetNoCollideDamage(int time = 5) {
            if (!hasCollideDamage) {
                return;
            }
            hasCollideDamage = false;
            NPC.damage = 0;
            SetMaxAlpha(0.2f, time);
        }

        private readonly int LaserProjType = ProjectileID.DeathLaser;//ModContent.ProjectileType<MachineLaser>();
        private readonly int AlertProjType = ModContent.ProjectileType<AlertLine>();
        private readonly int DeathrayProjType = ModContent.ProjectileType<MachineDeathray>();
        private readonly int SpitProjType = ProjectileID.GoldenShowerHostile;

        private void AttackSegmentAlert(Vector2 pos, int lasts) {
            var proj = NewProjectile(AlertProjType, NPC.Center, Vector2.Zero);
            if (proj != null) {
                var modProj = proj.ModProjectile as AlertLine;
                modProj.Set(pos, lasts, maxAlpha: 0.4f);
            }
        }
        private void AttackLaserAlert(float rad, int lasts) {
            var pos = NPC.Center + Utils.Radius(rad, 400);
            AttackSegmentAlert(pos, lasts);
        }

        internal void AttackLaser(float rad) {
            float speed = Utils.FloatNoise(10, 12);
            Vector2 vel = Utils.Radius(rad, speed);
            Vector2 pos = NPC.Center + vel * 5f;
            NewProjectile(LaserProjType, pos, vel, damage: owner.GetDamage("Laser"));
        }

        internal void AttackDeathrayZapAlert(float rad, int lasts) {
            var proj = NewProjectile(AlertProjType, NPC.Center);
            if (proj != null) {
                var modProj = proj.ModProjectile as AlertLine;
                int width = MachineDeathray.GetWidth();
                TimeRatioFunc alphaTRF = (timeRatio) => {
                    if (timeRatio < 0.25f) {
                        return TimeRatioFuncSet.SinLerpConcave(timeRatio / 0.25f);
                    }
                    else if (timeRatio >= 0.75f) {
                        return (1 - timeRatio) / 0.25f;
                    }
                    else {
                        return 1f;
                    }
                };
                modProj.Set(rad, lasts, npcHandle: NPC.whoAmI, offset: NPC.width * 0.5f, tileCollideWidth: width, alphaTRF: alphaTRF);
            }
        }

        internal void AttackDeathrayZap(float rad, int lasts) {
            var proj = NewProjectile(DeathrayProjType, NPC.Center, damage: owner.GetDamage("Deathray"));
            if (proj != null) {
                var modProj = proj.ModProjectile as MachineDeathray;
                modProj.Set(rad, lasts, NPC.whoAmI, shotOffset: NPC.width * 0.5f);
            }
        }

        internal void AttackDeathrayLerpAlert(float rad, LerpData<float> data) {
            var proj = NewProjectile(AlertProjType, NPC.Center);
            if (proj != null) {
                var modProj = proj.ModProjectile as AlertLine;
                int width = MachineDeathray.GetWidth();
                TimeRatioFunc alphaTRF = (timeRatio) => {
                    if (timeRatio < 0.25f) {
                        return TimeRatioFuncSet.SinLerpConcave(timeRatio / 0.25f);
                    }
                    else if (timeRatio >= 0.75f) {
                        return (1 - timeRatio) / 0.25f;
                    }
                    else {
                        return 1f;
                    }
                };
                modProj.Set(rad, data, npcHandle: NPC.whoAmI, offset: NPC.width * 0.5f, tileCollideWidth: width, alphaTRF: alphaTRF);
            }
        }

        internal void AttackDeathrayLerp(float rad, LerpData<float> data) {
            var proj = NewProjectile(DeathrayProjType, NPC.Center, damage: owner.GetDamage("Deathray"));
            if (proj != null) {
                var modProj = proj.ModProjectile as MachineDeathray;
                modProj.Set(rad, data, NPC.whoAmI, shotOffset: NPC.width * 0.5f);
            }
        }

        internal void AttackSpit(float rad) {
            float speed = Utils.FloatNoise(10, 12);
            Vector2 vel = Utils.Radius(rad, speed);
            Vector2 pos = NPC.Center + vel * 5f;
            NewProjectile(SpitProjType, pos, vel, damage: owner.GetDamage("GuardSpit"));
        }

        // Ride
        private int index;
        internal void SetIndex(int index) {
            this.index = index;
        }
        private Offset offset {
            get => offsetHandler.Value;
        }
        internal void SetOffset(Offset offset) {
            offsetHandler = new AsyncLerper<Offset>(offset);
        }
        internal Offset GetOffset() {
            return offset;
        }

        private AsyncLerper<Offset> offsetHandler;
        private static Offset OffsetLerpFunc(Offset start, Offset end, float ratio) {
            float theta = LerpFuncSet.Radian(start.theta, end.theta, ratio);
            float rou = LerpFuncSet.Scalar(start.rou, end.rou, ratio);
            if (end.stare) {
                return new Offset(theta, rou);
            }
            else {
                if (start.stare) {
                    Main.NewText("Start.stare");
                }
                float phi = LerpFuncSet.Radian(start.phi, end.phi, ratio);
                return new Offset(theta, rou, phi);
            }
        }

        internal void SetRideLerp(Offset target, int time, LerpFunc<Offset> LF = null, TimeRatioFunc TRF = null) {
            if (offset.stare) {
                var offset = offsetHandler.Value;
                offset.UnsetStare(Rotation);
                offsetHandler.SetValue(offset);
            }

            offsetHandler.SetLerp(new LerpData<Offset>(target, time, LF ?? OffsetLerpFunc, TRF));
        }

        internal void ResetRide(int time = 15) {
            SetRideLerp(MachineBrain.GetGuardInitOffset(index), time);
        }

        private void ActRide() {
            offsetHandler.Update();

            Teleport2(owner.GetRidePos(offset), resetVel: true);
            if (offset.stare) {
                RotStare();
            }
            else {
                Rotation = offset.phi;
            }
        }

        // Invade
        private Vector2 invadePos;
        private Vector2 attackPos;
        private int invadesToDo;

        private void SetInvade(int prejudgeTime, AllInvadeState state, int invadesToDo) {
            attackPos = PrejudgeTargetPos(prejudgeTime);
            attackPos += Utils.Radius(Utils.RadNoise(180), Utils.FloatNoise(160));
            float theta = 0, rou = 0;
            switch (state) {
                case AllInvadeState.charge:
                case AllInvadeState.superCharge:
                case AllInvadeState.laserSpin:
                    theta = Utils.ClipRad(Utils.RadNoise(180));
                    rou = Main.rand.Next(240, 320 + 1);
                    break;
                case AllInvadeState.laserFork:
                    theta = Utils.ClipRad(PlayerTarget.velocity.ToRotation() + Utils.RadNoise(45));
                    rou = Main.rand.Next(480, 720 + 1);
                    break;
                case AllInvadeState.deathrayZap:
                case AllInvadeState.deathrayStorm:
                    theta = Utils.ClipRad(PlayerTarget.velocity.ToRotation() + (float)Math.PI + Utils.RadNoise(45));
                    rou = Main.rand.Next(480, 720 + 1);
                    break;
            }
            invadePos = attackPos + Utils.Radius(theta, rou);
            State = AllState.invade;
            InvadeState = AllInvadeState.invade;
            InvadeAttack = state;
            this.invadesToDo = invadesToDo;
            Timer = -1;
        }

        internal void SetInvade(int invadesToDo = 1, bool super = false) {
            AllInvadeState invadeState;
            if (!super) {
                invadeState = AllInvadeAttack[Main.rand.Next(AllInvadeAttack.Length)];
            }
            else {
                invadeState = AllInvadeAttackSuper[Main.rand.Next(AllInvadeAttackSuper.Length)];
            }
            SetInvade(Main.rand.Next(0, 10 + 1), invadeState, invadesToDo);
        }

        private bool isInvadeFast;
        private int flyTime;
        private const float flySpeed = 15f;
        private bool ActInvadeInvade(int timer) {
            const int invadeTimeLimit = 60;
            const int invadeTimeFast = 30;
            const int invadeRange = (int)(flySpeed * invadeTimeLimit);

            if (timer == 0) {
                float distance = Vector2.Distance(invadePos, NPC.Center);
                isInvadeFast = distance > invadeRange;
                if (isInvadeFast) {
                    flyTime = invadeTimeFast;
                }
                else {
                    flyTime = (int)(distance / flySpeed);
                    SetCollideDamage();

                    AttackSegmentAlert(invadePos, 20);
                }
            }
            if (timer < flyTime) {
                RotStare(attackPos);
                if (isInvadeFast) {
                    float ratio = 1f / (flyTime - timer);
                    Teleport2(Vector2.Lerp(NPC.Center, invadePos, ratio), resetVel: true);
                }
                else {
                    NPC.velocity = Vec2Rescale(invadePos, flySpeed);
                }
            }
            if (timer == flyTime) {
                if (isInvadeFast) {
                    SetCollideDamage();
                }
            }
            return timer >= flyTime;
        }

        private bool ActInvadeRetreat(int timer) {
            const int retreatTimeLimit = 15;

            if (timer == 0) {
                SetNoCollideDamage();
                owner.HandleIndexOnRetreat();

                var distance = Vector2.Distance(NPC.Center, owner.NPC.Center);
                flyTime = (int)MathHelper.Clamp(distance / flySpeed, 1, retreatTimeLimit);
                NPC.velocity = Vec2Rescale(owner.NPC.Center, flySpeed);
                SetAlphaLerp(0, flyTime);
            }
            if (timer == flyTime) {
                //Main.NewText($"Guard {index} teleport to init offset {MathHelper.ToDegrees(MachineBrain.GetGuardInitOffset(index).theta)}");
                SetOffset(MachineBrain.GetGuardInitOffset(index));
                Teleport2(owner.GetRidePos(offset), resetVel: true);
                SetAlphaLerp(1, 10);
            }

            RotStare();

            return timer >= flyTime;
        }

        private void MoveHover() {
            VelDecay(0.2f, 0.1f);
        }

        private bool ActCharge(int timer, float chargeSpeed, int chargeLasts, int recoverLasts, float clipAt = 0.1f, bool super = false) {
            int chargeStart = 0, recoverStart = chargeStart + chargeLasts, actEnd = recoverStart + recoverLasts;

            if (timer == chargeStart) {
                var pos = PrejudgeTargetPos(Main.rand.Next(0, 10 + 1));
                NPC.velocity = Vec2Rescale(pos, chargeSpeed);
                if (super) {
                    pos = NPC.Center + Vec2Rescale(pos, chargeSpeed * chargeLasts);
                    AttackSegmentAlert(pos, 10);
                }
            }
            if (timer < recoverStart) {
                RotCharge();
            }
            if (timer >= recoverStart && timer < actEnd) {
                VelDecay(0.9f, 0.1f);
                if (NPC.velocity.Length() < clipAt) {
                    return true;
                }
                RotStare();
            }

            return timer >= actEnd;
        }

        // normal invade takes 30 + (95/85/80) + 15 <= 140
        // super invade takes 30 + (135/130/140) + 15 = 185
        private int localTimer;
        private int chargesToDo;
        private bool ActInvadeAttackCharge(int timer) {
            const int delay = 20;

            if (timer < delay) {
                MoveHover();
                RotStare();
            }
            if (timer == delay) {
                chargesToDo = 3;
                localTimer = timer;
            }
            if (timer >= delay) {
                if (ActCharge(timer - localTimer, 16f, 15, 10)) {
                    chargesToDo--;
                    if (chargesToDo == 0) {
                        localTimer = 0;
                        return true;
                    }
                    else {
                        localTimer = timer + 1;
                    }
                }
            }

            return false;
        }

        private bool ActInvadeAttackSuperCharge(int timer) {
            const int delay1 = 20, delay2 = 20;

            if (timer < delay1) {
                NPC.velocity = Vector2.Zero;
                RotStare();
            }
            if (timer == delay1) {
                chargesToDo = 3;
                localTimer = timer;
            }
            if (timer >= delay1) {
                if (chargesToDo > 1) {
                    if (ActCharge(timer - localTimer, 20f, 15, 10)) {
                        chargesToDo--;
                        localTimer = timer + 1;
                    }
                }
                else {
                    if (timer - localTimer >= delay2) {
                        if (ActCharge(timer - localTimer - delay2, 30f, 40, 15, super: true)) {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private float attackRad;
        private bool ActInvadeAttackLaserFork(int timer) {
            MoveHover();
            RotStare(attackPos);

            const int delay1 = 20, shootLasts = 45, delay2 = 20;
            const int shootStart = delay1, shootEnd = shootStart + shootLasts, actEnd = shootEnd + delay2;

            if (timer == shootStart) {
                attackRad = Utils.ClipRad((attackPos - NPC.Center).ToRotation());
                AttackLaserAlert(Utils.ClipRad(attackRad + MathHelper.ToRadians(30)), 15);
                AttackLaserAlert(Utils.ClipRad(attackRad - MathHelper.ToRadians(30)), 15);
            }
            if (timer >= shootStart && timer < shootEnd) {
                if (Main.rand.NextBool(5)) {
                    float rad = Utils.ClipRad(attackRad + Utils.SignNoise() * MathHelper.ToRadians(30) + Utils.RadNoise(2.5f));
                    AttackLaser(rad);
                }
            }

            return timer >= actEnd;
        }

        private bool ActInvadeAttackLaserSpin(int timer) {
            NPC.velocity = Vector2.Zero;

            const int delay1 = 20, shootLasts = 90, delay2 = 20;
            const int shootStart = delay1, shootEnd = shootStart + shootLasts, actEnd = shootEnd + delay2;


            if (timer >= shootStart && timer < shootEnd) {
                float delta = MathHelper.ToRadians(360 / 60);
                Rotation = Utils.ClipRad(Rotation + delta);
                if (timer % 5 == 0) {
                    float rad = Utils.ClipRad(Rotation);
                    AttackLaser(rad);

                    int index = timer / 5;
                    if (index == 0) {
                        for (int i = 0; i < 4; i++) {
                            rad = Utils.ClipRad(Rotation + delta * i * 5);
                            AttackLaserAlert(rad, 15);
                        }
                    }
                    if (index > 0 && index < 18 - 3) {
                        rad = Utils.ClipRad(Rotation + delta * 3 * 5);
                        AttackLaserAlert(rad, 15);
                    }
                }
            }

            return timer >= actEnd;
        }

        private bool ActInvadeAttackDeathrayZap(int timer) {
            MoveHover();
            RotStare(attackPos);

            const int delay1 = 20, alertLasts = 30, zapLasts = 10, delay2 = 20;
            const int alertStart = delay1, zapStart = alertStart + alertLasts, zapEnd = zapStart + zapLasts, actEnd = zapEnd + delay2;
            if (timer == alertStart) {
                attackRad = Utils.ClipRad((attackPos - NPC.Center).ToRotation());
                AttackDeathrayZapAlert(attackRad, alertLasts);
            }
            if (timer == zapStart) {
                AttackDeathrayZap(attackRad, zapLasts);
            }

            return timer >= actEnd;
        }

        private bool ActInvadeAttackDeathrayStorm(int timer) {
            MoveHover();
            RotStare(attackPos);

            const int delay1 = 20, delay2 = 20;
            // 0-20, 20-30; 20-40, 40-50; 60-80, 80-120
            if (timer == delay1 + 0) {
                attackRad = Utils.ClipRad(Rot2(PrejudgeTargetPos(0)) + Utils.RadNoise(15));
                AttackDeathrayZapAlert(attackRad, 20);
            }
            if (timer == delay1 + 20) {
                AttackDeathrayZap(attackRad, 10);

                attackRad = Utils.ClipRad(Rot2(PrejudgeTargetPos(10)) + Utils.RadNoise(15));
                AttackDeathrayZapAlert(attackRad, 20);
            }
            if (timer == delay1 + 40) {
                AttackDeathrayZap(attackRad, 10);
            }
            if (timer == delay1 + 50) {
                attackRad = Utils.ClipRad(Rot2(PrejudgeTargetPos(20)) + Utils.RadNoise(25));
                float rad = Utils.ClipRad(attackRad + MathHelper.ToRadians(15));
                AttackDeathrayLerpAlert(rad, new LerpData<float>(attackRad, 20, LerpFuncSet.Radian, TimeRatioFuncSet.SinLerpConvex));
                rad = Utils.ClipRad(attackRad - MathHelper.ToRadians(15));
                AttackDeathrayLerpAlert(rad, new LerpData<float>(attackRad, 20, LerpFuncSet.Radian, TimeRatioFuncSet.SinLerpConvex));

                //sign = Utils.SignNoise();
                //float start = Utils.ClipRad(attackRad + sign * MathHelper.ToRadians(30) * 2);
                //float end = Utils.ClipRad(attackRad + sign * MathHelper.ToRadians(30));
                //AttackDeathrayLerpAlert(start, new LerpData<float>(end, 20, LerpFuncSet.Radian));
            }
            if (timer == delay1 + 80) {
                //float start = Utils.ClipRad(attackRad + sign * MathHelper.ToRadians(30));
                //AttackDeathrayLerp(start, new LerpData<float>(attackRad, 20, LerpFuncSet.Radian));
                AttackDeathrayZap(attackRad, 40);
            }

            return timer >= delay1 + 120 + delay2;
        }

        private bool ActInvadeAttack(int timer) {
            ActFunc actInvadeAttack = (timer) => false;
            switch (InvadeAttack) {
                case AllInvadeState.charge:
                    actInvadeAttack = ActInvadeAttackCharge;
                    break;
                case AllInvadeState.laserFork:
                    actInvadeAttack = ActInvadeAttackLaserFork;
                    break;
                case AllInvadeState.deathrayZap:
                    actInvadeAttack = ActInvadeAttackDeathrayZap;
                    break;
                case AllInvadeState.superCharge:
                    actInvadeAttack = ActInvadeAttackSuperCharge;
                    break;
                case AllInvadeState.laserSpin:
                    actInvadeAttack = ActInvadeAttackLaserSpin;
                    break;
                case AllInvadeState.deathrayStorm:
                    actInvadeAttack = ActInvadeAttackDeathrayStorm;
                    break;
            }
            return actInvadeAttack(timer);
        }

        internal Offset SolveOffset() {
            var ToInitPos = owner.GetRidePos(MachineBrain.GetGuardInitOffset(index)) - NPC.Center;
            float theta = Utils.ClipRad(ToInitPos.ToRotation());
            float rou = ToInitPos.Length();
            var offset = new Offset(theta, rou, 0);
            offset.SetStare();
            return offset;
        }

        private void ActInvade() {
            Timer++;
            switch (InvadeState) {
                case AllInvadeState.invade:
                    if (ActInvadeInvade(Timer - 1)) {
                        Timer = 0;
                        InvadeState = InvadeAttack;
                    }
                    break;
                case AllInvadeState.charge:
                case AllInvadeState.laserFork:
                case AllInvadeState.deathrayZap:
                case AllInvadeState.superCharge:
                case AllInvadeState.laserSpin:
                case AllInvadeState.deathrayStorm:
                    if (ActInvadeAttack(Timer - 1)) {
                        Timer = 0;
                        invadesToDo--;
                        if (invadesToDo == 0) {
                            InvadeState = AllInvadeState.retreat;
                        }
                        else {
                            SetInvade(invadesToDo);
                        }
                    }
                    break;
                case AllInvadeState.retreat:
                    if (ActInvadeRetreat(Timer - 1)) {
                        Timer = 0;
                        State = AllState.ride;
                        InvadeState = AllInvadeState.none;
                    }
                    break;
            }
        }

        // Free
        internal void SetFree() {
            State = AllState.free;
            FreeState = AllFreeState.follow;
            SetStat();
        }

        private bool near {
            get => Vector2.Distance(NPC.Center, PlayerTarget.Center) < 1000f;
        }
        private Vector2 followPos;
        private int followTime;
        private bool ActFollow(int timer) {
            float flySpeed = 15f, beta = 1 / 10f;
            if (timer == 0) {
                float theta = Utils.ClipRad(Rot2(PlayerTarget.Center) + Utils.RadNoise(45) + MathHelper.ToRadians(180));
                float rou = Main.rand.Next(320, 640 + 1);
                followPos = Utils.Radius(theta, rou);
                followTime = 0;
                spitAt = Main.rand.Next(0, attackPeriod);
            }

            var pos = PlayerTarget.Center + followPos;
            RotStare();
            FlyMomentum(pos, flySpeed, beta);

            if (near) {
                attackTimer++;
                AttackLaserAndSpit();
            }

            followTime++;

            return followTime >= 60 + index * 10;
        }

        private int attackTimer = 0;
        private int attackPeriod = 60, attackMaxAcc = 20;
        private int spitAt;
        private void AttackLaserAndSpit() {
            if (attackTimer == spitAt) {
                float rad = Utils.ClipRad(Rot2(PlayerTarget.Center) + Utils.RadNoise(30));
                AttackSpit(rad);
                if (Main.rand.NextBool(3)) {
                    spitAt = Main.rand.Next(spitAt + 1, attackPeriod + 1);
                }
                else {
                    spitAt = -1;
                }
            }
            if (attackTimer == attackPeriod) {
                float rad = Utils.ClipRad(Rot2(PlayerTarget.Center) + Utils.RadNoise(5));
                AttackLaser(rad);
            }
            if (attackTimer >= attackPeriod) {
                attackTimer = Main.rand.Next(0, attackMaxAcc + 1);
            }
        }

        private Vector2 GetPosNear(int near, int far, int prejudgeTime) {
            float theta = Utils.ClipRad(Utils.RadNoise(180));
            float rou = Main.rand.Next(near, far + 1);
            var pos = PrejudgeTargetPos(prejudgeTime);
            pos += Utils.Radius(theta, rou);
            return pos;
        }

        Vector2? teleportPos = null;

        private bool ActTeleport(int timer) {
            int delayStart = 20, delayEnd = 20;
            int fadeLasts = 20;

            if (timer == delayStart - fadeLasts) {
                var pos = GetPosNear(640, 1280, 0);
                teleportPos = pos;
                SetAlphaLerp(0, fadeLasts);
            }
            if (teleportPos.HasValue) {
                for (int i = 0; i < 5; i++) {
                    int handle = Dust.NewDust(teleportPos.Value, 1, 1, DustID.Ichor);
                    if (handle >= 0 && handle < Main.maxDust) {
                        var dust = Main.dust[handle];
                        dust.velocity = Utils.Radius(Utils.RadNoise(180), 5f);
                        dust.scale = Main.rand.NextFloat(0.5f, 1.0f);
                    }
                }
            }
            if (timer == delayStart) {
                Teleport2(teleportPos.Value, resetVel: true);
                teleportPos = null;
                SetAlphaLerp(1, fadeLasts);
            }

            return timer >= delayStart + fadeLasts + delayEnd;
        }

        private float chargeSpeed;
        private int chargeLasts, recoverLasts;
        private bool ActFreeCharge(int timer) {
            if (timer == 0) {
                // swift
                if (Main.rand.Next(3) < 1) {
                    chargeSpeed = 18f;
                    chargeLasts = Main.rand.Next(30, 60 + 1);
                    recoverLasts = Main.rand.Next(0, 20 + 1);
                }
                // clumsy
                else {
                    chargeSpeed = 20f;
                    chargeLasts = Main.rand.Next(30, 60 + 1);
                    recoverLasts = Main.rand.Next(20, 40 + 1);
                }
            }

            return ActCharge(timer, chargeSpeed, chargeLasts, recoverLasts, clipAt: 1f);
        }

        private bool ActSpinSpit(int timer) {
            return false;
        }

        private void Schedule() {
            switch (FreeState) {
                case AllFreeState.follow:
                    FreeState = near ? AllFreeState.charge : AllFreeState.teleport;
                    break;
                case AllFreeState.teleport:
                    FreeState = Main.rand.NextBool(3) ? AllFreeState.follow : AllFreeState.charge;
                    break;
                case AllFreeState.charge:
                    FreeState = Main.rand.NextBool(3) ? AllFreeState.charge : AllFreeState.follow;
                    break;
            }
        }

        private void ActFree() {
            ActFunc actFunc = (timer) => false;
            switch (FreeState) {
                case AllFreeState.follow:
                    actFunc = ActFollow;
                    break;
                case AllFreeState.teleport:
                    actFunc = ActTeleport;
                    break;
                case AllFreeState.charge:
                    actFunc = ActFreeCharge;
                    break;
            }

            if (actFunc(Timer)) {
                Timer = -1;
                Schedule();
            }

            Timer++;
        }

        //private static Dictionary<int, Color> colorMap = new Dictionary<int, Color> { { 0, Color.Red }, { 1, Color.Silver }, { 2, Color.Yellow }, { 3, Color.Green }, { 4, Color.Cyan }, { 5, Color.Blue }, { 6, Color.Purple }, };
        public override void AI() {
            // Detect owner
            if (ownerHandle < 0 || ownerHandle >= Main.maxNPCs ||
                Main.npc[ownerHandle].type != ModContent.NPCType<MachineBrain>() || !Main.npc[ownerHandle].active) {
                Kill();
                return;
            }
            owner = Main.npc[ownerHandle].ModNPC as MachineBrain;

            // Target Player
            NPC.target = owner.NPC.target;

            switch (State) {
                case AllState.ride:
                    ActRide();
                    break;
                case AllState.invade:
                    ActInvade();
                    break;
                case AllState.free:
                    ActFree();
                    break;
            }

            //Utils.DustPoint(NPC.Center, colorMap[index]);
        }

        // Draw
        public override void FindFrame(int frameHeight) {
            int firstFrame = 0;
            int lastFrame = Main.npcFrameCount[Type] - 1;
            switch (frameAppearance) {
                case 1:
                case 2:
                    firstFrame = 0;
                    lastFrame = 0;
                    break;
                case 3:
                    firstFrame = 1;
                    lastFrame = 1;
                    break;
            }
            LoopFrame(frameHeight, firstFrame, lastFrame, 10);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            var texture = TextureAssets.Npc[Type].Value;
            var mainPos = NPC.Center;
            var color = drawColor * Alpha;
            var drawOrigin = NPC.frame.Size() * 0.5f;
            //if (hasCollideDamage) {
            //    color = drawColor * Alpha;
            //}
            //else {
            //    color = Color.Azure;
            //}

            // After image effect
            if (!IsRiding) {
                for (int i = 0; i < NPC.oldPos.Length; i++) {
                    var afterImagePos = NPC.oldPos[i] + drawOrigin;
                    color = drawColor * Alpha * ((float)(NPC.oldPos.Length - i) / (NPC.oldPos.Length + 1)) * 0.75f;
                    float rotation = NPC.oldRot[i];
                    spriteBatch.Draw(texture, afterImagePos - screenPos, NPC.frame, color, rotation, drawOrigin, NPC.scale, SpriteEffects.None, 0);
                }
            }

            spriteBatch.Draw(texture, mainPos - screenPos, NPC.frame, color, NPC.rotation, drawOrigin, NPC.scale, SpriteEffects.None, 0);

            return false;
        }

        private static Asset<Texture2D> glowAsset;
        public override void Load() {
            glowAsset = ModContent.Request<Texture2D>($"{Workdir}/MachineBrainGuardGlow");
        }
        public override void Unload() {
            glowAsset = null;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            var glowTexture = glowAsset.Value;
            var pos = NPC.Center;
            var color = Color.White * Alpha;
            spriteBatch.Draw(glowTexture, pos - screenPos, NPC.frame, color, NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0);
        }
        // Stats
        private void SetStat() {
            NPC.dontTakeDamage = false;
            NPC.damage = owner.GetDamage("GuardContact");
            NPC.defense = 0;
            NPC.lifeMax = NPC.life = 10000;
            NPC.knockBackResist = 0.05f;
        }
        public override void SetDefaults() {
            NPC.width = 32;
            NPC.height = 32;

            NPC.dontTakeDamage = true;
            NPC.damage = 1;
            NPC.defense = 0;
            NPC.lifeMax = 1;
            NPC.knockBackResist = 0f;

            NPC.noGravity = true;
            NPC.noTileCollide = true;

            NPC.npcSlots = 0f;

            NPC.HitSound = SoundID.NPCHit9;
            NPC.DeathSound = SoundID.NPCDeath11;

            NPC.aiStyle = -1;
        }

        // Misc
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Guard");

            NPCDebuffImmunityData debuffData = new() {
                SpecificallyImmuneTo = new int[]
                {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);

            Main.npcFrameCount[Type] = 2;
            NPCID.Sets.TrailCacheLength[Type] = 5;
            NPCID.Sets.TrailingMode[Type] = 3;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            cooldownSlot = ImmunityCooldownID.Bosses;
            return true;
        }
        internal int frameAppearance;
    }
}