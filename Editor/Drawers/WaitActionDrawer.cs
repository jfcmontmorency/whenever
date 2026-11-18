using JFCartier.Whenever;
using UnityEditor;
using UnityEngine;

namespace JFCartier.Whenever.Editor.Drawers
{
    /// <summary>
    /// Shows Mode, and Seconds only when Mode is Seconds - "Until Previous Actions Finish" has no number
    /// to enter.
    /// </summary>
    public static class WaitActionDrawer
    {
        public static void Draw(Rect rect, SerializedObject so)
        {
            Layout(rect, so, true);
        }

        public static float GetHeight(SerializedObject so)
        {
            return Layout(default, so, false);
        }

        private static float Layout(Rect rect, SerializedObject so, bool draw)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float y = rect.y;

            SerializedProperty modeProp = so.FindProperty("mode");
            if (draw) EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineHeight), modeProp);
            y += lineHeight + 2f;

            if ((WaitAction.WaitMode)modeProp.enumValueIndex == WaitAction.WaitMode.Seconds)
            {
                SerializedProperty secondsProp = so.FindProperty("seconds");
                if (draw) EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineHeight), secondsProp);
                y += lineHeight + 2f;
            }

            return y - rect.y;
        }
    }
}
