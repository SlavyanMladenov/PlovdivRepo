using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Float packing class handles 
/// </summary>
/// <notes>
/// step,exp2,abs,floor,mod,log2 methods are included here to mimic shader
/// functionality.  Some methods were copied from Nvidia's Cg reference
/// implementations, others are just wrappers around C# functions.  
/// 
/// In practice I don't think it matters.
/// 
/// Code taken directly from: http://stackoverflow.com/questions/7059962/how-do-i-convert-a-vec4-rgba-value-to-a-float (hrehfeld's answer)
/// </notes> 
/// 
public static class FloatPacking
{
	public static float step(float edge, float x)
	{
		return (x < edge) ? 0.0f : 1.0f;
	}
	public static float exp2(float x)
	{
		return (float)Math.Pow(2, x);
	}
	public static float abs(float x)
	{
		return (float)Math.Abs(x);
	}
	public static int floor(float x)
	{
		return (int)Math.Floor(x);
	}
	public static float mod(float x, float y)
	{
		return x - y * floor(x / y);
	}
	public static float log2(float x)
	{
		return (float)Math.Log(x, 2);
	}
	//unpack a 32bit float from 4 8bit, [0;1] clamped floats
	public static float FromFloat4(Vector4 _packed)
	{
		Vector4 rgba = 255.0f * _packed;
		float sign = step(-128.0f, -rgba.y) * 2.0f - 1.0f;
		float exponent = rgba.x - 127.0f;
		if (abs(exponent + 127.0f) < 0.001f)
			return 0.0f;
		float mantissa = mod(rgba.y, 128.0f) * 65536.0f + rgba.z * 256.0f + rgba.w + (0x800000);
		return sign * exp2(exponent - 23.0f) * mantissa;
	}

	//pack a 32bit float into 4 8bit, [0;1] clamped floats
	public static Vector4 ToFloat4(float f)
	{
		float F = abs(f);
		if (F == 0.0)
		{
			return new Vector4(0, 0, 0, 0);
		}
		float Sign = step(0.0f, -f);
		float Exponent = floor(log2(F));

		float Mantissa = F / exp2(Exponent);
		//std::cout << "  sign: " << Sign << ", exponent: " << Exponent << ", mantissa: " << Mantissa << std::endl;
		//denormalized values if all exponent bits are zero
		if (Mantissa < 1.0f)
			Exponent -= 1;

		Exponent += 127;

		Vector4 rgba = new Vector4(0, 0, 0, 0);
		rgba.x = Exponent;
		rgba.y = 128.0f * Sign + mod(floor(Mantissa * 128.0f), 128.0f);
		rgba.z = floor(mod(floor(Mantissa * exp2(23.0f - 8.0f)), exp2(8.0f)));
		rgba.w = floor(exp2(23.0f) * mod(Mantissa, exp2(-15.0f)));

		return (1 / 255.0f) * rgba;
	}

	public static Color ToColor(float f)
	{
		Vector4 tmp = ToFloat4(f);
		return new Color(tmp.x, tmp.y, tmp.z, tmp.w);
	}
}
