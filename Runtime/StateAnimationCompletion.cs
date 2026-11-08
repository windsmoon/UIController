using System;
using DG.Tweening;

namespace Windsmoon.UIController
{
    internal sealed class StateAnimationCompletion
    {
        #region fields
        private Action _onCompleted;
        private int _pendingCount = 1;
        #endregion

        #region constructors
        internal StateAnimationCompletion(Action onCompleted)
        {
            _onCompleted = onCompleted;
        }
        #endregion

        #region methods
        internal Action Register(Tween tween)
        {
            _pendingCount++;
            bool isFinished = false;
            // Tween 正常播完后也可能自动触发 OnKill；此时 isFinished 已经是 true，所以不会重复减计数。
            tween.OnKill(() =>
            {
                if (isFinished == false)
                {
                    isFinished = true;
                    CompleteOne();
                }
            });
            return () =>
            {
                if (isFinished)
                {
                    return;
                }

                isFinished = true;
                CompleteOne();
            };
        }

        internal void CompleteOne()
        {
            _pendingCount--;
            if (_pendingCount == 0)
            {
                Action onCompleted = _onCompleted;
                _onCompleted = null;
                onCompleted?.Invoke();
            }
        }

        internal void Cancel()
        {
            _onCompleted = null;
        }
        #endregion
    }
}
