using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GLMapPacker;

// One least-squares height problem on a texel grid:
//   minimise  sum over links of (h[head] - h[tail] - target)^2
// Grid links join a texel to its right / lower neighbour and can be switched off (island borders);
// extra links join any two texels (UV seams, where both sides are the same point on the model).
internal sealed class HeightProblem
{
	public readonly int W;

	public readonly int H;

	public readonly bool[] Valid;

	public readonly bool[] LinkX;

	public readonly bool[] LinkY;

	public readonly float[] TargetX;

	public readonly float[] TargetY;

	public readonly List<HeightLink> Extra = new List<HeightLink>();

	public HeightProblem(int w, int h)
	{
		W = w;
		H = h;
		int n = w * h;
		Valid = new bool[n];
		LinkX = new bool[n];
		LinkY = new bool[n];
		TargetX = new float[n];
		TargetY = new float[n];
	}
}

internal struct HeightLink
{
	public int A;

	public int B;

	// Wanted h[B] - h[A].
	public float Target;

	public HeightLink(int a, int b, float target)
	{
		A = a;
		B = b;
		Target = target;
	}
}

// Solves HeightProblems coarse to fine: each level starts from the upsampled coarser answer and runs
// Jacobi-preconditioned conjugate gradients, so broad shapes converge on small grids and fine levels
// only add detail. Each connected piece is finally centred on its median height.
internal static class HeightSolver
{
	// Tiny pull towards 0 that keeps the equations solvable; weak enough not to flatten broad shapes.
	private const float Anchor = 1e-8f;

	public sealed class Result
	{
		public float[] Height;

		public bool[] Valid;
	}

	public static Result Solve(int width, int height, Func<int, int, HeightProblem> build, IProgress<int> progress, int pFrom, int pTo)
	{
		List<Size2> levels = new List<Size2>();
		int lw = width;
		int lh = height;
		while (true)
		{
			levels.Insert(0, new Size2(lw, lh));
			if (Math.Max(lw, lh) <= 64)
			{
				break;
			}
			lw = Math.Max(1, (lw + 1) / 2);
			lh = Math.Max(1, (lh + 1) / 2);
		}
		float[] x = null;
		Size2 prev = default(Size2);
		HeightProblem problem = null;
		for (int li = 0; li < levels.Count; li++)
		{
			Size2 s = levels[li];
			problem = build(s.W, s.H);
			float[] start = x == null ? new float[s.W * s.H] : Upsample(x, prev.W, prev.H, s.W, s.H);
			int iterations = li == 0 ? 600 : (li == levels.Count - 1 ? 160 : 100);
			x = ConjugateGradient(problem, start, iterations);
			FillInvalid(x, problem.Valid, s.W, s.H);
			prev = s;
			progress?.Report(pFrom + (pTo - pFrom) * (li + 1) / levels.Count);
		}
		CenterComponents(problem, x);
		FillInvalid(x, problem.Valid, width, height);
		return new Result
		{
			Height = x,
			Valid = problem.Valid
		};
	}

	private struct Size2
	{
		public int W;

		public int H;

		public Size2(int w, int h)
		{
			W = w;
			H = h;
		}
	}

	private static float[] ConjugateGradient(HeightProblem p, float[] x, int iterations)
	{
		int w = p.W;
		int h = p.H;
		int n = w * h;
		float[] b = new float[n];
		float[] invDiag = new float[n];
		// Right-hand side and diagonal of the normal equations.
		for (int i = 0; i < n; i++)
		{
			if (!p.Valid[i])
			{
				continue;
			}
			int col = i % w;
			float d = Anchor;
			if (p.LinkX[i])
			{
				b[i] -= p.TargetX[i];
				b[i + 1] += p.TargetX[i];
				d += 1f;
			}
			if (col > 0 && p.LinkX[i - 1])
			{
				d += 1f;
			}
			if (p.LinkY[i])
			{
				b[i] -= p.TargetY[i];
				b[i + w] += p.TargetY[i];
				d += 1f;
			}
			if (i >= w && p.LinkY[i - w])
			{
				d += 1f;
			}
			invDiag[i] = d;
		}
		foreach (HeightLink link in p.Extra)
		{
			b[link.A] -= link.Target;
			b[link.B] += link.Target;
			invDiag[link.A] += 1f;
			invDiag[link.B] += 1f;
		}
		for (int i = 0; i < n; i++)
		{
			invDiag[i] = p.Valid[i] ? 1f / invDiag[i] : 0f;
			if (!p.Valid[i])
			{
				x[i] = 0f;
			}
		}
		float[] r = new float[n];
		float[] dir = new float[n];
		float[] ad = new float[n];
		Apply(p, x, ad);
		double rz = 0.0;
		for (int i = 0; i < n; i++)
		{
			r[i] = b[i] - ad[i];
			dir[i] = r[i] * invDiag[i];
			rz += (double)r[i] * dir[i];
		}
		double start = rz;
		for (int it = 0; it < iterations && rz > start * 1e-10 && rz > 1e-20; it++)
		{
			Apply(p, dir, ad);
			double dAd = 0.0;
			for (int i = 0; i < n; i++)
			{
				dAd += (double)dir[i] * ad[i];
			}
			if (dAd <= 0.0)
			{
				break;
			}
			float alpha = (float)(rz / dAd);
			double rzNew = 0.0;
			for (int i = 0; i < n; i++)
			{
				x[i] += alpha * dir[i];
				r[i] -= alpha * ad[i];
				rzNew += (double)r[i] * r[i] * invDiag[i];
			}
			float beta = (float)(rzNew / rz);
			rz = rzNew;
			for (int i = 0; i < n; i++)
			{
				dir[i] = r[i] * invDiag[i] + beta * dir[i];
			}
		}
		return x;
	}

