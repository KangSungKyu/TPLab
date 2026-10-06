using System;
using MyLab.Core.SceneManagement;

namespace MyLab.Core.Tests
{
    /// <summary>Editor validation fixture whose evaluation is observable and always fails if invoked.</summary>
    public sealed class EditorConditionProbe : SceneTransitionCondition
    {
        public static int EvaluationCount;
        public string Id = "policy";
        public override string ConditionId => Id;

        public override bool Evaluate(SceneTransitionContext context)
        {
            ++EvaluationCount;
            throw new InvalidOperationException("Editor validation must not evaluate runtime policies.");
        }
    }
}
