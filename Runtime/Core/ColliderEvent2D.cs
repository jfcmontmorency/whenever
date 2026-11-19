using System.Collections.Generic;
using UnityEngine;

namespace JFCartier.Whenever
{
    /// <summary>
    /// The 2D counterpart of ColliderEvent: attach any Collider2D (mark it "Is Trigger") to a GameObject
    /// alongside this component to build a zone. When something enters, the Conditions are checked; once
    /// they're met (and stay met for Hold Time) the Actions run. Everything else - Conditions, Actions,
    /// Hold Time, After Trigger, Hint Material - works exactly like ColliderEvent.
    /// </summary>
    [AddComponentMenu("Whenever/Collider Event 2D")]
    [RequireComponent(typeof(Collider2D))]
    public sealed class ColliderEvent2D : ColliderEventBase
    {
        [Tooltip("Only objects with one of these tags can trigger this zone. Leave empty to allow anything with a Collider2D.")]
        public string[] requiredTags = System.Array.Empty<string>();

        [Tooltip("The colour of the zone gizmo in the editor.")]
        public Color debugColor = new Color(1f, 0.4f, 0f, 0.3f);

        private Collider2D m_Collider;

        // Every object currently inside the zone, in the order they entered. The zone stays occupied until
        // the last one has left (see ZoneOccupants).
        private readonly ZoneOccupants m_Occupants = new ZoneOccupants();

        protected override void Start()
        {
            base.Start();
            m_Collider = GetComponent<Collider2D>();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Unity sends no OnTriggerExit2D for a disabled trigger - forget who was inside rather than
            // keep entries that may be stale by the time it is enabled again.
            m_Occupants.Clear();
        }

        protected override void OnFiredAndReset()
        {
            // Require the object to leave and re-enter the zone before this can fire again.
            StopChecking();
        }

        // Objects destroyed or deactivated while inside send no OnTriggerExit2D, so they're dropped here.
        // The object handed to Entering Objects Actions is always the latest one still inside; if nobody
        // is left, the zone empties just as if the last one had walked out.
        protected override void RefreshOccupants()
        {
            GameObject latest = m_Occupants.Latest;

            if (latest != null)
            {
                collidingObject = latest;
            }
            else
            {
                ZoneEmptied();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!PassesTagFilter(other)) return;

            m_Occupants.Enter(other.gameObject);
            collidingObject = other.gameObject;
            BeginChecking();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!m_Occupants.Contains(other.gameObject)) return;

            m_Occupants.Exit(other.gameObject);

            if (!m_Occupants.IsEmpty)
            {
                // Someone is still inside, so the zone stays occupied; only the object handed to Entering
                // Objects Actions changes, to the latest one left.
                collidingObject = m_Occupants.Latest;
                return;
            }

            // The last one has left: any Exit Actions that target the entering object act on this one.
            collidingObject = other.gameObject;
            ZoneEmptied();
        }

        private void ZoneEmptied()
        {
            if (afterTrigger == AfterTrigger.ExecuteExitActions && (HasFired || IsExecutingActions))
            {
                TriggerExit();
            }
            else
            {
                StopChecking();
            }
        }

        private bool PassesTagFilter(Collider2D other)
        {
            if (requiredTags == null || requiredTags.Length == 0) return true;

            for (int i = 0; i < requiredTags.Length; i++)
            {
                if (other.CompareTag(requiredTags[i])) return true;
            }

            return false;
        }

        // A hair larger than the real collider so the translucent gizmo doesn't z-fight with a same-sized
        // visible sprite on the object. Proportional rather than a fixed add-on, so it stays negligible
        // regardless of the object's actual size.
        private const float GizmoInflation = 1.0001f;

        // 2D zones are flat: the gizmo is drawn as a very thin slab rather than a volume.
        private const float GizmoDepth = 0.01f;

        private readonly List<Vector2> m_PathPoints = new List<Vector2>();

        private void OnDrawGizmos()
        {
            if (m_Collider == null) m_Collider = GetComponent<Collider2D>();
            if (m_Collider == null) return;

            Gizmos.color = debugColor;

            switch (m_Collider)
            {
                case BoxCollider2D box:
                {
                    // Drawn in the collider's own local space (via Gizmos.matrix) so it follows the
                    // object's rotation and scale.
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(box.offset, new Vector3(box.size.x * GizmoInflation, box.size.y * GizmoInflation, GizmoDepth));
                    Gizmos.matrix = Matrix4x4.identity;
                    break;
                }

                case CircleCollider2D circle:
                {
                    // Mirrors Unity's own physics behaviour for CircleCollider2D: non-uniform scale is
                    // ignored, only the largest axis is used.
                    Vector3 circleLossyScale = transform.lossyScale;
                    float circleScale = Mathf.Max(Mathf.Abs(circleLossyScale.x), Mathf.Abs(circleLossyScale.y));

                    Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(circle.offset), transform.rotation, new Vector3(1f, 1f, GizmoDepth));
                    Gizmos.DrawSphere(Vector3.zero, circle.radius * circleScale * GizmoInflation);
                    Gizmos.matrix = Matrix4x4.identity;
                    break;
                }

                case PolygonCollider2D polygon:
                {
                    DrawPolygonOutline(polygon);
                    break;
                }

                default:
                {
                    // Capsule, Edge, Composite and any other Collider2D: fall back to the bounding box.
                    Bounds bounds = m_Collider.bounds;
                    Gizmos.DrawCube(bounds.center, new Vector3(bounds.size.x * GizmoInflation, bounds.size.y * GizmoInflation, GizmoDepth));
                    break;
                }
            }
        }

        // A polygon can't be drawn as a filled shape with Gizmos, so its outline is drawn instead (fully
        // opaque - the translucent fill colour would be hard to see on a thin line).
        private void DrawPolygonOutline(PolygonCollider2D polygon)
        {
            Color outline = debugColor;
            outline.a = 1f;
            Gizmos.color = outline;

            for (int path = 0; path < polygon.pathCount; path++)
            {
                polygon.GetPath(path, m_PathPoints);

                for (int i = 0; i < m_PathPoints.Count; i++)
                {
                    Vector3 from = transform.TransformPoint(polygon.offset + m_PathPoints[i]);
                    Vector3 to = transform.TransformPoint(polygon.offset + m_PathPoints[(i + 1) % m_PathPoints.Count]);
                    Gizmos.DrawLine(from, to);
                }
            }
        }
    }
}
