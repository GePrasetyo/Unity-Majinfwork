using UnityEngine;

namespace Majinfwork.FSM {
    [SharedBetweenAnimators]
    public abstract class GameStateMachine : StateMachineBehaviour {
        public sealed override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            Begin();
        }

        public sealed override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            Tick();
        }

        public sealed override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            End();
        }

        #region mirror
        public abstract void Begin();

        public abstract void Tick();

        public abstract void End();
        #endregion
    }
}