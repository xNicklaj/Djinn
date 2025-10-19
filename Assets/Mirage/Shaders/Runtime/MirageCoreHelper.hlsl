// Copyright (c) 2024 Léo Chaumartin
// The pipeline and platform independant impostor core helper in HLSL

#ifndef MIRAGE_CORE_HELPER
#define MIRAGE_CORE_HELPER

#ifndef PI
#define PI 3.14159
#endif
#ifndef HALF_PI
#define HALF_PI 1.570795
#endif
#ifndef DEG2RAD
#define DEG2RAD 0.01745328
#endif
#ifndef TWO_PI
#define TWO_PI 6.28318
#endif

float3 SaturateColor(float3 color, float saturation)
{
    float gray = dot(color, float3(0.299, 0.587, 0.114));
    return lerp(float3(gray, gray, gray), color, saturation);
}

float3 RGBToHSV(float3 rgb)
{
    float r = rgb.r, g = rgb.g, b = rgb.b;
    float maxVal = max(max(r, g), b);
    float minVal = min(min(r, g), b);
    float delta = maxVal - minVal;

    float h = 0.0;
    float s = 0.0;
    float v = maxVal;

    if (delta != 0.0)
    {
        s = delta / maxVal;

        if (r == maxVal)
            h = (g - b) / delta;
        else if (g == maxVal)
            h = 2 + (b - r) / delta;
        else
            h = 4 + (r - g) / delta;

        h /= 6.0;
        if (h < 0.0)
            h += 1.0;
    }

    return float3(h, s, v);
}

float3 HSVToRGB(float3 hsv)
{
    float h = hsv.x, s = hsv.y, v = hsv.z;
    float r = 0.0, g = 0.0, b = 0.0;

    int i = int(h * 6);
    float f = h * 6 - i;
    float p = v * (1 - s);
    float q = v * (1 - f * s);
    float t = v * (1 - (1 - f) * s);

    if (i == 0) { r = v; g = t; b = p; }
    else if (i == 1) { r = q; g = v; b = p; }
    else if (i == 2) { r = p; g = v; b = t; }
    else if (i == 3) { r = p; g = q; b = v; }
    else if (i == 4) { r = t; g = p; b = v; }
    else { r = v; g = p; b = q; }

    return float3(r, g, b);
}

float Dither(float2 pos, float alpha) {
    float2 uv = pos.xy * _ScreenParams.xy;
    const float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    return alpha - DITHER_THRESHOLDS[index];
}

void ApplyAsinAtanSteepness_float(float value, float coef, out float output) {
    float c = 1.0 + coef * 3.0;
    float t = 0.5 * atan(c * (2.0 * value - 1.0)) / atan(c) + 0.5;
    float s = 0.5 * asin((2.0 * value - 1.0)) / asin(1.0) + 0.5;
    
    if (coef < 0.0)
        output = lerp(s, value, coef + 1.0);
    else 
        output = lerp(value, t, coef);
}


