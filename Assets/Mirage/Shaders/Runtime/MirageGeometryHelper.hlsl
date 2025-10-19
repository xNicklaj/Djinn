// Copyright (c) 2024 Léo Chaumartin
// The pipeline and platform independant impostor geometry helper in HLSL


#ifndef MIRAGE_GEOMETRY_HELPER
#define MIRAGE_GEOMETRY_HELPER

#ifndef DEG2RAD
#define DEG2RAD 0.01745328
#endif

#ifndef UNITY_MATRIX_I_M
#define UNITY_MATRIX_I_M unity_WorldToObject
#endif

float CircularClamp(float a, float a1, float a2, out bool clamped)
{
    clamped = false;

    a = frac(a / TWO_PI) * TWO_PI;
    if (a < 0.0) a += TWO_PI;

    a1 = frac(a1 / TWO_PI) * TWO_PI;
    if (a1 < 0.0) a1 += TWO_PI;

    a2 = frac(a2 / TWO_PI) * TWO_PI;
    if (a2 < 0.0) a2 += TWO_PI;

    if (a1 > a2)
    {
        if (a >= a1 || a <= a2)
            return a;
    }
    else
    {
        if (a >= a1 && a <= a2)
            return a;
    }

    clamped = true;

    float distToA1 = abs(a - a1);
    float distToA2 = abs(a - a2);

    distToA1 = min(distToA1, TWO_PI - distToA1);
    distToA2 = min(distToA2, TWO_PI - distToA2);

    return (distToA1 < distToA2) ? a1 : a2;
}

void Billboarding(inout float3 vertex, inout float3 normal, inout float3 tangent, out bool clamped) {
    clamped = false;
    if (_BillboardingEnabled < 0.5)
        return;
    float3 viewVec;

    // Ugly fix until we get some macros consistency across pipelines
    if (length(UNITY_MATRIX_V._m03_m13_m23 - UNITY_MATRIX_I_V._m03_m13_m23) > 0.01)
        viewVec = -_WorldSpaceCameraPos + mul(UNITY_MATRIX_M, float4(0, 0, 0, 1)).xyz;
    else
        viewVec = -UNITY_MATRIX_V._m03_m13_m23 + mul(UNITY_MATRIX_M, float4(0, 0, 0, 1)).xyz;

    float3 viewDir = normalize(viewVec);

    float3x3 rotationScaleMatrix = float3x3(UNITY_MATRIX_M[0].xyz, UNITY_MATRIX_M[1].xyz, UNITY_MATRIX_M[2].xyz);
    float3x3 rotationOnlyMatrix;
    rotationOnlyMatrix[0] = normalize(rotationScaleMatrix[0]);
    rotationOnlyMatrix[1] = normalize(rotationScaleMatrix[1]);
    rotationOnlyMatrix[2] = normalize(rotationScaleMatrix[2]);

    float3x3 rotationInCameraSpace = mul((float3x3)UNITY_MATRIX_V, rotationOnlyMatrix);
    
    float rotationForwardInCameraSpace = asin(rotationInCameraSpace._12);
    float cameraRoll = atan2(UNITY_MATRIX_V._m01, UNITY_MATRIX_V._m11);
    float correctedRotation = rotationForwardInCameraSpace - cameraRoll;
    
    float c = cos(correctedRotation);
    float s = sin(correctedRotation);

    float3x3 forwardAxisRotationMatrix = float3x3(
        float3(c, -s, 0),
        float3(s, c, 0),
        float3(0, 0, 1)
        );
    vertex.xyz = mul(vertex.xyz, forwardAxisRotationMatrix);

    if (_ClampBillboarding) {
        float currentLatitude = asin(viewDir.y);
        float currentLongitude = atan2(viewDir.z, viewDir.x);


        float longitudeOffset = 0.0;
        if (_LongitudeStep < 360.0 / _LongitudeSamples)
        {
            float3x3 rotationScaleMatrix = float3x3(UNITY_MATRIX_M[0].xyz, UNITY_MATRIX_M[1].xyz, UNITY_MATRIX_M[2].xyz);

            float3x3 rotationOnlyMatrix;
            rotationOnlyMatrix[0] = normalize(rotationScaleMatrix[0]);
            rotationOnlyMatrix[1] = normalize(rotationScaleMatrix[1]);
            rotationOnlyMatrix[2] = normalize(rotationScaleMatrix[2]);
            float trYawOffset = -atan2(rotationOnlyMatrix._31, rotationOnlyMatrix._11);

            float longitudeMin = (_LongitudeOffset * DEG2RAD + trYawOffset) % TWO_PI - PI;
            float longitudeMax = (((_LongitudeSamples - 1.0) * _LongitudeStep + _LongitudeOffset) * DEG2RAD + trYawOffset) % TWO_PI - PI;
            currentLongitude = CircularClamp(currentLongitude, longitudeMin, longitudeMax, clamped);
        }

        float latitudeMin = (-_LatitudeOffset - _LatitudeSamples) * _LatitudeStep * DEG2RAD;
        float latitudeMax = (-_LatitudeOffset + _LatitudeSamples) * _LatitudeStep * DEG2RAD;
        if (currentLatitude < latitudeMin || currentLatitude > latitudeMax) {
            clamped = true;
            currentLatitude = clamp(currentLatitude, latitudeMin, latitudeMax);
        }

        float3 clampedViewDir;
        clampedViewDir.y = sin(currentLatitude);
        float cosLatitude = cos(currentLatitude);
        clampedViewDir.x = cosLatitude * cos(currentLongitude);
        clampedViewDir.z = cosLatitude * sin(currentLongitude);
        viewDir = clampedViewDir;
    }
    float3 M2 = mul((float3x3)UNITY_MATRIX_I_M, viewDir);
    float3 M0 = cross(mul((float3x3)UNITY_MATRIX_I_M, float3(0, 1, 0)), M2);
    float3 M1 = cross(M2, M0);
    float3x3 mat = float3x3(normalize(M0), normalize(M1), M2);

    if (_ZOffset > 0) {
        float trYawOffset = atan2(UNITY_MATRIX_M._31, UNITY_MATRIX_M._11);
        float3x3 rotationMatrix = float3x3(
            cos(trYawOffset), 0, sin(trYawOffset),
            0, 1, 0,
            -sin(trYawOffset), 0, cos(trYawOffset)
            );
        float t = UNITY_MATRIX_P._m11;
        float fov = atan(1.0f / t) * 2.0 / DEG2RAD;
        vertex.xyz *= (1.0 - _ZOffset / length(viewVec)); // Scaling to keep the same pixel size than the original object
        vertex.xyz = mul(vertex.xyz, mat) - mul(rotationMatrix, viewDir) * _ZOffset;
    }
    else
        vertex.xyz = mul(vertex.xyz, mat);

   

    
    normal.xyz = mul(normal.xyz, mat);
    tangent.xyz = mul(tangent.xyz, mat);
}

void Billboarding_float(in float3 vertex, in float3 normal, in float3 tangent, out float3 vertexOut, out float3 normalOut, out float3 tangentOut, out bool clamped) {
    vertexOut = vertex;
    normalOut = normal;
    tangentOut = tangent;
    Billboarding(vertexOut, normalOut, tangentOut, clamped);
}

#endif