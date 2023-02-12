using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ReLogic.Content;

namespace MyMod.Contents.Projectiles {
    public abstract class ProjectileBase : ModProjectile {

        protected bool inited = false;
        protected virtual bool Init() {
            return true;
        }
        protected float rotationOffset;
        internal float Rotation {
            get => Utils.ClipRad(Projectile.rotation + rotationOffset);
            set => Projectile.rotation = Utils.ClipRad(value - rotationOffset);
        }
        internal float Alpha {
            get => 1 - Projectile.alpha / 255f;
            set => Projectile.alpha = (int)(255 * (1 - value));
        }
        protected virtual string Workdir => GetType().Namespace.Replace('.', '/');

        protected int frameCountY = 1;
        protected Rectangle Frame(int frameY) {
            var texture = TextureAssets.Projectile[Type].Value;
            int frameWidth = texture.Width;
            int frameHeight = texture.Height / frameCountY - 2;
            return new Rectangle(0, (frameHeight + 2) * frameY, frameWidth, frameHeight);
        }

        protected void Kill() {
            Projectile.active = false;
            Projectile.netUpdate = true;
        }

        protected int playerHandle;

        protected void TeleportTo(Vector2 pos, bool resetVel = false) {
            Projectile.position = pos - new Vector2(Projectile.width, Projectile.height) * 0.5f;
            if (resetVel) {
                Projectile.velocity = Vector2.Zero;
            }
        }

        protected int NewProjectile(int type, Vector2 projPos, Vector2 projVel, int damage = 0, float knockback = 0f, float ai0 = 0, float ai1 = 0) {
            if (Main.netMode != NetmodeID.MultiplayerClient) {
                var entitySource = Projectile.GetSource_FromAI();
                int projectileHandle = Projectile.NewProjectile(entitySource, projPos, projVel, type, damage, knockback, Main.myPlayer, ai0, ai1);
                if (projectileHandle >= Main.maxProjectiles) {
                    return -1;
                }
                return projectileHandle;
            }
            return -1;
        }

        protected void LoopFrame(int firstFrame, int lastFrame, int frameLasts = 10) {
            if (Projectile.frame < firstFrame) {
                Projectile.frame = firstFrame;
            }
            Projectile.frameCounter += 1;
            if (Projectile.frameCounter >= frameLasts) {
                Projectile.frameCounter = 0;
                Projectile.frame += 1;
                if (Projectile.frame > lastFrame) {
                    Projectile.frame = firstFrame;
                }
            }
        }
    }
}