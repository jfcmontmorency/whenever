using UnityEngine;

namespace JFCartier.Whenever
{
    /// <summary>
    /// Pauses the Action list. Everything above a Wait runs first; the Actions below it only run once the
    /// wait is over. Put one between two Actions to space them out in time, e.g. "open the door, wait
    /// 2 seconds, play a sound". It does nothing on its own.
    /// </summary>
    public sealed class WaitAction : ActionBase
    {
        public enum WaitMode
        {
            Seconds,
            UntilPreviousActionsFinish,
        }

        [Tooltip("Seconds waits a fixed time. Until Previous Actions Finish waits for the longest-running Action above this one (e.g. a Transform animated over 2 seconds), however long that turns out to be.")]
        public WaitMode mode = WaitMode.Seconds;

        [Tooltip("How long to wait, in seconds. Used when Mode is Seconds.")]
        [Min(0f)]
        public float seconds = 1f;

        public override bool PausesSequence => true;

        public override float GetPauseSeconds(float secondsUntilPreviousActionsFinish)
        {
            return mode == WaitMode.Seconds ? Mathf.Max(0f, seconds) : secondsUntilPreviousActionsFinish;
        }

        // The sequence reads PausesSequence/GetPauseSeconds instead of calling this.
        public override void Execute() { }

        public override bool TryGetValidationWarning(out string message)
        {
            if (mode == WaitMode.Seconds && seconds <= 0f)
            {
                message = "Waiting 0 seconds does nothing - set Seconds above 0, or remove this Wait.";
                return true;
            }

            message = null;
            return false;
        }
    }
}
