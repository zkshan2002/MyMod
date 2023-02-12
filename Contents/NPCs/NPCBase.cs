using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ReLogic.Content;

namespace MyMod.Contents.NPCs {
    public abstract class NPCBase : ModNPC {

        protected virtual string Workdir => GetType().Namespace.Replace('.', '/');

        protected bool inited = false;
        protected virtual bool Init() {
            alphaHandler = new AsyncLerper<float>(NPC.alpha);
            return true;
        }
        public override bool PreAI() {
            if (!base.PreAI()) {
                return false;
            }
            if (!inited) {
                inited = true;
                if (!Init()) {
                    Kill();
                    return false;
                }
            }
            return true;
        }
        
        protected void Kill() {
            NPC.active = false;
            NPC.netUpdate = true;
            if (Main.netMode == NetmodeID.Server) {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
        }

        protected int despawnTimer = 0;
        protected virtual void Despawn() {
            if (despawnTimer < 120) {
                despawnTimer += 1;
            }
            else {
                Kill();
            }
            if (despawnTimer >= 60) {
                NPC.velocity.Y += (despawnTimer - 60) * 0.25f;
            }
        }
        protected virtual void ResetDespawn() {
            if (despawnTimer > 0) {
                despawnTimer -= 1;
            }
        }

        internal Player PlayerTarget {
            get {
                if (NPC.target < 0 || NPC.target >= Main.maxPlayers) {
                    return null;
                }
                return Main.player[NPC.target];
            }
        }
        protected bool TargetPlayer(bool force = false) {
            if (force || NPC.target < 0 || NPC.target >= Main.maxPlayers || Main.player[NPC.target].dead || !Main.player[NPC.target].active) {
                NPC.TargetClosest();
                if (NPC.target < 0 || NPC.target == Main.maxPlayers) {
                    return false;
                }
            }
            return true;
        }
        
        protected Vector2 PrejudgeTargetPos(int prejudgeTime) {
            return PlayerTarget.Center + PlayerTarget.velocity * prejudgeTime;
        }
        protected Vector2 Vec2Rescale(Vector2 target, float scale) {
            return (target - NPC.Center).SafeNormalize(-Vector2.UnitY) * scale;
        }
        protected float Rot2(Vector2 target) {
            return Utils.ClipRad(Vec2Rescale(target, 1f).ToRotation());
        }

        protected void Teleport2(Vector2 pos, bool resetVel = false) {
            NPC.position = pos - new Vector2(NPC.width, NPC.height) * 0.5f;
            if (resetVel) {
                NPC.velocity = Vector2.Zero;
            }
        }
        protected void VelDecay(float ratio, float? clip = null) {
            NPC.velocity *= ratio;
            if (clip.HasValue) {
                if (Math.Abs(NPC.velocity.X) < clip.Value) {
                    NPC.velocity.X = 0f;
                }
                if (Math.Abs(NPC.velocity.Y) < clip.Value) {
                    NPC.velocity.Y = 0f;
                }
            }
        }
        protected void VelCorrection(Vector2 target, float delta) {
            if (NPC.velocity.X < target.X) {
                NPC.velocity.X += delta;
                if (NPC.velocity.X < 0f && target.X > 0f) {
                    NPC.velocity.X += delta;
                }
            }
            else if (NPC.velocity.X > target.X) {
                NPC.velocity.X -= delta;
                if (NPC.velocity.X > 0f && target.X < 0f) {
                    NPC.velocity.X -= delta;
                }
            }
            if (NPC.velocity.Y < target.Y) {
                NPC.velocity.Y += delta;
                if (NPC.velocity.Y < 0f && target.Y > 0f) {
                    NPC.velocity.Y += delta;
                }
            }
            else if (NPC.velocity.Y > target.Y) {
                NPC.velocity.Y -= delta;
                if (NPC.velocity.Y > 0f && target.Y < 0f) {
                    NPC.velocity.Y -= delta;
                }
            }
        }
        protected void FlySimple(Vector2 targetPos, float speed, float delta) {
            VelCorrection(Vec2Rescale(targetPos, speed), delta);
        }
        
        protected void FlyUniform(Vector2 targetPos, float speed) {
            Vector2 toTarget = targetPos - NPC.Center;
            if (toTarget.Length() < speed) {
                NPC.velocity = toTarget;
            }
            else {
                NPC.velocity = Vec2Rescale(targetPos, speed);
            }
        }
        
        protected void VelSoftUpdate(Vector2 targetVel, float beta) {
            NPC.velocity = Vector2.Lerp(NPC.velocity, targetVel, beta);
        }
        protected void FlyMomentum(Vector2 targetPos, float speed, float beta) {
            VelSoftUpdate(Vec2Rescale(targetPos, speed), beta);
        }
        // Rotation
        protected int spriteRotationDeg = 0;
        internal float Rotation {
            get => Utils.ClipRad(NPC.rotation + MathHelper.ToRadians(spriteRotationDeg));
            set => NPC.rotation = Utils.ClipRad(value - MathHelper.ToRadians(spriteRotationDeg));
        }
        protected void RotCharge() {
            Rotation = Utils.ClipRad(NPC.velocity.ToRotation());
        }

        protected float rotCorrectionFactor;
        protected void RotCorrection(float target, float? deltaScale = null) {
            Rotation = Utils.RotationCorrection(Rotation, target, deltaScale ?? rotCorrectionFactor);
        }
        protected void RotStare(Vector2? target = null, float? deltaScale = null) {
            RotCorrection(Rot2(target ?? PlayerTarget.Center), deltaScale);
        }

        protected void LoopFrame(int frameHeight, int firstFrame, int lastFrame, int frameLasts) {
            if (NPC.frame.Y < firstFrame * frameHeight) {
                NPC.frame.Y = firstFrame * frameHeight;
            }
            NPC.frameCounter += 1f;
            if (NPC.frameCounter >= frameLasts) {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y > lastFrame * frameHeight) {
                    NPC.frame.Y = firstFrame * frameHeight;
                }
            }
        }

