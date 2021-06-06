// Copyright (c) Arctium.

using Newtonsoft.Json;
using RealmServer.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RealmServer.Managers
{
    public class DataManager
    {
        public Dictionary<ulong, Character> Characters;

        public DataManager()
        {
            LoadFile(typeof(Character), ref Characters);
        }

        public void LoadFile<T>(Type dataType, ref T obj) where T : new()
        {
            Directory.CreateDirectory($"{Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\\Data");

            var filePath = $"{Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\\Data\\{dataType.Name}.json";


            if (File.Exists(filePath))
                obj = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
            else
                File.CreateText(filePath).Dispose();

            if (obj == null)
                obj = new T();
        }

        public void Add(Character character)
        {
            Characters.Add(character.Id, character);

            WriteCharacterFile();
        }

        public void Remove(Character character)
        {
            Characters.Remove(character.Id);

            WriteCharacterFile();
        }

        public void RemoveCharacterById(ulong characterId)
        {
            Characters.Remove(characterId);

            WriteCharacterFile();
        }

        public void UpdateCharacter(Character character)
        {
            Characters[character.Id] = character;

            WriteCharacterFile();
        }

        void WriteCharacterFile()
        {
            File.WriteAllText($"{Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)}\\Data\\{typeof(Character).Name}.json", JsonConvert.SerializeObject(Characters, Formatting.Indented));
        }
    }
}
