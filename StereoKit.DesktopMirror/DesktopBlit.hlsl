#include "stereokit.hlsli"

//--source = white
//--cursor = white
//--cursor_pos = 0,0

float2 cursor_pos;
float2 cursor_size;

Texture2D    source   : register(t0);
SamplerState source_s : register(s0);

Texture2D    cursor   : register(t1);
SamplerState cursor_s : register(s1);

cbuffer TransformBuffer : register(b3) {
	float sk_width;
	float sk_height;
	float sk_pixel_width;
	float sk_pixel_height;
};

struct vsIn {
	float4 pos  : SV_Position;
	float3 norm : NORMAL0;
	float2 uv   : TEXCOORD0;
	float4 col  : COLOR0;
};
struct psIn {
	float4 pos : SV_POSITION;
	float2 uv  : TEXCOORD0;
	float2 cuv : TEXCOORD1;
	uint view_id : SV_RenderTargetArrayIndex;
};

psIn vs(vsIn input, uint id : SV_InstanceID) {
	psIn o;
	o.view_id = id % sk_view_count;
	o.pos = input.pos;
	o.uv  = input.uv;
	o.cuv = (input.uv-cursor_pos) / cursor_size;
	return o;
}

float4 ps(psIn input) : SV_TARGET {
	half4  col        = source.Sample(source_s, input.uv);
	float4 cursor_col = cursor.Sample(cursor_s, input.cuv);
	
	float4 desktop_col = pow(abs(col), 2.2);
	float2 bounds      = input.cuv <= 1 && input.cuv >= 0;
	return lerp(desktop_col, cursor_col, cursor_col.a * min(bounds.x, bounds.y));
}