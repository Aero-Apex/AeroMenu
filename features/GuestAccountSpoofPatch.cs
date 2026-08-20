#nullable disable
using AeroMenu;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.StartInitialLoginFlow))]
public static class GuestAccountSpoofPatch
{
    public static bool Prefix(EOSManager __instance)
    {
        bool guest = AeroMenuGUI.spoofGuestAccount ||
            PlayerPrefs.GetInt("M_SpoofGuestAccount", 0) == 1;
        if (!guest || __instance == null) return true;

        try
        {
            __instance.StartTempAccountFlow();
            __instance.CloseStartupWaitScreen();
            return false;
        }
        catch
        {
            return true;
        }
    }
}
