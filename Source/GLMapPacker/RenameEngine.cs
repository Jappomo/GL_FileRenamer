using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GLMapPacker;

public static class RenameEngine
{
	public class Rule
	{
		public string Search = "";

		public string Replace = "";

		public string Prefix = "";

		public string Suffix = "";

		public int Digits = 0;

		public bool KeepExtension = true;

		public bool IncludeSubfolders = false;

		public bool SortByName = true;
	}

	public class Item
	{
		public string Directory;

		public string OldName;

		public string NewName;

		public bool Changed;

		public string Warning;

		public string OldPath => Path.Combine(Directory, OldName);

		public string NewPath => Path.Combine(Directory, NewName);
	}

	public static List<Item> Plan(string directory, Rule rule)
	{
		List<Item> list = new List<Item>();
		if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
		{
			return list;
		}
		IEnumerable<string> enumerable = EnumerateFiles(directory, rule.IncludeSubfolders);
		if (rule.SortByName)
		{
			enumerable = enumerable.OrderBy((string f) => f, StringComparer.OrdinalIgnoreCase);
		}
		int num = 1;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string item4 in enumerable)
		{
			string directoryName = Path.GetDirectoryName(item4);
			string fileName = Path.GetFileName(item4);
			string text;
			string text2;
			if (rule.KeepExtension)
			{
				text = Path.GetExtension(fileName);
				text2 = fileName.Substring(0, fileName.Length - text.Length);
			}
			else
			{
				text2 = fileName;
				text = "";
			}
			string text3 = ((!string.IsNullOrEmpty(rule.Search)) ? text2.Replace(rule.Search, rule.Replace) : text2);
			text3 = rule.Prefix + text3 + rule.Suffix;
			if (rule.Digits > 0)
			{
				text3 = text3 + "_" + num.ToString(new string('0', rule.Digits));
				num++;
			}
			string text4 = text3 + text;
			Item item = new Item();
			item.Directory = directoryName;
			item.OldName = fileName;
			item.NewName = text4;
			item.Changed = !string.Equals(fileName, text4, StringComparison.Ordinal);
			Item item2 = item;
			string item3 = Path.Combine(directoryName, text4).ToLowerInvariant();
			if (!hashSet.Add(item3))
			{
				item2.Warning = "duplicate target name";
			}
			else if (item2.Changed && File.Exists(item2.NewPath))
			{
				item2.Warning = "would overwrite an existing file";
			}
			list.Add(item2);
		}
		return list;
	}

	public static int Apply(List<Item> items, bool force, out List<string> errors)
	{
		errors = new List<string>();
		List<Item> list = items.Where((Item i) => i.Changed && (force || string.IsNullOrEmpty(i.Warning))).ToList();
		if (list.Count == 0)
		{
			return 0;
		}
		List<KeyValuePair<string, Item>> list2 = new List<KeyValuePair<string, Item>>();
		foreach (Item item in list)
		{
			string text = Path.Combine(item.Directory, "__gmp_tmp_" + Guid.NewGuid().ToString("N") + "__");
			try
			{
				File.Move(item.OldPath, text);
				list2.Add(new KeyValuePair<string, Item>(text, item));
			}
			catch (Exception ex)
			{
				errors.Add(item.OldName + ": " + ex.Message);
			}
		}
		int num = 0;
		foreach (KeyValuePair<string, Item> item2 in list2)
		{
			try
			{
				if (File.Exists(item2.Value.NewPath) && force)
				{
					File.Delete(item2.Value.NewPath);
				}
				File.Move(item2.Key, item2.Value.NewPath);
				num++;
			}
			catch (Exception ex2)
			{
				errors.Add(item2.Value.NewName + ": " + ex2.Message);
				try
				{
					File.Move(item2.Key, item2.Value.OldPath);
				}
				catch
				{
				}
			}
		}
		return num;
	}

	private static IEnumerable<string> EnumerateFiles(string directory, bool recurse)
	{
		return Directory.EnumerateFiles(directory, "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
	}
}
