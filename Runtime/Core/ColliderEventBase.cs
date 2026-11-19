using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JFCartier.Whenever
{
    /// <summary>
    /// Shared logic for ColliderEvent (zone-based) and ConditionWatcher (always-on). Holds the ordered
    /// Condition/Action lists, folds Conditions left-to-right with each condition's And/Or Operator,
    /// waits for Minimum Hold Time, runs Actions once - in list order, pausing at any Wait Action - then
    /// applies After Trigger.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class ColliderEventBase : MonoBehaviour
    {
        [SerializeField] private List<ConditionBase> conditions = new List<ConditionBase>();
        [SerializeField] private List<ActionBase> actions = new List<ActionBase>();
        [SerializeField] private List<ActionBase> exitActions = new List<ActionBase>();

        public List<ConditionBase> Conditions => conditions;
        public List<ActionBase> Actions => actions;
        public List<ActionBase> ExitActions => exitActions;

        [Tooltip("How many seconds the conditions must stay true, without interruption, before the Actions run. 0 fires as soon as they're true.")]
        public float holdTime = 0f;

        [Tooltip("What happens to this GameObject after the Actions have run.")]
        public AfterTrigger afterTrigger = AfterTrigger.DoNothing;

        [Tooltip("If true, logs to the console when this fires and when it exits.")]
        public bool debugLogging = true;

        [Tooltip("Optional. Applies a material to a target while the Conditions above aren't all met yet, and restores the original once they are (or once checking stops before they are) - a hint that an interaction is available.")]
        public bool showHintMaterial;

        [Tooltip("Used when Show Hint Material is on. Entering Objects highlights whatever triggered the zone; Specific Object always highlights the same Renderer below.")]
        public TargetMode hintTargetMode = TargetMode.SpecificObject;

        [Tooltip("Used when Hint Target Mode is Specific Object.")]
        public Renderer hintRenderer;

        [Tooltip("Applied to every material slot on the target Renderer while the Conditions aren't all met.")]
        public Material hintMaterial;

        [Tooltip("While the zone is occupied (Condition Watcher: while the Conditions stay true), runs the Actions again every Repeat Interval seconds, re-checking the Conditions each time. Only available when After Trigger is Do Nothing or Execute Exit Actions - the other modes remove or deactivate this GameObject after the first run.")]
        public bool repeat;

        [Tooltip("Seconds between the end of one run of the Actions and the start of the next. Used when Repeat is on.")]
        [Min(0.01f)]
        public float repeatInterval = 1f;

        [Tooltip("How many extra times to run the Actions after the first one. 0 keeps repeating for as long as the zone stays occupied. Used when Repeat is on.")]
        [Min(0)]
        public int maxRepeats = 0;

        [Tooltip("Only with After Trigger set to Execute Exit Actions and a Max Repeats above 0: runs the Exit Actions as soon as the last repeat has finished, instead of waiting for the object to leave. The cycle then ends as if it had left - the Exit Actions don't run a second time when it does.")]
        public bool exitActionsWhenRepeatsFinish;

        [Tooltip("Zone: the zone as a whole fires once, however many objects are inside. Each Object: every object that enters gets its own run - its own Hold Time, repeats and exit - so Actions set to Entering Objects act on each one in turn.")]
        public TriggerFor triggerFor = TriggerFor.Zone;

        /// <summary>
        /// The state of one run of the Conditions -> Hold Time -> Actions -> After Trigger cycle. A zone in
        /// Zone mode (and a Condition Watcher) has exactly one, m_Zone. In Each Object mode every object
        /// inside the zone has its own, so each one gets its own Hold Time, repeats and exit.
        /// </summary>
        private sealed class Cycle
        {
            // Each Object mode only: the object this cycle belongs to. The Zone cycle follows collidingObject.
            public GameObject Target;

            public bool Checking;
            public bool HasFired;
            public bool ActionsExecuting;
            public float HoldTimer;
            public Coroutine Sequence;

            // Repeat state: true while waiting for the next run, when that run is due, and how many runs
            // there have been since the zone was entered (or the Conditions became true).
            public bool RepeatPending;
            public float RepeatAt;
            public int RunCount;

            public bool FinishingRepeats;

            // The Renderer currently wearing the Hint Material on behalf of this cycle, if any.
            public Renderer HintRenderer;
        }

        private readonly Cycle m_Zone = new Cycle();
        private readonly List<Cycle> m_ObjectCycles = new List<Cycle>();

        private const float MinRepeatInterval = 0.01f;

        /// <summary>
        /// The GameObject that triggered this, when relevant. Set by the owning subclass in Zone mode. Passed
        /// to Conditions/Actions whose RequiresCollisionObjectData is true. In Each Object mode every
        /// object has its own and this is unused.
        /// </summary>
        protected GameObject collidingObject;

        /// <summary>
        /// Whether Update() should currently be folding the condition list. Controlled by BeginChecking()/
        /// StopChecking(), not polled - subclasses push state changes instead of Update() polling for them.
        /// </summary>
        protected bool Checking => m_Zone.Checking;

        /// <summary>
        /// True from the moment Actions have run (with AfterTrigger.ExecuteExitActions) until Exit Actions fire.
        /// </summary>
        protected bool HasFired => m_Zone.HasFired;

        /// <summary>
        /// True while the Action list is still running - between the first Action starting and the last
        /// one being reached, which lasts longer than a single frame once the list contains a Wait.
        /// </summary>
        protected bool IsExecutingActions => m_Zone.ActionsExecuting;

        /// <summary>
        /// True only while the Exit Actions are being run because a limited series of repeats just ended
        /// (as opposed to the object leaving). ConditionWatcher reads it from OnFiredAndReset().
        /// </summary>
        protected bool IsFinishingRepeats => m_Zone.FinishingRepeats;

        /// <summary>
        /// Whether this component can give every object its own run (Trigger For = Each Object). Only the
        /// zones can: a Condition Watcher has no objects.
        /// </summary>
        protected virtual bool SupportsEachObject => false;

        /// <summary>
        /// True when objects entering the zone each get a run of their own.
        /// </summary>
        protected bool EachObject => SupportsEachObject && triggerFor == TriggerFor.EachObject;

        /// <summary>
        /// After Trigger as actually applied. Set Inactive/Destroy/Destroy Parent would remove the whole
        /// zone for every object after the first one, so Each Object treats them as Do Nothing.
        /// </summary>
        protected AfterTrigger EffectiveAfterTrigger
        {
            get
            {
                if (EachObject &&
                    (afterTrigger == AfterTrigger.SetInactive ||
                     afterTrigger == AfterTrigger.Destroy ||
                     afterTrigger == AfterTrigger.DestroyParent))
                {
                    return AfterTrigger.DoNothing;
                }

                return afterTrigger;
            }
        }

        /// <summary>
        /// When true, Update() re-checks the condition fold while HasFired is true, and treats it flipping
        /// back to false as the exit trigger (used by ConditionWatcher, which has no physical exit event).
        /// ColliderEvent leaves this false - its exit is pushed from OnTriggerExit instead.
        /// </summary>
        protected virtual bool UseConditionFalseAsExit => false;

        /// <summary>
        /// Called at the start of every Update() while checking, in Zone mode. A zone overrides it to bring
        /// collidingObject up to date with who is actually inside (and to react if nobody is left), because
        /// Unity sends no exit event for an object that is destroyed or deactivated while overlapping a
        /// trigger. ConditionWatcher has no zone, so it leaves this empty.
        /// </summary>
        protected virtual void RefreshOccupants() { }

        protected virtual void Start()
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i]) conditions[i].OnAwake();
            }

            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i]) actions[i].OnAwake();
            }

            for (int i = 0; i < exitActions.Count; i++)
            {
                if (exitActions[i]) exitActions[i].OnAwake();
            }
        }

        protected virtual void OnDisable()
        {
            // Disabling this component stops its coroutines, including a running Action sequence. Without
            // this reset the "executing" flag would stay true forever and this could never fire again.
            ClearRunState(m_Zone);

            // Unity sends no exit event for a disabled trigger: every object's own cycle ends here.
            m_ObjectCycles.Clear();
        }

        protected virtual void Update()
        {
            if (EachObject)
            {
                UpdateObjectCycles();
                return;
            }

            if (!m_Zone.Checking)
            {
                return;
            }

            // May stop checking (e.g. the last object inside was destroyed), hence the second test.
            RefreshOccupants();

            if (!m_Zone.Checking)
            {
                return;
            }

            UpdateCycle(m_Zone);
        }

        /// <summary>
        /// One frame of one cycle: where it is in Conditions -> Hold Time -> Actions -> repeat/exit.
        /// </summary>
        private void UpdateCycle(Cycle c)
        {
            if (c.ActionsExecuting)
            {
                // Execute Exit Actions on a Condition Watcher: it has no physical exit event, so the
                // Conditions going false is the exit - including while the Action sequence is still
                // running (e.g. paused on a Wait), which cuts the sequence short.
                if (UseConditionFalseAsExit && EffectiveAfterTrigger == AfterTrigger.ExecuteExitActions && !EvaluateConditions(c))
                {
                    TriggerExit(c);
                }

                return;
            }

            if (c.HasFired)
            {
                if (UseConditionFalseAsExit && !EvaluateConditions(c))
                {
                    RunExitActionsAndReset(c);
                }
                else if (c.RepeatPending)
                {
                    TryRepeat(c);
                }

                return;
            }

            if (c.RepeatPending)
            {
                TryRepeat(c);
                return;
            }

            bool conditionsMet = EvaluateConditions(c);
            UpdateHintMaterial(c, conditionsMet);

            if (conditionsMet)
            {
                c.HoldTimer += Time.deltaTime;

                if (c.HoldTimer >= holdTime)
                {
                    c.Sequence = StartCoroutine(RunActionsCoroutine(c));
                }
            }
            else
            {
                c.HoldTimer = 0f;
            }
        }

        // ---- Each Object mode -------------------------------------------------------------------------

        /// <summary>
        /// Each Object mode: something entered the zone. Gives it a cycle of its own, which starts checking
        /// straight away. Does nothing if that object already has one.
        /// </summary>
        protected void ObjectEntered(GameObject go)
        {
            if (go == null || FindObjectCycle(go) != null) return;

            m_ObjectCycles.Add(new Cycle { Target = go, Checking = true });
        }

        /// <summary>
        /// Each Object mode: something left the zone. Ends its cycle the way leaving ends the zone's in Zone
        /// mode - Exit Actions run if its Actions had fired (Execute Exit Actions), a run still going
        /// otherwise carries on to its end.
        /// </summary>
        protected void ObjectExited(GameObject go)
        {
            Cycle c = FindObjectCycle(go);

            if (c != null)
            {
                EndObjectCycle(c);
            }
        }

        private Cycle FindObjectCycle(GameObject go)
        {
            for (int i = 0; i < m_ObjectCycles.Count; i++)
            {
                if (ReferenceEquals(m_ObjectCycles[i].Target, go)) return m_ObjectCycles[i];
            }

            return null;
        }

        private void EndObjectCycle(Cycle c)
        {
            m_ObjectCycles.Remove(c);

            if (EffectiveAfterTrigger == AfterTrigger.ExecuteExitActions && (c.HasFired || c.ActionsExecuting))
            {
                TriggerExit(c);
            }
            else
            {
                StopCycle(c);
            }
        }

        private void UpdateObjectCycles()
        {
            // Unity sends no exit event for an object that is destroyed or deactivated while inside, so
            // those cycles end here, as if the object had walked out.
            for (int i = m_ObjectCycles.Count - 1; i >= 0; i--)
            {
                if (i >= m_ObjectCycles.Count) continue;

                Cycle c = m_ObjectCycles[i];
                if (c.Target == null || !c.Target.activeInHierarchy)
                {
                    EndObjectCycle(c);
                }
            }

            // Re-reads Count each time: an Action may disable this component, which empties the list.
            for (int i = 0; i < m_ObjectCycles.Count; i++)
            {
                Cycle c = m_ObjectCycles[i];

                if (c.Checking)
                {
                    UpdateCycle(c);
                }
            }
        }

        // ---- Checking / exit ---------------------------------------------------------------------------

        /// <summary>
        /// Starts (or resumes) evaluating the condition list every frame.
        /// </summary>
        protected void BeginChecking()
        {
            m_Zone.Checking = true;
        }

        /// <summary>
        /// Stops evaluating the condition list and resets the hold timer. Also restores the Hint Material
        /// if one is currently showing - Update() won't run again to notice the Conditions changed, so
        /// leaving the zone before they were ever met would otherwise leave it stuck on.
        /// </summary>
        protected void StopChecking()
        {
            StopCycle(m_Zone);
        }

        private void StopCycle(Cycle c)
        {
            c.Checking = false;
            c.HoldTimer = 0f;
            c.RepeatPending = false;
            c.RunCount = 0;
            RevertHintMaterial(c);
        }

        /// <summary>
        /// Called by the zone subclasses' OnTriggerExit when the colliding object leaves while AfterTrigger
        /// is ExecuteExitActions, once Actions have run or are still running. If the Action list is still
        /// running (e.g. paused on a Wait), it is cut short here. Then runs Exit Actions and resets state.
        /// </summary>
        protected void TriggerExit()
        {
            TriggerExit(m_Zone);
        }

        private void TriggerExit(Cycle c)
        {
            if (EffectiveAfterTrigger != AfterTrigger.ExecuteExitActions)
            {
                return;
            }

            if (c.ActionsExecuting)
            {
                StopSequence(c);
                c.ActionsExecuting = false;
                c.HasFired = true;
            }

            if (c.HasFired)
            {
                RunExitActionsAndReset(c);
            }
        }

        /// <summary>
        /// Called after AfterTrigger.DoNothing (or Exit Actions) has finished resetting. ColliderEvent
        /// overrides this to require leaving and re-entering the zone before firing again; ConditionWatcher
        /// overrides it to require the Conditions to go false before it can fire again.
        /// </summary>
        protected virtual void OnFiredAndReset() { }

        // ---- Conditions --------------------------------------------------------------------------------

        protected bool EvaluateConditions()
        {
            return EvaluateConditions(m_Zone);
        }

        private bool EvaluateConditions(Cycle c)
        {
            GameObject target = TargetOf(c);
            bool result = true;

            for (int i = 0; i < conditions.Count; i++)
            {
                ConditionBase condition = conditions[i];
                if (!condition) continue;

                bool met = condition.RequiresCollisionObjectData
                    ? condition.Evaluate(target)
                    : condition.Evaluate();

                if (condition.Invert)
                {
                    met = !met;
                }

                if (i == 0)
                {
                    result = met;
                }
                else if (condition.Operator == LogicOperator.And)
                {
                    result = result && met;
                }
                else
                {
                    result = result || met;
                }
            }

            return result;
        }

        /// <summary>
        /// The object a cycle is about: whatever the zone currently follows in Zone mode, the cycle's own
        /// object in Each Object mode.
        /// </summary>
        private GameObject TargetOf(Cycle c)
        {
            return ReferenceEquals(c, m_Zone) ? collidingObject : c.Target;
        }

        // ---- Hint Material -----------------------------------------------------------------------------

        private void UpdateHintMaterial(Cycle c, bool conditionsMet)
        {
            if (!showHintMaterial) return;

            Renderer renderer = conditionsMet || hintMaterial == null ? null : ResolveHintRenderer(c);
            if (renderer == c.HintRenderer) return;

            Renderer previous = c.HintRenderer;
            c.HintRenderer = renderer;

            if (renderer != null) RendererMaterialOverride.Apply(renderer, hintMaterial);
            RestoreIfUnused(previous);
        }

        private Renderer ResolveHintRenderer(Cycle c)
        {
            if (hintTargetMode != TargetMode.EnteringObjects) return hintRenderer;

            GameObject target = TargetOf(c);
            return target != null ? target.GetComponent<Renderer>() : null;
        }

        private void RevertHintMaterial(Cycle c)
        {
            Renderer previous = c.HintRenderer;
            c.HintRenderer = null;
            RestoreIfUnused(previous);
        }

        // In Each Object mode several cycles can wear the Hint Material on the same Renderer (a Specific
        // Object target): it only comes off once none of them needs it any more.
        private void RestoreIfUnused(Renderer renderer)
        {
            if (renderer == null) return;

            if (ReferenceEquals(m_Zone.HintRenderer, renderer)) return;

            for (int i = 0; i < m_ObjectCycles.Count; i++)
            {
                if (ReferenceEquals(m_ObjectCycles[i].HintRenderer, renderer)) return;
            }

            RendererMaterialOverride.Restore(renderer);
        }

        // ---- Running the Actions -----------------------------------------------------------------------

        private void StopSequence(Cycle c)
        {
            if (c.Sequence == null) return;

            StopCoroutine(c.Sequence);
            c.Sequence = null;
        }

        /// <summary>
        /// Runs a list of Actions in order. Each Action starts as soon as the one above it has been started
        /// (they don't wait for each other to finish), except at a Wait, which pauses the rest of the list.
        /// A list without any Wait therefore runs entirely within a single call, same frame, as it always did.
        /// </summary>
        /// <param name="c">The cycle the Actions run for; its object is what Entering Objects Actions act on.</param>
        /// <param name="list">The Actions or Exit Actions to run.</param>
        /// <param name="waitForAllToFinish">
        /// If true, ends only once the longest-running Action started has finished. Needed before Set
        /// Inactive/Destroy, which would otherwise cut an Action off mid-animation.
        /// </param>
        private IEnumerator RunActionList(Cycle c, List<ActionBase> list, bool waitForAllToFinish)
        {
            // When the longest-running Action started so far will be done (absolute scaled time).
            float finishAt = Time.time;

            for (int i = 0; i < list.Count; i++)
            {
                ActionBase action = list[i];
                if (!action) continue;

                if (action.PausesSequence)
                {
                    float pause = action.GetPauseSeconds(Mathf.Max(0f, finishAt - Time.time));

                    if (pause > 0f)
                    {
                        yield return new WaitForSeconds(pause);
                    }

                    continue;
                }

                if (action.RequiresCollisionObjectData)
                {
                    action.Execute(TargetOf(c));
                }
                else
                {
                    action.Execute();
                }

                float end = Time.time + action.Duration;
                if (end > finishAt)
                {
                    finishAt = end;
                }
            }

            // Always yields at least once here (even when nothing is still running), as the old single
            // WaitForSeconds(longest Duration) did, so Set Inactive/Destroy still land a frame later.
            if (waitForAllToFinish)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, finishAt - Time.time));
            }
        }

        private IEnumerator RunActionsCoroutine(Cycle c)
        {
            c.ActionsExecuting = true;

            if (debugLogging)
            {
                Debug.Log(gameObject.name + " (Whenever) fired.", this);
            }

            AfterTrigger mode = EffectiveAfterTrigger;

            bool waitForAllToFinish = mode == AfterTrigger.SetInactive ||
                                      mode == AfterTrigger.Destroy ||
                                      mode == AfterTrigger.DestroyParent ||
                                      IsFinalRunWithFinale(c);

            // Driven by hand rather than with "yield return RunActionList(...)": that way a list with no
            // Wait still completes inside the very call that started this coroutine, instead of taking a
            // frame, exactly like the single loop that used to live here.
            IEnumerator sequence = RunActionList(c, actions, waitForAllToFinish);
            while (sequence.MoveNext())
            {
                yield return sequence.Current;
            }

            switch (mode)
            {
                case AfterTrigger.SetInactive:
                    gameObject.SetActive(false);
                    break;

                case AfterTrigger.Destroy:
                    Destroy(gameObject);
                    break;

                case AfterTrigger.DestroyParent:
                    Destroy(transform.parent != null ? transform.parent.gameObject : gameObject);
                    break;

                case AfterTrigger.DoNothing:
                    c.ActionsExecuting = false;

                    // With Repeat on, this stays armed (still Checking) for the next run instead of resetting.
                    if (!ScheduleRepeat(c))
                    {
                        ResetAfterFiring(c);
                    }

                    break;

                case AfterTrigger.ExecuteExitActions:
                    c.ActionsExecuting = false;
                    c.HasFired = true;
                    ScheduleRepeat(c);
                    break;
            }
        }

        private void RunExitActionsAndReset(Cycle c)
        {
            // Actions are shared by every object's cycle: cancelling them would also cut off what another
            // object's run is still doing, so in Each Object mode that only happens for the last one.
            if (!AnotherCycleIsRunning(c))
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    if (actions[i]) actions[i].CancelExecution();
                }
            }

            if (debugLogging)
            {
                Debug.Log(gameObject.name + " (Whenever) exited.", this);
            }

            // Exit Actions can contain a Wait too. A list without one still runs completely inside this
            // call; with one, the rest of it carries on after the reset below.
            StartCoroutine(RunActionList(c, exitActions, false));

            ResetAfterFiring(c);
        }

        private bool AnotherCycleIsRunning(Cycle c)
        {
            for (int i = 0; i < m_ObjectCycles.Count; i++)
            {
                Cycle other = m_ObjectCycles[i];

                if (!ReferenceEquals(other, c) && (other.HasFired || other.ActionsExecuting)) return true;
            }

            return false;
        }

        // ---- Repeat ------------------------------------------------------------------------------------

        /// <summary>
        /// Repeat only makes sense when this GameObject survives its own firing: Set Inactive/Destroy remove
        /// it after the first run.
        /// </summary>
        private bool RepeatApplies =>
            repeat && (EffectiveAfterTrigger == AfterTrigger.DoNothing || EffectiveAfterTrigger == AfterTrigger.ExecuteExitActions);

        /// <summary>
        /// Called each time a run of the Actions has just finished. If another run is due, starts waiting for
        /// it and returns true. Returns false when Repeat is off, the Max Repeats limit has been reached, or
        /// the zone was left while the run was still going - in which case the caller carries on exactly as
        /// it would without Repeat.
        /// </summary>
        private bool ScheduleRepeat(Cycle c)
        {
            c.RunCount++;

            // Checking is false when the zone was left mid-run: there's nothing left to repeat for.
            if (!RepeatApplies || !c.Checking) return false;

            // RunCount counts the first run too, so Max Repeats extra runs means RunCount up to Max + 1.
            if (maxRepeats > 0 && c.RunCount > maxRepeats)
            {
                // That was the last run of the series.
                if (EffectiveAfterTrigger == AfterTrigger.ExecuteExitActions && exitActionsWhenRepeatsFinish)
                {
                    FinishRepeatsWithExitActions(c);
                }

                return false;
            }

            ResetElements();
            c.RepeatAt = Time.time + Mathf.Max(repeatInterval, MinRepeatInterval);
            c.RepeatPending = true;
            return true;
        }

        /// <summary>
        /// True for the last run of a limited series that ends with the Exit Actions. Its Actions are waited
        /// on until they finish first - otherwise the Exit Actions would cut off whatever is still running,
        /// since running them cancels the Actions.
        /// </summary>
        private bool IsFinalRunWithFinale(Cycle c) =>
            RepeatApplies
            && exitActionsWhenRepeatsFinish
            && EffectiveAfterTrigger == AfterTrigger.ExecuteExitActions
            && maxRepeats > 0
            && c.RunCount >= maxRepeats;

        /// <summary>
        /// Runs the Exit Actions and resets, as if the object had just left, because the series of repeats
        /// is over. The flag lets ConditionWatcher tell this apart from a normal exit.
        /// </summary>
        private void FinishRepeatsWithExitActions(Cycle c)
        {
            c.FinishingRepeats = true;
            RunExitActionsAndReset(c);
            c.FinishingRepeats = false;
        }

        /// <summary>
        /// While a repeat is pending: once it's due and the Conditions are true, runs the Actions again.
        /// Hold Time only applies to the first run, so a repeat starts straight away.
        /// </summary>
        private void TryRepeat(Cycle c)
        {
            if (Time.time < c.RepeatAt) return;

            if (!EvaluateConditions(c))
            {
                // A Condition Watcher has no zone, so its Conditions going false is what ends a repeating
                // cycle - the same role leaving plays for a Collider Event, which instead just waits here
                // for them to be true again while the object is still inside.
                if (UseConditionFalseAsExit && !c.HasFired)
                {
                    ResetAfterFiring(c);
                }

                return;
            }

            c.RepeatPending = false;
            c.Sequence = StartCoroutine(RunActionsCoroutine(c));
        }

        private void ResetElements()
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i]) conditions[i].ResetState();
            }

            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i]) actions[i].ResetState();
            }
        }

        private void ClearRunState(Cycle c)
        {
            c.ActionsExecuting = false;
            c.Sequence = null;
            c.RepeatPending = false;
            c.RunCount = 0;
        }

        private void ResetAfterFiring(Cycle c)
        {
            c.HasFired = false;
            c.HoldTimer = 0f;
            c.RepeatPending = false;
            c.RunCount = 0;

            ResetElements();

            if (ReferenceEquals(c, m_Zone))
            {
                OnFiredAndReset();
            }
            else
            {
                // Each Object: this object has to leave and come back before it can fire again.
                StopCycle(c);
            }
        }
    }
}
