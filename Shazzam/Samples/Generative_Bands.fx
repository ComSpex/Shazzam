﻿/// <class>GenerateStar</class>

/// <description>An effect that swirls the input in alternating clockwise and counterclockwise bands.</description>

//-----------------------------------------------------------------------------------------
// Shader constant register mappings (scalars - float, double, Point, Color, Point3D, etc.)
//-----------------------------------------------------------------------------------------

/// <summary>The center of the star.  </summary>
/// <minValue>0,0</minValue>
/// <maxValue>2,2</maxValue>
/// <defaultValue>.5,.5</defaultValue>
float2 Center : register(C0);


/// <summary>The strength of the effect.</summary>
/// <minValue>-2</minValue>
/// <maxValue>2</maxValue>
/// <defaultValue>1.8</defaultValue>
float ColorStrength : register(C2);


/// <defaultValue>Green</defaultValue>
float4 mainColor :  register(C4); 

/// <defaultValue>Purple</defaultValue> 
float4 secondaryColor : register(C5);   

/// <minValue>-8</minValue>
/// <maxValue>8</maxValue>
/// <defaultValue>6</defaultValue>
float  ringMultiplier : register(C6);  
 
//--------------------------------------------------------------------------------------
// Sampler Inputs (Brushes, including ImplicitInput)
//--------------------------------------------------------------------------------------

sampler2D inputSource : register(S0);

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------

float4 main(float2 uv : TEXCOORD) : COLOR
{
   float scaledDistance = tanh(dot(Center.y -uv.y, Center.x -uv.x)) * ringMultiplier; 
 
    float blendFactor = tex2D (inputSource, scaledDistance) * ColorStrength; 
  
    return lerp (secondaryColor, mainColor, blendFactor); 
}
