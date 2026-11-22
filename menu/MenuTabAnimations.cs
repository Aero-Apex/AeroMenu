#nullable disable
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace AeroMenu
{
    public partial class AeroMenuGUI : MonoBehaviour
    {
        private sealed class MultiTabAnim
        {
            public float Progress = 1f;
            public int Dir = 1;
        }

        private readonly Dictionary<string, MultiTabAnim> multiTabAnims = new Dictionary<string, MultiTabAnim>();

        [HideFromIl2Cpp]
        private MultiTabAnim GetMultiTabAnim(string key)
        {
            if (!multiTabAnims.TryGetValue(key, out MultiTabAnim anim))
            {
                anim = new MultiTabAnim();
                multiTabAnims[key] = anim;
            }

            return anim;
        }

        private bool SetMultiTab(string key, ref int current, int tab, int count, bool resetScroll = true)
        {
            tab = Mathf.Clamp(tab, 0, count - 1);
            if (current == tab)
                return false;
            if (resetScroll)
                scrollPosition = Vector2.zero;

            MultiTabAnim anim = GetMultiTabAnim(key);
            anim.Dir = tab > current ? 1 : -1;
            anim.Progress = 0f;
            current = tab;
            return true;
        }

        private void BeginMultiTabContent(string key, out Matrix4x4 oldMatrix, out Color oldColor)
        {
            MultiTabAnim anim = GetMultiTabAnim(key);
            if (Event.current != null && Event.current.type == EventType.Repaint && anim.Progress < 1f)
            {
                anim.Progress += Time.unscaledDeltaTime * 10f;
                if (anim.Progress > 1f)
                    anim.Progress = 1f;
            }

            float ease = SmoothMenuTab(anim.Progress);
            // Never mutate GUI.matrix here. The slide value depends on Progress,
            // which only advances on Repaint; setting a matrix from it during
            // Layout makes GUILayout cache rects under a different transform and
            // the body gets drawn on top of the sidebar. Capture it only so the
            // existing try/finally call sites stay valid; leave it untouched.
            oldMatrix = GUI.matrix;
            oldColor = GUI.color;
            GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, oldColor.a * Mathf.Clamp01(ease * 1.25f));
        }

        private static void EndMultiTabContent(Matrix4x4 oldMatrix, Color oldColor)
        {
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }
    }
}
