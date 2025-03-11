using System.Reflection;

namespace MassiveAI
{
    public class StateBase<T>
    {
        private readonly string class_name;
        public readonly T Owner;

        // User-specified callbacks
        private System.Func<bool> EnterCallback  { get; set; } = null;
        private System.Action     UpdateCallback { get; set; } = null;
        private System.Func<bool> ExitCallback   { get; set; } = null;

        // Public wrappers for the state machine to invoke
        public bool Enter()  => OnEnter();
        public void Update() => OnUpdate();
        public bool Exit()   => OnExit();

        // Constructor
        public StateBase(T owner)
        {
            class_name = GetType().Name.Split('`')[0];
            Owner = owner;
        }

        // Constructor with explicit enter, update, exit
        public StateBase(
			T owner,
			string stateName,
			System.Action update,
			System.Func<bool> enter = null,
			System.Func<bool> exit  = null)
        {
            class_name = stateName;
            Owner = owner;

            EnterCallback = enter;
            UpdateCallback = update;
            ExitCallback = exit;
        }

        // Default implementations of Enter, Update, and Exit methods
        protected virtual bool OnEnter()
        {
            if (EnterCallback != null)
                return EnterCallback();

            return true;
        }

        protected virtual void OnUpdate()
        {
            UpdateCallback?.Invoke();
        }

        protected virtual bool OnExit()
        {
            if (ExitCallback != null)
                return ExitCallback();

            return true;
        }

        public override string ToString() => class_name;
    }
}
