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

namespace MyMod.Contents.Projectiles.Deathray
{
    public class MoonLordDeathray : ProjectileBase
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Moon Lord Deathray");
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.aiStyle = 0;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.penetrate = -1;
        }

        private ref float Timer => ref Projectile.ai[0];
        private ref float Length => ref Projectile.ai[1];

        protected override bool Init()
        {
            playerHandle = (int)Projectile.ai[0];
            rotationOffset = MathHelper.PiOver2;
            return true;
        }

        private const int lasts = 60;

        public override void AI()
        {
            if (!inited)
            {
                inited = true;
                if (!Init())
                {
                    Kill();
                    return;
                }
            }

            if (playerHandle < 0 || playerHandle >= Main.maxPlayers || Main.player[playerHandle].dead ||
                !Main.player[playerHandle].active)
            {
                Kill();
                return;
            }

            TeleportTo(Main.player[playerHandle].Center + Projectile.velocity * 14f);


            Timer += 1;
            if (Timer >= lasts)
            {
                Kill();
                return;
            }

            Projectile.scale = MathHelper.Clamp((float)Math.Sin((float)Timer / lasts * MathHelper.Pi) * 10f, 0f, 1f);

            float rotationDelta = 0; // MathHelper.ToRadians(1f);
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
            {
                Projectile.velocity = -Vector2.UnitY;
            }
            Rotation = Utils.ClipRad(Projectile.velocity.ToRotation() + rotationDelta);
            Projectile.velocity = Rotation.ToRotationVector2();
            

            // Determine length
            const int numSample = 3;
            const float maxLength = 2400f;
            float[] result = new float[numSample];
            Collision.LaserScan(Projectile.Center, Projectile.velocity, Projectile.width * Projectile.scale, maxLength,
                result);
            float targetLength = 0;
            foreach (float value in result)
            {
                targetLength += value / numSample;
            }

            Length = MathHelper.Lerp(Length, targetLength, 0.5f);

            // Dust
            Vector2 dustPoint = Projectile.Center + Projectile.velocity * (Length - 14f);
            for (int i = 0; i < 2; i++)
            {
                float rou = Utils.FloatNoise(2f, 4f);
                float theta = Utils.ClipRad(Rotation + Utils.SignNoise() * MathHelper.Pi * 0.5f);
                Vector2 vel = rou * Vector2.UnitX.RotatedBy(theta);
                Utils.NewDust(dustPoint, 0, 0, DustID.Vortex, vel, scale: 1.7f, noGravity: true);
            }

            if (Main.rand.NextBool(5))
            {
                Vector2 pos = dustPoint + Projectile.velocity.RotatedBy(MathHelper.Pi * 0.5) * Utils.SignNoise() *
                    Projectile.width * 0.5f;
                const int range = 8;
                int dustHandle = Utils.NewDust(pos - Vector2.One * range * 0.5f, range, range, DustID.Smoke,
                    Vector2.Zero, 100, 1.5f);
                if (dustHandle >= 0)
                {
                    Dust dust = Main.dust[dustHandle];
                    Dust dustClone = dust;
                    dustClone.velocity *= 0.5f;
                    dust.velocity.Y =
                        0f - Math.Abs(dust.velocity.Y);
                }
            }

            Vector2 end = Projectile.Center + Projectile.velocity * Length;
            Utils.AddLightLineTile(Projectile.Center, end, Projectile.width * Projectile.scale,
                new Color(0.3f, 0.65f, 0.7f));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0;
            Vector2 end = Projectile.Center + Projectile.velocity * Length;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, Projectile.width * Projectile.scale, ref point);
        }

        private static Asset<Texture2D> launchAsset;
        private static Asset<Texture2D> beamAsset;
        private static Asset<Texture2D> collideAsset;
        public override void Load() {
            launchAsset = ModContent.Request<Texture2D>("MyMod/Contents/Projectiles/Deathray/MoonLordDeathray");
            beamAsset = ModContent.Request<Texture2D>("MyMod/Contents/Projectiles/Deathray/Extra_21");
            collideAsset = ModContent.Request<Texture2D>("MyMod/Contents/Projectiles/Deathray/Extra_22");
        }

        public override void Unload() {
            launchAsset = null;
            beamAsset = null;
            collideAsset = null;
        }

        public override bool PreDraw(ref Color lightColor) {
            // Color drawColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
            Color drawColor = new Color(255, 255, 255, 0);
            var launchTexture = launchAsset.Value;
            var beamTexture = beamAsset.Value;
            var collideTexture = collideAsset.Value;

            int lengthDrawn = 0;

            Main.EntitySpriteDraw(launchTexture, Projectile.Center - Main.screenPosition,
                null, drawColor, Projectile.rotation,
                new Vector2(launchTexture.Width * 0.5f, 0), new Vector2(Projectile.scale, 1), SpriteEffects.None, 0);

            lengthDrawn += launchTexture.Height;
            
            for (int i = 0; launchTexture.Height + beamTexture.Height * (i + 1) + collideTexture.Height < Length; i++)
            {
                Main.EntitySpriteDraw(beamTexture,
                    Projectile.Center + Rotation.ToRotationVector2() * lengthDrawn - Main.screenPosition, null, drawColor, Projectile.rotation,
                    new Vector2(beamTexture.Width * 0.5f, 0), new Vector2(Projectile.scale, 1), SpriteEffects.None, 0);
                lengthDrawn += beamTexture.Height;
            }

            int residual = (int)(Length - lengthDrawn - collideTexture.Height);

            Main.EntitySpriteDraw(beamTexture,
                Projectile.Center + Rotation.ToRotationVector2() * lengthDrawn -
                Main.screenPosition, new Rectangle(0, 0, beamTexture.Width, residual), drawColor, Projectile.rotation,
                new Vector2(beamTexture.Width * 0.5f, 0), new Vector2(Projectile.scale, 1), SpriteEffects.None, 0);
            lengthDrawn += residual;

            Main.EntitySpriteDraw(collideTexture,
                Projectile.Center +
                Rotation.ToRotationVector2() * lengthDrawn -
                Main.screenPosition, null, drawColor, Projectile.rotation,
                new Vector2(collideTexture.Width * 0.5f, 0), new Vector2(Projectile.scale, 1), SpriteEffects.None, 0);
            lengthDrawn += collideTexture.Height;

            if (Math.Abs(lengthDrawn - Length) > 1)
            {
                Main.NewText($"{Length} {lengthDrawn}");
            }

            return false;
        }
    }
}