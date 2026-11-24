#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
using HarmonyLib;

namespace AeroMenu
{
    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.HasServerTimestamp), MethodType.Getter)]
    public static class AprilDateSpoof_HasServerTimestamp_Patch
    {
        public static void Postfix(ref bool __result)
        {
            if (AeroMenuGUI.spoofAprilFoolsDate)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.ApproximateServerTime), MethodType.Getter)]
    public static class AprilDateSpoof_ApproximateServerTime_Patch
    {
        public static void Postfix(ref Il2CppSystem.DateTime __result)
        {
            if (!AeroMenuGUI.spoofAprilFoolsDate) return;

            var dt = new System.DateTime(System.DateTime.UtcNow.Year, 4, 1, 7, 1, 0, System.DateTimeKind.Utc);
            // ApproximateServerTime normally returns a UTC DateTime. The ticks-only
            // constructor yields Kind=Unspecified, so keep the UTC kind explicitly.
            __result = Il2CppSystem.DateTime.SpecifyKind(
                new Il2CppSystem.DateTime(dt.Ticks), Il2CppSystem.DateTimeKind.Utc);
        }
    }

    [HarmonyPatch(typeof(AprilFoolsMode), nameof(AprilFoolsMode.ShouldFlipSkeld))]
    public static class FlipSkeld_ShouldFlipSkeld_Patch
    {
        public static void Postfix(ref bool __result)
        {
            if (AeroMenuGUI.flipSkeld)
                __result = true;
        }
    }
}
