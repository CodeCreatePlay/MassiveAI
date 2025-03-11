using System;
using System.Collections.Generic;
using UnityEngine;

namespace MassiveAI
{
    public class TransitionBuilder<TState>
    {
        private TransitionBuilder() { }

        public static TransitionBlock Begin()
        {
            return new TransitionBlock();
        }
		
		public static GlobalTransitionsToBuilder GlobalBlock()
		{
			var context = new TransitionContext
			{
				FromState         = default,
				TransitionBlock   = new(),
				ParentFromBuilder = null,
			};
			
			return new GlobalTransitionsToBuilder(context);
		}

		public class TransitionContext
		{
			public TState FromState { get; set; }
			public TState ToState { get; set; }
			public Func<bool> Trigger { get; set; }
			public Func<bool> ConcreteCondition { get; set; }

			public TransitionBlock TransitionBlock { get; set; }
			public TransitionFromBuilder ParentFromBuilder { get; set; }
			public TransitionToBuilder ParentToBuilder { get; set; }
			public GlobalTransitionsToBuilder ParentGlobalToBuilder { get; set; }

			public void Reset()
			{
				FromState = default;
				ToState = default;
				Trigger = null;
				ConcreteCondition = null;
				ParentFromBuilder = null;
				ParentToBuilder = null;
				ParentGlobalToBuilder = null;
			}
		}

		public class TransitionBlock
		{
			public class TransitionsData
			{
				public Func<bool> Trigger;
				public List<Transition<TState>> Transitions;
				public Transition<TState> DefaultTransition;

				public TransitionsData(List<Transition<TState>> transitions, Transition<TState> defaultTransition)
				{
					Transitions = transitions;
					DefaultTransition = defaultTransition;
				}
				
				public void Add(Transition<TState> transition, Func<bool> trigger)
				{
					Transitions.Add(transition);
					this.Trigger = trigger;
				}
				
				public void AddDefault(Transition<TState> transition, Func<bool> trigger)
				{
					DefaultTransition = transition;
					this.Trigger = trigger;
				}
			}

			private Dictionary<TState, TransitionsData> _transitionsMap;
			private TransitionsData _globalTransitions;

			public TransitionBlock()
			{
				_transitionsMap = new();
				_globalTransitions = null;
			}

			public TransitionFromBuilder Transitions()
			{
				return new TransitionFromBuilder(this);
			}
			
			public TransitionBlock SetGlobalState(GlobalTransitionsToBuilder builder)
			{
				TransitionsData transitions = new( new(), default );
				TransitionsData transitionsData = builder.GetContext().TransitionBlock.GetTransitionsMap()[default];
				
				foreach(var transition in transitionsData.Transitions)
					transitions.Add(transition, null);
				
				transitions.AddDefault(transitionsData.DefaultTransition, null);
				_globalTransitions = transitions;
				return this;
			}				
			
			public TransitionBlock Build()
			{
				Validate();
				return this;
			}
			
			public void Validate()
			{
				if (_transitionsMap == null && _transitionsMap.Count == 0)
					throw new InvalidOperationException("No transitions defined.");

				foreach (KeyValuePair<TState, TransitionsData> kvp in _transitionsMap)
				{
					foreach(var transition in kvp.Value.Transitions)
						if (transition.Evaluations == null || transition.Evaluations.Length == 0)
							throw new InvalidOperationException($"Transition from {transition.FromState} to {transition.ToState} is missing evaluations.");
				}
			}

			public TState Evaluate()
			{			
				if (_transitionsMap == null)
					throw new InvalidOperationException("TransitionBlock is not finalized or ShadowAIEntity is null.");

				float bestScore;
				bool allConditionsFailed = true;
				Transition<TState> bestTransition;
				
				if (_globalTransitions != null)
				{
					bestTransition = _GetBestTransition(_globalTransitions.Transitions, out bestScore, out allConditionsFailed);
					if (bestTransition != null)
						return bestTransition.ToState;
				}

				foreach (KeyValuePair<TState, TransitionsData> kvp in _transitionsMap)
				{
					if (!kvp.Value.Trigger())
						continue;
					
					bestTransition = _GetBestTransition(kvp.Value.Transitions, out bestScore, out allConditionsFailed);

					if (bestTransition != null)
					{
						Debug.Log($"Transitioning from {bestTransition.FromState} to {bestTransition.ToState} with score {bestScore}");
						return bestTransition.ToState;
					}

					// If no transitions are selected, use the default transition
					if (kvp.Value.DefaultTransition != null)
					{
						bestTransition = kvp.Value.DefaultTransition;
						Debug.Log($"Default transition triggered from {bestTransition.FromState} to {bestTransition.ToState} with score {bestScore}");
						return bestTransition.ToState;
					}
				}

				return default;
			}

			private Transition<TState> _GetBestTransition(
				List<Transition<TState>> transitions,
				out float maxScore,
				out bool allConditionsFailed)
			{
				Transition<TState> bestTransition = null;
				maxScore = 0.0f;
				allConditionsFailed = true;

				foreach (var transition in transitions)
				{
					float totalScore = 0;

					if (transition.HasConcreteCondition() && !transition.ConcreteCondition())
					{
						allConditionsFailed = false;
						continue;
					}

					foreach (var condition in transition.Evaluations)
						totalScore += condition();

					if (transition.Evaluations.Length > 0)
						totalScore /= transition.Evaluations.Length;

					if (totalScore > maxScore)
					{
						maxScore = totalScore;
						bestTransition = transition;
					}
				}

				return bestTransition;
			}
			
