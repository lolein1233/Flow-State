using UnityEngine;

/// <summary>
/// Deterministic intent rules shared by the graffiti input bridge and its tests.
/// They keep raw mouse input out of the paint/deposition code.
/// </summary>
public static class GraffitiGestureDynamics
{
    public static Vector2 CompressControlDelta(
        Vector2 controlDelta,
        float linearRadius,
        float maximumRadius)
    {
        float magnitude = controlDelta.magnitude;
        if (magnitude <= 0.000001f)
            return Vector2.zero;

        linearRadius = Mathf.Max(0f, linearRadius);
        maximumRadius = Mathf.Max(linearRadius, maximumRadius);
        if (magnitude <= linearRadius)
            return controlDelta;

        // Keep deliberate, slow motion one-to-one, then ease large mouse spikes
        // toward a ceiling instead of hard-clipping them. The radial scale
        // preserves gesture direction and avoids different diagonal sensitivity.
        float compressionRange = maximumRadius - linearRadius;
        if (compressionRange <= 0.000001f)
            return controlDelta * (maximumRadius / magnitude);

        float excess = magnitude - linearRadius;
        float compressedMagnitude = linearRadius
            + compressionRange * (1f - Mathf.Exp(-excess / compressionRange));
        compressedMagnitude = Mathf.Min(compressedMagnitude, maximumRadius);
        return controlDelta * (compressedMagnitude / magnitude);
    }

    public static float GetCameraShare(
        float currentHandAxis,
        float prospectiveHandAxis,
        float inputAxis,
        float comfortZone,
        float maximumShare)
    {
        if (Mathf.Abs(inputAxis) <= 0.0001f)
            return 0f;

        // Pulling back toward the centre must always move the hand immediately.
        if (Mathf.Abs(currentHandAxis) > 0.0001f && Mathf.Sign(inputAxis) != Mathf.Sign(currentHandAxis))
            return 0f;

        comfortZone = Mathf.Clamp(comfortZone, 0.1f, 0.98f);
        float edge = Mathf.Max(Mathf.Abs(currentHandAxis), Mathf.Abs(prospectiveHandAxis));
        float edge01 = Mathf.InverseLerp(comfortZone, 1f, edge);
        return Mathf.SmoothStep(0f, Mathf.Clamp01(maximumShare), edge01);
    }

    public static float EvaluateCone(
        float gestureSpeed,
        float turnRateDegrees,
        bool painting,
        float precisionSpeed,
        float coverageSpeed,
        float precisionCone,
        float idleCone,
        float cornerStartDegrees,
        float cornerFullDegrees,
        float cornerPrecision)
    {
        float speed01 = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(precisionSpeed, Mathf.Max(precisionSpeed + 0.01f, coverageSpeed), gestureSpeed));
        float target = Mathf.Lerp(Mathf.Clamp01(precisionCone), 1f, speed01);

        float corner01 = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(cornerStartDegrees, Mathf.Max(cornerStartDegrees + 1f, cornerFullDegrees), turnRateDegrees));
        target = Mathf.Lerp(target, Mathf.Clamp01(precisionCone), corner01 * Mathf.Clamp01(cornerPrecision));

        // With the valve released, return to a useful medium preview instead of
        // leaving the next stroke at the width of an old gesture.
        if (!painting && gestureSpeed < precisionSpeed)
            target = Mathf.Clamp01(idleCone);

        return Mathf.Clamp01(target);
    }
}
