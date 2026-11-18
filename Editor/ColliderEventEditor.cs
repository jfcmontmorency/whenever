using System.Linq;
using JFCartier.Whenever;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace JFCartier.Whenever.Editor
{
    /// <summary>
    /// The Inspector for both zone types: ColliderEvent (3D) and ColliderEvent2D share every field it
    /// draws. Unity allows only one [CustomEditor] per class, so each zone type gets its own thin subclass
    /// at the bottom of this file.
    /// </summary>
    public abstract class ColliderEventEditorBase : UnityEditor.Editor
    {
        private const string AnyTagLabel = "Any";

        private SerializedProperty m_HoldTime;
        private SerializedProperty m_AfterTrigger;
        private SerializedProperty m_Repeat;
        private SerializedProperty m_RepeatInterval;
        private SerializedProperty m_MaxRepeats;
        private SerializedProperty m_DebugLogging;
        private SerializedProperty m_DebugColor;
        private SerializedProperty m_RequiredTags;
        private SerializedProperty m_ShowHintMaterial;
        private SerializedProperty m_HintTargetMode;
        private SerializedProperty m_HintRenderer;
        private SerializedProperty m_HintMaterial;
        private SerializedProperty m_Conditions;
        private SerializedProperty m_Actions;
        private SerializedProperty m_ExitActions;

        private ReorderableList m_ConditionsList;
        private ReorderableList m_ActionsList;
        private ReorderableList m_ExitActionsList;

        protected virtual void OnEnable()
        {
            m_HoldTime = serializedObject.FindProperty("holdTime");
            m_AfterTrigger = serializedObject.FindProperty("afterTrigger");
            m_Repeat = serializedObject.FindProperty("repeat");
            m_RepeatInterval = serializedObject.FindProperty("repeatInterval");
            m_MaxRepeats = serializedObject.FindProperty("maxRepeats");
            m_DebugLogging = serializedObject.FindProperty("debugLogging");
            m_DebugColor = serializedObject.FindProperty("debugColor");
            m_RequiredTags = serializedObject.FindProperty("requiredTags");
            m_ShowHintMaterial = serializedObject.FindProperty("showHintMaterial");
            m_HintTargetMode = serializedObject.FindProperty("hintTargetMode");
            m_HintRenderer = serializedObject.FindProperty("hintRenderer");
            m_HintMaterial = serializedObject.FindProperty("hintMaterial");
            m_Conditions = serializedObject.FindProperty("conditions");
            m_Actions = serializedObject.FindProperty("actions");
            m_ExitActions = serializedObject.FindProperty("exitActions");

            GameObject owner = ((Component)target).gameObject;
            m_ConditionsList = ConditionActionListDrawer.Build(m_Conditions, typeof(ConditionBase), owner);
            m_ActionsList = ConditionActionListDrawer.Build(m_Actions, typeof(ActionBase), owner);
            m_ExitActionsList = ConditionActionListDrawer.Build(m_ExitActions, typeof(ActionBase), owner);
        }

        public override void OnInspectorGUI()
        {
            // In Play Mode, an Action (or After Trigger) can destroy this GameObject while it's still the
            // selected object - Unity calls OnInspectorGUI one more time before the Inspector clears, and
            // by then target/serializedObject point at a destroyed object. Bail out rather than redraw.
            if (target == null) return;

            serializedObject.Update();

            DrawCollidesWith();
            EditorGUILayout.PropertyField(m_DebugColor);
            ColliderEventEditorShared.DrawDebugLogging(m_DebugLogging);

            ColliderEventEditorShared.DrawCommonFields(m_HoldTime, m_AfterTrigger, m_Repeat, m_RepeatInterval, m_MaxRepeats);
            ColliderEventEditorShared.DrawHintMaterial(serializedObject, m_ShowHintMaterial, m_HintTargetMode, m_HintRenderer, m_HintMaterial);
            ColliderEventEditorShared.DrawLists(m_ConditionsList, m_ActionsList, m_ExitActionsList, m_AfterTrigger);

            serializedObject.ApplyModifiedProperties();
        }

        // requiredTags stays an array in code (room to allow several tags later), but for now the
        // Inspector only lets you pick one - simpler to use while that's all most zones need.
        private void DrawCollidesWith()
        {
            string[] allTags = InternalEditorUtility.tags;
            string[] options = new[] { AnyTagLabel }.Concat(allTags).ToArray();

            string currentTag = m_RequiredTags.arraySize > 0 ? m_RequiredTags.GetArrayElementAtIndex(0).stringValue : null;
            int currentIndex = currentTag != null ? System.Array.IndexOf(allTags, currentTag) + 1 : 0;
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup(new GUIContent("Collides With", "Only an object with this tag can trigger this zone. Any lets anything with a Collider trigger it."), currentIndex, options);

            if (newIndex != currentIndex)
            {
                m_RequiredTags.ClearArray();

                if (newIndex > 0)
                {
                    m_RequiredTags.arraySize = 1;
                    m_RequiredTags.GetArrayElementAtIndex(0).stringValue = options[newIndex];
                }
            }
        }
    }

    [CustomEditor(typeof(ColliderEvent))]
    public sealed class ColliderEventEditor : ColliderEventEditorBase { }

    [CustomEditor(typeof(ColliderEvent2D))]
    public sealed class ColliderEvent2DEditor : ColliderEventEditorBase { }
}
