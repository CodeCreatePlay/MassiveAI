using UnityEngine;
using UnityEngine.InputSystem;

namespace MassiveAI.StateMachineDemo
{
    public class PatrolState<T> : StateBase<T> where T : StateMachineDemo
    {
		private float patrolStateDuration = 1.5f;
		
		
        public PatrolState(T owner) : base(owner) {}
		
		protected override bool OnEnter() {
			
			// ** enter logic **
			return true;
		}
		
        protected override void OnUpdate()
        {			
			if(Owner.stateMachine.Duration() > patrolStateDuration) 
				Owner.stateMachine.SwitchState(StateMachineDemo.COMBAT_STATE);
        }
		
		protected override bool OnExit() {
			
			// ** exit logic ** 
			return true;
		}
    }

    public class CombatState<T> : StateBase<T> where T : StateMachineDemo
    {
		private float prepareDuration  = 1.5f;
		private float combatDuration   = 2f;
		private float coolDownDuration = 1f;
		
		
        public CombatState(T owner) : base(owner) {}
		
		protected override bool OnEnter()
		{						
			if(Owner.stateMachine.Duration() < prepareDuration)
				return false;
			
			return true;
		}
		
		protected override void OnUpdate() {

			if(Owner.stateMachine.Duration() > combatDuration)
				Owner.stateMachine.SwitchState(StateMachineDemo.IDLE_STATE);
		}
		
		protected override bool OnExit() {
			
			if(Owner.stateMachine.Duration() < coolDownDuration)
				return false;
			
			return true;
		}
    }

    public class DeathState<T> : StateBase<T>
    {
        public DeathState(T owner) : base(owner) {}

		protected override void OnUpdate()
        {
			
        }
    }
	
	
    public class StateMachineDemo : MonoBehaviour
    {
        // StateMachine instance
        public StateMachine<StateMachineDemo> stateMachine;
		
        // States instances
		private PatrolState<StateMachineDemo> patrol_state;
        private CombatState<StateMachineDemo> combat_state;
		private DeathState<StateMachineDemo>  death_state;
		
		// Uninque identifiers for each state
		public static readonly int IDLE_STATE   = 0;
        public static readonly int PATROL_STATE = 1;
		public static readonly int COMBAT_STATE = 2;
		
        // Basic character stats
        private int current_health = 100;
		
		// Debug
		[SerializeField] private string info;
		
		// Other
		private InputAction spaceAction;


        private void Start()
        {
            // StateMachine instance
            stateMachine = new(this);

            // Create State instances
			patrol_state = new(this);
            combat_state = new(this);
            death_state  = new(this);

            // Add states to the StateMachine
            stateMachine.AddState(
								"IdleState",
								IDLE_STATE,
								OnIdleUpdate,
								enterCallback: OnIdleEnter, exitCallback: OnIdleExit);
			
            stateMachine.AddState(patrol_state, PATROL_STATE);
			stateMachine.AddState(combat_state, COMBAT_STATE);

            // Add a global state for death
            stateMachine.AddFinalState(death_state, triggerCallback: IsAlive);

            // Switch to a default state
            stateMachine.SwitchState(IDLE_STATE);
			
			// Other
			// Initialize input system
			spaceAction = new InputAction("Jump", binding: "<Keyboard>/space");
			spaceAction.performed += ctx => current_health = 0;
			spaceAction.Enable();
        }
		
	   private void OnDestroy()
		{
			spaceAction.Disable();
		}

        private void Update()
        {			
            if (stateMachine != null)
                stateMachine.Update();
			
			info = stateMachine.GetExecInfo();
        }

		private bool IsAlive()
		{
            return current_health == 0;
        }
		
		// ------------------------------------------------------------------------------- //
		//              ** Enter, Update and Exit methods for IDLE state **
		// ------------------------------------------------------------------------------- //
		
		private float idleStateDuration = 3.0f;   // Time in seconds to stay in the Idle state.
		
        private bool OnIdleEnter()
		{
			// ** enter logic **
            return true;
        }

        private void OnIdleUpdate()
		{
            if (stateMachine.Duration() > idleStateDuration)
                stateMachine.SwitchState(PATROL_STATE);
        }

        private bool OnIdleExit()
		{
			// ** exit logic **
            return true;
        }
    }
}
