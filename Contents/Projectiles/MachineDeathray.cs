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
    public class MachineDeathray : ProjectileBase {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Machine Deathray");
        }

        internal static int GetWidth() {
            return 10;
        }

        public override void SetDefaults() {
            Projectile.width = GetWidth();
            Projectile.height = 0;

            Projectile.friendly = false;
            Projectile.hostile = true;

            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.penetrate = -1;

            Projectile.aiStyle = 0;
        }


        private AsyncLerper<float> rotationHandler;
        private int lasts;
        private int npcHandle;
        private Vector2 fixedOffset;
        private float shotOffset;

        private int Timer {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        private float TimeRatio => (float)Timer / lasts;

        private ref float Length => ref Projectile.ai[1];
        private float maxLength;
        private Vector2 EndPoint => Projectile.Center + Projectile.velocity * Length;
        private float Width => Projectile.width * Projectile.scale;

        internal void Set(float rad, int lasts, int npcHandle, Vector2? fixedOffset = null, float shotOffset = 0, float maxLength = 2400f) {
            rotationHandler = new AsyncLerper<float>(rad);

            this.lasts = lasts;
            Projectile.timeLeft = lasts + 600;
            this.maxLength = maxLength;

            this.npcHandle = npcHandle;
            this.fixedOffset = fixedOffset ?? Vector2.Zero;
            this.shotOffset = shotOffset;
        }
        internal void Set(float rad, LerpData<float> lerpData, int npcHandle, Vector2? fixedOffset = null, float shotOffset = 0, float maxLength = 2400f) {
            rotationHandler = new AsyncLerper<float>(rad);
            rotationHandler.SetLerp(lerpData);

            lasts = lerpData.period;
            Projectile.timeLeft = lasts + 600;
            this.maxLength = maxLength;

            this.npcHandle = npcHandle;
            this.fixedOffset = fixedOffset ?? Vector2.Zero;
            this.shotOffset = shotOffset;
        }

        protected override bool Init() {
            Timer = 0;
            Length = 0;

            rotationOffset = MathHelper.PiOver2;
            frameCountY = 3;

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
            if (npcHandle < 0 || npcHandle >= Main.maxNPCs || !Main.npc[npcHandle].active) {
                Kill();
                return;
            }
            //if (Main.npc[npcHandle].ModNPC.Type == ModContent.NPCType<MachineBrainGuard>()) {
            //    var modNPC = Main.npc[npcHandle].ModNPC as MachineBrainGuard;
            //    if (modNPC.IsRetreating) {
            //        Kill();
            //        return;
            //    }
            //}

            TeleportTo(Main.npc[npcHandle].Center + fixedOffset + Projectile.velocity * shotOffset);

            // Rotation
            rotationHandler.Update();
            Rotation = Utils.ClipRad(rotationHandler.Value);
            Projectile.velocity = Rotation.ToRotationVector2();

            // Length
            const int numSample = 3;
            var result = new float[numSample];
            Collision.LaserScan(Projectile.Center, Projectile.velocity, Width, maxLength, result);
            var targetLength = result.Average();
            Length = MathHelper.Lerp(Length, targetLength, 0.75f);

            // Scale
            float ratio = TimeRatioFuncSet.SinEmergence(4f)(TimeRatio);
            Projectile.scale = 1f * ratio;

            // Alpha
            Alpha = 1f * ratio;

            // Tile light
            Utils.AddLightLineTile(Projectile.Center, EndPoint, Width, new Color(255, 0, 0));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float point = 0;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, EndPoint, Width, ref point);
        }

        public override bool PreDraw(ref Color lightColor) {
            var texture = TextureAssets.Projectile[Type].Value;
            var headTextureSource = Frame(0);
            var bodyTextureSource = Frame(1);
            var tailTextureSource = Frame(2);
            int frameHeight = (int)(Frame(0).Size().Y);

            float effectiveLength = Length + 6;
            int lengthDrawn = 0;

            var color = Color.White * Alpha;
            var origin = new Vector2(texture.Width * 0.5f, 0);
            var scale = new Vector2(Projectile.scale, 1);

            // Draw a head
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, headTextureSource, color, Projectile.rotation, origin, scale, SpriteEffects.None, 0);
            lengthDrawn += frameHeight;
            // Draw several body
            for (int i = 0; frameHeight + frameHeight * (i + 1) + frameHeight < effectiveLength; i++) {
                Main.EntitySpriteDraw(texture, Projectile.Center + Projectile.velocity * lengthDrawn - Main.screenPosition, bodyTextureSource, color, Projectile.rotation, origin, scale, SpriteEffects.None, 0);
                lengthDrawn += frameHeight;
            }
            // Draw a residual body
            int residualLength = (int)(effectiveLength - lengthDrawn - frameHeight);
            var residualBodySource = new Rectangle(bodyTextureSource.Location.X, bodyTextureSource.Location.Y, texture.Width, residualLength);
            Main.EntitySpriteDraw(texture, Projectile.Center + Projectile.velocity * lengthDrawn - Main.screenPosition, residualBodySource, color, Projectile.rotation, origin, scale, SpriteEffects.None, 0);
            lengthDrawn += residualLength;
            // Draw a tail
            Main.EntitySpriteDraw(texture, Projectile.Center + Projectile.velocity * lengthDrawn - Main.screenPosition, tailTextureSource, color, Projectile.rotation, origin, scale, SpriteEffects.None, 0);

            return false;
        }

    }
}