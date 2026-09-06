using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ROMHub.Models;

namespace ROMHub.Data
{
    public static class RomJsonStore
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RomHub",
            "roms.json");

        private static readonly object FileLock = new object();

        public static List<Rom> Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new List<Rom>();

                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<List<Rom>>(json) ?? new List<Rom>();
            }
            catch
            {
                return new List<Rom>();
            }
        }

        public static void Save(List<Rom> roms)
        {
            lock (FileLock)
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(roms, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
        }

        public static void Add(Rom rom)
        {
            lock (FileLock)
            {
                var roms = Load();
                rom.Id = roms.Any() ? roms.Max(r => r.Id) + 1 : 1;
                roms.Add(rom);
                Save(roms);
            }
        }
    }
}
