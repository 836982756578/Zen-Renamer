// Zen Renamer - Akıllı Toplu Dosya Yeniden Adlandırıcı
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace ZenRenamer
{
    public enum CaseMode
    {
        NoChange,
        LowerCase,
        UpperCase,
        TitleCase
    }

    public enum NumberPosition
    {
        Prefix,
        Suffix,
        ReplaceName
    }

    public class RenameRules
    {
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string FindText { get; set; }
        public string ReplaceText { get; set; }
        public bool MatchCase { get; set; }

        public bool EnableNumbering { get; set; }
        public int StartNumber { get; set; }
        public int NumberDigits { get; set; } // 1, 2 (01), 3 (001)
        public NumberPosition NumPosition { get; set; }
        public string NumberSeparator { get; set; }

        public CaseMode CaseOption { get; set; }

        public bool EnableDateStamp { get; set; }
        public string DateFormat { get; set; } // "yyyy-MM-dd", "yyyyMMdd", etc.
        public bool DateAtPrefix { get; set; } // true = prefix, false = suffix

        public bool ReplaceSpaces { get; set; }
        public string SpaceReplacement { get; set; } // "_", "-", ""

        public RenameRules()
        {
            Prefix = "";
            Suffix = "";
            FindText = "";
            ReplaceText = "";
            MatchCase = false;
            EnableNumbering = false;
            StartNumber = 1;
            NumberDigits = 2;
            NumPosition = NumberPosition.Suffix;
            NumberSeparator = "_";
            CaseOption = CaseMode.NoChange;
            EnableDateStamp = false;
            DateFormat = "yyyy-MM-dd";
            DateAtPrefix = true;
            ReplaceSpaces = false;
            SpaceReplacement = "_";
        }
    }

    public class FileRenameItem
    {
        public string FullPath { get; set; }
        public string Directory { get; set; }
        public string OriginalFileName { get; set; }
        public string Extension { get; set; }
        public string NewFileName { get; set; }
        public string Status { get; set; }
        public bool HasConflict { get; set; }
        public DateTime CreatedDate { get; set; }

        public FileRenameItem(string path)
        {
            FullPath = path;
            Directory = Path.GetDirectoryName(path);
            OriginalFileName = Path.GetFileNameWithoutExtension(path);
            Extension = Path.GetExtension(path);
            NewFileName = OriginalFileName;
            Status = "Bekliyor";
            try
            {
                CreatedDate = File.GetCreationTime(path);
            }
            catch
            {
                CreatedDate = DateTime.Now;
            }
        }

        public string GetOriginalFullName()
        {
            return OriginalFileName + Extension;
        }

        public string GetNewFullName()
        {
            return NewFileName + Extension;
        }

        public string GetNewFullPath()
        {
            return Path.Combine(Directory, GetNewFullName());
        }
    }

    public static class RenamerEngine
    {
        public static void PreviewRules(List<FileRenameItem> items, RenameRules rules)
        {
            if (items == null) return;

            int currentNum = rules.StartNumber;
            HashSet<string> seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                string name = item.OriginalFileName;

                // 1. Find & Replace
                if (!string.IsNullOrEmpty(rules.FindText))
                {
                    if (rules.MatchCase)
                    {
                        name = name.Replace(rules.FindText, rules.ReplaceText ?? "");
                    }
                    else
                    {
                        name = Regex.Replace(name, Regex.Escape(rules.FindText), rules.ReplaceText ?? "", RegexOptions.IgnoreCase);
                    }
                }

                // 2. Replace spaces
                if (rules.ReplaceSpaces)
                {
                    name = name.Replace(" ", rules.SpaceReplacement ?? "");
                }

                // 3. Case Conversion
                switch (rules.CaseOption)
                {
                    case CaseMode.LowerCase:
                        name = name.ToLower(CultureInfo.CurrentCulture);
                        break;
                    case CaseMode.UpperCase:
                        name = name.ToUpper(CultureInfo.CurrentCulture);
                        break;
                    case CaseMode.TitleCase:
                        name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower(CultureInfo.CurrentCulture));
                        break;
                }

                // 4. Numbering
                if (rules.EnableNumbering)
                {
                    string formatPattern = "D" + Math.Max(1, rules.NumberDigits);
                    string numStr = currentNum.ToString(formatPattern);
                    string sep = rules.NumberSeparator ?? "";

                    switch (rules.NumPosition)
                    {
                        case NumberPosition.Prefix:
                            name = numStr + sep + name;
                            break;
                        case NumberPosition.Suffix:
                            name = name + sep + numStr;
                            break;
                        case NumberPosition.ReplaceName:
                            name = numStr;
                            break;
                    }
                    currentNum++;
                }

                // 5. Prefix & Suffix
                if (!string.IsNullOrEmpty(rules.Prefix))
                {
                    name = rules.Prefix + name;
                }
                if (!string.IsNullOrEmpty(rules.Suffix))
                {
                    name = name + rules.Suffix;
                }

                // 6. Date Stamp
                if (rules.EnableDateStamp)
                {
                    string dateStr = DateTime.Now.ToString(rules.DateFormat);
                    if (rules.DateAtPrefix)
                    {
                        name = dateStr + "_" + name;
                    }
                    else
                    {
                        name = name + "_" + dateStr;
                    }
                }

                // Sanitize invalid path characters
                char[] invalidChars = Path.GetInvalidFileNameChars();
                foreach (char c in invalidChars)
                {
                    name = name.Replace(c.ToString(), "");
                }

                item.NewFileName = name;
                string newFull = Path.Combine(item.Directory, item.GetNewFullName());

                if (seenNames.Contains(newFull) || (File.Exists(newFull) && !string.Equals(newFull, item.FullPath, StringComparison.OrdinalIgnoreCase)))
                {
                    item.HasConflict = true;
                    item.Status = "⚠️ Çakışma Var";
                }
                else
                {
                    item.HasConflict = false;
                    item.Status = (item.OriginalFileName == item.NewFileName) ? "Değişiklik Yok" : "Hazır";
                }

                seenNames.Add(newFull);
            }
        }

        public static List<Tuple<string, string>> ApplyRename(List<FileRenameItem> items)
        {
            var history = new List<Tuple<string, string>>();

            foreach (var item in items)
            {
                if (item.HasConflict) continue;
                if (string.Equals(item.OriginalFileName, item.NewFileName, StringComparison.Ordinal))
                {
                    item.Status = "Değişiklik Yok";
                    continue;
                }

                try
                {
                    string oldPath = item.FullPath;
                    string newPath = item.GetNewFullPath();

                    if (File.Exists(oldPath))
                    {
                        File.Move(oldPath, newPath);
                        history.Add(new Tuple<string, string>(oldPath, newPath));
                        item.FullPath = newPath;
                        item.OriginalFileName = item.NewFileName;
                        item.Status = "✓ Değiştirildi";
                    }
                }
                catch (Exception ex)
                {
                    item.Status = "✕ Hata: " + ex.Message;
                }
            }

            return history;
        }

        public static void UndoRename(List<Tuple<string, string>> history, List<FileRenameItem> items)
        {
            if (history == null || history.Count == 0) return;

            for (int i = history.Count - 1; i >= 0; i--)
            {
                var entry = history[i];
                string original = entry.Item1;
                string current = entry.Item2;

                try
                {
                    if (File.Exists(current))
                    {
                        File.Move(current, original);
                        foreach (var itm in items)
                        {
                            if (string.Equals(itm.FullPath, current, StringComparison.OrdinalIgnoreCase))
                            {
                                itm.FullPath = original;
                                itm.OriginalFileName = Path.GetFileNameWithoutExtension(original);
                                itm.NewFileName = itm.OriginalFileName;
                                itm.Status = "↩️ Geri Alındı";
                                break;
                            }
                        }
                    }
                }
                catch { }
            }
            history.Clear();
        }
    }
}
