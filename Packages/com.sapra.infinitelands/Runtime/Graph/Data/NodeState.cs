using System;
using UnityEngine;

namespace sapra.InfiniteLands{
    public enum State{Idle, SettingInputValues, Processing, Done}
    public struct NodeState
    {
        public State state { get; private set; }
        public int SubState { get; private set; }
        public bool completed => state == State.Done;

        public static NodeState Default => new NodeState()
        {
            state = State.Idle,
            SubState = 0,
        };
        public void SetState(State state)
        {
            this.state = state;
            this.SetSubState(0);
        }

        public void SetSubState(int value)
        {
            SubState = value;
        }
        public void IncreaseSubState()
        {
            SubState++;
        }
    }
}