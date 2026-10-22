using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace GLMapPacker;

// Low-poly mesh with UVs, read from OBJ, for mesh-aware normal-to-height integration.
// It answers two questions the texture alone can't: which texels sit on which UV island (and how much
// surface one texel covers there), and which island edges are the same edge on the model (UV seams).
public sealed class UvMesh
{
	public sealed class Seam
	{
		public int TriA;

		public int TriB;

		// The shared edge's two ends, in the same order on both sides.
		public float UA0;

		public float VA0;

		public float UA1;

		public float VA1;

		public float UB0;

		public float VB0;

		public float UB1;

		public float VB1;
	}

	public sealed class Raster
	{
		public int W;

		public int H;

		// Triangle covering each texel, or -1.
		public int[] Tri;

		// Texels claimed by two different islands (overlapping or stacked UVs).
		public int OverlapTexels;
	}

	private float[] px;

	private float[] py;

	private float[] pz;

	private float[] tu;

	private float[] tv;

	private int[] triP;

	private int[] triT;

	public string Name { get; private set; }

	public int TriangleCount { get; private set; }

	public int IslandCount { get; private set; }

	public int[] TriIsland { get; private set; }

	// World length of one texture width along U, and one texture height along V, per triangle.
	public float[] TriLenU { get; private set; }

	public float[] TriLenV { get; private set; }

	public List<Seam> Seams { get; private set; }

	public static UvMesh LoadObj(string path)
	{
		List<float> pos = new List<float>();
		List<float> uv = new List<float>();
		List<int> faceP = new List<int>();
		List<int> faceT = new List<int>();
		CultureInfo inv = CultureInfo.InvariantCulture;
		int faceWithoutUv = 0;
		foreach (string rawLine in File.ReadLines(path))
		{
			string line = rawLine.Trim();
			if (line.Length < 2)
			{
				continue;
			}
			string[] parts = line.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			switch (parts[0])
			{
			case "v":
				pos.Add(float.Parse(parts[1], inv));
				pos.Add(float.Parse(parts[2], inv));
				pos.Add(float.Parse(parts[3], inv));
				break;
			case "vt":
				uv.Add(float.Parse(parts[1], inv));
				uv.Add(parts.Length > 2 ? float.Parse(parts[2], inv) : 0f);
				break;
			case "f":
			{
				int count = parts.Length - 1;
				int[] vp = new int[count];
				int[] vt = new int[count];
				bool hasUv = true;
				for (int i = 0; i < count; i++)
				{
					string[] idx = parts[i + 1].Split('/');
					vp[i] = ResolveIndex(idx[0], pos.Count / 3);
					if (idx.Length > 1 && idx[1].Length > 0)
					{
						vt[i] = ResolveIndex(idx[1], uv.Count / 2);
					}
					else
					{
						hasUv = false;
					}
				}
				if (!hasUv)
				{
					faceWithoutUv++;
					break;
				}
				// Fan-triangulate polygons.
				for (int i = 1; i + 1 < count; i++)
				{
					faceP.Add(vp[0]);
					faceP.Add(vp[i]);
					faceP.Add(vp[i + 1]);
					faceT.Add(vt[0]);
					faceT.Add(vt[i]);
					faceT.Add(vt[i + 1]);
				}
				break;
			}
			}
		}
		if (faceP.Count == 0)
		{
			throw new InvalidDataException(faceWithoutUv > 0 ? "The model has no UVs. Export the OBJ with UVs (texture coordinates)." : "No faces found in the OBJ file.");
		}
		UvMesh mesh = new UvMesh
		{
			Name = Path.GetFileName(path),
			px = new float[pos.Count / 3],
			py = new float[pos.Count / 3],
			pz = new float[pos.Count / 3],
			tu = new float[uv.Count / 2],
			tv = new float[uv.Count / 2],
			triP = faceP.ToArray(),
			triT = faceT.ToArray(),
			TriangleCount = faceP.Count / 3
		};
		for (int i = 0; i < mesh.px.Length; i++)
		{
			mesh.px[i] = pos[i * 3];
			mesh.py[i] = pos[i * 3 + 1];
			mesh.pz[i] = pos[i * 3 + 2];
		}
		for (int i = 0; i < mesh.tu.Length; i++)
		{
			mesh.tu[i] = uv[i * 2];
			mesh.tv[i] = uv[i * 2 + 1];
		}
		mesh.Analyse();
		return mesh;
	}

