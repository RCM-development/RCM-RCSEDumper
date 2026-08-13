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
using UnityEngine.UIElements;
namespace RCM_RCSEDumper{

    [BepInDependency(RCMManager.IDENTIFIER, BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(IDENTIFIER, "RCSE Dumper Plugin", "1.0.0.0")]
    internal class RCSEDumper : BaseUnityPlugin{
        const string export_folder = "RCM\\RCSEDumper\\";
        const string IDENTIFIER = "RCM.plugins.rscedumper";
        private void Awake(){
            new Harmony(IDENTIFIER).PatchAll();
            // create folder for dumping
            if (!Directory.Exists(export_folder)) Directory.CreateDirectory(export_folder);

            RCMManager.ConnectMod("RCSE Dumper").ContinueWith(t => {
                RCMModUI mod = t.Result;

                // begin mod UI construction here...
                mod.CreateButtonField("Unlock Enemy Units", UnlockEnemyUnits);
                mod.CreateButtonField("Export Units Json", ExportUnitsJson);
                mod.CreateButtonField("Export Hacks Json", ExportHacksJson);
                //mod.CreateButtonField("Broken Upgrades Json", ExportUgradesJson);
                mod.CreateButtonField("Export ID lists", ExportIDLists);
                mod.CreateButtonField("Dump Aiming values", ExportAimingValues);
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
        private void ExportUnitsJson(){
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

            RCMManager.Log("RCSE Dumper: successfully exported unit json's");
        }
        private void ExportHacksJson(){
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
        private void ExportUgradesJson(){
            if (!VerifyValid()) return;

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            using (StreamWriter sw = new StreamWriter(export_folder + "upgrades.json"))
            using (JsonWriter writer = new JsonTextWriter(sw)){

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
        private void ExportIDLists(){
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
            // export all blueprint pool types
            using (StreamWriter writer = new StreamWriter(export_folder + "blueprint_pools.txt")){
                foreach (BlueprintPool blueprintPool in Resources.LoadAll<BlueprintPool>("BlueprintPools"))
                    writer.Write(blueprintPool.poolId + "\t");
            }

        
            RCMManager.Log("RCSE Dumper: successfully dumped ID's");
        }

        string ParseAiming(SingleTargetAction action)
        {
            string type = "No Aiming";
            if (action == null) return type;

            type = action.GetType().ToString();
            
            if (action.GetType() == typeof(RotateInSingleTargetDirectionAroundAxisAction))
                type = type + (((RotateInSingleTargetDirectionAroundAxisAction)action).direction == RectTransform.Axis.Vertical ? " | Vertical" : " | Horizontal");
            else if (action.GetType() == typeof(SerialSingleTargetAction))
            {
                SerialSingleTargetAction serialAction = (SerialSingleTargetAction)action;
                for (int i = 0; i < serialAction.actions.Count; i++)
                    type = type + " ["+i+"]: " + ParseAiming(serialAction.actions[i]);
            }
            return type;
        }


        void ExportAimingValues(){
            if (!VerifyValid()) return;

            List<string> no_prefab = new List<string>();
            List<string> no_controller = new List<string>();
            List<string> has_skill_aiming = new List<string>();
            List<string> no_aiming = new List<string>();
            List<string> has_child_entities = new List<string>();
            //List<string> laser_turret = new List<string>();
            List<string> invalid_horizontal_pivot = new List<string>();
            List<string> compatible = new List<string>();

            List<string> exta_logs = new List<string>();


            foreach (var item in EntityBalancingStore.EntityBalancingParametersList) {

                string text = EntityBalancingStore.PrefabLocation(item.entityId);
                var auto = Resources.Load(text);
                if (auto == null) no_prefab.Add(item.entityId);
                else {
                    GameObject gameObject = (GameObject)GameObject.Instantiate(auto, new Vector3(0, 0, 0), Quaternion.identity);
                    EntityController entityController = gameObject.GetComponent<EntityController>();

                    if (entityController == null) no_controller.Add(item.entityId);
                    //else if (entityController.skillAiming != null) has_skill_aiming.Add(item.entityId);
                    else if (entityController.aiming == null) no_aiming.Add(item.entityId);
                    else if (entityController.childEntityControllers.Count > 0) has_child_entities.Add(item.entityId);
                    else
                    {

                        // if using laser based weapon, drop it
                        //bool has_projectile = false;
                        //foreach (var e in entityController.events)
                        //{
                        //    if (e.@event == EntityController.Event.OnReadyToShoot)
                        //    {
                        //        foreach (var a in e.actions)
                        //        {
                        //            if (a.GetType() == typeof(ShootProjectile))
                        //            {
                        //                has_projectile = true;
                        //            }
                        //        }
                        //        foreach (var a in e.conditionalActions)
                        //        {
                        //            foreach(var b in a.actions)
                        //            {
                        //                if (b.GetType() == typeof(ShootProjectile))
                        //                {
                        //                    has_projectile = true;
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                        //if (!has_projectile){
                        //    laser_turret.Add(item.entityId);
                        //    goto break_thingo;
                        //}

                        // if using serial aiming, then we just check for more than 2 enties and its a definite yes
                        SingleTargetAction aiming = entityController.aiming;
                        if (entityController.aiming.GetType() == typeof(SerialSingleTargetAction))
                        {
                            SerialSingleTargetAction serialAction = (SerialSingleTargetAction)entityController.aiming;
                            if (serialAction.actions.Count > 1){
                                compatible.Add(item.entityId);
                                goto break_thingo;
                            } else if (serialAction.actions.Count == 1){
                                aiming = serialAction.actions[0];
                            } else if (serialAction.actions.Count == 0){
                                no_aiming.Add(item.entityId);
                                exta_logs.Add(item.entityId + " has serial but no enties, marked as no aiming.");
                                goto break_thingo;
                            }
                        }

                        // now we evaluate the current aiming to see if it holds an actual turret
                        string aiming_root_name = "";
                        GameObject aiming_root_parent = null;
                        if (aiming.GetType() == typeof(RotateInSingleTargetDirectionAroundAxisAction)){
                            RotateInSingleTargetDirectionAroundAxisAction curr = (RotateInSingleTargetDirectionAroundAxisAction)aiming;
                            aiming_root_name = curr.transformToRotate.gameObject.name;
                            aiming_root_parent = curr.transformToRotate.parent.gameObject;
                        }
                        else if (aiming.GetType() == typeof(RotateInSingleTargetDirectionAction)){
                            RotateInSingleTargetDirectionAction curr = (RotateInSingleTargetDirectionAction)aiming;
                            aiming_root_name = curr.transformToRotate.gameObject.name;
                            aiming_root_parent = curr.transformToRotate.parent.gameObject;
                        }
                        else if (aiming.GetType() == typeof(RotateToBallisticAngleSingleTargetAction)){
                            RotateToBallisticAngleSingleTargetAction curr = (RotateToBallisticAngleSingleTargetAction)aiming;
                            aiming_root_name = curr.transformToRotate.gameObject.name;
                            aiming_root_parent = curr.transformToRotate.parent.gameObject;
                        }
                        else{
                            no_aiming.Add(item.entityId);
                            exta_logs.Add(item.entityId + " has unsupported aiming type, marking no aiming");
                            goto break_thingo;
                        }

                        aiming_root_name = aiming_root_name.ToLower();

                        switch (aiming_root_name){
                            case "rothor":
                            case "gun":
                            case "simplemachinegunmesh": // armored oil harvester
                            case "castleroofmachinegun":
                            case "head": // animals mostly
                            case "turretdoublecannon":
                            case "hexagon_castlebattlement":
                            case "rot hor": // spider tank
                            case "tankhead":
                            case "vehiclesimplemachinegun":
                            case "rotor":
                            case "rotator":
                            case "rotatehorizontal":
                            case "aimer":
                            case "turrethead": // PCX launcher factory
                            case "horizontalrotator": // PCX mobile command
                            case "upperbody":
                            case "detail_pipeend 1 (1)": // PCX termite
                            case "horizontalhealarm": // repair trike
                            case "detail_pipeend": 
                            case "detailtargetsight (1)": // PCX big bomer

                                compatible.Add(item.entityId);
                                if (gameObject == aiming_root_parent)
                                    exta_logs.Add(item.entityId + " has whitelisted name (" + aiming_root_name + ") but is top level child, so it must be incorrectly identified");
                                 break;
                            default:
                                invalid_horizontal_pivot.Add(item.entityId);
                                if (gameObject == aiming_root_parent)
                                    exta_logs.Add(item.entityId + " has bad horizontal pivot, obj name: " + aiming_root_name + " also is a top level child...");
                                else exta_logs.Add(item.entityId + " has bad horizontal pivot, obj name: " + aiming_root_name);
                                break;
                        }
                        
                    }
                break_thingo:
                    GameObject.Destroy(gameObject);
                }


            }
            using (StreamWriter writer = new StreamWriter(export_folder + "aiming_values.txt"))
            {
                foreach (string s in no_prefab)
                    writer.WriteLine(s + ": no prefab");
                writer.WriteLine("====================================================================");
                foreach (string s in no_controller)
                    writer.WriteLine(s + ": no controller");
                writer.WriteLine("====================================================================");
                foreach (string s in no_aiming)
                    writer.WriteLine(s + ": no aiming");
                writer.WriteLine("====================================================================");
                foreach (string s in has_child_entities)
                    writer.WriteLine(s + ": contains child entities");
                writer.WriteLine("====================================================================");
                foreach (string s in invalid_horizontal_pivot)
                    writer.WriteLine(s + ": no horizontal pivot");
                writer.WriteLine("====================================================================");
                foreach (string s in compatible)
                    writer.WriteLine(s + ": compatible");
                writer.WriteLine("====================================================================");
                writer.WriteLine("============================= ERRORS ===============================");
                writer.WriteLine("====================================================================");
                foreach (string s in exta_logs)
                    writer.WriteLine(s);
            }
            RCMManager.Log("RCSE Dumper: successfully dumped aiming values");
        }
    }
}
