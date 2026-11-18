using UnityEngine;

namespace JFCartier.Whenever
{
    /// <summary>
    /// The 2D counterpart of RigidbodyAction: changes one or more properties on a Rigidbody2D. Every
    /// property has its own on/off toggle, so only the ones you check are touched.
    /// </summary>
    public sealed class Rigidbody2DAction : ActionBase
    {
        public TargetMode targetMode = TargetMode.SpecificObject;

        [Tooltip("The Rigidbody2D to modify. Used when Target Mode is Specific Object.")]
        public Rigidbody2D target;

        public bool modifyBodyType;
        public RigidbodyType2D bodyType = RigidbodyType2D.Dynamic;

        public bool modifyMass;
        public float mass = 1f;

        public bool modifyLinearDamping;
        public float linearDamping;

        public bool modifyAngularDamping;
        public float angularDamping;

        public bool modifyGravityScale;
        public float gravityScale = 1f;

        public override bool RequiresCollisionObjectData => targetMode == TargetMode.EnteringObjects;

        public override void Execute()
        {
            Apply(target);
        }

        public override void Execute(GameObject collidingObject)
        {
            Apply(collidingObject != null ? collidingObject.GetComponent<Rigidbody2D>() : null);
        }

        private void Apply(Rigidbody2D rb)
        {
            if (rb == null) return;

            if (modifyBodyType) rb.bodyType = bodyType;
            if (modifyMass) rb.mass = mass;
            if (modifyLinearDamping) rb.linearDamping = linearDamping;
            if (modifyAngularDamping) rb.angularDamping = angularDamping;
            if (modifyGravityScale) rb.gravityScale = gravityScale;
        }
    }
}
