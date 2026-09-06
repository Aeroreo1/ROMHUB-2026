
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ROMHub.Models;

namespace ROMHub.Data
{
    public static class EmulatorJsonStore
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RomHub",
            "emulators.json");

        private static readonly object FileLock = new object();

        public static List<Emulator> Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new List<Emulator>();

                var json = File.ReadAllText(FilePath);

                return JsonSerializer.Deserialize<List<Emulator>>(json)
                       ?? new List<Emulator>();
            }
            catch
            {
                return new List<Emulator>();
            }
        }

        public static void Save(List<Emulator> emulators)
        {
            lock (FileLock)
            {
                var directory = Path.GetDirectoryName(FilePath);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonSerializer.Serialize(
                    emulators,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                File.WriteAllText(FilePath, json);
            }
        }

        public static void Add(Emulator emulator)
        {
            lock (FileLock)
            {
                var emulators = Load();

                emulator.Id = emulators.Any()
                    ? emulators.Max(e => e.Id) + 1
                    : 1;

                emulators.Add(emulator);

                Save(emulators);
            }
        }
    }
}

