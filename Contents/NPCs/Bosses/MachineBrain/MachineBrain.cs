#define VANILLA_SPRITE

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
    [AutoloadBossHead]
    public class MachineBrain : NPCBase {
        private enum AllState {
            // spawn
            spawn,
            // first stage
            setInvade,
            teleportAndShoot,
            teleportAndCharge,
            // transition
            transition,
            // second stage
            setInvade2,
            teleportAndShoot2,
            teleportAndCharge2,
            // transition2
            transition2,
            // third stage
            teleportAndCharge3,
            // death
            deathAnimation,
        };
        private AllState State {
            get => (AllState)NPC.ai[0];
            set => NPC.ai[0] = (float)value;
        }
        private static readonly AllState[] FirstStageState =
        {
            AllState.setInvade,
            AllState.teleportAndShoot,
            AllState.teleportAndCharge,
        };
        private bool IsFirstStage => Array.IndexOf(FirstStageState, State) != -1;
        private static readonly AllState[] SecondStageState =
        {
            AllState.setInvade2,
            AllState.teleportAndShoot2,
            AllState.teleportAndCharge2,
        };
        private bool IsSecondStage => Array.IndexOf(SecondStageState, State) != -1;
        private bool IsThirdStage => State == AllState.teleportAndCharge3;
        private static readonly AllState[] TransitionState =
        {
            AllState.transition,
            AllState.transition2,
        };

        private int Timer {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        // Guards
        private readonly int guardType = ModContent.NPCType<MachineBrainGuard>();
        private int[] guardHandles;
        private const int guardCount = 7;
        private MachineBrainGuard[] guards;

        private int[] rideIndices, invadeIndices;
        private static readonly int[] rideIndexSeq = new int[] { 3, 2, 4, 1, 5, 0, 6 };
        private void GetRideIndices(int count) {
            if (count < 0 || count > guardCount) {
                DebugMsg("Count out of range");
            }
            rideIndices = rideIndexSeq.Take(count).ToArray();
            Array.Sort(rideIndices);
            return;
        }

        private void HandleIndexSetInvade(int count) {
            int rideCount = guards.Sum(x => x.IsRiding || x.IsRetreating ? 1 : 0);
            if (rideCount < count) {
                DebugMsg("RideCount < Count");
                count = rideCount;
            }
            //Main.NewText($"___Set invade {count}");
            //Print();
            // Get indices for previous invade and current invade
            GetRideIndices(rideCount - count);
            var allInvadeIndices = new int[guardCount - rideCount + count];
            int k = 0, l = 0;
            for (int i = 0; i < guardCount; i++) {
                if (k < rideIndices.Length && rideIndices[k] == i) {
                    k++;
                    continue;
                }
                allInvadeIndices[l++] = i;
            }
            // Fill indices
            var pickInvade = Utils.PickK(rideCount, count, shuffle: false);
            //Utils.PrintArray(pickInvade, "Set invade, pickInvade");
            var previous2new = new int[guardCount];
            invadeIndices = new int[count];
            k = 0; l = 0;
            int m = 0, n = 0, o = 0;
            for (int i = 0; i < guardCount; i++) {
                var guard = guards[i];
                // Previous Invade
                if (!(guard.IsRiding || guard.IsRetreating)) {
                    previous2new[i] = allInvadeIndices[m++];
                    l++;
                    continue;
                }
                // Current Invade
                if (k < pickInvade.Length && i == pickInvade[k] + l) {
                    previous2new[i] = allInvadeIndices[m++];
                    invadeIndices[o++] = previous2new[i];
                    k++;
                    continue;
                }
                // Ride
                previous2new[i] = rideIndices[n++];
            }
            //Utils.PrintArray(previous2new, "Set invade, previous2new");
            //Utils.PrintArray(rideIndices, "Set invade, new rideIndices");
            //Utils.PrintArray(invadeIndices, "Set invade, new invadeIndices");
            var guardsClone = (MachineBrainGuard[])guards.Clone();
            for (int i = 0; i < guardCount; i++) {
                int newIndex = previous2new[i];
                guards[newIndex] = guardsClone[i];
                guards[newIndex].SetIndex(newIndex);
            }
            for (int i = 0; i < guardCount; i++) {
                var guard = guards[i];
                if (guard.IsRiding) {
                    guard.ResetRide();
                }
            }
        }

        internal void HandleIndexOnRetreat() {
            int rideCount = guards.Sum(x => x.IsRiding || x.IsRetreating ? 1 : 0);
            //Main.NewText($"___Set retreat {index}");
            //Print();
            // Get indices for previous invade and current invade
            GetRideIndices(rideCount);
            // Get indices for previous invade
            var previousInvadeIndices = new int[guardCount - rideCount];
            int k = 0, l = 0;
            for (int i = 0; i < guardCount; i++) {
                if (k < rideIndices.Length && rideIndices[k] == i) {
                    k++;
                    continue;
                }
                previousInvadeIndices[l++] = i;
            }
            // Fill indices
            var previous2new = new int[guardCount];
            k = 0; l = 0;
            for (int i = 0; i < guardCount; i++) {
                var guard = guards[i];
                // Previous Invade
                if (!(guard.IsRiding || guard.IsRetreating)) {
                    previous2new[i] = previousInvadeIndices[k++];
                }
                // Ride
                else {
                    previous2new[i] = rideIndices[l++];
                }
            }
            //Utils.PrintArray(previous2new, "Set retreat, previous2new");
            var guardsClone = (MachineBrainGuard[])guards.Clone();
            for (int i = 0; i < guardCount; i++) {
                int newIndex = previous2new[i];
                guards[newIndex] = guardsClone[i];
                guards[newIndex].SetIndex(newIndex);
            }
            for (int i = 0; i < guardCount; i++) {
                var guard = guards[i];
                if (guard.IsRiding) {
                    guard.ResetRide();
                }
            }
            //Print();
        }

        internal Vector2 GetRidePos(Offset offset) {
            return RideCenter + Utils.Radius(offset.theta, offset.rou);
        }

        internal static Offset GetGuardInitOffset(int index) {
            float theta = Utils.ClipRad(MathHelper.ToRadians(270 + (index - 3) * 40));
            var offset = new Offset(theta, 150f);
            return offset;
        }

        private bool InitSpawnGuards() {
            guardHandles = new int[guardCount];
            guards = new MachineBrainGuard[guardCount];
            for (int i = 0; i < guardCount; i++) {
                var offset = GetGuardInitOffset(i);
                var pos = GetRidePos(offset);
                int handle = NewNPC(guardType, pos);
                if (handle != -1) {
                    var minion = Main.npc[handle];
                    var guard = minion.ModNPC as MachineBrainGuard;
                    guard.SetOwner(NPC.whoAmI);
                    guard.SetIndex(i);
                    guard.SetOffset(offset);
                    guard.NPC.damage = GetDamage("GuardContact");
                    guard.frameAppearance = 1;
                    guards[i] = guard;

                    minion.netUpdate = true;
                }
                else {
                    return false;
                }

                guardHandles[i] = handle;
            }

            return true;
        }

        protected override bool Init() {
            if (!base.Init()) {
                return false;
            }
            if (!InitSpawnGuards()) {
                return false;
            }

            InitSprite();

            invadeIndices = Array.Empty<int>();
            rideIndices = Array.Empty<int>();

            return true;
        }

        // Action
        private bool ActSpawn(int timer) {
            const int guardAppearLasts = 10;
            const int appearEnd = 20, guardAppearEnd = appearEnd + guardAppearLasts * guardCount, delayEnd = guardAppearEnd + 30;

            if (timer == 0) {
                SetAlpha(0);
                SetAlphaLerp(1, appearEnd);
            }
            if (timer == 0) {
                foreach (var guard in guards) {
                    guard.SetAlpha(0);
                }
            }
            if (timer >= appearEnd && timer < guardAppearEnd) {
                if ((timer - appearEnd) % guardAppearLasts == 0) {
                    int i = (timer - appearEnd) / guardAppearLasts;
                    guards[i].SetAlphaLerp(1, guardAppearLasts);
                }
            }
            return timer >= delayEnd;
        }

        private bool ActTransition(int timer) {
            VelocityDecay(0.9f, 0.1f);

            const int delay1 = 120, delay2 = 20;
            const int actEnd = delay1 + delay2;
            if (timer == delay1) {
                frameAppearance = 2;
                SetStatsSecondStage();
                for (int i = 0; i < guardCount; i++) {
                    var guard = guards[i];
                    guard.frameAppearance = 2;
                    guard.ResetRide();
                }
            }

            return timer >= actEnd;
        }

        private bool ActTransition2(int timer) {
            VelocityDecay(0.9f, 0.1f);

            const int delay1 = 60, delay2 = 20;
            const int actEnd = delay1 + delay2;
            if (timer == delay1) {
                frameAppearance = 3;
                SetStatsThirdStage();
                for (int i = 0; i < guardCount; i++) {
                    var guard = guards[i];
                    guard.frameAppearance = 3;
                    guard.SetFree();
                }
            }

            return timer >= actEnd;
        }
        
        private bool ActDeathAnimation(int timer) {
            int delay = 60, fadeLasts = 30;
            int fade = delay, actEnd = delay + fadeLasts;

            if (timer == fade) {
                SetRideAlphaLerp(0, fadeLasts);
            }
            if (timer == actEnd) {
                NPC.life = 0;
                NPC.checkDead();
            }

            VelocityDecay(0.9f, 0.1f);

            return false;
        }
        private void SetRideAlphaLerp(float target, int time) {
            SetAlphaLerp(target, time);
            foreach (var guard in guards) {
                if (guard.IsRiding) {
                    guard.SetAlphaLerp(target, time);
                }
            }
        }
        private void SetRideAlpha(float target) {
            SetAlpha(target);
            foreach (var guard in guards) {
                if (guard.IsRiding) {
                    guard.SetAlpha(target);
                }
            }
        }

        private Vector2? teleportPos = null;
        private bool justPrepareTeleport = false;
        private bool justDoneTeleport = false;
        private const int fadeTime = 15;
        private void PrepareTeleport(Vector2 pos) {
            teleportPos = pos;
            SetRideAlphaLerp(0, fadeTime);
            justPrepareTeleport = true;
        }
        private void DoTeleport(bool resetVel = false) {
            TeleportTo(teleportPos.Value, resetVel);
            teleportPos = null;
            SetRideAlpha(1);
            justDoneTeleport = true;
        }

        private Vector2 GetPosNear(int near, int far, int prejudgeTime) {
            float theta = Utils.ClipRad(Utils.RadNoise(180));
            float rou = Main.rand.Next(near, far + 1);
            var pos = PrejudgePlayerTargetPos(prejudgeTime);
            pos += Utils.Radius(theta, rou);
            return pos;
        }

        private bool ActSetInvade(int timer) {
            int delayStart = 20, delayEnd = IsFirstStage ? 60 : 90;
            int delayTeleport = 20, delay = IsFirstStage ? 60 : 90;
            int teleport = delayStart, set1 = teleport + delayTeleport, set2 = set1 + delay;
            int teleport2 = IsFirstStage ? -1 : set2 + delay;
            int set3 = (IsFirstStage ? set2 + delay : teleport2 + delayTeleport), actEnd = set3 + delay;

            if (timer == teleport - fadeTime) {
                PrepareTeleport(GetPosNear(480, 640, Main.rand.Next(0, 10 + 1)));
            }
            if (timer == teleport) {
                DoTeleport();
            }
            if (timer == set1) {
                if (DetectTransition()) {
                    return true;
                }
                if (IsFirstStage) {
                    HandleIndexSetInvade(3);
                    foreach (int i in invadeIndices) {
                        var guard = guards[i];
                        guard.SetInvade(2);
                    }
                }
                else {
                    HandleIndexSetInvade(3);
                    for (int i = 0; i < 3; i++) {
                        var guard = guards[invadeIndices[i]];
                        guard.SetInvade(super: i < 1);
                    }
                }
                invadeIndices = Array.Empty<int>();
                rideIndices = Array.Empty<int>();
            }
            if (timer == set2) {
                if (DetectTransition()) {
                    return true;
                }
                if (IsFirstStage) {
                    HandleIndexSetInvade(2);
                    foreach (int i in invadeIndices) {
                        var guard = guards[i];
                        guard.SetInvade();
                    }
                }
                else {
                    HandleIndexSetInvade(3);
                    for (int i = 0; i < 3; i++) {
                        var guard = guards[invadeIndices[i]];
                        guard.SetInvade(super: i < 2);
                    }
                }

                invadeIndices = Array.Empty<int>();
                rideIndices = Array.Empty<int>();
            }

            if (IsSecondStage) {
                if (timer == teleport2 - fadeTime) {
                    var pos = GetPosNear(80, 160, Main.rand.Next(0, 10 + 1));
                    pos += new Vector2(0, -400);
                    PrepareTeleport(pos);
                }
                if (timer == teleport2) {
                    DoTeleport();
                }
            }

            if (timer == set3) {
                if (DetectTransition()) {
                    return true;
                }
                if (IsFirstStage) {
                    HandleIndexSetInvade(2);
                    foreach (int i in invadeIndices) {
                        var guard = guards[i];
                        guard.SetInvade();
                    }
                }
                else {
                    int rideCount = guards.Sum(x => x.IsRiding || x.IsRetreating ? 1 : 0);
                    HandleIndexSetInvade(rideCount);
                    for (int i = 0; i < rideCount; i++) {
                        var guard = guards[invadeIndices[i]];
                        guard.SetInvade(super: i < 3);
                    }
                }
                invadeIndices = Array.Empty<int>();
                rideIndices = Array.Empty<int>();
            }

            float speed = Utils.Correction(NPC.velocity.Length(), IsFirstStage ? 5f : 8f, IsFirstStage ? 0.25f : 0.5f);
            FlyUniform(PlayerTarget.Center, speed);

            return timer >= actEnd + delayEnd;
        }

        private float sign;
        private Vector2 attackPos;
        private int[] attackIndices;
        private float attackRad;

        private readonly int DeathrayProjType = ModContent.ProjectileType<MachineDeathray>();
        internal void AttackDeathrayLerp(float rad, LerpData<float> data) {
            var handle = NewProjectile(DeathrayProjType, RideCenter, damage: GetDamage("Deathray"));
            if (handle >= 0) {
                var modProj = Main.projectile[handle].ModProjectile as MachineDeathray;
                modProj.Set(rad, data, NPC.whoAmI, fixedOffset: FixedOffset, shotOffset: 16);
            }
        }

        private MachineBrainGuard[] attackingGuards;
        private AsyncLerper<float> radDeltaHandler;
        private float sweepRad;
        private bool ActTeleportAndShoot(int timer) {
            int delayStart = 20, delayEnd = IsFirstStage ? 60 : 90;
            int delayTeleport = 20, shootLasts = IsFirstStage ? 60 : 40, delay = 20, shootLasts2 = IsFirstStage ? -1 : 120;
            int teleport1 = delayStart, shoot1 = teleport1 + delayTeleport, shootEnd1 = shoot1 + shootLasts;
            int teleport2 = shootEnd1 + delay, shoot2 = teleport2 + delayTeleport, shootEnd2 = shoot2 + shootLasts;
            int teleport3 = shootEnd2 + delay, shoot3 = teleport3 + delayTeleport, actEnd = shoot3 + (IsFirstStage ? shootLasts : shootLasts2);

            // teleport to side, rapidly fire lasers
            if (timer == teleport1 - fadeTime) {
                sign = Utils.SignNoise();
                var pos = PrejudgePlayerTargetPos(Main.rand.Next(0, 10 + 1));
                pos += new Vector2(sign * Main.rand.Next(320, 640 + 1), 0);
                PrepareTeleport(pos);
            }
            if (timer == teleport1) {
                DoTeleport();
            }
            if (timer == teleport1) {
                attackPos = PrejudgePlayerTargetPos(Main.rand.Next(0, 10 + 1));
            }
            if (timer == shoot1) {
                if (DetectTransition()) {
                    attackPos = Vector2.Zero;
                    return true;
                }
            }
            if (timer >= shoot1 && timer < shootEnd1) {
                for (int i = 0; i < guardCount; i++) {
                    var guard = guards[i];
                    if (guard.IsRiding) {
                        if (Main.rand.NextBool(IsFirstStage ? 30 : 20)) {
                            float rad = RotationTo(attackPos);
                            rad += Utils.RadNoise(10);
                            guard.AttackLaser(rad);
                            break;
                        }
                    }
                }
            }
            if (timer == shootEnd1) {
                attackPos = Vector2.Zero;
            }

            // teleport to other side, fast zap 4/6 times
            // alert: 0-20, 15-35, 30-50, 45-65
            // zap: 20-30, 35-45, 50-60, 65-75
            // alert: 0-16, 8-24, 16-32, 24-40, 32-48, 40-56
            // zap: 16-24, 24-32, 32-40, 40-48, 48-56, 56-64
            int numZaps = IsFirstStage ? 4 : 6;
            int alertLasts = IsFirstStage ? 20 : 16;
            int zapLasts = IsFirstStage ? 10 : 8;
            int overlap = IsFirstStage ? 15 : 16;

            if (timer == teleport2 - fadeTime) {
                var pos = PrejudgePlayerTargetPos(Main.rand.Next(0, 10 + 1));
                pos += new Vector2(-sign * Main.rand.Next(320, 640 + 1), 0);
                PrepareTeleport(pos);
            }
            if (timer == teleport2) {
                DoTeleport();

                attackIndices = Utils.PickK(guardCount, numZaps);
                foreach (int i in attackIndices) {
                    var guard = guards[i];
                    if (!guard.IsRiding) {
                        DebugMsg($"TeleportAndShoot1-2 Guard {i} not riding");
                    }
                }
            }
            if (timer == shoot2) {
                if (DetectTransition()) {
                    attackIndices = Array.Empty<int>();
                    return true;
                }
            }
            if (timer >= teleport2 + 1 && timer <= teleport2 + (alertLasts + zapLasts - overlap) * (numZaps - 1) + alertLasts + 1) {
                if ((timer - teleport2 - 1) % (alertLasts + zapLasts - overlap) == 0) {
                    int index = (timer - teleport2 - 1) / (alertLasts + zapLasts - overlap);
                    if (index < numZaps) {
                        var guard = guards[attackIndices[index]];
                        float theta = guard.GetOffset().theta;
                        float rou = 150f;
                        float phi = Utils.ClipRad((PrejudgePlayerTargetPos((index - 2) * 5) - guard.NPC.Center).ToRotation());
                        var target = new Offset(theta, rou, phi);
                        guard.SetRideLerp(target, alertLasts);
                        guard.AttackDeathrayZapAlert(target.phi, alertLasts);
                    }
                }
                if ((timer - teleport2 - alertLasts - 1) % (alertLasts + zapLasts - overlap) == 0) {
                    int index = (timer - teleport2 - alertLasts - 1) / (alertLasts + zapLasts - overlap);
                    if (index >= 0) {
                        var guard = guards[attackIndices[index]];
                        guard.AttackDeathrayZap(guard.Rotation, 10);
                        guard.ResetRide();
                    }
                }
            }
            if (timer == shootEnd2 + 1) {
                for (int i = 0; i < guardCount; i++) {
                    var guard = guards[i];
                    guard.ResetRide();
                }
                attackIndices = Array.Empty<int>();
            }

            // teleport to side above, mixed attack:
            // owner deathray sweep, set invade, laser and deathray zap
            if (IsFirstStage) {
                if (timer == teleport3 - fadeTime) {
                    var pos = PrejudgePlayerTargetPos(Main.rand.Next(0, 10 + 1));
                    pos += new Vector2(Utils.SignNoise() * Main.rand.Next(320, 640 + 1), -300);
                    PrepareTeleport(pos);
                }
                if (timer == teleport3) {
                    DoTeleport();
                }
                if (timer == teleport3) {
                    attackRad = Utils.ClipRad(RotationTo(PrejudgePlayerTargetPos(Main.rand.Next(0, 10 + 1))));

                    var TRF = TimeRatioFuncSet.SinLerpConcave;
                    float start = Utils.ClipRad(attackRad - MathHelper.ToRadians(90));
                    float end = Utils.ClipRad(attackRad - MathHelper.ToRadians(30));
                    AttackDeathrayLerp(start, new LerpData<float>(end, 80, LerpFuncSet.Radian, TRF));
                    start = Utils.ClipRad(attackRad + MathHelper.ToRadians(90));
                    end = Utils.ClipRad(attackRad + MathHelper.ToRadians(30));
                    AttackDeathrayLerp(start, new LerpData<float>(end, 80, LerpFuncSet.Radian, TRF));

                    int rideCount = guards.Sum(x => x.IsRiding || x.IsRiding ? 1 : 0);
                    if (rideCount < guardCount) {
                        DebugMsg($"TeleportAndShoot1-3 only {rideIndices.Length}/7 guards riding");
                    }
                    HandleIndexSetInvade(2);
                    foreach (int i in invadeIndices) {
                        var guard = guards[i];
                        guard.SetInvade();
                    }
                    invadeIndices = Array.Empty<int>();
                    Utils.ShuffleArray(ref rideIndices);
                    attackingGuards = new MachineBrainGuard[5];
                    for (int i = 0; i < 5; i++) {
                        attackingGuards[i] = guards[rideIndices[i]];
                    }
                    rideIndices = Array.Empty<int>();
                    // 0-3 deathray
                    // 3-5 lasers
                    attackPos = PrejudgePlayerTargetPos(Main.rand.Next(0, 10 + 1));
                }
                if (timer == shoot3) {
                    if (DetectTransition()) {
                        attackPos = Vector2.Zero;
                        attackingGuards = Array.Empty<MachineBrainGuard>();
                        // todo: kill channeling deathrays, command retreat, etc.
                        return true;
                    }
                }
                if (timer >= shoot3 && timer <= shoot3 + 60) {
                    // lasers
                    for (int i = 3; i < 5; i++) {
                        if (Main.rand.NextBool(15)) {
                            var guard = attackingGuards[i];
                            float rad = RotationTo(attackPos);
                            rad += Utils.RadNoise(10);
                            guard.AttackLaser(rad);
                        }
                    }
                    // deathrays
                    if ((timer - shoot3) % 20 == 0) {
                        int index = (timer - shoot3) / 20;
                        if (index < 3) {
                            var guard = attackingGuards[index];

                            var TRF = TimeRatioFuncSet.SinLerpConcave;
                            float timeRatio = (float)(timer - teleport3) / 80;
                            float radMax = MathHelper.ToRadians(LerpFuncSet.Scalar(90, 30, TRF(timeRatio)));
                            float theta = Utils.FloatNoise(radMax) + attackRad;
                            float rou = Vector2.Distance(PlayerTarget.Center, guard.NPC.Center);
                            var pos = RideCenter + Utils.Radius(theta, rou);
                            float rad = Utils.ClipRad((pos - guard.NPC.Center).ToRotation());
                            guard.SetRideLerp(new Offset(guard.GetOffset().theta, 150f, rad), 20);
                            guard.AttackDeathrayZapAlert(rad, 20);
                        }
                        if (index >= 1) {
                            var guard = attackingGuards[index - 1];
                            guard.AttackDeathrayZap(guard.Rotation, 10);
                            guard.ResetRide();
                        }
                    }
                }
                if (timer == shoot3 + 60) {
                    attackingGuards = Array.Empty<MachineBrainGuard>();
                }
            }
            // teleport to side above, owner deathray sweep and set invade
            else {
                if (timer == teleport3 - fadeTime) {
                    var pos = PrejudgePlayerTargetPos(Main.rand.Next(0, 10 + 1));
                    pos += new Vector2(Utils.SignNoise() * Main.rand.Next(320, 640 + 1), -300);
                    PrepareTeleport(pos);
                }
                if (timer == teleport3) {
                    DoTeleport();
                }
                if (timer == teleport3) {
                    radDeltaHandler = new AsyncLerper<float>(0);
                    float maxRadDelta = Utils.SignNoise() * MathHelper.ToRadians(45f / 60);
                    radDeltaHandler.SetLerp(new LerpData<float>(maxRadDelta, 30, LerpFuncSet.Radian, TimeRatioFuncSet.SinLerpConcave));
                    sweepRad = MathHelper.ToRadians(90);

                    LerpFunc<float> LF(int index) {
                        LerpFunc<float> _LF = (start, end, ratio) => {
                            float rad = Utils.ClipRad(sweepRad + MathHelper.ToRadians(index * 360 / 5));
                            return rad;
                        };
                        return _LF;
                    }
                    for (int i = 0; i < 5; i++) {
                        AttackDeathrayLerp(0, new LerpData<float>(0, delayTeleport + shootLasts2, LF(i)));
                    }

                    HandleIndexSetInvade(3);
                    for (int i = 0; i < 3; i++) {
                        var guard = guards[invadeIndices[i]];
                        guard.SetInvade(super: i < 1);
                    }
                    invadeIndices = Array.Empty<int>();
                    rideIndices = Array.Empty<int>();
                }
                //if (timer == shoot3 + shootLasts2 / 2 - 30) {
                //    if (Main.rand.NextBool(2)) {
                //        radDeltaHandler.SetLerp(new LerpData<float>(-radDeltaHandler.Value, 30 * 2, LerpFuncSet.Radian, TimeRatioFuncSet.SinLerpConvex));
                //    }
                //}
                if (timer == shoot3 + shootLasts2 / 2) {
                    HandleIndexSetInvade(4);
                    for (int i = 0; i < 3; i++) {
                        var guard = guards[invadeIndices[i]];
                        guard.SetInvade(super: i < 2);
                    }
                    invadeIndices = Array.Empty<int>();
                    rideIndices = Array.Empty<int>();
                }
                if (timer == shoot3 + shootLasts2 - 30) {
                    radDeltaHandler.SetLerp(new LerpData<float>(0, 30, LerpFuncSet.Radian, TimeRatioFuncSet.SinLerpConcave));
                }
                if (timer >= shoot3 && timer <= shoot3 + shootLasts2) {
                    radDeltaHandler.Update();
                    sweepRad += radDeltaHandler.Value;
                }
            }

            VelocityDecay(0.9f, 0.1f);

            return timer >= actEnd + delayEnd;
        }

        private bool ActTeleportAndCharge(int timer) {
            float chargeSpeed = IsFirstStage ? 16f : 20f, beta = 1 / (IsFirstStage ? 15f : 25f);

            int delayStart = 20, delayEnd = 20;
            int delayTeleport = 20, chargeLasts = 45, recoverLasts = 45;
            int teleport1 = delayStart, charge1 = teleport1 + delayTeleport, recover1 = charge1 + chargeLasts;
            int teleport2 = recover1 + recoverLasts, charge2 = teleport2 + delayTeleport, recover2 = charge2 + chargeLasts;
            int teleport3 = recover2 + recoverLasts, charge3 = teleport3 + delayTeleport, recover3 = charge3 + chargeLasts;
            int actEnd = recover3 + recoverLasts;

            void DoCharge() {
                var pos = PrejudgePlayerTargetPos(IsFirstStage ? 0 : 10);
                NPC.velocity = Vec2Target(pos, chargeSpeed);
            }
            void DuringCharge() {
                float speed = NPC.velocity.Length() * 0.95f;
                var pos = PrejudgePlayerTargetPos(IsFirstStage ? 0 : 10);
                FlyMomentum(pos, speed, beta);
            }
            void DuringRecover() {
                VelocityDecay(0.95f, 0.1f);
            }

            Vector2 GetTeleportPos() {
                return GetPosNear(480, 640, 10);
            }

            if (IsSecondStage) {
                if (timer >= delayStart && timer < actEnd) {
                    mirrorImageShouldActive = true;
                }
                else {
                    mirrorImageShouldActive = false;
                }
            }

            if (timer == teleport1 - fadeTime) {
                PrepareTeleport(GetTeleportPos());
            }
            if (timer == teleport1) {
                DoTeleport();
            }
            if (timer == charge1) {
                if (DetectTransition()) {
                    return true;
                }
                DoCharge();
            }
            if (timer >= charge1 && timer < recover1) {
                DuringCharge();
            }
            if (timer >= recover1 && timer < charge2) {
                DuringRecover();
            }
            // zap1: sweep alert and zap
            if (timer == recover1 - 20) {
                attackIndices = IsFirstStage ? new int[] { 0, 3, 6 } : new int[] { 0, 1, 2, 3, 4, 5, 6 };
                float radDelta = Utils.RadNoise(90);
                sign = Math.Sign(radDelta);
                float rad = Utils.ClipRad(guards[3].GetOffset().theta + radDelta);
                foreach (int i in attackIndices) {
                    var guard = guards[i];
                    if (!guard.IsRiding) {
                        DebugMsg($"TeleportAndCharge1-1 Guard {i} not riding");
                        continue;
                    }
                    float theta = IsFirstStage ? guard.GetOffset().theta + radDelta : rad + (i - 3) * MathHelper.ToRadians(360 / 7);
                    guard.SetRideLerp(new Offset(Utils.ClipRad(theta), 150f, theta), 20);
                }
            }
            if (timer == recover1) {
                foreach (int i in attackIndices) {
                    var guard = guards[i];
                    float sweepDeg = IsFirstStage ? 120 + Utils.FloatNoise(10) : 150 + Utils.FloatNoise(20);
                    float theta = Utils.ClipRad(guard.GetOffset().theta + MathHelper.ToRadians(sweepDeg) * sign);
                    var target = new Offset(theta, 150f, theta);
                    guard.SetRideLerp(target, 20);
                    guard.AttackDeathrayLerpAlert(guard.Rotation, new LerpData<float>(target.phi, 20, LerpFuncSet.Radian));
                }
            }
            if (timer == recover1 + 20) {
                foreach (int i in attackIndices) {
                    var guard = guards[i];
                    guard.AttackDeathrayZap(guard.Rotation, 10);
                }
            }
            if (timer == recover1 + 30) {
                var guardsClone = new MachineBrainGuard[guardCount];
                for (int i = 0; i < guardCount; i++) {
                    guardsClone[i] = guards[i];
                }
                float deg = Utils.ClipDeg(MathHelper.ToDegrees(guards[3].GetOffset().theta) - 270) / (360 / attackIndices.Length);
                int permute = sign > 0 ? (int)Math.Ceiling(deg) : (int)Math.Floor(deg);

                if (IsFirstStage) {
                    switch (permute) {
                        case 1:
                            guards[6] = guardsClone[0];
                            guards[0] = guardsClone[3];
                            guards[3] = guardsClone[6];
                            break;
                        case 2:
                            guards[3] = guardsClone[0];
                            guards[6] = guardsClone[3];
                            guards[0] = guardsClone[6];
                            break;
                    }
                }
                else {
                    for (int i = 0; i < guardCount; i++) {
                        guards[i] = guardsClone[(i + guardCount - permute) % guardCount];
                    }
                }
                foreach (int i in attackIndices) {
                    var guard = guards[i];
                    guard.SetIndex(i);
                    guard.ResetRide();
                }
                attackIndices = Array.Empty<int>();
            }

            if (timer == teleport2 - fadeTime) {
                PrepareTeleport(GetTeleportPos());
            }
            if (timer == teleport2) {
                DoTeleport();
            }
            if (timer == charge2) {
                if (DetectTransition()) {
                    return true;
                }
                DoCharge();
            }
            if (timer >= charge2 && timer < recover2) {
                DuringCharge();
            }
            if (timer >= recover2 && timer < charge3) {
                DuringRecover();
            }
            // zap2: focus
            if (timer == charge2 + 15) {
                attackIndices = IsFirstStage ? new int[] { 0, 1, 5, 6 } : new int[] { 0, 1, 2, 3, 4, 5, 6 };
                float rad = Utils.ClipRad((PrejudgePlayerTargetPos(10) - (NPC.Center + NPC.velocity * 10f)).ToRotation());
                TimeRatioFunc TRF = (timeRatio) => (float)Math.Clamp(Math.Pow(timeRatio, 3) * 2, 0, 1);
                for (int i = 0; i < guardCount; i++) {
                    var guard = guards[i];
                    if (!guard.IsRiding) {
                        DebugMsg($"TeleportAndCharge1-2 Guard {i} not riding");
                        continue;
                    }
                    float theta = Utils.ClipRad(rad + (i - 3) * MathHelper.ToRadians(360 / 7));
                    guard.SetRideLerp(new Offset(theta, 150f, rad), chargeLasts, TRF: TRF);
                }
                foreach (int i in attackIndices) {
                    var guard = guards[i];
                    guard.AttackDeathrayLerpAlert(guard.Rotation, new LerpData<float>(rad, 45, LerpFuncSet.Radian, TRF));
                }
            }
            if (timer == recover2 + 15) {
                foreach (int i in attackIndices) {
                    var guard = guards[i];
                    guard.AttackDeathrayZap(guard.Rotation, 10);
                }
                attackIndices = Array.Empty<int>();
            }

            if (timer == teleport3 - fadeTime) {
                PrepareTeleport(GetTeleportPos());
            }
            if (timer == teleport3) {
                DoTeleport();
            }
            if (timer == charge3) {
                if (DetectTransition()) {
                    return true;
                }
                DoCharge();
            }
            if (timer >= charge3 && timer < recover3) {
                DuringCharge();
            }
            if (timer >= recover3 && timer < recover3 + recoverLasts) {
                DuringRecover();
            }
            // zap3: wing, then sweep alert and zap
            float wingTheta1 = 15, wingTheta2 = 25;
            float wingPhi1 = 15, wingPhi2 = 25;
            if (timer == teleport3) {
                attackIndices = IsFirstStage ? new int[] { 1, 2, 4, 5 } : new int[] { 0, 1, 2, 4, 5, 6 };
                LerpFunc<float> RadLF(int index, float deg) {
                    LerpFunc<float> LF = (start, end, ratio) => {
                        float rad = RotationTo(PrejudgePlayerTargetPos(10)) + MathHelper.Pi;
                        return Utils.ClipRad(rad + (index - 3) * MathHelper.ToRadians(deg));
                    };
                    return LF;
                };
                LerpFunc<Offset> OffsetLF(int index) {
                    LerpFunc<Offset> LF = (start, end, ratio) => {
                        float theta = LerpFuncSet.Radian(start.theta, RadLF(index, wingTheta1)(0, 0, 0), ratio);
                        float phi = LerpFuncSet.Radian(start.phi, RadLF(index, wingPhi1)(0, 0, 0), ratio);
                        return new Offset(theta, 150f, phi);
                    };
                    return LF;
                }
                for (int i = 0; i < guardCount; i++) {
                    var guard = guards[i];
                    if (!guard.IsRiding) {
                        DebugMsg($"TeleportAndCharge1-3 Guard {i} not riding");
                        continue;
                    }
                    guard.SetRideLerp(new Offset(), 19, LF: OffsetLF(i));
                }
                foreach (int i in attackIndices) {
                    var guard = guards[i];
                    guard.AttackDeathrayLerpAlert(0, new LerpData<float>(0, 19, RadLF(i, wingPhi1)));
                }
            }
            if (timer == charge3) {
                LerpFunc<float> RadLF(int index, float degStart, float degEnd) {
                    LerpFunc<float> LF = (start, end, ratio) => {
                        float rad = (-NPC.velocity).ToRotation();
                        float deg = LerpFuncSet.Scalar(degStart, degEnd, ratio);
                        return Utils.ClipRad(rad + (index - 3) * MathHelper.ToRadians(deg));
                    };
                    return LF;
                };
                LerpFunc<Offset> OffsetLF(int index) {
                    LerpFunc<Offset> LF = (start, end, ratio) => {
                        return new Offset(RadLF(index, wingTheta1, wingTheta2)(0, 0, 0), 150f, RadLF(index, wingPhi1, wingPhi2)(0, 0, 0));
                    };
                    return LF;
                }
                for (int i = 0; i < guardCount; i++) {
                    var guard = guards[i];
                    guard.SetRideLerp(new Offset(), chargeLasts, LF: OffsetLF(i));
                }
                foreach (int i in attackIndices) {
                    var guard = guards[i];
                    guard.AttackDeathrayLerp(0, new LerpData<float>(0, chargeLasts, RadLF(i, wingPhi1, wingPhi2)));
                }
                attackIndices = Array.Empty<int>();
            }
            if (timer == recover3) {
                float rad = (-NPC.velocity).ToRotation();
                for (int i = 0; i < guardCount; i++) {
                    var guard = guards[i];
                    float theta = Utils.ClipRad(rad + (i - 3) * MathHelper.ToRadians(360f / 7));
                    TimeRatioFunc TRF = (timeRatio) => (float)Math.Clamp(Math.Pow(timeRatio, 3) * 2, 0, 1);
                    guard.SetRideLerp(new Offset(theta, 150f, theta), 35, TRF: TRF);
                    guard.AttackDeathrayLerpAlert(guard.Rotation, new LerpData<float>(theta, 35, LerpFuncSet.Radian, TRF));
                }
            }
            if (timer == recover3 + 35) {
                for (int i = 0; i < guardCount; i++) {
                    var guard = guards[i];
                    guard.AttackDeathrayZap(guard.Rotation, 10);
                }
            }
            if (timer == recover3 + recoverLasts) {
                for (int i = 0; i < guardCount; i++) {
                    var guard = guards[i];
                    guard.ResetRide();
                }
            }

            return timer >= actEnd + delayEnd;
        }

        private float chargeSpeed, beta;
        private int chargeLasts, recoverLasts;
        private bool ActTeleportAndCharge3(int timer) {
            int delayStart = 20, delayEnd = 0;
            int delayTeleport = 20;
            int teleport = delayStart, charge = teleport + delayTeleport, recover = charge + chargeLasts, actEnd = recover + recoverLasts;

            if (timer > delayStart && timer < actEnd) {
                mirrorImageShouldActive = true;
            }
            else {
                mirrorImageShouldActive = false;
            }

            if (timer == teleport - fadeTime) {
                int near, far;
                // swift
                if (Main.rand.Next(3) < 1) {
                    chargeSpeed = 18f;
                    beta = 1 / 15f;
                    near = 480;
                    far = 640;
                    chargeLasts = Main.rand.Next(60, 120 + 1);
                    recoverLasts = 0;
                }
                // clumsy
                else {
                    chargeSpeed = 22f;
                    beta = 1 / 25f;
                    near = 640;
                    far = 800;
                    chargeLasts = Main.rand.Next(30, 60 + 1);
                    recoverLasts = Main.rand.Next(60, 120 + 1);
                }
                var pos = GetPosNear(near, far, 10);
                PrepareTeleport(pos);
            }
            if (timer == teleport) {
                DoTeleport(resetVel: true);
            }
            if (timer == charge) {
                var pos = PrejudgePlayerTargetPos(10);
                NPC.velocity = Vec2Target(pos, chargeSpeed);
            }
            if (timer >= charge && timer < recover) {
                var pos = PrejudgePlayerTargetPos(10);
                FlyMomentum(pos, chargeSpeed, beta);
            }
            if (timer >= recover && timer < actEnd) {
                VelocityDecay(0.95f);
                if (NPC.velocity.Length() < 5f) {
                    return true;
                }
            }

            return timer >= actEnd + delayEnd;
        }

        private static float SecondStageAt {
            get => Main.expertMode ? 0.5f : 0.4f;
        }
        private static float ThirdStageAt {
            get => Main.expertMode ? 0.15f : 0f;
        }
        
        private bool DetectTransition() {
            if (IsFirstStage && NPC.life < NPC.lifeMax * SecondStageAt) {
                return true;
            }
            if (IsSecondStage && (Main.expertMode && NPC.life < NPC.lifeMax * ThirdStageAt)) {
                return true;
            }
            return false;
        }
        private void Schedule() {
            if (DetectTransition()) {
                if (IsFirstStage) {
                    State = AllState.transition;
                }
                else if (IsSecondStage) {
                    State = AllState.transition2;
                }
            }
            else {
                if (IsFirstStage) {
                    switch (State) {
                        case AllState.setInvade:
                            State = AllState.teleportAndShoot;
                            break;
                        case AllState.teleportAndShoot:
                            State = AllState.teleportAndCharge;
                            break;
                        case AllState.teleportAndCharge:
                            State = AllState.setInvade;
                            break;
                    }
                }
                else if (IsSecondStage) {
                    switch (State) {
                        case AllState.setInvade2:
                            State = AllState.teleportAndShoot2;
                            break;
                        case AllState.teleportAndShoot2:
                            State = AllState.teleportAndCharge2;
                            break;
                        case AllState.teleportAndCharge2:
                            State = AllState.setInvade2;
                            break;
                    }
                }
                else if (IsThirdStage) {
                    switch (State) {
                        case AllState.teleportAndCharge3:
                            State = AllState.teleportAndCharge3;
                            break;
                    }
                }
                else {
                    switch (State) {
                        case AllState.spawn:
                            State = AllState.setInvade;
                            break;
                        case AllState.transition:
                            State = AllState.setInvade2;
                            break;
                        case AllState.transition2:
                            State = AllState.teleportAndCharge3;
                            break;
                    }
                }
                
            }

            NPC.netUpdate = true;
            return;
        }

        public override void AI() {
            // Target player
            if (RetargetPlayers()) {
                ResetDespawn();
            }
            else {
                Despawn();
                return;
            }

            // Despawn condition
            if (Utils.L1Distance(NPC.Center, PlayerTarget.Center) > 6000f) {
                Kill();
                return;
            }

            HandleMirrorImage();

            ActFunc actFunc = (timer) => false;
            switch (State) {
                case AllState.setInvade:
                case AllState.setInvade2:
                    actFunc = ActSetInvade;
                    break;
                case AllState.teleportAndShoot:
                case AllState.teleportAndShoot2:
                    actFunc = ActTeleportAndShoot;
                    break;
                case AllState.teleportAndCharge:
                case AllState.teleportAndCharge2:
                    actFunc = ActTeleportAndCharge;
                    break;
                case AllState.teleportAndCharge3:
                    actFunc = ActTeleportAndCharge3;
                    break;
                case AllState.spawn:
                    actFunc = ActSpawn;
                    break;
                case AllState.transition:
                    actFunc = ActTransition;
                    break;
                case AllState.transition2:
                    actFunc = ActTransition2;
                    break;
                case AllState.deathAnimation:
                    actFunc = ActDeathAnimation;
                    break;
            }

            if (actFunc(Timer)) {
                Timer = -1;
                Schedule();
            }

            Timer++;
            NPC.life = 300000;
        }

        public override bool CheckDead() {
            bool result = base.CheckDead();
            if (result && State != AllState.deathAnimation) {
                State = AllState.deathAnimation;
                Timer = 0;
                NPC.life = 1;
                NPC.dontTakeDamage = true;
                return false;
            }
            return result;
        }
        // Draw
        private int frameAppearance = 1;
        public override void FindFrame(int frameHeight) {
            int firstFrame = 0;
            int lastFrame = Main.npcFrameCount[Type] - 1;
#if VANILLA_SPRITE
            switch (frameAppearance) {
                case 1:
                    firstFrame = 0;
                    lastFrame = 3;
                    break;
                case 2:
                    firstFrame = 0;
                    lastFrame = 3;
                    break;
                case 3:
                    firstFrame = 4;
                    lastFrame = 7;
                    break;
            }
#else
            switch (frameAppearance) {
                case 1:
                    firstFrame = 0;
                    lastFrame = 0;
                    break;
                case 2:
                    firstFrame = 1;
                    lastFrame = 1;
                    break;
                case 3:
                    firstFrame = 2;
                    lastFrame = 2;
                    break;
            }
#endif
            LoopFrame(frameHeight, firstFrame, lastFrame, 10);
        }
#if VANILLA_SPRITE
        public override string Texture => $"{Workdir}/NPC_266";
#endif
        public override string BossHeadTexture => $"{Workdir}/BossHead";

        private void InitSprite() {
#if VANILLA_SPRITE
            NPC.width = 200;
            NPC.height = 180;
#else
            NPC.width = 96 * 2;
            NPC.height = 77 * 2;
#endif
        }
#if VANILLA_SPRITE
        private Vector2 TextureHead => NPC.Center - Vector2.UnitY * NPC.height * 0.5f;
        private Vector2 RideCenter => TextureHead + Vector2.UnitY * 65 * 2;
#else
        private Vector2 TextureHead => NPC.Center - Vector2.UnitY * 40 * 2;
        private Vector2 RideCenter => TextureHead + Vector2.UnitY * 63 * 2;
#endif
        private Vector2 FixedOffset => RideCenter - NPC.Center;

        // mirror image: fade in during teleport, either fade out or last for a moment
        private readonly Vector2[] mirrorImagePos = new Vector2[3];
        private bool mirrorImageIsActive = false;
        private bool mirrorImageShouldActive = false;
        private float mirrorImageAlpha {
            get {
                float lifeRatio = (float)NPC.life / NPC.lifeMax;
                lifeRatio = (lifeRatio - ThirdStageAt) / (SecondStageAt - ThirdStageAt);
                float alpha = IsSecondStage ? 1 - lifeRatio : 0.8f;
                if (justPrepareTeleport || !teleportPos.HasValue) {
                    return alpha;
                }
                alpha *= 1 - Alpha;
                return alpha;
            }
        }
        private readonly Vector2[] mirrorImagePosFading = new Vector2[3];
        private float mirrorImageAlphaFading = 0;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            void Draw(Vector2 pos, Color color) {
                var texture = TextureAssets.Npc[Type].Value;
                spriteBatch.Draw(texture, pos - screenPos, NPC.frame, color, NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0);
            }
            void DrawRidingGuards(Vector2 pos, Color color) {
                var texture = TextureAssets.Npc[guards[0].NPC.type].Value;
                var delta = pos - NPC.Center;
                for (int i = 0; i < guardCount; i++) {
                    var guard = guards[i];
                    if (guard.IsRiding) {
                        var posGuard = guard.NPC.Center + delta;
                        spriteBatch.Draw(texture, posGuard - screenPos, guard.NPC.frame, color, guard.NPC.rotation, guard.NPC.frame.Size() * 0.5f, guard.NPC.scale, SpriteEffects.None, 0);
                    }
                }
            }

            var drawPos = TextureHead + Vector2.UnitY * NPC.frame.Height * 0.5f;
            var color = drawColor * Alpha;
            Draw(drawPos, color);

            // teleport alert
            if (teleportPos.HasValue) {
                var delta = teleportPos.Value - NPC.Center;
                drawPos += delta;
                color = drawColor * (1 - Alpha);
                Draw(drawPos, color);
                DrawRidingGuards(drawPos, color);
            }

            // mirror image
            void FillImagePos(Vector2[] imagePos, Vector2 delta) {
                for (int i = 1; i < 4; i++) {
                    imagePos[i - 1] = PlayerTarget.Center + new Vector2(delta.X * (i % 2 == 1 ? -1 : 1), delta.Y * (i / 2 == 1 ? -1 : 1));
                }
            }
            void Active2Fade() {
                for (int i = 0; i < 3; i++) {
                    mirrorImagePosFading[i] = mirrorImagePos[i];
                }
                mirrorImageAlphaFading = mirrorImageAlpha;
                mirrorImageIsActive = false;
            }
            // teleport may overwrite mirror images. Thus, set existing images to fade
            if (justPrepareTeleport) {
                if (mirrorImageIsActive) {
                    Active2Fade();
                }
            }
            if ((IsSecondStage && teleportPos.HasValue) || mirrorImageShouldActive) {
                var delta = drawPos - PlayerTarget.Center;
                FillImagePos(mirrorImagePos, delta);
                mirrorImageIsActive = true;
            }
            else {
                if (mirrorImageIsActive) {
                    Active2Fade();
                    mirrorImageIsActive = false;
                }
            }

            if (mirrorImageIsActive) {
                color = drawColor * mirrorImageAlpha;
                foreach (var pos in mirrorImagePos) {
                    Draw(pos, color);
                    DrawRidingGuards(pos, color);
                }
            }
            if (mirrorImageAlphaFading > 0) {
                color = drawColor * mirrorImageAlphaFading;
                foreach (var pos in mirrorImagePosFading) {
                    Draw(pos, color);
                    DrawRidingGuards(pos, color);
                }
            }

            return false;
        }

        private void HandleMirrorImage() {
            if (justPrepareTeleport) {
                justPrepareTeleport = false;
            }
            if (justDoneTeleport) {
                justDoneTeleport = false;
            }
            if (mirrorImageAlphaFading > 0) {
                mirrorImageAlphaFading -= 1 / 40f;
            }
        }

#if !VANILLA_SPRITE
        private static Asset<Texture2D> glowAsset;
        public override void Load() {
            glowAsset = ModContent.Request<Texture2D>($"{Workdir}/GlowMachineBrain");
        }
        public override void Unload() {
            glowAsset = null;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
            var texture = glowAsset.Value;
            var pos = TextureHead + Vector2.UnitY * NPC.frame.Height * 0.5f;
            var color = Color.White * Alpha;
            spriteBatch.Draw(texture, pos - screenPos, NPC.frame, color, NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0);
        }
#endif

        // Stats
        private void SetStats() {
            NPC.damage = 1;
            NPC.defense = 100;
            NPC.lifeMax = 300000;
            NPC.knockBackResist = 0f;
        }
        private void SetStatsSecondStage() {
            NPC.damage = 1;
            NPC.defense = 75;
        }
        private void SetStatsThirdStage() {
            NPC.damage = 1;
            NPC.defense = 50;
        }
        // ToDo: disable master mod stat scale; endurance
        private static readonly Dictionary<string, float> DamageDict = new() { { "Contact", 2f }, { "Laser", 1f }, { "Deathray", 1.4f }, { "GuardContact", 1f }, { "GuardSpit", 1f } };
        internal int GetDamage(string tag) {
            return (int)(DamageDict[tag] * NPC.damage);
        }
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) {
            NPC.lifeMax = 800000;
        }
        public override void SetDefaults() {
            SetStats();

            NPC.noGravity = true;
            NPC.noTileCollide = true;

            NPC.SpawnWithHigherTime(30);
            NPC.boss = true;
            NPC.npcSlots = 6f;

            NPC.HitSound = SoundID.NPCHit9;
            NPC.DeathSound = SoundID.NPCDeath11;
            Music = MusicID.Boss3;

            NPC.aiStyle = -1;
        }
        // Misc
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("The Intelligence");
            NPCID.Sets.MPAllowedEnemies[Type] = true;

            NPCDebuffImmunityData debuffData = new() {
                SpecificallyImmuneTo = new int[]
                {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
#if VANILLA_SPRITE
            Main.npcFrameCount[Type] = 8;
#else
            Main.npcFrameCount[Type] = 3;
#endif
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
            cooldownSlot = ImmunityCooldownID.Bosses;
            return true;
        }

        // Debug
        internal readonly bool debug = true;
        private void DebugMsg(string msg) {
            if (debug) {
                Main.NewText(msg);
            }
        }
    }
}