	private static int ResolveIndex(string s, int count)
	{
		int i = int.Parse(s, CultureInfo.InvariantCulture);
		return i < 0 ? count + i : i - 1;
	}

	private void Analyse()
	{
		int tris = TriangleCount;
		TriLenU = new float[tris];
		TriLenV = new float[tris];
		for (int t = 0; t < tris; t++)
		{
			// dP/du and dP/dv from the triangle's position and UV edges.
			int a = triP[t * 3];
			int b = triP[t * 3 + 1];
			int c = triP[t * 3 + 2];
			int ta = triT[t * 3];
			int tb = triT[t * 3 + 1];
			int tc = triT[t * 3 + 2];
			double e1x = px[b] - px[a];
			double e1y = py[b] - py[a];
			double e1z = pz[b] - pz[a];
			double e2x = px[c] - px[a];
			double e2y = py[c] - py[a];
			double e2z = pz[c] - pz[a];
			double du1 = tu[tb] - tu[ta];
			double dv1 = tv[tb] - tv[ta];
			double du2 = tu[tc] - tu[ta];
			double dv2 = tv[tc] - tv[ta];
			double det = du1 * dv2 - du2 * dv1;
			if (Math.Abs(det) < 1e-12)
			{
				continue;
			}
			double r = 1.0 / det;
			double ux = (e1x * dv2 - e2x * dv1) * r;
			double uy = (e1y * dv2 - e2y * dv1) * r;
			double uz = (e1z * dv2 - e2z * dv1) * r;
			double vx = (e2x * du1 - e1x * du2) * r;
			double vy = (e2y * du1 - e1y * du2) * r;
			double vz = (e2z * du1 - e1z * du2) * r;
			TriLenU[t] = (float)Math.Sqrt(ux * ux + uy * uy + uz * uz);
			TriLenV[t] = (float)Math.Sqrt(vx * vx + vy * vy + vz * vz);
		}
		// Islands: triangles sharing an edge with the same UVs on both ends.
		int[] parent = new int[tris];
		for (int i = 0; i < tris; i++)
		{
			parent[i] = i;
		}
		Dictionary<string, int> uvEdges = new Dictionary<string, int>();
		Dictionary<string, List<int>> posEdges = new Dictionary<string, List<int>>();
		for (int t = 0; t < tris; t++)
		{
			for (int e = 0; e < 3; e++)
			{
				int i0 = t * 3 + e;
				int i1 = t * 3 + (e + 1) % 3;
				string uvKey = EdgeKey(UvKey(triT[i0]), UvKey(triT[i1]));
				if (uvEdges.TryGetValue(uvKey, out var other))
				{
					HeightSolver.Union(parent, t, other);
				}
				else
				{
					uvEdges[uvKey] = t;
				}
				string pKey = EdgeKey(PosKey(triP[i0]), PosKey(triP[i1]));
				if (!posEdges.TryGetValue(pKey, out var list))
				{
					list = new List<int>(2);
					posEdges[pKey] = list;
				}
				list.Add(i0);
			}
		}
		Dictionary<int, int> islandIds = new Dictionary<int, int>();
		TriIsland = new int[tris];
		for (int t = 0; t < tris; t++)
		{
			int root = HeightSolver.Find(parent, t);
			if (!islandIds.TryGetValue(root, out var id))
			{
				id = islandIds.Count;
				islandIds[root] = id;
			}
			TriIsland[t] = id;
		}
		IslandCount = islandIds.Count;
		// Seams: an edge on the model whose two triangles have different UVs along it.
		Seams = new List<Seam>();
		foreach (List<int> users in posEdges.Values)
		{
			if (users.Count != 2)
			{
				continue;
			}
			int a0 = users[0];
			int a1 = a0 / 3 * 3 + (a0 % 3 + 1) % 3;
			int b0 = users[1];
			int b1 = b0 / 3 * 3 + (b0 % 3 + 1) % 3;
			// Line the two sides up by position (the second triangle usually runs the edge backwards).
			if (PosKey(triP[a0]) != PosKey(triP[b0]))
			{
				int t = b0;
				b0 = b1;
				b1 = t;
			}
			if (UvKey(triT[a0]) == UvKey(triT[b0]) && UvKey(triT[a1]) == UvKey(triT[b1]))
			{
				continue;
			}
			Seams.Add(new Seam
			{
				TriA = a0 / 3,
				TriB = b0 / 3,
				UA0 = tu[triT[a0]],
				VA0 = tv[triT[a0]],
				UA1 = tu[triT[a1]],
				VA1 = tv[triT[a1]],
				UB0 = tu[triT[b0]],
				VB0 = tv[triT[b0]],
				UB1 = tu[triT[b1]],
				VB1 = tv[triT[b1]]
			});
		}
	}

