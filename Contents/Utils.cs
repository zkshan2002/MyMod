using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using Terraria.Utilities;

namespace MyMod.Contents {


    internal delegate T LerpFunc<T>(T start, T end, float ratio);
    internal static class LerpFuncSet {
        public static LerpFunc<float> Scalar = MathHelper.Lerp;
        public static float Radian(float start, float end, float ratio) {
            start = Utils.ClipRad(start);
            end = Utils.ClipRad(end);
            if (end - start > MathHelper.Pi) {
                end -= MathHelper.TwoPi;
            }
            else if (end - start < -MathHelper.Pi) {
                end += MathHelper.TwoPi;
            }
            float rotation = MathHelper.Lerp(start, end, ratio);
            return Utils.ClipRad(rotation);
        }
    }

    internal delegate float TimeRatioFunc(float timeRatio);
    internal static class TimeRatioFuncSet {
        public static TimeRatioFunc Identity = (timeRatio) => timeRatio;
        public static TimeRatioFunc SinEmergence(float scale = 4f) {
            return (timeRatio) => MathHelper.Clamp((float)Math.Sin(timeRatio * MathHelper.Pi) * scale, 0f, 1f);
        }
        public static TimeRatioFunc SinLerpConcave = (timeRatio) => (float)Math.Sin(timeRatio * MathHelper.PiOver2);
        public static TimeRatioFunc SinLerpConvex = (timeRatio) => 1 - (float)Math.Cos(timeRatio * MathHelper.PiOver2);
    }

    internal struct LerpData<T> {
        public readonly T end;
        public readonly int period;
        public readonly LerpFunc<T> LF;
        public readonly TimeRatioFunc TRF;
        public LerpData(T end, int period, LerpFunc<T> LF, TimeRatioFunc TRF = null) {
            this.end = end;
            this.period = period;
            this.LF = LF;
            this.TRF = TRF ?? TimeRatioFuncSet.Identity;
        }
    }

    internal class AsyncLerper<T> {
        private T start, end, current;
        private int period = 0, timer;
        private LerpFunc<T> LF;
        private TimeRatioFunc TRF;

        public bool Active => period != 0;
        public AsyncLerper(T init) {
            current = init;
        }
        public void SetValue(T current) {
            this.current = current;
            period = 0;
        }
        public void SetLerp(LerpData<T> data) {
            start = current;
            end = data.end;
            period = data.period;
            LF = data.LF;
            TRF = data.TRF;

            timer = 0;
        }
        public void Update() {
            if (Active) {
                timer++;
                float timeRatio = (float)timer / period;
                float ratio = TRF(timeRatio);
                current = LF(start, end, ratio);
                if (timeRatio >= 1) {
                    period = 0;
                    timer = 0;
                }
            }
        }
        public T Value {
            get => current;
        }
    }

    internal delegate bool ActFunc(int timer);

    internal static class Utils {
        // Vector
        public static float L1Norm(Vector2 vector) {
            return Math.Abs(vector.X) + Math.Abs(vector.Y);
        }
        public static float L1Distance(Vector2 src, Vector2 dst) {
            return L1Norm(dst - src);
        }
        public static Vector2 Radius(float theta, float rou = 1f) {
            return rou * Vector2.UnitX.RotatedBy(theta);
        }
        // Rotation
        public static float ClipRad(float rad) {
            int div = (int)Math.Floor(rad / MathHelper.TwoPi);
            rad -= div * MathHelper.TwoPi;
            return rad;
        }
        public static float ClipDeg(float deg) {
            int div = (int)Math.Floor(deg / 360f);
            deg -= div * 360f;
            return deg;
        }
        public static float Correction(float value, float target, float delta) {
            if (value < target - delta) {
                value += delta;
            }
            else if (value > target + delta) {
                value -= delta;
            }
            else {
                value = target;
            }
            return value;
        }
        public static float RotationCorrection(float rotation, float target, float deltaScale = 1f) {
            float delta = MathHelper.TwoPi / 60 * deltaScale;
            rotation = ClipRad(rotation);
            target = ClipRad(target);
            if (target - rotation > MathHelper.Pi) {
                target -= MathHelper.TwoPi;
            }
            else if (target - rotation < -MathHelper.Pi) {
                target += MathHelper.TwoPi;
            }
            rotation = Correction(rotation, target, delta);
            return ClipRad(rotation);
        }
        // Noise
        public static int SignNoise() {
            return Main.rand.Next(2) * 2 - 1;
        }
        public static float SignedNoise() {
            return Main.rand.NextFloat() * SignNoise();
        }
        public static float FloatNoise(float min, float max) {
            return SignedNoise() * (max - min) / 2 + (max + min) / 2;
        }
        public static float FloatNoise(float scale) {
            return SignedNoise() * scale;
        }
        public static float RadNoise(float degScale) {
            float degNoise = degScale * SignedNoise();
            return MathHelper.ToRadians(degNoise);
        }
        public static Vector2 Vector2Noise(float scale) {
            return new Vector2(FloatNoise(scale), FloatNoise(scale));
        }
        // Debug
        public static void DustBox(Vector2 topLeft, Vector2 size, Color? optionalColor = null) {
            Color color = optionalColor ?? Color.Red;
            Dust.QuickBox(topLeft, topLeft + size, 8, color, default);
        }
        public static void DustBox(Rectangle box, Color? optionalColor = null) {
            DustBox(box.TopLeft(), box.Size(), optionalColor);
        }
        public static void DustPoint(Vector2 pos, Color? optionalColor = null) {
            Color color = optionalColor ?? Color.Red;
            for(int i = 0; i < 8; i++) {
                Dust.QuickDust(pos, color);
            }
        }
        public static void DustLine(Vector2 begin, float rad, float length, Color? optionalColor = null) {
            Color color = optionalColor ?? Color.Red;
            Dust.QuickDustLine(begin, begin + Vector2.UnitX.RotatedBy(rad) * length, length / 4, color);
        }
        public static void DustLine(Vector2 begin, Vector2 dir, float length, Color? optionalColor = null) {
            DustLine(begin, dir.ToRotation(), length, optionalColor);
        }
        public static void DustLine(Vector2 begin, Vector2 end, Color? optionalColor = null) {
            Vector2 aim = end - begin;
            DustLine(begin, aim.ToRotation(), aim.Length(), optionalColor);
        }
        public static void DisplayPos(Vector2 pos, string tag) {
            Main.NewText(tag + " " + pos.X.ToString() + ", " + pos.Y.ToString());
        }
        public static void MarkNPC(NPC npc, Color? optionalColor = null) {
            DustBox(npc.Hitbox, optionalColor);
        }
        public static void DisplayRad(float rad, string tag) {
            Main.NewText($"{tag} {MathHelper.ToDegrees(rad)}");
        }

