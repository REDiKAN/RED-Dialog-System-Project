#ifndef RM_COMMON
#define RM_COMMON

/// <summary>
/// Constants
/// </summary>

#define RM_PI                    3.14159265359
#define RM_DEGREES_TO_RADIANS    (RM_PI / 180.0)

#define RM_HALF_MIN              6.103515625e-5  // 2^-14, the same value for 10, 11 and 16-bit: https://www.khronos.org/opengl/wiki/Small_Float_Formats

#define RM_MIN_CORNER_VALUE      1e-3
#define RM_MIN_CORNER_VALUE_RECT 1e-3

/// <summary>
/// SDF methods.
/// </summary>

inline float RMPointVsRoundedBox(in float2 position, in float2 cornerCircleDistance, in float cornerCircleRadius)
{
    return length(max(abs(position) - cornerCircleDistance, 0.0)) - cornerCircleRadius;
}

inline float FilterDistance(in float distance)
{
    float pixelDistance = distance / fwidth(distance);

#if defined(_INDEPENDENT_CORNERS) || defined(_UI_CLIP_RECT_ROUNDED_INDEPENDENT)
    // To avoid artifacts at discontinuities in the SDF distance increase the pixel width.
    return saturate(1.0 - pixelDistance);
#else
    return saturate(0.5 - pixelDistance);
#endif
}

inline float RMRoundCornersSmooth(in float2 position, in float2 cornerCircleDistance, in float cornerCircleRadius, in float smoothingValue)
{
    float distance = RMPointVsRoundedBox(position, cornerCircleDistance, cornerCircleRadius);
#if defined(_EDGE_SMOOTHING_AUTOMATIC)
    return FilterDistance(distance);
#else
    return smoothstep(1.0, 0.0, distance / smoothingValue);
#endif
}

inline float RMRoundCorners(in float2 position, in float2 cornerCircleDistance, in float cornerCircleRadius, in float smoothingValue)
{
#if defined(_TRANSPARENT)
    return RMRoundCornersSmooth(position, cornerCircleDistance, cornerCircleRadius, smoothingValue);
#else
    return (RMPointVsRoundedBox(position, cornerCircleDistance, cornerCircleRadius) < 0.0);
#endif
}

inline float RMFindCornerRadius(in float2 uv, in float4 radii)
{
    if (uv.x < 0.5)
    {
        if (uv.y > 0.5) { return radii.x; } // Top left.
        else { return radii.z; } // Bottom left.
    }
    else
    {
        if (uv.y > 0.5) { return radii.y; } // Top right.
        else { return radii.w; } // Bottom right.
    }
}

/// <summary>
/// UnityUI methods.
/// </summary>

inline float RMGet2DClippingRounded(in float2 position, in float4 clipRect, in float radius)
{
    float2 halfSize = (clipRect.zw - clipRect.xy) * 0.5;
    float2 center = clipRect.xy + halfSize;
    float2 offset = position - center;

    return RMPointVsRoundedBox(offset, halfSize - radius, radius);
}

inline float RMGet2DClippingRoundedSoft(in float2 position, in float4 clipRect, in float radius)
{
    return saturate(FilterDistance(RMGet2DClippingRounded(position, clipRect, radius)));
}

inline float RMGet2DClippingRoundedIndependent(in float2 position, in float4 clipRect, in float4 radii)
{
    float2 halfSize = (clipRect.zw - clipRect.xy) * 0.5;
    float2 center = clipRect.xy + halfSize;
    float2 offset = position - center;
    float radius = RMFindCornerRadius(offset, radii);

    return RMPointVsRoundedBox(offset, halfSize - radius, radius);
}

inline float RMGet2DClippingRoundedIndependentSoft(in float2 position, in float4 clipRect, in float4 radii)
{
    return saturate(FilterDistance(RMGet2DClippingRoundedIndependent(position, clipRect, radii)));
}

inline float RMUnityUIClipRect(in float2 position, in float4 clipRect, in float4 radii)
{
    radii = max(radii, RM_MIN_CORNER_VALUE_RECT);
#if defined(UNITY_UI_ALPHACLIP)
    return RMGet2DClippingRoundedIndependent(position, clipRect, radii) <= 0.0;
#else
    return RMGet2DClippingRoundedIndependentSoft(position, clipRect, radii);
#endif
}
#endif // RM_COMMON
