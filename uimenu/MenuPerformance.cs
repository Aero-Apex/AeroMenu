#nullable disable
using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace AeroMenu
{
    // Menu draw performance.
    //
    // The full menu costs several milliseconds per IMGUI pass, and Unity runs a
    // Layout pass + an event pass for EVERY input event. A trackpad sends several
    // scroll events per frame, so scrolling used to multiply the menu cost and
    // tank the frame rate. Two mitigations live here:
    //   1. Section culling: big sections that are scrolled out of view are replaced
    //      by an empty space of their last measured height.
    //   2. Scroll coalescing: only one ScrollWheel event per frame redraws the
    //      menu; extra ones in the same frame are folded into the next one.
    public partial class AeroMenuGUI : MonoBehaviour
    {
        private struct CullState
        {
            public float Y;
            public float H;
            public bool Measured;
            public bool Visible;
        }

        private static readonly Dictionary<string, CullState> cullStates = new Dictionary<string, CullState>();
        private static float cullViewTop;
        private static float cullViewBottom = 100000f;
        private const float CullMargin = 250f;

        private static int scrollHandledFrame = -1;
        private static Vector2 pendingScrollDelta;
        private static float pendingScrollAt;

        // Called at the start of the menu body, before any culled section.
        private void BeginMenuCullingFrame(float viewHeight)
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.Layout) return;
            cullViewTop = scrollPosition.y - CullMargin;
            cullViewBottom = scrollPosition.y + viewHeight + CullMargin;
        }

        [HideFromIl2Cpp]
        private void DrawCulled(string key, Action draw)
        {
            Event e = Event.current;
            EventType type = e != null ? e.type : EventType.Repaint;

            CullState st;
            cullStates.TryGetValue(key, out st);

            // Decide only in Layout and reuse the decision for the paired event,
            // so both passes always contain the same controls.
            if (type == EventType.Layout)
            {
                st.Visible = !st.Measured || (st.Y + st.H >= cullViewTop && st.Y <= cullViewBottom);
                cullStates[key] = st;
            }
            bool visible = st.Visible || !st.Measured;

            GUILayout.BeginVertical();
            try
            {
                if (visible) draw();
                else GUILayout.Space(Mathf.Max(1f, st.H));
            }
            finally { GUILayout.EndVertical(); }

            if (type == EventType.Repaint)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                st = cullStates.TryGetValue(key, out CullState cur) ? cur : st;
                st.Y = r.y;
                if (visible) st.H = r.height;
                st.Measured = true;
                cullStates[key] = st;
            }
        }

        // Returns false when this ScrollWheel event should not redraw the menu.
        private static bool CoalesceMenuScroll()
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.ScrollWheel) return true;

            float now = Time.unscaledTime;
            if (now - pendingScrollAt > 0.25f) pendingScrollDelta = Vector2.zero;

            if (scrollHandledFrame == Time.frameCount)
            {
                pendingScrollDelta += e.delta;
                pendingScrollAt = now;
                try { e.Use(); } catch { }
                return false;
            }

            scrollHandledFrame = Time.frameCount;
            if (pendingScrollDelta != Vector2.zero)
            {
                try { e.delta = e.delta + pendingScrollDelta; } catch { }
                pendingScrollDelta = Vector2.zero;
            }
            return true;
        }
    }
}