			public TransitionsData GetGlobalTransitions() => _globalTransitions;
			
			public Dictionary<TState, TransitionsData> GetTransitionsMap() => _transitionsMap;
			
			public TransitionsData GetTransitions(TState key)
			{				
				if (!_transitionsMap.ContainsKey(key))
					_transitionsMap[key] = new( new(), default );
				
				return _transitionsMap[key];
			}
		}

        public class TransitionFromBuilder
        {
            private readonly TransitionBlock _transitionBlock;

            public TransitionFromBuilder(TransitionBlock transitionBlock)
            {
                _transitionBlock = transitionBlock;
            }

            public WhenConditionsBuilder From(TState state)
            {
                var context = new TransitionContext
                {
                    FromState = state,
                    TransitionBlock = _transitionBlock,
                    ParentFromBuilder = this
                };

                return new WhenConditionsBuilder(context);
            }
        }
		
		// ------------------------------ Classes for creating the actual transitions ------------------------------ //
		
		public class BaseBuilder
		{
			protected readonly TransitionContext _context;
			
			public BaseBuilder(TransitionContext context)
			{
				_context = context;
			}
		}
		
		public class WhenConditionsBuilder : BaseBuilder
		{			
			public WhenConditionsBuilder(TransitionContext context) : base(context) {}
			
			public TransitionToBuilder When(Func<bool> trigger)
			{
				_context.Trigger = trigger;
				return new TransitionToBuilder(_context);
			}
		}

        public class TransitionToBuilder : BaseBuilder
        {			
            public TransitionToBuilder(TransitionContext context) : base(context)
				=> _context.ParentToBuilder = this;

            public TransitionEvaluationsBuilder To(TState state)
            {
                _context.ToState = state;
                return new TransitionEvaluationsBuilder(_context);
            }

			public TransitionBlock Default(TState state)
			{
				_context.ToState = state;

				// Create a new Transition object explicitly
				var transition = new Transition<TState>(
					_context.FromState,
					_context.ToState,
					new Func<float>[] { () => 1 },  // Default evaluation
					null, 							// No concrete condition for default
					true                            // Mark this as a default transition
				);

				_context.TransitionBlock.GetTransitions(_context.FromState).AddDefault(transition, _context.Trigger);
				return _context.TransitionBlock;
			}

            public TransitionBlock End()
            {
                return _context.TransitionBlock;
            }
        }
				
		public class TransitionEvaluationsBuilder : BaseBuilder
        {
            public TransitionEvaluationsBuilder(TransitionContext context) : base(context) {}

			public TransitionEvaluationsBuilder CConditions( Func<bool> condition )
			{
				_context.ConcreteCondition = condition;
				return this;
			}

            public TransitionToBuilder Evaluations(params Func<float>[] _evaluations)
            {
                if (_evaluations == null || _evaluations.Length == 0)
                    throw new ArgumentException("Evaluations must not be null or empty.");

                var transition = new Transition<TState>(
					_context.FromState,
					_context.ToState,
					_evaluations,
					_context.ConcreteCondition);

				_context.TransitionBlock.GetTransitions(_context.FromState).Add(transition, _context.Trigger);
				_context.ConcreteCondition = null;
				
                return _context.ParentToBuilder;
            }
        }
		
		// ------------------------------ Classes for creating global transitions ------------------------------ //
		
		public class GlobalTransitionsToBuilder : BaseBuilder
        {			
            public GlobalTransitionsToBuilder(TransitionContext context) : base(context)
            {
				_context.ParentGlobalToBuilder = this;
            }

            public GlobalTransitionEvaluationsBuilder To(TState state)
            {
                _context.ToState = state;
                return new GlobalTransitionEvaluationsBuilder(_context);
            }

			public TransitionContext GetContext() => _context;
        }
		
		public class GlobalTransitionEvaluationsBuilder : BaseBuilder
        {
            public GlobalTransitionEvaluationsBuilder(TransitionContext context) : base(context) {}

			public GlobalTransitionEvaluationsBuilder CConditions( Func<bool> condition )
			{
				_context.ConcreteCondition = condition;
				return this;
			}

            public GlobalTransitionsToBuilder Evaluations(params Func<float>[] _evaluations)
            {
                if (_evaluations == null || _evaluations.Length == 0)
                    throw new ArgumentException("Evaluations must not be null or empty.");

                var transition = new Transition<TState>(
					_context.FromState,
					_context.ToState,
					_evaluations,
					_context.ConcreteCondition);

				_context.TransitionBlock.GetTransitions(_context.FromState).Add(transition, _context.Trigger);
				_context.ConcreteCondition = null;
				
                return _context.ParentGlobalToBuilder;
            }
        }
		
        public class Transition<T>
        {
            public T FromState { get; }
            public T ToState { get; }
            public Func<bool> ConcreteCondition { get; }
            public Func<float>[] Evaluations { get; }
            public bool IsDefault { get; }

            public Transition(T fromState, T toState, Func<float>[] evaluations, Func<bool> concreteCondition = null, bool isDefault = false)
            {
                FromState = fromState;
                ToState = toState;
                Evaluations = evaluations;
                ConcreteCondition = concreteCondition;
                IsDefault = isDefault;
            }

            public bool HasConcreteCondition() => ConcreteCondition != null;
        }
    }
}
