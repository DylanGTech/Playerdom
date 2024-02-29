using System;

namespace Playerdom.Shared;

public sealed class CubicNoise
{
	private static readonly int RND_A = 134775813;
	private static readonly int RND_B = 1103515245;

	private int seed;
	private int octave;
	private int periodx = int.MaxValue;
	private int periody = int.MaxValue;

	public CubicNoise(int seed, int octave, int periodx, int periody)
	{
		this.seed = seed;
		this.octave = octave;
		this.periodx = periodx;
		this.periody = periody;
	}

	public CubicNoise(int seed, int octave)
	{
		this.seed = seed;
		this.octave = octave;
	}

	public float Sample(double x)
	{
		int xi = (int)Math.Floor(x / octave);
		double lerp = x / octave - xi;

		return Interpolate(
				Randomize(seed, Tile(xi - 1, periodx), 0),
				Randomize(seed, Tile(xi, periodx), 0),
				Randomize(seed, Tile(xi + 1, periodx), 0),
				Randomize(seed, Tile(xi + 2, periodx), 0),
				lerp) * 0.5f + 0.25f;
	}

	public float Sample(double x, double y)
	{
		int xi = (int)Math.Floor(x / octave);
		double lerpx = x / octave - xi;
		int yi = (int)Math.Floor(y / octave);
		double lerpy = y / octave - yi;

		float[] xSamples = new float[4];

		for (int i = 0; i < 4; ++i)
			xSamples[i] = Interpolate(
					Randomize(seed, Tile(xi - 1, periodx), Tile(yi - 1 + i, periody)),
					Randomize(seed, Tile(xi, periodx), Tile(yi - 1 + i, periody)),
					Randomize(seed, Tile(xi + 1, periodx), Tile(yi - 1 + i, periody)),
					Randomize(seed, Tile(xi + 2, periodx), Tile(yi - 1 + i, periody)),
					lerpx);

		return Interpolate(xSamples[0], xSamples[1], xSamples[2], xSamples[3], lerpy) * 0.666666f + 0.166666f;
	}

	private static float Randomize(int seed, int x, int y)
	{
		return (float)((((x ^ y) * RND_A) ^ (seed + x)) * (((RND_B * x) << 16) ^ (RND_B * y) - RND_A)) / int.MaxValue;
	}

	private static int Tile(int coordinate, int period)
	{
		return coordinate % period;
	}

	private static float Interpolate(float a, float b, float c, float d, double x)
	{
		double p = (d - c) - (a - b);

		return (float)(x * (x * (x * p + ((a - b) - p)) + (c - a)) + b);
	}
}