        public static int NewDust(Vector2 pos, int width, int height, int type, Vector2 vel, int alpha = 0, float scale = 1, bool noGravity = false) {
            int handle = Dust.NewDust(pos, width, height, type, SpeedX: vel.X, SpeedY: vel.Y, Alpha: alpha, Scale: scale);
            if (handle >= Main.maxDust) {
                return -1;
            }
            Dust dust = Main.dust[handle];
            dust.noGravity = noGravity;
            return handle;
        }
        public static void Swap<T>(ref T lhs, ref T rhs) {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
        // Array
        public static void ShuffleArray<T>(ref T[] array) {
            int length = array.Length;
            var randomKeys = new float[length];
            for (int i = 0; i < length; i++) {
                randomKeys[i] = Main.rand.NextFloat();
            }
            Array.Sort(randomKeys, array);
        }
        public static int[] PickK(int length, int k, bool shuffle = true) {
            var randomIndices = new int[length];
            for (int i = 0; i < length; i++) {
                randomIndices[i] = i;
            }
            ShuffleArray(ref randomIndices);
            var result = new int[k];
            for (int i = 0; i < k; i++) {
                result[i] = randomIndices[i];
            }
            if (!shuffle) {
                Array.Sort(result);
            }
            return result;
        }
        public static void PrintArray(int[] array, string name) {
            string str = name;
            foreach (int i in array) {
                str = $"{str} {i}";
            }
            Main.NewText(str);
        }

        // under refinement

        internal delegate bool PixelAction(Point point);

        private static bool LineActionTile(Vector2 start, Vector2 end, float width, PixelAction plot) {
            Point startPoint = start.ToTileCoordinates();
            Point endPoint = end.ToTileCoordinates();
            Vector2 normal = (end - start).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.Pi * 0.5);
            normal *= width / 2;
            Point leftShoulder = (start - normal).ToTileCoordinates();
            Point rightShoulder = (start + normal).ToTileCoordinates();
            Point toLeft = leftShoulder - startPoint;
            Point toRight = rightShoulder - startPoint;

            return LineActionPixel(startPoint, endPoint, (Point) => LineActionPixel(Point + toLeft, Point + toRight, plot, jump: false));
        }

        private static bool LineActionPixel(Point start, Point end, PixelAction plot, bool jump = true) {
            int x0 = start.X, y0 = start.Y, x1 = end.X, y1 = end.Y;
            if (x0 == x1 && y0 == y1) {
                return plot(start);
            }
            bool flag = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            if (flag) {
                Swap(ref x0, ref y0);
                Swap(ref x1, ref y1);
            }
            int num = Math.Abs(x1 - x0);
            int num2 = Math.Abs(y1 - y0);
            int num3 = num / 2;
            int num4 = y0;
            int num5 = ((x0 < x1) ? 1 : (-1));
            int num6 = ((y0 < y1) ? 1 : (-1));
            for (int i = x0; i != x1; i += num5) {
                if (flag) {
                    if (!plot(new(num4, i))) {
                        return false;
                    }
                }
                else if (!plot(new(i, num4))) {
                    return false;
                }
                num3 -= num2;
                if (num3 >= 0) {
                    continue;
                }
                num4 += num6;
                if (!jump) {
                    if (flag) {
                        if (!plot(new(num4, i))) {
                            return false;
                        }
                    }
                    else if (!plot(new(i, num4))) {
                        return false;
                    }
                }
                num3 += num;
            }
            return true;
        }

        private static Vector3 colorBuffer;
        private static bool CastLight(Point point) {
            int x = point.X, y = point.Y;
            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY || Main.tile[x, y] == null) {
                return false;
            }
            Lighting.AddLight(x, y, colorBuffer.X, colorBuffer.Y, colorBuffer.Z);
            return true;
        }

        public static void AddLightLineTile(Vector2 start, Vector2 end, float width, Color color) {
            colorBuffer = color.ToVector3();
            LineActionTile(start, end, width, CastLight);
        }
    }
}
