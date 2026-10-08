using System.Threading;
using UnityEngine;

namespace TPLab.UI
{
    /// <summary>Identifies one row binding generation over a component-owned reusable cell.</summary>
    /// <remarks>
    /// Project async work must return to Unity's main thread and check IsCurrent and LifetimeToken before applying results.
    /// Index, generation, and the cached token remain readable after cancellation/source disposal. Native view access
    /// and IsCurrent require the main thread. This value borrows the view; it never owns cells, data, or subscriptions.
    /// </remarks>
    public readonly struct VirtualCellBinding
    {
        private readonly VirtualScrollRect.Cell _cell;
        private readonly int _index;
        private readonly long _generation;
        private readonly RectTransform _view;
        private readonly CancellationToken _token;

        internal VirtualCellBinding(VirtualScrollRect.Cell cell, int index, long generation,
            RectTransform view, CancellationToken token)
        {
            _cell = cell;
            _index = index;
            _generation = generation;
            _view = view;
            _token = token;
        }

        /// <summary>Gets the cached logical row index for this generation.</summary>
        public int Index => _index;
        /// <summary>Gets the monotonically allocated binding generation for this cell.</summary>
        public long Generation => _generation;
        /// <summary>Gets the borrowed view on the main thread; always verify IsCurrent before writing to a recycled view.</summary>
        public RectTransform View
        {
            get
            {
                UIContext.EnsureMainThread();
                return _view;
            }
        }
        /// <summary>Gets the cached token cancelled by unbinding, disable/destruction, or binding failure.</summary>
        public CancellationToken LifetimeToken => _token;
        /// <summary>Gets whether this generation is still bound, including its prepare callback before native activation.</summary>
        public bool IsCurrent
        {
            get
            {
                UIContext.EnsureMainThread();
                return _cell != null && _cell.Owner != null && _cell.Owner.IsBindingCurrent(_cell, _generation);
            }
        }
    }
}