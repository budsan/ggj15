﻿Shader "Custom/WallBeat" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_BaseColor ("Base Color", Color) = (1, 1, 1, 1)
		_OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
		_OutlineWidth ("Outline width", Range(0, 0.5)) = 0.1
		_BeatTime ("BeatTime", float) = 0.5
		_TransformScale ("Transform Scale", Vector) = (1, 1, 1)
	}
	
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
		#pragma exclude_renderers gles
		#include "UnityCG.cginc"
		#include "Assets/Shaders/CustomLight2.cginc"
		#pragma surface surf CustomLight2 vertex:vert

		uniform sampler2D _MainTex;
		uniform float4 _BaseColor;
		uniform float _OutlineWidth;
		uniform float4 _OutlineColor;
		uniform float3 _TransformScale;
		uniform float _BeatTime;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			float3 localNormal;
		};

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
			o.localNormal = abs(v.normal);
		}
		
		void surf (Input IN, inout SurfaceOutput o) {
			float4 c = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb * _BaseColor.rgb;
			float3x2 scaleMatrix = { 
				_TransformScale.z, _TransformScale.y, _TransformScale.x, _TransformScale.z, _TransformScale.x, _TransformScale.y
			};
			float2 scaleChachi = mul(IN.localNormal, scaleMatrix);
			float2 uv = IN.uv_MainTex;
			float2 ow = float2(_OutlineWidth,_OutlineWidth)/scaleChachi;
			if ( uv.x < ow.x || uv.y < ow.y || uv.x > (1.0-ow.x) || uv.y > (1.0-ow.y)) {
				o.Albedo = _OutlineColor.rgb;
				o.Gloss = 1;
			}
			else {
				o.Gloss = 0;
				float modded = torramod(_BeatTime,1.0f);
				float percent; //percent between beat and no beat
				//float s = sin(modded*3.1415+0.5);
				//percent = 1-abs(s);
				//if(modded < 0.5) percent = percent*percent*percent*percent;
				//else percent = sqrt(percent);
				modded += sin((_BeatTime+0.5)*10.0)*0.1 + sin((_BeatTime+1.0)*20.0)*0.1;
				percent = 1-modded;
				float distFromFive = abs(0.5-uv.x);
				distFromFive = distFromFive-torramod(distFromFive,0.05f);
				percent *= 1-(distFromFive/0.5);
				if(uv.y <= percent && (torramod(uv.x-0.0025, .05) < .04)) {
					o.Gloss = 0.5f;
					o.Albedo = _OutlineColor.rgb;
					o.Specular = uv.y/percent;
				}
			}
			o.Alpha = c.a;
			
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
