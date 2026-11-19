using System.Collections.Generic;
using UnityEngine;

namespace JFCartier.Whenever
{
    /// <summary>
    /// Keeps track of every object currently inside a zone, in the order they entered. ColliderEvent and
    /// ColliderEvent2D use it so a zone stays occupied until the LAST object has left, instead of following
    /// only the most recent one - and so the object handed to Entering Objects Actions is always the
    /// latest one still inside.
    ///
    /// Unity sends no exit event for an object that is destroyed or deactivated while overlapping a trigger,
    /// so such objects are dropped as soon as they're looked at (see Latest and IsEmpty).
    /// </summary>
    internal sealed class ZoneOccupants
    {
        private readonly List<GameObject> m_Objects = new List<GameObject>();

        /// <summary>
        /// The object that entered most recently and is still inside, or null if the zone is empty.
        /// </summary>
        public GameObject Latest
        {
            get
            {
                Prune();
                return m_Objects.Count > 0 ? m_Objects[m_Objects.Count - 1] : null;
            }
        }

        public bool IsEmpty
        {
            get
            {
                Prune();
                return m_Objects.Count == 0;
            }
        }

        public bool Contains(GameObject go)
        {
            return IndexOf(go) >= 0;
        }

        /// <summary>
        /// Adds an object (it becomes the latest). Does nothing if it is already tracked.
        /// </summary>
        public void Enter(GameObject go)
        {
            if (go == null || Contains(go)) return;

            m_Objects.Add(go);
        }

        public void Exit(GameObject go)
        {
            int index = IndexOf(go);
            if (index >= 0) m_Objects.RemoveAt(index);
        }

        public void Clear()
        {
            m_Objects.Clear();
        }

        // Compared by reference, not with UnityEngine.Object's ==: two destroyed objects both compare equal
        // to null there, so they would also compare equal to each other.
        private int IndexOf(GameObject go)
        {
            for (int i = 0; i < m_Objects.Count; i++)
            {
                if (ReferenceEquals(m_Objects[i], go)) return i;
            }

            return -1;
        }

        private void Prune()
        {
            for (int i = m_Objects.Count - 1; i >= 0; i--)
            {
                GameObject go = m_Objects[i];

                if (go == null || !go.activeInHierarchy)
                {
                    m_Objects.RemoveAt(i);
                }
            }
        }
    }
}
