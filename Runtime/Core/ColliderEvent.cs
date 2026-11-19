using UnityEngine;

namespace JFCartier.Whenever
{
    /// <summary>
    /// Attach any native Collider (mark it "Is Trigger") to a GameObject alongside this component to build
    /// a zone: when something enters, the Conditions are checked; once they're met (and stay met for
    /// Hold Time) the Actions run. For a version that isn't tied to a physical zone, use
    /// ConditionWatcher instead.
    /// </summary>
    [AddComponentMenu("Whenever/Collider Event")]
    [RequireComponent(typeof(Collider))]
    public sealed class ColliderEvent : ColliderEventBase
    {
        [Tooltip("Only objects with one of these tags can trigger this zone. Leave empty to allow anything with a Collider.")]
        public string[] requiredTags = System.Array.Empty<string>();

        [Tooltip("The colour of the zone gizmo in the editor.")]
        public Color debugColor = new Color(1f, 0.4f, 0f, 0.3f);

        private Collider m_Collider;

        // Every object currently inside the zone, in the order they entered. The zone stays occupied until
        // the last one has left (see ZoneOccupants).
        private readonly ZoneOccupants m_Occupants = new ZoneOccupants();

        protected override void Start()
        {
            base.Start();
            m_Collider = GetComponent<Collider>();
        }

        // Only Collider Event and Collider Event 2D can give each object its own run (Trigger For).
        protected override bool SupportsEachObject => true;

        protected override void OnDisable()
        {
            base.OnDisable();

            // Unity sends no OnTriggerExit for a disabled trigger - forget who was inside rather than
            // keep entries that may be stale by the time it is enabled again.
            m_Occupants.Clear();
        }

        protected override void OnFiredAndReset()
        {
            // Require the object to leave and re-enter the zone before this can fire again.
            StopChecking();
        }

        // Objects destroyed or deactivated while inside send no OnTriggerExit, so they're dropped here.
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

        private void OnTriggerEnter(Collider other)
        {
            if (!PassesTagFilter(other)) return;

            if (EachObject)
            {
                ObjectEntered(other.gameObject);
                return;
            }

            m_Occupants.Enter(other.gameObject);
            collidingObject = other.gameObject;
            BeginChecking();
        }

        private void OnTriggerExit(Collider other)
        {
            if (EachObject)
            {
                ObjectExited(other.gameObject);
                return;
            }

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

        private bool PassesTagFilter(Collider other)
        {
            if (requiredTags == null || requiredTags.Length == 0) return true;

            for (int i = 0; i < requiredTags.Length; i++)
            {
                if (other.CompareTag(requiredTags[i])) return true;
            }

            return false;
        }

        // A hair larger than the real collider so the translucent gizmo doesn't z-fight with a same-sized
        // visible mesh on the object. Proportional rather than a fixed add-on, so it stays negligible
        // regardless of the object's actual size.
        private const float GizmoInflation = 1.0001f;

        private void OnDrawGizmos()
        {
            if (m_Collider == null) m_Collider = GetComponent<Collider>();
            if (m_Collider == null) return;

            Gizmos.color = debugColor;

            switch (m_Collider)
            {
                case BoxCollider box:
                    // Bounds is always an axis-aligned world-space box, so it can't follow the object's
                    // rotation. Drawing in the collider's own local space (via Gizmos.matrix) can.
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(box.center, box.size * GizmoInflation);
                    Gizmos.matrix = Matrix4x4.identity;
                    break;

                case SphereCollider sphere:
                    // Mirrors Unity's own physics behaviour for SphereCollider: non-uniform scale is
                    // ignored, only the largest axis is used.
                    Vector3 sphereLossyScale = transform.lossyScale;
                    float sphereScale = Mathf.Max(Mathf.Max(sphereLossyScale.x, sphereLossyScale.y), sphereLossyScale.z);
                    Gizmos.DrawSphere(transform.TransformPoint(sphere.center), sphere.radius * sphereScale * GizmoInflation);
                    break;

                case CapsuleCollider capsule:
                    DrawCapsuleGizmo(capsule);
                    break;

                case MeshCollider meshCollider when meshCollider.sharedMesh != null:
                    Gizmos.DrawMesh(meshCollider.sharedMesh, transform.position, transform.rotation, transform.lossyScale * GizmoInflation);
                    break;

                default:
                    // Uncommon collider type (e.g. TerrainCollider) - fall back to the bounding box.
                    Bounds bounds = m_Collider.bounds;
                    Gizmos.DrawCube(bounds.center, bounds.size * GizmoInflation);
                    break;
            }
        }

        private Mesh m_CapsuleMesh;
        private const float CapsulePrimitiveHeight = 2f;
        private const float CapsulePrimitiveRadius = 0.5f;

        private void DrawCapsuleGizmo(CapsuleCollider capsule)
        {
            if (m_CapsuleMesh == null)
            {
                // Resources.GetBuiltinResource's mesh names are undocumented/internal and have drifted
                // between Unity versions, so grab the primitive's mesh the reliable way instead.
                GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                m_CapsuleMesh = temp.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(temp);
            }

            // Mirrors Unity's own physics behaviour for CapsuleCollider under scale: the axis picked by
            // "direction" drives the height, the other two axes' largest value drives the radius.
            Vector3 scale = transform.lossyScale;
            Quaternion axisRotation;
            float heightScale, radiusScale;
            switch (capsule.direction)
            {
                case 0:
                    axisRotation = Quaternion.Euler(0f, 0f, 90f);
                    heightScale = Mathf.Abs(scale.x);
                    radiusScale = Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                    break;
                case 2:
                    axisRotation = Quaternion.Euler(90f, 0f, 0f);
                    heightScale = Mathf.Abs(scale.z);
                    radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                    break;
                default:
                    axisRotation = Quaternion.identity;
                    heightScale = Mathf.Abs(scale.y);
                    radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                    break;
            }

            float radius = capsule.radius * radiusScale * GizmoInflation;
            float height = Mathf.Max(capsule.height * heightScale, radius * 2f) * GizmoInflation;

            Vector3 meshScale = new Vector3(radius / CapsulePrimitiveRadius, height / CapsulePrimitiveHeight, radius / CapsulePrimitiveRadius);
            Gizmos.DrawMesh(m_CapsuleMesh, transform.TransformPoint(capsule.center), transform.rotation * axisRotation, meshScale);
        }
    }
}
