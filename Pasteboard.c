// limit dir
void LimitDir() {
if (timer == teleport3 - fadeIn) {
    Vector2 GetAttackDir() {
        float deg = Utils.ClipDeg(MathHelper.ToDegrees(PlayerTarget.velocity.ToRotation()));
        bool flagLeft, flagChange = false;
        const int marginDeg = 15;
        // If still or vertical, decide randomly
        if (PlayerTarget.velocity.Length() < 0.1f || Math.Abs(deg - 90) < marginDeg || Math.Abs(deg - 270) < marginDeg) {
            flagLeft = Main.rand.NextBool(2);
        }
        // Else, select opposite side
        // If upward, 1/3 chance to flip to same side
        else {
            flagChange = (deg < marginDeg || deg > 180 - marginDeg) && Main.rand.NextBool(3);
            flagLeft = (deg < 90 || deg > 270) ^ flagChange;
        }
        float resultDeg = Main.rand.Next(0, 45 + 1);
        if (flagLeft) {
            resultDeg += 90 + 30;
        }
        else {
            resultDeg += 0 + 15;
        }
        return Utils.Radius(MathHelper.ToRadians(resultDeg));
    }

    Vector2 GetTeleportPos(Vector2 limitDir) {
        int prejudgeTime = Main.rand.Next(20, 30 + 1);
        var pos = PrejudgePlayerTargetPos(prejudgeTime);
        pos -= limitDir * (Main.rand.Next(300, 500 + 1));
        pos += Utils.Vector2Noise(100f);
        return pos;
    }

    attackDir = GetAttackDir();
    teleportPos = GetTeleportPos(attackDir);

    SetRideAlphaLerp(0, fadeIn);
}
if (timer == teleport3) {
    TeleportTo(teleportPos);

    SetRideAlphaLerp(1, fadeOut);

    Offset GetGuardOffsetTarget(float centerDeg, int index, bool attacking) {
        float theta = Utils.ClipRad(MathHelper.ToRadians(centerDeg + (index - 3) * 25));
        float rou = attacking ? 175 : 150f;
        float phi = Utils.ClipRad(MathHelper.ToRadians(centerDeg + (index - 3) * 15));
        return new Offset(theta, rou, phi);
    }

    float centerDeg = Utils.ClipDeg(MathHelper.ToDegrees(attackDir.ToRotation()) + 180);
    attackIndices = new int[] { 0, 1, 5, 6 };
    for (int i = 0; i < guardCount; i++) {
        var guard = guards[i];
        if (!guard.IsRiding) {
            Main.NewText($"Guard {i} not riding");
            continue;
        }
        Offset target;
        if (Array.IndexOf(attackIndices, i) != -1) {
            target = GetGuardOffsetTarget(centerDeg, i, true);
        }
        else {
            target = GetGuardOffsetTarget(centerDeg, i, false);
        }
        guard.SetRideLerp(target, 20, TimeRatioFuncSet.Identity);
    }
    Main.NewText("Teleport And Shoot 3 / 3!");
}
if (timer == shoot3) {
    foreach(int i in attackIndices) {
        var guard = guards[i];
        if (!guard.IsRiding) {
            Main.NewText($"Guard {i} not riding");
            continue;
        }
        guard.AttackDeathrayZapAlert(guard.Rotation, 20);
    }
}
if (timer == shoot3 + 20) {
    Offset GetGuardOffsetTarget(float centerDeg, int index, bool attacking) {
        float theta = Utils.ClipRad(MathHelper.ToRadians(centerDeg + (index - 3) * 50));
        float rou = attacking ? 225f : 150f;
        float phi = Utils.ClipRad(MathHelper.ToRadians(centerDeg + (index - 3) * 55));
        return new Offset(theta, rou, phi);
    }

    float centerDeg = Utils.ClipDeg(MathHelper.ToDegrees(attackDir.ToRotation()) + 180);
    TimeRatioFunc TRF = (timeRatio) = > (float)Math.Clamp(Math.Pow(timeRatio, 3) * 2, 0, 1);
    for (int i = 0; i < guardCount; i++) {
        var guard = guards[i];
        if (!guard.IsRiding) {
            Main.NewText($"Guard {i} not riding");
            continue;
        }
        Offset target;
        if (Array.IndexOf(attackIndices, i) != -1) {
            target = GetGuardOffsetTarget(centerDeg, i, true);
            guard.AttackDeathrayLerp(guard.Rotation, new LerpData<float>(target.phi, 40, LerpFuncSet.RadLinear, TRF));
        }
        else {
            target = GetGuardOffsetTarget(centerDeg, i, false);
        }
        guard.SetRideLerp(target, 40, TRF);
    }
}
if (timer == shoot3 + shootLasts) {
    for (int i = 0; i < guardCount; i++) {
        var guard = guards[i];
        if (!guard.IsRiding) {
            Main.NewText($"Guard {i} not riding");
            continue;
        }
        guard.ResetRide();
    }
}
}

//

{
    public override string Texture = > $"{Workdir}/MachineDeathray";
    public override void Load() {
        _beamAsset = ModContent.Request<Texture2D>($"{Workdir}/MachineDeathrayBeam");
    }

    public override void Unload() {
        _beamAsset = null;
    }

    public override bool PreDraw(ref Color lightColor) {
        var headTexture = TextureAssets.Projectile[Type].Value;
        var headRad = Utils.ClipRad(Rotation - MathHelper.PiOver2);
        var beamTexture = _beamAsset.Value;
        var beamRad = Utils.ClipRad(Rotation - MathHelper.PiOver2);
        var rearTexture = TextureAssets.Projectile[Type].Value;
        var rearRad = Utils.ClipRad(Rotation - MathHelper.PiOver2 + MathHelper.Pi);

        float effectiveLength = Length + 10;
        int lengthDrawn = 0;

        var drawColor = new Color(255, 255, 255, 0) * Alpha;
        var scale = new Vector2(Projectile.scale, 1);

        // Draw a head
        Main.EntitySpriteDraw(headTexture, Projectile.Center - Main.screenPosition, null, drawColor, headRad, new Vector2(headTexture.Width * 0.5f, 0), scale, SpriteEffects.None, 0);
        lengthDrawn += headTexture.Height;
        // Draw several beam
        for (int i = 0; headTexture.Height + beamTexture.Height * (i + 1) + rearTexture.Height < effectiveLength; i++) {
            Main.EntitySpriteDraw(beamTexture, Projectile.Center + Projectile.velocity * lengthDrawn - Main.screenPosition, null, drawColor, beamRad, new Vector2(beamTexture.Width * 0.5f, 0), scale, SpriteEffects.None, 0);
            lengthDrawn += beamTexture.Height;
        }
        // Draw a residual beam
        int residual = (int)(effectiveLength - lengthDrawn - rearTexture.Height);
        Main.EntitySpriteDraw(beamTexture, Projectile.Center + Projectile.velocity * lengthDrawn - Main.screenPosition, new Rectangle(0, 0, beamTexture.Width, residual), drawColor, beamRad, new Vector2(beamTexture.Width * 0.5f, 0), scale, SpriteEffects.None, 0);
        lengthDrawn += residual;
        // Draw a rear
        Main.EntitySpriteDraw(rearTexture, Projectile.Center + Projectile.velocity * lengthDrawn - Main.screenPosition, null, drawColor, rearRad, new Vector2(rearTexture.Width * 0.5f, 0), scale, SpriteEffects.None, 0);

        return false;
    }
}