void GetImpostorUV_float(float2 uv_MainTex, float3 viewDir, out float2 gridUv, out float2 gridUvLB, out float2 gridUvRB, out float2 gridUvLT, out float2 gridUvRT, out float alpha, out float beta)
{
    if (_LongitudeStep < 0.01) //retrocompatibility
        _LongitudeStep = 360.0 / _LongitudeSamples;

    float gridSide = round(sqrt(_GridSize));
    float gridStep = 1.0 / gridSide;
    float3x3 rotationScaleMatrix = float3x3(UNITY_MATRIX_M[0].xyz, UNITY_MATRIX_M[1].xyz, UNITY_MATRIX_M[2].xyz);

    float3x3 rotationOnlyMatrix;
    rotationOnlyMatrix[0] = normalize(rotationScaleMatrix[0]);
    rotationOnlyMatrix[1] = normalize(rotationScaleMatrix[1]);
    rotationOnlyMatrix[2] = normalize(rotationScaleMatrix[2]);

    viewDir = mul(viewDir,rotationOnlyMatrix);

    float trYawOffset = 0.0;
    float trPitchOffset = 0.0;
  
    float yaw = (atan2(viewDir.z, viewDir.x)) + TWO_PI + _YawOffset + trYawOffset;
    
    float elevation = asin(viewDir.y) + _ElevationOffset + trPitchOffset;
    
    float unclampedElevationId = floor(elevation / (_LatitudeStep * DEG2RAD)) - _LatitudeOffset;
    float elevationId = clamp(unclampedElevationId, -_LatitudeSamples, _LatitudeSamples);
    
    float elevationFrac = frac(elevation / (_LatitudeStep * DEG2RAD));
    float offset = 0;

    float currentLongitudeSamples;
    float lastLongitudeSamples;
    float scaledYaw;
    bool yawClamped = false;
    if (_SmartSphere > 0.5) {
        for (int l = -_LatitudeSamples; l < elevationId; ++l) {
            if (l == elevationId - 1) {
                lastLongitudeSamples = round(cos(l * HALF_PI / (_LatitudeSamples + 1.0)) * _LongitudeSamples);
                offset += lastLongitudeSamples;
            }
            else
                offset += round(cos(l * HALF_PI / (_LatitudeSamples + 1.0)) * _LongitudeSamples);
        }
        currentLongitudeSamples = round(cos(elevationId * _LatitudeStep * DEG2RAD) * _LongitudeSamples);
        yaw -= _LongitudeOffset * DEG2RAD;
        yaw %= TWO_PI;
        scaledYaw = clamp(yaw / TWO_PI * currentLongitudeSamples, 0.0, currentLongitudeSamples);
    }
    else 
    {
        currentLongitudeSamples = _LongitudeSamples;
        float longitudeMin = (_LongitudeOffset * DEG2RAD + PI) % TWO_PI - PI;
        float longitudeMax = (((_LongitudeSamples - 1.0) * _LongitudeStep + _LongitudeOffset) * DEG2RAD + PI) % TWO_PI - PI;
        yaw = CircularClamp(yaw, longitudeMin, longitudeMax, yawClamped);
        yaw -= _LongitudeOffset * DEG2RAD;
        yaw = (yaw+TWO_PI)%TWO_PI;
        scaledYaw = (yaw / (currentLongitudeSamples * _LongitudeStep * DEG2RAD) * currentLongitudeSamples);
        
    }
    
    float yawId;
    yawId = round(scaledYaw) % currentLongitudeSamples;
    yawClamped = yawClamped && (yawId > (currentLongitudeSamples - 1.5));
    if (_Smooth > 0.5) {
        yawId = floor(scaledYaw) % currentLongitudeSamples;
        gridUv = float2(0.0, 0.0);
        float yawFrac = frac(scaledYaw);
        float subdLB;
        float subdRB;
        float subdLT;
        float subdRT;
        alpha = 1.0 - yawFrac;
        beta = 1.0 - elevationFrac;
        if (_SmartSphere < 0.5) {
            bool clampedLatitude = unclampedElevationId < -_LatitudeSamples || unclampedElevationId >= _LatitudeSamples;
            subdLB = yawId + (elevationId + _LatitudeSamples) * currentLongitudeSamples;
            subdRB = (yawId + (yawClamped ? 0 : 1)) % currentLongitudeSamples + (elevationId + _LatitudeSamples) * currentLongitudeSamples;
            subdLT = (yawId + (elevationId + (clampedLatitude ? 0 : 1) + _LatitudeSamples) * currentLongitudeSamples); ///
            subdRT = ((yawId + (yawClamped ? 0 : 1)) % currentLongitudeSamples + (elevationId + (clampedLatitude ? 0 : 1) + _LatitudeSamples) * currentLongitudeSamples);
        }
        else {
            subdLB = yawId + offset;
            subdRB = (yawId + 1) % currentLongitudeSamples + offset;
            subdLT = subdLB;
            subdRT = subdRB;
        }
        
        gridUvLB = uv_MainTex / gridSide + float2((subdLB % gridSide), floor(subdLB / gridSide)) * gridStep;
        gridUvRB = uv_MainTex / gridSide + float2((subdRB % gridSide), floor(subdRB / gridSide)) * gridStep;
        gridUvRT = uv_MainTex / gridSide + float2((subdRT % gridSide), floor(subdRT / gridSide)) * gridStep;
        gridUvLT = uv_MainTex / gridSide + float2((subdLT % gridSide), floor(subdLT / gridSide)) * gridStep;
    }
    else {
        gridUvLB = gridUvLT = gridUvRB = gridUvRT = float2(0.0, 0.0); // unused
        alpha = beta = 0.0; // unused
        float subd;
        if (_SmartSphere < 0.5) {
                subd = yawId + (elevationId + _LatitudeSamples) * currentLongitudeSamples;
        }
        else
            subd = yawId + offset;
        gridUv = uv_MainTex / gridSide + float2((subd % gridSide), floor(subd / gridSide)) * gridStep;
    }
}
#endif