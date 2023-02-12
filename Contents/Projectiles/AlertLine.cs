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
    public class AlertLine : ProjectileBase {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Alert Line");
        }

        public override void SetDefaults() {
            Projectile.width = 0;
            Projectile.height = 0;

            Projectile.friendly = false;
            Projectile.hostile = false;

            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.penetrate = -1;

            Projectile.aiStyle = 0;
        }

        private int Timer {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        private int lasts;
        private float TimeRatio => (float)Timer / lasts;
        private ref float Length => ref Projectile.ai[1];
        private float maxLength;
        private int width;

        private bool fromNPC = false;
        private int npcHandle;
        private float offset;

        private bool toPoint = false;
        private Vector2 lockedEndPoint;

        private AsyncLerper<float> rotationHandler;

        private bool tileCollide = false;
        private int tileCollideWidth;

        private float maxAlpha;
        private TimeRatioFunc alphaTRF;
        private Color color;

        internal void Set(float rad, int lasts, float maxLength = 2400f, int width = 4, int? npcHandle = null, float offset = 0, int? tileCollideWidth = null, float maxAlpha = 0.8f, TimeRatioFunc alphaTRF = null, Color? color = null) {
            rotationHandler = new AsyncLerper<float>(rad);

            this.lasts = lasts;
            Projectile.timeLeft = lasts + 600;
            this.maxLength = maxLength;
            this.width = width;

            if (npcHandle.HasValue) {
                fromNPC = true;
                this.npcHandle = npcHandle.Value;
                this.offset = offset;
            }
            if (tileCollideWidth.HasValue) {
                tileCollide = true;
                this.tileCollideWidth = tileCollideWidth.Value;
            }

            this.maxAlpha = maxAlpha;
            this.alphaTRF = alphaTRF ?? TimeRatioFuncSet.SinEmergence(4f);
            this.color = color ?? Color.Red;
        }
        internal void Set(float rad, LerpData<float> lerpData, float maxLength = 2400f, int width = 4, int? npcHandle = null, float offset = 0, int? tileCollideWidth = null, float maxAlpha = 0.8f, TimeRatioFunc alphaTRF = null, Color? color = null) {
            rotationHandler = new AsyncLerper<float>(rad);
            rotationHandler.SetLerp(lerpData);

            lasts = lerpData.period;
            Projectile.timeLeft = lasts + 600;
            this.maxLength = maxLength;
            this.width = width;

            if (npcHandle.HasValue) {
                fromNPC = true;
                this.npcHandle = npcHandle.Value;
                this.offset = offset;
            }
            if (tileCollideWidth.HasValue) {
                tileCollide = true;
                this.tileCollideWidth = tileCollideWidth.Value;
            }
            this.maxAlpha = maxAlpha;
            this.alphaTRF = alphaTRF ?? TimeRatioFuncSet.SinEmergence(4f);
            this.color = color ?? Color.Red;
        }
        internal void Set(Vector2 endPoint, int lasts, int width = 4, int? npcHandle = null, float offset = 0, float maxAlpha = 0.8f, TimeRatioFunc alphaTRF = null, Color? color = null) {
            toPoint = true;
            lockedEndPoint = endPoint;

            this.lasts = lasts;
            Projectile.timeLeft = lasts + 600;
            this.width = width;

            if (npcHandle.HasValue) {
                fromNPC = true;
                this.npcHandle = npcHandle.Value;
                this.offset = offset;
            }
            this.maxAlpha = maxAlpha;
            this.alphaTRF = alphaTRF ?? TimeRatioFuncSet.SinEmergence(4f);
            this.color = color ?? Color.Red;
        }

        protected override bool Init() {
            Timer = 0;
            Length = 0;
            return true;
        }

        public override void AI() {

            if (!inited) {
                inited = true;
                if (!Init()) {
                    Kill();
                    return;
                }
            }

            Timer += 1;
            if (Timer >= lasts) {
                Kill();
                return;
            }

            // Position
            if (fromNPC) {
                if (npcHandle < 0 || npcHandle >= Main.maxNPCs || !Main.npc[npcHandle].active) {
                    Kill();
                    return;
                }
                TeleportTo(Main.npc[npcHandle].Center + Projectile.velocity * offset);
            }

            // Rotation
            if (toPoint) {
                Rotation = Utils.ClipRad((lockedEndPoint - Projectile.Center).ToRotation());
            }
            else {
                rotationHandler.Update();
                Rotation = Utils.ClipRad(rotationHandler.Value);
            }
            Projectile.velocity = Rotation.ToRotationVector2();

            // Length
            if (toPoint) {
                Length = (lockedEndPoint - Projectile.Center).Length();
            }
            else if (tileCollide) {
                const int numSample = 3;
                var result = new float[numSample];
                Collision.LaserScan(Projectile.Center, Projectile.velocity, tileCollideWidth, maxLength, result);
                Length = result.Average();
            }
            else {
                Length = maxLength;
            }

            // Alpha
            float ratio = alphaTRF(TimeRatio);
            Alpha = maxAlpha * ratio;
        }

        public override bool PreDraw(ref Color lightColor) {
            var drawColor = color * Alpha;

            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, 1, 1), drawColor, Rotation, Vector2.Zero, new Vector2(Length, width), SpriteEffects.None, 0);

            return false;
        }
    }
}