
using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace MC_SVTractorBeam
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "mc.starvalor.tractor";
        public const string pluginName = "SV Tractor Beam";
        public const string pluginVersion = "2.0.0";

        private enum Stat { cnt, maxSpeed, acceleration }

        private static Dictionary<SpaceShip, float[]> baseStats = new Dictionary<SpaceShip, float[]>();

        private static BepInEx.Logging.ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Main));
        }


        [HarmonyPatch(typeof(BuffTowing), "Begin")]
        [HarmonyPrefix]
        private static void BuffTowingBegin_Post(BuffTowing __instance)
        {
            Entity targetEntity = (Entity)AccessTools.Field(typeof(BuffTowing), "targetEntity").GetValue(__instance);
            if (targetEntity == null || targetEntity.gameObject.GetComponent<AIControl>() == null)
                return;

            SpaceShip ss = targetEntity.gameObject.GetComponent<SpaceShip>();
            if (ss == null)
                return;
            
            int factor = 11 - (ss.shipClass * 2);
            if (factor < 1)
                return;

            if (!baseStats.ContainsKey(ss))
                baseStats.Add(ss, new float[] {1f, ss.stats.maxSpeed, ss.stats.acceleration });
            else
                baseStats[ss][(int)Stat.cnt]++;

            ss.stats.maxSpeed /= factor;
            ss.stats.acceleration /= factor;
        }

        [HarmonyPatch(typeof(BuffTowing), "End")]
        [HarmonyPrefix]
        private static void BuffTowingEnd_Pre(BuffTowing __instance)
        {
            Entity targetEntity = (Entity)AccessTools.Field(typeof(BuffTowing), "targetEntity").GetValue(__instance);
            if (targetEntity == null || targetEntity.gameObject.GetComponent<AIControl>() == null)
                return;

            SpaceShip ss = targetEntity.gameObject.GetComponent<SpaceShip>();
            if (ss == null)
                return;

            if (!baseStats.ContainsKey(ss))
                return;

            baseStats[ss][(int)Stat.cnt]--;
            if (baseStats[ss][(int)Stat.cnt] <= 0)
            {
                ss.stats.maxSpeed = baseStats[ss][(int)Stat.maxSpeed];
                ss.stats.acceleration = baseStats[ss][(int)Stat.acceleration];
                baseStats.Remove(ss);
            }
        }
    }
}
