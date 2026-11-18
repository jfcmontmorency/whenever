using UnityEngine;

namespace JFCartier.Whenever
{
    /// <summary>
    /// Base class for all Actions. Inherit from this to create a new action type -
    /// it will automatically appear in the "Add Action" list in the Inspector.
    /// </summary>
    [AddComponentMenu("")]
    public abstract class ActionBase : MonoBehaviour
    {
        /// <summary>
        /// If true, Execute(GameObject) is called instead of Execute() so this action can use the
        /// GameObject that triggered the zone. Only meaningful under ColliderEvent, not ConditionWatcher.
        /// </summary>
        public virtual bool RequiresCollisionObjectData => false;

        /// <summary>
        /// How long (in seconds) this action takes to finish. Used to delay AfterTrigger until the
        /// longest-running action among all Actions has completed. Leave as 0 for instant actions.
        /// </summary>
        public virtual float Duration => 0f;

        /// <summary>
        /// If true, this Action doesn't do anything itself when its turn comes: it pauses the rest of the
        /// Action list instead (see GetPauseSeconds). Only WaitAction needs this - every other Action leaves
        /// it false and just runs.
        /// </summary>
        public virtual bool PausesSequence => false;

        /// <summary>
        /// Only called when PausesSequence is true, in place of Execute(). Returns how many seconds the
        /// Actions below this one should wait before they run.
        /// </summary>
        /// <param name="secondsUntilPreviousActionsFinish">
        /// How much longer the longest-running Action started so far (above this one) still has to run.
        /// </param>
        public virtual float GetPauseSeconds(float secondsUntilPreviousActionsFinish) => 0f;

        /// <summary>
        /// Runs this action.
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Same as Execute(), but with access to the GameObject that triggered the zone.
        /// Only called when RequiresCollisionObjectData is true.
        /// </summary>
        public virtual void Execute(GameObject collidingObject) => Execute();

        /// <summary>
        /// Called once when the game starts. Use this for one-time setup.
        /// </summary>
        public virtual void OnAwake() { }

        /// <summary>
        /// Called if this action is still animating/running (Duration > 0) when Exit Actions fire early.
        /// Override to stop any coroutine or in-progress work this action started.
        /// </summary>
        public virtual void CancelExecution() { }

        /// <summary>
        /// Called when the owning ColliderEvent/ConditionWatcher resets after firing (AfterTrigger.DoNothing).
        /// </summary>
        public virtual void ResetState() { }

        /// <summary>
        /// Optional inline validation shown in the Inspector as a warning box. Return true and set message
        /// when something required is missing (e.g. an empty reference field).
        /// </summary>
        public virtual bool TryGetValidationWarning(out string message)
        {
            message = null;
            return false;
        }
    }
}
