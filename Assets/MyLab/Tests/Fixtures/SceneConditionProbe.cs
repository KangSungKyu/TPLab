using System;
using MyLab.Core.SceneManagement;

namespace MyLab.Core.Tests
{
    public sealed class SceneConditionProbe : SceneTransitionCondition
    {
        public string Id;
        public bool Allowed = true;
        public int Calls;
        [NonSerialized] public Exception Failure;
        [NonSerialized] public Action<SceneTransitionContext> Evaluating;
        [NonSerialized] public SceneTransitionContext LastContext;
        public override string ConditionId => Id;
        public override bool Evaluate(SceneTransitionContext context)
        {
            ++Calls;
            LastContext = context;
            Evaluating?.Invoke(context);
            if (Failure != null) throw Failure;
            return Allowed;
        }
    }
}