	private string PosKey(int i)
	{
		return Q(px[i]) + "," + Q(py[i]) + "," + Q(pz[i]);
	}

	private string UvKey(int i)
	{
		return Q(tu[i]) + "," + Q(tv[i]);
	}

	private static string Q(float v)
	{
		return Math.Round(v * 100000.0).ToString(CultureInfo.InvariantCulture);
	}

	private static string EdgeKey(string a, string b)
	{
		return string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;
	}

	// Which triangle covers each texel centre, at the given texture size. V points up in UV space and
	// rows go down in the image.
	public Raster Rasterize(int w, int h)
	{
		Raster r = new Raster
		{
			W = w,
			H = h,
			Tri = new int[w * h]
		};
		for (int i = 0; i < r.Tri.Length; i++)
		{
			r.Tri[i] = -1;
		}
		for (int t = 0; t < TriangleCount; t++)
		{
			if (TriLenU[t] <= 0f)
			{
				continue;
			}
			float x0 = tu[triT[t * 3]] * w - 0.5f;
			float y0 = (1f - tv[triT[t * 3]]) * h - 0.5f;
			float x1 = tu[triT[t * 3 + 1]] * w - 0.5f;
			float y1 = (1f - tv[triT[t * 3 + 1]]) * h - 0.5f;
			float x2 = tu[triT[t * 3 + 2]] * w - 0.5f;
			float y2 = (1f - tv[triT[t * 3 + 2]]) * h - 0.5f;
			int minX = Math.Max(0, (int)Math.Floor(Math.Min(x0, Math.Min(x1, x2))));
			int maxX = Math.Min(w - 1, (int)Math.Ceiling(Math.Max(x0, Math.Max(x1, x2))));
			int minY = Math.Max(0, (int)Math.Floor(Math.Min(y0, Math.Min(y1, y2))));
			int maxY = Math.Min(h - 1, (int)Math.Ceiling(Math.Max(y0, Math.Max(y1, y2))));
			float area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
			if (Math.Abs(area) < 1e-9f)
			{
				continue;
			}
			float inv = 1f / area;
			const float eps = -1e-4f;
			for (int y = minY; y <= maxY; y++)
			{
				for (int x = minX; x <= maxX; x++)
				{
					float w0 = ((x1 - x) * (y2 - y) - (x2 - x) * (y1 - y)) * inv;
					float w1 = ((x2 - x) * (y0 - y) - (x0 - x) * (y2 - y)) * inv;
					float w2 = 1f - w0 - w1;
					if (w0 < eps || w1 < eps || w2 < eps)
					{
						continue;
					}
					int i = y * w + x;
					if (r.Tri[i] < 0)
					{
						r.Tri[i] = t;
					}
					else if (TriIsland[r.Tri[i]] != TriIsland[t])
					{
						r.OverlapTexels++;
					}
				}
			}
		}
		return r;
	}

	// Nearest texel within a couple of pixels of (u, v) that lies on the given island, or -1.
	public int FindTexel(Raster r, float u, float v, int island)
	{
		float fx = u * r.W - 0.5f;
		float fy = (1f - v) * r.H - 0.5f;
		int cx = (int)Math.Round(fx);
		int cy = (int)Math.Round(fy);
		int best = -1;
		float bestD = float.MaxValue;
		for (int dy = -2; dy <= 2; dy++)
		{
			for (int dx = -2; dx <= 2; dx++)
			{
				int x = cx + dx;
				int y = cy + dy;
				if (x < 0 || y < 0 || x >= r.W || y >= r.H)
				{
					continue;
				}
				int t = r.Tri[y * r.W + x];
				if (t < 0 || TriIsland[t] != island)
				{
					continue;
				}
				float d = (x - fx) * (x - fx) + (y - fy) * (y - fy);
				if (d < bestD)
				{
					bestD = d;
					best = y * r.W + x;
				}
			}
		}
		return best;
	}
}
