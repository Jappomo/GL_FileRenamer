using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace GLMapPacker;

public static class MaterialPreset
{
	public const string ShaderOpenPBR = "openPBRSurface";

	public const string ShaderUSD = "usdPreviewSurface";

	private static readonly Dictionary<MapType, string[]> Suffixes = new Dictionary<MapType, string[]>
	{
		{
			MapType.Albedo,
			new string[4] { "_albedo", "_basecolor", "_diffuse", "_color" }
		},
		{
			MapType.Roughness,
			new string[2] { "_roughness", "_rough" }
		},
		{
			MapType.Metallic,
			new string[2] { "_metallic", "_metal" }
		},
		{
			MapType.Normal,
			new string[2] { "_normal", "_nrm" }
		}
	};

	public static string Key(MapType t)
	{
		return t switch
		{
			MapType.Albedo => "albedo", 
			MapType.Roughness => "roughness", 
			MapType.Metallic => "metallic", 
			MapType.Normal => "normal", 
			MapType.AO => "ao", 
			MapType.Opacity => "opacity", 
			MapType.Emission => "emission", 
			_ => t.ToString().ToLowerInvariant(), 
		};
	}

	public static string Label(MapType t)
	{
		if (t == MapType.AO)
		{
			return "Ambient Occlusion";
		}
		return t.ToString();
	}

	public static bool IsCore(MapType t)
	{
		return t == MapType.Albedo || t == MapType.Roughness || t == MapType.Metallic || t == MapType.Normal;
	}

	public static string DeriveMaterialName(IDictionary<MapType, string> maps)
	{
		MapType chosenType;
		string text = PickSourceForName(maps, out chosenType);
		if (string.IsNullOrEmpty(text))
		{
			return "M_material";
		}
		string stem = FileStemCrossPlatform(text);
		string name = StripKnownSuffix(stem, chosenType);
		name = StripTexturePrefix(name);
		if (string.IsNullOrEmpty(name))
		{
			name = "material";
		}
		return "M_" + name;
	}

	private static string StripTexturePrefix(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return name;
		}
		bool flag = true;
		while (flag)
		{
			flag = false;
			string[] array = new string[2] { "t_", "m_" };
			foreach (string text in array)
			{
				if (name.Length >= text.Length && name.Substring(0, text.Length).Equals(text, StringComparison.OrdinalIgnoreCase))
				{
					name = name.Substring(text.Length);
					flag = true;
				}
			}
		}
		return name.Trim('_');
	}

	private static string FileStemCrossPlatform(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return "";
		}
		int num = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
		string text = ((num < 0) ? path : path.Substring(num + 1));
		int num2 = text.LastIndexOf('.');
		if (num2 > 0)
		{
			text = text.Substring(0, num2);
		}
		return text;
	}

	private static string PickSourceForName(IDictionary<MapType, string> maps, out MapType chosenType)
	{
		if (maps.TryGetValue(MapType.Albedo, out var value) && !string.IsNullOrEmpty(value))
		{
			chosenType = MapType.Albedo;
			return value;
		}
		MapType[] array = (MapType[])Enum.GetValues(typeof(MapType));
		foreach (MapType mapType in array)
		{
			if (maps.TryGetValue(mapType, out var value2) && !string.IsNullOrEmpty(value2))
			{
				chosenType = mapType;
				return value2;
			}
		}
		chosenType = MapType.Albedo;
		return null;
	}

	private static string StripKnownSuffix(string stem, MapType type)
	{
		if (Suffixes.TryGetValue(type, out var value))
		{
			string[] array = value;
			foreach (string text in array)
			{
				if (stem.EndsWith(text, StringComparison.OrdinalIgnoreCase))
				{
					return stem.Substring(0, stem.Length - text.Length);
				}
			}
		}
		return stem;
	}

	public static string BuildJson(string message, string materialName, string shaderType, IDictionary<MapType, string> maps)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("{\n");
		stringBuilder.Append("  \"message\": ").Append(JsonString(message)).Append(",\n");
		stringBuilder.Append("  \"materialName\": ").Append(JsonString(materialName)).Append(",\n");
		stringBuilder.Append("  \"shaderType\": ").Append(JsonString(shaderType)).Append(",\n");
		stringBuilder.Append("  \"maps\": {");
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		MapType[] array = (MapType[])Enum.GetValues(typeof(MapType));
		foreach (MapType mapType in array)
		{
			if (maps.TryGetValue(mapType, out var value) && !string.IsNullOrEmpty(value))
			{
				list.Add(new KeyValuePair<string, string>(Key(mapType), ForwardSlashes(value)));
			}
		}
		if (list.Count == 0)
		{
			stringBuilder.Append("}\n");
		}
		else
		{
			stringBuilder.Append('\n');
			for (int j = 0; j < list.Count; j++)
			{
				stringBuilder.Append("    ").Append(JsonString(list[j].Key)).Append(": ")
					.Append(JsonString(list[j].Value));
				stringBuilder.Append((j >= list.Count - 1) ? "\n" : ",\n");
			}
			stringBuilder.Append("  }\n");
		}
		stringBuilder.Append("}\n");
		return stringBuilder.ToString();
	}

	public static string ForwardSlashes(string path)
	{
		return path?.Replace('\\', '/');
	}

	private static string JsonString(string s)
	{
		if (s == null)
		{
			return "null";
		}
		StringBuilder stringBuilder = new StringBuilder(s.Length + 2);
		stringBuilder.Append('"');
		foreach (char c in s)
		{
			switch (c)
			{
			case '\b':
				stringBuilder.Append("\\b");
				continue;
			case '\f':
				stringBuilder.Append("\\f");
				continue;
			case '\n':
				stringBuilder.Append("\\n");
				continue;
			case '\r':
				stringBuilder.Append("\\r");
				continue;
			case '\t':
				stringBuilder.Append("\\t");
				continue;
			}
			if (c != '"')
			{
				if (c == '\\')
				{
					stringBuilder.Append("\\\\");
				}
				else if (c < ' ')
				{
					StringBuilder stringBuilder2 = stringBuilder.Append("\\u");
					int num = c;
					stringBuilder2.Append(num.ToString("x4", CultureInfo.InvariantCulture));
				}
				else
				{
					stringBuilder.Append(c);
				}
			}
			else
			{
				stringBuilder.Append("\\\"");
			}
		}
		stringBuilder.Append('"');
		return stringBuilder.ToString();
	}

	public static string TargetPath()
	{
		string path = Path.Combine(Path.GetTempPath(), "maya_bridge");
		return Path.Combine(path, "material_preset.json");
	}

	public static string WriteAtomic(string json)
	{
		string text = TargetPath();
		string directoryName = Path.GetDirectoryName(text);
		Directory.CreateDirectory(directoryName);
		string text2 = text + ".tmp";
		File.WriteAllText(text2, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		if (File.Exists(text))
		{
			File.Replace(text2, text, null);
		}
		else
		{
			File.Move(text2, text);
		}
		return text;
	}
}
