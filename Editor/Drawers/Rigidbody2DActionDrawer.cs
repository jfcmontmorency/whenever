using JFCartier.Whenever;
using JFCartier.Whenever.Editor;
using UnityEditor;
using UnityEngine;

namespace JFCartier.Whenever.Editor.Drawers
{
    /// <summary>
    /// Groups fields under "Target" and "Effect" section labels, like RigidbodyActionDrawer. Target Mode
    /// only shows when the owner has a Collider Event (Condition Watcher has no entering object, so it's
    /// locked to Specific Object). Every Effect property shows its value field only when its own Modify
    /// toggle is checked.
    /// </summary>
    public static class Rigidbody2DActionDrawer
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

            if (draw) EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineHeight), "Target", EditorStyles.boldLabel);
            y += lineHeight + 2f;

            EditorGUI.indentLevel++;

            SerializedProperty targetModeProp = so.FindProperty("targetMode");
            bool supportsEnteringObjects = TargetModeGui.SupportsEnteringObjects(so);

            if (supportsEnteringObjects)
            {
                if (draw) EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineHeight), targetModeProp);
                y += lineHeight + 2f;
            }
            else
            {
                targetModeProp.enumValueIndex = (int)TargetMode.SpecificObject;
            }

            if (!supportsEnteringObjects || (TargetMode)targetModeProp.enumValueIndex == TargetMode.SpecificObject)
            {
                SerializedProperty targetProp = so.FindProperty("target");
                if (draw) EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineHeight), targetProp);
                y += lineHeight + 2f;
            }

            EditorGUI.indentLevel--;
            y += 6f;

            if (draw) EditorGUI.LabelField(new Rect(rect.x, y, rect.width, lineHeight), "Effect", EditorStyles.boldLabel);
            y += lineHeight + 2f;

            EditorGUI.indentLevel++;

            y = DrawToggledValue(rect, so, "modifyBodyType", "bodyType", y, draw);
            y = DrawToggledValue(rect, so, "modifyMass", "mass", y, draw);
            y = DrawToggledValue(rect, so, "modifyLinearDamping", "linearDamping", y, draw);
            y = DrawToggledValue(rect, so, "modifyAngularDamping", "angularDamping", y, draw);
            y = DrawToggledValue(rect, so, "modifyGravityScale", "gravityScale", y, draw);

            EditorGUI.indentLevel--;

            return y - rect.y;
        }

        private static float DrawToggledValue(Rect rect, SerializedObject so, string toggleName, string valueName, float y, bool draw)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;

            SerializedProperty toggleProp = so.FindProperty(toggleName);
            if (draw) EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineHeight), toggleProp);
            y += lineHeight + 2f;

            if (toggleProp.boolValue)
            {
                SerializedProperty valueProp = so.FindProperty(valueName);
                if (draw) EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineHeight), valueProp);
                y += lineHeight + 2f;
            }

            return y;
        }
    }
}
