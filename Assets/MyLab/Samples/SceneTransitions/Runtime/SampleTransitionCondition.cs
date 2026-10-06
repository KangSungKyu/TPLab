using MyLab.Core.SceneManagement;
using UnityEngine;

namespace MyLab.Samples.SceneTransitions
{
    /// <summary>Sample business flag; policy reads never start transitions or change configuration.</summary>
    public sealed class SampleTransitionCondition : SceneTransitionCondition
    {
        [SerializeField] private SceneTransitionSampleController _controller;
        [SerializeField] private SampleSceneConsumer _consumer;
        public override string ConditionId => "sample-policy";
        public void Configure(SceneTransitionSampleController controller, SampleSceneConsumer consumer)
        {
            _controller = controller;
            _consumer = consumer;
        }
        public override bool Evaluate(SceneTransitionContext context) => (_controller != null ? _controller : _consumer.Controller).PolicyAllowed;
    }
}
