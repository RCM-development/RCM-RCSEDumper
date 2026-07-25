using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TestMod;
using UnityEngine;
namespace RCM_RCSEDumper{

    [BepInDependency(RCMManager.IDENTIFIER, BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(IDENTIFIER, "RCSE Dumper Plugin", "1.0.0.0")]
    internal class RCSEDumper : BaseUnityPlugin{
        const string export_folder = "RCSEDumper\\";
        const string IDENTIFIER = "RCM.plugins.rscedumper";
        private void Awake(){
            new Harmony(IDENTIFIER).PatchAll();
            // create folder for dumping
            if (!Directory.Exists(export_folder)) Directory.CreateDirectory(export_folder);

            RCMManager.ConnectMod("RCSE Dumper v1").ContinueWith(t => {
                RCMModUI mod = t.Result;

                // begin mod UI construction here...
                mod.CreateButtonField("Unlock Enemy Units", UnlockEnemyUnits);
                mod.CreateButtonField("Export Units Json", ExportUnitsJson);
                mod.CreateButtonField("Export Hacks Json", ExportHacksJson);
                mod.CreateButtonField("Broken Upgrades Json", ExportUgradesJson);
                mod.CreateButtonField("Export ID lists", ExportIDLists);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }



        bool VerifyValid(){
            if (EntityBalancingStore.EntityBalancingParametersList == null) return RCMManager.LogRetFalse("RCSE Dumper: no entity bank??");
            if (RelicBalancingStore._relicBalancingScriptableObject == null) return RCMManager.LogRetFalse("RCSE Dumper: no relic bank??");
            if (UpgradeBalancingStore._upgradeBalancingScriptableObject == null) return RCMManager.LogRetFalse("RCSE Dumper: no upgrade bank??");
            if (GameBalancingStore._gameBalancingScriptableObject.levelProgressionParameters == null) return RCMManager.LogRetFalse("RCSE Dumper: no level bank??");

            return RCMManager.LogRetTrue("RCSE Dumper: cards loaded: " + EntityBalancingStore.EntityBalancingParametersList.Count);
        }
        private void UnlockEnemyUnits(){
            if (!VerifyValid()) return;

            List<EntityBalancingParameters> added_entities = new List<EntityBalancingParameters>();
            foreach (var item in EntityBalancingStore.EntityBalancingParametersList){
                // if entity is a building spawner, 
                if ((item.roles & UnitRole.Factory) != UnitRole.None
                && (item.roles & UnitRole.Building) != UnitRole.None
                && (item.roles & UnitRole.PCXCard) != UnitRole.None
                && item.isAllowedForAi){
                    // then we duplicate the struct and make it a friendly card??
                    EntityBalancingParameters converted_unit = item;

                    converted_unit.roles &= ~UnitRole.PCXCard; // clear PCXCard role
                    converted_unit.isAllowedForAi = false;
                    converted_unit.isAllowedAsBlueprint = true;

                    added_entities.Add(converted_unit);
                }
            }
            // then loop back and add all the new units in
            foreach (var item in added_entities) EntityBalancingStore.EntityBalancingParametersList.Add(item);
            RCMManager.Log("RCSE Dumper: successfully unlocked all enemy units");
        }
        private void ExportUnitsJson()
        {
            if (!VerifyValid()) return;

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;
            // Create the file.

            using (StreamWriter sw = new StreamWriter(export_folder + "units.txt"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {

                writer.Formatting = Formatting.Indented;
                //using (FileStream fs = File.Create(export_folder + "units.txt")){
                int index = 0;
                sw.Write("{\n");
                foreach (var item in EntityBalancingStore.EntityBalancingParametersList)
                {
                    if (index > 0) sw.Write(",\n");
                    sw.Write("\"" + index + "\": ");
                    //string serialized_unit = Newtonsoft.Json.JsonConvert.SerializeObject(item);
                    //fs.Write(Encoding.UTF8.GetBytes(serialized_unit), 0, serialized_unit.Length);

                    serializer.Serialize(writer, item);
                    index++;
                }
                sw.Write("\n}");
            }
            //}

            RCMManager.Log("RCSE Dumper: successfully exported unit json's");
        }
        private void ExportHacksJson()
        {
            if (!VerifyValid()) return;



            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;
            using (StreamWriter sw = new StreamWriter(export_folder + "hacks.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                int index = 0;
                sw.Write("{\n");
                foreach (var item in RelicBalancingStore._relicBalancingScriptableObject.parameters)
                {
                    if (index > 0) sw.Write(",\n");
                    sw.Write("\"" + index + "\": ");
                    serializer.Serialize(writer, item);
                    index++;
                }
                sw.Write("\n}");
            }
            RCMManager.Log("RCSE Dumper: successfully exported hack json's");
        }
        private void ExportUgradesJson()
        {
            if (!VerifyValid()) return;

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            using (StreamWriter sw = new StreamWriter(export_folder + "upgrades.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {

                writer.Formatting = Formatting.Indented;
                int index = 0;
                sw.Write("{\n");
                foreach (var item in UpgradeBalancingStore._upgradeBalancingScriptableObject.parameters)
                {
                    if (index > 0) sw.Write(",\n");
                    sw.Write("\"" + index + "\": ");
                    serializer.Serialize(writer, item);
                    index++;
                }
                sw.Write("\n}");
            }
            RCMManager.Log("RCSE Dumper: successfully exported upgrade json's");
        }
        private void ExportIDLists()
        {
            if (!VerifyValid()) return;

            // export entity names and descriptions
            using (StreamWriter writer = new StreamWriter(export_folder + "entity_name.txt"))
                foreach (var entry in Loca.BlueprintNameDictionary["en-US"])
                    writer.Write(entry.Key + "\t" + entry.Value + "\t");
            using (StreamWriter writer = new StreamWriter(export_folder + "entity_desc.txt"))
                foreach (var entry in Loca.BlueprintDescriptionDictionary["en-US"])
                    writer.Write(entry.Key + "\t" + entry.Value + "\t");

            // export relic names and descriptions
            using (StreamWriter writer = new StreamWriter(export_folder + "relic_name.txt"))
                foreach (var entry in Loca.RelicNameDictionary["en-US"])
                    writer.Write(entry.Key + "\t" + entry.Value + "\t");
            using (StreamWriter writer = new StreamWriter(export_folder + "relic_desc.txt"))
                foreach (var entry in Loca.RelicDescriptionDictionary["en-US"])
                    writer.Write(entry.Key + "\t" + entry.Value + "\t");

            // export upgrade names and descriptions
            using (StreamWriter writer = new StreamWriter(export_folder + "upgrade_name.txt"))
                foreach (var entry in Loca.UpgradeNameDictionary["en-US"])
                    writer.Write(entry.Key + "\t" + entry.Value + "\t");
            using (StreamWriter writer = new StreamWriter(export_folder + "upgrade_desc.txt"))
                foreach (var entry in Loca.UpgradeDescriptionDictionary["en-US"])
                    writer.Write(entry.Key + "\t" + entry.Value + "\t");

            // export entity roles
            using (StreamWriter writer = new StreamWriter(export_folder + "entity_roles.txt"))
                foreach (var item in EntityBalancingStore.EntityBalancingParametersList)
                    writer.Write(item.entityId.ToLower() + "\t" + (int)item.roles + "\t");

            // export all non-lowercase IDs
            using (StreamWriter writer = new StreamWriter(export_folder + "entity_IDs.txt"))
                foreach (var item in EntityBalancingStore.EntityBalancingParametersList)
                    writer.Write(item.entityId + "\t");
            using (StreamWriter writer = new StreamWriter(export_folder + "relic_IDs.txt"))
                foreach (var item in RelicBalancingStore._relicBalancingScriptableObject.parameters)
                    writer.Write(item.relicId + "\t");
            using (StreamWriter writer = new StreamWriter(export_folder + "upgrade_IDs.txt"))
                foreach (var item in UpgradeBalancingStore._upgradeBalancingScriptableObject.parameters)
                    writer.Write(item.upgradeId + "\t");

            // export all AI types
            using (StreamWriter writer = new StreamWriter(export_folder + "enemy_decks.txt"))
            {
                foreach (var item in GameBalancingStore._gameBalancingScriptableObject.levelProgressionParameters.defaultAis) writer.Write(item.name + "\t");
                if (GameBalancingStore._gameBalancingScriptableObject.levelProgressionParametersHeat?.defaultAis != null)
                    foreach (var item in GameBalancingStore._gameBalancingScriptableObject.levelProgressionParametersHeat?.defaultAis) writer.Write(item.name + "\t");
                if (GameBalancingStore._gameBalancingScriptableObject.levelProgressionParametersFirstRun?.defaultAis != null)
                    foreach (var item in GameBalancingStore._gameBalancingScriptableObject.levelProgressionParametersFirstRun?.defaultAis) writer.Write(item.name + "\t");
            }
            // export all landscape generators
            using (StreamWriter writer = new StreamWriter(export_folder + "landscapes.txt"))
            {
                foreach (var item in GameBalancingStore._gameBalancingScriptableObject.levelProgressionParameters.defaultLandscapeGenerators) writer.Write(item.name + "\t");
                if (GameBalancingStore._gameBalancingScriptableObject.levelProgressionParametersHeat?.defaultLandscapeGenerators != null)
                    foreach (var item in GameBalancingStore._gameBalancingScriptableObject.levelProgressionParametersHeat?.defaultLandscapeGenerators) writer.Write(item.name + "\t");
                if (GameBalancingStore._gameBalancingScriptableObject.levelProgressionParametersFirstRun?.defaultLandscapeGenerators != null)
                    foreach (var item in GameBalancingStore._gameBalancingScriptableObject.levelProgressionParametersFirstRun?.defaultLandscapeGenerators) writer.Write(item.name + "\t");
            }
            // export all world types
            using (StreamWriter writer = new StreamWriter(export_folder + "worlds.txt"))
            {
                foreach (var item in GameBalancingStore._gameBalancingScriptableObject.levelProgressionParameters.defaultWorlds) writer.Write(item.nameLocaId + "\t");
                if (GameBalancingStore._gameBalancingScriptableObject.levelProgressionParametersHeat?.defaultWorlds != null)
                    foreach (var item in GameBalancingStore._gameBalancingScriptableObject.levelProgressionParametersHeat?.defaultWorlds) writer.Write(item.nameLocaId + "\t");
                if (GameBalancingStore._gameBalancingScriptableObject.levelProgressionParametersFirstRun?.defaultWorlds != null)
                    foreach (var item in GameBalancingStore._gameBalancingScriptableObject.levelProgressionParametersFirstRun?.defaultWorlds) writer.Write(item.nameLocaId + "\t");
            }


            RCMManager.Log("RCSE Dumper: successfully dumped ID's");
        }
    }
}
