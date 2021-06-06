// Copyright (c) Arctium.

using Framework.Misc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RealmServer.Managers
{
    public class TableManager
    {
        public List<CharacterCreation> CharacterCreations { get; private set; }
        public List<CharacterCreationArmorSet> CharacterCreationArmorSet { get; private set; }
        public List<CharacterCustomization> CharacterCustomizations { get; private set; }
        public List<CharacterCustomizationLabel> CharacterCustomizationLabels { get; private set; }
        public List<DyeColorRamp> DyeColorRamps { get; private set; }
        public List<Item2> Item2 { get; private set; }
        public List<Item2Type> Item2Type { get; private set; }
        public List<ItemDisplay> ItemDisplay { get; private set; }
        public List<ItemSlot> ItemSlot { get; private set; }

        public TableManager()
        {
            CharacterCreations = CreateTable<CharacterCreation>("CharacterCreation");
            CharacterCreationArmorSet = CreateTable<CharacterCreationArmorSet>("CharacterCreationArmorSet");
            CharacterCustomizations = CreateTable<CharacterCustomization>("CharacterCustomization");
            CharacterCustomizationLabels = CreateTable<CharacterCustomizationLabel>("CharacterCustomizationLabel");
            DyeColorRamps = CreateTable<DyeColorRamp>("DyeColorRamp");
            Item2 = CreateTable<Item2>("Item2");
            Item2Type = CreateTable<Item2Type>("Item2Type");
            ItemDisplay = CreateTable<ItemDisplay>("ItemDisplay");
            ItemSlot = CreateTable<ItemSlot>("ItemSlot");
        }

        static List<T> CreateTable<T>(string file)
        {
            var retList = new List<T>();

            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"Arctium_WildStar_Sandbox.ClientTables.{file}.csv")))
            {
                var fieldNames = reader.ReadLine().Split(',').Select(s => s.Replace("\"", "")).ToArray();
                var typeFields = typeof(T).GetProperties();

                while (!reader.EndOfStream)
                {
                    var values = reader.ReadLine().Split(new string[] { ",\"" }, StringSplitOptions.None).Select(s => s.Replace("\"", "")).ToArray();

                    if (fieldNames.Length != values.Length)
                    {
                        Console.WriteLine("fieldNames.Length != values.Length");
                        continue;
                    }

                    var obj = Activator.CreateInstance<T>();

                    for (var i = 0; i < values.Length; i++)
                    {
                        if (typeFields[i].PropertyType == typeof(float))
                            values[i] = values[i].Replace(",", ".");

                        var val = Convert.ChangeType(values[i], typeFields[i].PropertyType, CultureInfo.GetCultureInfo("en-US").NumberFormat);

                        typeFields[i].SetValue(obj, val);
                    }

                    retList.Add(obj);
                }

            }

            Console.WriteLine($"{typeof(T).Name}: Added {retList.Count} entries.");

            return retList;
        }
    }
}
