#nullable disable
using UnityEngine;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        internal static GUIStyle themedSliderS;
        internal static GUIStyle themedSliderThumbS;

        // Menu-themed horizontal slider. The default skin slider renders with no
        // visible track in this build, so every feature slider routes through here.
        internal static float ThemedSlider(float value, float min, float max, params GUILayoutOption[] options)
        {
            if (options == null || options.Length == 0)
                options = new GUILayoutOption[] { GUILayout.MinWidth(60f), GUILayout.ExpandWidth(true) };
            if (themedSliderS == null || themedSliderThumbS == null)
                return GUILayout.HorizontalSlider(value, min, max, options);
            return GUILayout.HorizontalSlider(value, min, max, themedSliderS, themedSliderThumbS, options);
        }
    }
}
