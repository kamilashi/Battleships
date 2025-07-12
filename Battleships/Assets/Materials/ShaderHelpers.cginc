float2 UVtoPolar(float2 uv)
{
    float2 centered = uv - 0.5;
    float r = length(centered) * 1.4142;
    float theta = atan2(centered.y, centered.x); 
    return float2(r, theta);
}

float UVtoRadius(float2 uv, float radialScale)
{
    float2 centered = uv - 0.5;
    float r = length(centered) * radialScale;
return r;
}

float hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float noise(float2 uv, float scale)
{
    float2 p = uv * scale;
    float2 i = floor(p);
    float2 f = frac(p);

    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    float2 u = f * f * (3.0 - 2.0 * f); // smootherstep

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}