	// y = A x for the normal equations (a graph Laplacian plus a tiny anchor).
	private static void Apply(HeightProblem p, float[] x, float[] y)
	{
		int w = p.W;
		Parallel.For(0, p.H, row =>
		{
			int baseIndex = row * w;
			for (int col = 0; col < w; col++)
			{
				int i = baseIndex + col;
				if (!p.Valid[i])
				{
					y[i] = 0f;
					continue;
				}
				float xi = x[i];
				float s = Anchor * xi;
				if (p.LinkX[i])
				{
					s += xi - x[i + 1];
				}
				if (col > 0 && p.LinkX[i - 1])
				{
					s += xi - x[i - 1];
				}
				if (p.LinkY[i])
				{
					s += xi - x[i + w];
				}
				if (row > 0 && p.LinkY[i - w])
				{
					s += xi - x[i - w];
				}
				y[i] = s;
			}
		});
		foreach (HeightLink link in p.Extra)
		{
			float d = x[link.A] - x[link.B];
			y[link.A] += d;
			y[link.B] -= d;
		}
	}

	private static float[] Upsample(float[] src, int sw, int sh, int dw, int dh)
	{
		float[] dst = new float[dw * dh];
		float sx = (float)sw / dw;
		float sy = (float)sh / dh;
		Parallel.For(0, dh, y =>
		{
			float fy = Math.Max(0f, Math.Min(sh - 1, (y + 0.5f) * sy - 0.5f));
			int y0 = (int)fy;
			int y1 = Math.Min(sh - 1, y0 + 1);
			float ty = fy - y0;
			for (int x = 0; x < dw; x++)
			{
				float fx = Math.Max(0f, Math.Min(sw - 1, (x + 0.5f) * sx - 0.5f));
				int x0 = (int)fx;
				int x1 = Math.Min(sw - 1, x0 + 1);
				float tx = fx - x0;
				float a = src[y0 * sw + x0] + (src[y0 * sw + x1] - src[y0 * sw + x0]) * tx;
				float b = src[y1 * sw + x0] + (src[y1 * sw + x1] - src[y1 * sw + x0]) * tx;
				dst[y * dw + x] = a + (b - a) * ty;
			}
		});
		return dst;
	}

	// Gives every texel outside the islands the value of its nearest island texel (breadth-first),
	// which also pads the output so mipmaps don't pull in grey from the gutter.
	public static void FillInvalid(float[] data, bool[] valid, int w, int h)
	{
		int n = w * h;
		bool[] done = new bool[n];
		int[] queue = new int[n];
		int head = 0;
		int tail = 0;
		for (int i = 0; i < n; i++)
		{
			if (valid[i])
			{
				done[i] = true;
				queue[tail++] = i;
			}
		}
		if (tail == 0)
		{
			return;
		}
		while (head < tail)
		{
			int i = queue[head++];
			int x = i % w;
			int y = i / w;
			if (x > 0)
			{
				Visit(i - 1, i);
			}
			if (x < w - 1)
			{
				Visit(i + 1, i);
			}
			if (y > 0)
			{
				Visit(i - w, i);
			}
			if (y < h - 1)
			{
				Visit(i + w, i);
			}
		}
		void Visit(int j, int from)
		{
			if (!done[j])
			{
				done[j] = true;
				data[j] = data[from];
				queue[tail++] = j;
			}
		}
	}

	// Pieces that no link connects have no shared reference height; centre each one on its median.
	private static void CenterComponents(HeightProblem p, float[] x)
	{
		int w = p.W;
		int n = w * p.H;
		int[] parent = new int[n];
		for (int i = 0; i < n; i++)
		{
			parent[i] = i;
		}
		for (int i = 0; i < n; i++)
		{
			if (p.LinkX[i])
			{
				Union(parent, i, i + 1);
			}
			if (p.LinkY[i])
			{
				Union(parent, i, i + w);
			}
		}
		foreach (HeightLink link in p.Extra)
		{
			Union(parent, link.A, link.B);
		}
		Dictionary<int, List<float>> groups = new Dictionary<int, List<float>>();
		for (int i = 0; i < n; i++)
		{
			if (p.Valid[i])
			{
				int root = Find(parent, i);
				if (!groups.TryGetValue(root, out var list))
				{
					list = new List<float>();
					groups[root] = list;
				}
				list.Add(x[i]);
			}
		}
		Dictionary<int, float> median = new Dictionary<int, float>();
		foreach (KeyValuePair<int, List<float>> g in groups)
		{
			g.Value.Sort();
			median[g.Key] = g.Value[g.Value.Count / 2];
		}
		for (int i = 0; i < n; i++)
		{
			if (p.Valid[i])
			{
				x[i] -= median[Find(parent, i)];
			}
		}
	}

	internal static int Find(int[] parent, int i)
	{
		while (parent[i] != i)
		{
			parent[i] = parent[parent[i]];
			i = parent[i];
		}
		return i;
	}

	internal static void Union(int[] parent, int a, int b)
	{
		int ra = Find(parent, a);
		int rb = Find(parent, b);
		if (ra != rb)
		{
			parent[ra] = rb;
		}
	}
}
