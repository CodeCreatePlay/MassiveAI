namespace MassiveAI
{	
    public class StateMachine<T>
    {
		public static readonly int INVALID_STATE_ID = -1;
        public static readonly int FINAL_STATE_ID   = 1010;
		
        private System.Collections.Generic.Dictionary<int, StateBase<T>> statesmap;

        private StateBase<T> currentState;

        private int currentStateIdx = -1;
        private int nextStateIdx    = -1;
        private int lastStateIdx    = -1;

        private StateBase<T> finalState; // final should be the one where entiry stops to exist
		private System.Func<bool> finalStateTrigger = null; // global state condition callback, sm will switch
		// to global state as long as this will return true
		
        private bool hasFinalState  = false;
        private bool isInFinalState = false;

        public T Owner  { get; private set; }
        public StateBase<T> CurrentState { get { return currentState; } }
        public int CurrentStateIDX { get { return currentStateIdx; } }
        public System.Collections.Generic.Dictionary<int, StateBase<T>> StatesMap
        {
            get 
            {
                if (statesmap == null) statesmap = new();
                return statesmap;
            }
        }
		
        // others
        private bool shouldTransition = false;
		private Stopwatch stopWatch;


        // constructor
        public StateMachine(T owner)
        {
            this.Owner = owner;
			stopWatch = new();
        }

        public void AddState(StateBase<T> newState, int idx)
        {
            if (!CanAddState(state: newState, idx))
                return;

            StatesMap[idx] = newState;
        }

        public bool AddState(
			string stateName,
			int idx,
			System.Action updateCallback,
			System.Func<bool> enterCallback = null, System.Func<bool> exitCallback = null)
        {
            StateBase<T> tempState = new(Owner, stateName, updateCallback, enterCallback, exitCallback);

            if (!CanAddState(tempState, idx))
                return false;

            AddState(tempState, idx);
            return true;
        }

        public void AddFinalState(StateBase<T> state, System.Func<bool> triggerCallback)
        {
            if (state == null || triggerCallback == null)
            {
                UnityEngine.Debug.LogWarning("Failed to add global state.");
                return;
            }

            finalState = state;
            finalStateTrigger = triggerCallback;			
			StatesMap[FINAL_STATE_ID] = finalState;
			
            hasFinalState = true;
        }

        public StateBase<T> AddFinalState(
			string stateName,
            StateBase<T> state,
			System.Func<bool> triggerCallback,
			System.Action updateCallback,
            System.Func<bool> enterCallback = null, System.Func<bool> exitCallback = null)
        {
            if (state == null || triggerCallback == null)
            {
                UnityEngine.Debug.LogWarning("Failed to add global state, state instance or triggerCallback is null.");
                return null;
            }

			finalState = new(Owner, stateName, updateCallback, enterCallback, exitCallback);
            finalStateTrigger = triggerCallback;
			StatesMap[FINAL_STATE_ID] = finalState;
			
            hasFinalState = true;
            return finalState;
        }

        private bool CanAddState(StateBase<T> state, int idx)
        {
			if (state == null)
            {
                UnityEngine.Debug.LogWarningFormat("Unable to add state '{0}', value is null.", nameof(state));
                return false;
            }
			
            if(idx == INVALID_STATE_ID || idx == FINAL_STATE_ID)
            {
                UnityEngine.Debug.LogWarningFormat("Unable to add state '{0}', invalid state idx", nameof(state));
                return false;
            }

            if (StatesMap.ContainsKey(idx))
            {
                UnityEngine.Debug.LogWarningFormat("Unable to add state '{0}', key already exists.", idx);
                return false;
            }

            return true;
        }

        public void SwitchState(int idx)
        {			
			if (idx == currentStateIdx ||
				idx == INVALID_STATE_ID || 
				idx == FINAL_STATE_ID)
				return; 
			
            // make sure state exists
            if (!StatesMap.ContainsKey(idx))
            {
                UnityEngine.Debug.LogWarningFormat("Unable to switch state; state {0} does not exits.!", nameof(idx));
                return;
            }

            this.nextStateIdx = idx;
            shouldTransition = true;
        }

        /// <summary>
        /// Switches to the last state if it exists, it is up to the user to
        /// make sure last state exists using 'CanSwitchToLastState' function call.
        /// </summary>
        public void SwitchToLastState()
        {
            if (lastStateIdx != INVALID_STATE_ID)
                SwitchState(lastStateIdx);
        }
		
		private bool enterMethodExec_ = false;
		private bool exitMethodExec_  = false;
		
        private void Transition()
        {			
			// 1.
			if (nextStateIdx != FINAL_STATE_ID && !exitMethodExec_) {
				stopWatch.Begin();
				exitMethodExec_ = true;
			}
			
            if (nextStateIdx != FINAL_STATE_ID && currentState != null && !currentState.Exit()) {
				return;
			}
			
			// 2.
			if (nextStateIdx != currentStateIdx) {

				lastStateIdx    = currentStateIdx;
				currentStateIdx = nextStateIdx;
				currentState    = StatesMap[currentStateIdx];
			}
			
			// 3.
			if (!enterMethodExec_) {
				stopWatch.Begin();
				enterMethodExec_ = true;
			}
            if (!currentState.Enter())
                return;
			
			// 4.
            shouldTransition = false;
	
			enterMethodExec_ = false;
			exitMethodExec_  = false;
			
			stopWatch.Begin();
        }

        public void Update()
        {
            if (hasFinalState && finalStateTrigger())
            {
                if(!isInFinalState)
                {
                    // same logic as in switch state
                    isInFinalState = true;
                    nextStateIdx = FINAL_STATE_ID;
                    shouldTransition = true;
                }
            }

            if (currentStateIdx != INVALID_STATE_ID && !shouldTransition)
			{
				currentState.Update();
			}
            else if(shouldTransition) 
			{
                Transition();
			}
        }
		
		public float Duration() => stopWatch.GetSeconds();
		
		public bool IsInTransition() => !exitMethodExec_;
		
		public string GetExecInfo()
		{ 
			if(enterMethodExec_)
				return string.Format("CurrentState: {0} Method: Enter",  currentState);
			else if(exitMethodExec_)
				return string.Format("CurrentState: {0} Method: Exit",   currentState);
			else
				return string.Format("CurrentState: {0} Method: Update", currentState);
		}
    }
}
