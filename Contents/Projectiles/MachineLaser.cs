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
    public class MachineLaser : ProjectileBase {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Machine Laser");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5; // The length of old position to be recorded
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0; // The recording mode
        }

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;

            Projectile.friendly = false;
            Projectile.hostile = false;

            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.penetrate = -1;

            Projectile.aiStyle = 0;
        }

        public override void AI() {

        }

        public override bool PreDraw(ref Color lightColor) {
            //Main.instance.LoadProjectile(Type);
            var texture = TextureAssets.Projectile[Type].Value;

            // Redraw the projectile with the color not influenced by light
            var drawOrigin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
            for (int i = 0; i < Projectile.oldPos.Length; i++) {
                var pos = Projectile.oldPos[i] + drawOrigin - Main.screenPosition;
                var color = Projectile.GetAlpha(lightColor) * ((Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length);
                var scale = 1 - (float)(i + 1) / (Projectile.oldPos.Length + 1);
                Main.EntitySpriteDraw(texture, pos, null, color, Projectile.rotation, drawOrigin, scale, SpriteEffects.None, 0);
            }
            return true;
        }
    }
}