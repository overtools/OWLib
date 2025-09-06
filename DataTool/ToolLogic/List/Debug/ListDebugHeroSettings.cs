﻿using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.Flag;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.List.Debug;

[Tool("list-debug-herosettings", Description = "List hero settings (debug)", CustomFlags = typeof(ListFlags), IsSensitive = true)]
public class ListDebugHeroSettings : ITool {
    public void Parse(ICLIFlags toolFlags) {
        GetSoundbanks();
    }

    public void GetSoundbanks() {
        foreach (ulong key in Program.TrackedFiles[0x54]) {
            var settings = GetInstance<STUGenericSettings_HeroSettings>(key);
            if (settings == null) continue;

            Dictionary<ulong, string> categories = new Dictionary<ulong, string>();

            foreach (var category in settings.m_categories) {
                categories[category.m_identifier] = GetString(category.m_name);
            }

            foreach (var heroSpecificSettings in settings.m_heroSpecificSettings) {
                STUHero hero = GetInstance<STUHero>(heroSpecificSettings.m_hero);
                Console.Out.WriteLine($"{GetString(hero.m_0EDCE350)}:");

                PrintSettings(settings.m_142A3CA9.Concat(heroSpecificSettings.m_settings).ToArray(), categories);
            }
        }
    }

    public void PrintSettings(STUHeroSettingBase[] settings, Dictionary<ulong, string> categories) {
        Dictionary<ulong, List<STUHeroSettingBase>> settingCategories = new Dictionary<ulong, List<STUHeroSettingBase>>();
        foreach (var setting in settings) {
            if (!settingCategories.ContainsKey(setting.m_category)) {
                settingCategories[setting.m_category] = new List<STUHeroSettingBase>();
            }

            settingCategories[setting.m_category].Add(setting);
        }

        foreach (var settingCategory in settingCategories) {
            Console.Out.WriteLine($"    {categories[settingCategory.Key]}:");

            foreach (var setting in settingCategory.Value) {
                Console.Out.WriteLine($"        {GetString(setting.m_name)} ({setting.GetType().Name})");


                if (setting is STUHeroSettingIdentifier settingIdentifier) {
                    foreach (var identifierEntry in settingIdentifier.m_entries) {
                        if (identifierEntry is STU_DC1455DA identifierEntryString)
                            Console.Out.WriteLine($"            {GetString(identifierEntryString.m_name)}");
                        else if (identifierEntry is STU_31E34046 identifierEntryColor)
                            Console.Out.WriteLine($"            {identifierEntryColor.m_color}");
                        /*else if (identifierEntry is STU_714EA6C9 identifierEntryUx) {
                            // unused
                            Console.Out.WriteLine($"            {identifierEntryUx.m_name}");
                        }*/
                    }
                }
            }
        }
    }
}