        protected float MaxAlpha = 1f;
        internal float Alpha {
            get => (1 - NPC.alpha / 255f) / MaxAlpha;
            set => NPC.alpha = (int)(255 * (1 - value * MaxAlpha));
        }
        internal AsyncLerper<float> alphaHandler;
        internal void SetAlpha(float alpha) {
            if (alphaHandler != null) {
                alphaHandler.SetValue(alpha);
            }
            else {
                Alpha = alpha;
            }
        }
        internal void SetAlphaLerp(float target, int time) {
            alphaHandler.SetLerp(new LerpData<float>(target, time, LerpFuncSet.Scalar));
        }
        protected void SetMaxAlpha(float maxAlpha, int time) {
            float target = Alpha;
            SetAlpha(Alpha * MaxAlpha / maxAlpha);
            MaxAlpha = maxAlpha;
            alphaHandler.SetLerp(new LerpData<float>(target, time, LerpFuncSet.Scalar));
        }
        public override void PostAI() {
            alphaHandler.Update();
            Alpha = alphaHandler.Value;
            base.PostAI();
        }

        protected NPC NewNPC(int type, Vector2 pos, Vector2? vel = null, float ai0 = 0, float ai1 = 0, float ai2 = 0, float ai3 = 0, int target = 255) {
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                var entitySource = NPC.GetSource_FromAI();
                int handle = NPC.NewNPC(entitySource, (int)pos.X, (int)pos.Y, type, ai0: ai0, ai1: ai1, ai2: ai2, ai3: ai3, Target: target);
                if (handle < 0 || handle >= Main.maxNPCs) {
                    return null;
                }
                var npc = Main.npc[handle];
                if (vel.HasValue) {
                    npc.velocity = vel.Value;
                }
                if (Main.netMode == NetmodeID.Server) {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, handle);
                }
                return npc;
            }
            return null;
        }
        protected Projectile NewProjectile(int type, Vector2 pos, Vector2? vel = null, int damage = 0, float knockback = 0f, float ai0 = 0, float ai1 = 0) {
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                var entitySource = NPC.GetSource_FromAI();
                int handle = Projectile.NewProjectile(entitySource, pos, vel ?? Vector2.Zero, type, damage, knockback, Main.myPlayer, ai0, ai1);
                if (handle < 0 || handle >= Main.maxProjectiles) {
                    return null;
                }
                return Main.projectile[handle];
            }
            return null;
        }
    }
}