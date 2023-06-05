
using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace MC_SVFastRefining
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "mc.starvalor.tractor";
        public const string pluginName = "SV Tractor Beam";
        public const string pluginVersion = "1.0.1";

        private static Dictionary<BuffTowing, BuffTowData> data = new Dictionary<BuffTowing, BuffTowData>();

        private static BepInEx.Logging.ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Main));
        }

        [HarmonyPatch(typeof(BuffTowing), "FixedUpdate")]
        [HarmonyPrefix]
        private static bool BuffTowingFixedUpdate_Pre(BuffTowing __instance)
        {
            BuffTowData buffTowData;

            if (data.Count > 0 && data.ContainsKey(__instance))
            {
                buffTowData = data[__instance];
            }
            else
            {
                buffTowData = new BuffTowData()
                {
                    ownerTrans = (Transform)AccessTools.Field(typeof(BuffTowing), "ownerTrans").GetValue(__instance),
                    targetEntity = (Entity)AccessTools.Field(typeof(BuffTowing), "targetEntity").GetValue(__instance),
                    targetRb = (Rigidbody)AccessTools.Field(typeof(BuffTowing), "targetRb").GetValue(__instance),
                    desiredDistance = (float)AccessTools.Field(typeof(BuffTowing), "desiredDistance").GetValue(__instance)                    
                };
                Vector3 tSize = buffTowData.targetRb.gameObject.GetComponent<Collider>().bounds.size;
                Vector3 pSize = buffTowData.ownerTrans.gameObject.GetComponent<Collider>().bounds.size;
                buffTowData.additionalDist = (Mathf.Max(tSize.x, tSize.y) + Mathf.Max(pSize.x, pSize.y)) / 2;
                __instance.gameObject.GetComponent<BuffDistanceLimit>().maxDistance += buffTowData.additionalDist;
                buffTowData.desiredDistance += buffTowData.additionalDist;
                data.Add(__instance, buffTowData);
            }

            if (__instance.active && buffTowData.ownerTrans != null)
            {
                Vector3 normalized = (buffTowData.ownerTrans.position - buffTowData.targetEntity.transform.position).normalized;
                float d = Vector3.Distance(buffTowData.ownerTrans.position, buffTowData.targetEntity.transform.position) - buffTowData.desiredDistance;
                float f = 0;
                if (d > 0)
                    f = __instance.towingForce;
                else if (d < 0)
                    f = -__instance.towingForce;

                buffTowData.targetRb.AddForce(normalized * f, ForceMode.Force);
            }

            return false;
        }

        [HarmonyPatch(typeof(BuffTowing), "End")]
        [HarmonyPrefix]
        private static void BuffTowingEnd_Pre(BuffTowing __instance)
        {
            if (data.ContainsKey(__instance))
            {
                __instance.GetComponent<BuffDistanceLimit>().maxDistance -= data[__instance].additionalDist;
                data.Remove(__instance);
            }
        }
    }

    internal class BuffTowData
    {
        internal Transform ownerTrans;
        internal Entity targetEntity;
        internal Rigidbody targetRb;
        internal float desiredDistance;
        internal float additionalDist;
    }
}
