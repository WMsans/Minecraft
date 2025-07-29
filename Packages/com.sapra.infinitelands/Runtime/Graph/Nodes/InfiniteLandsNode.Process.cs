using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public partial class InfiniteLandsNode
    {
#if UNITY_EDITOR
        private System.Diagnostics.Stopwatch processTimes = new();
        private List<float> timings = new();
#endif

        private bool ProcessNodeInternal(BranchData branch)
        {
            IsNodeReadOnly();

            if (Traveller.Block) return false;

            bool shouldCreateCheckpoint = Traveller.IncreaseAndCreateCheckpoint(this);
            if (shouldCreateCheckpoint && !state.completed)
            {
                Traveller.NewCheckpoint(this, branch);
                return false;
            }

            return ProcessNode(branch);
        }

        /// <summary>
        /// Processes the node at it's fully. Meaning it will get input values, execute the process, and cache output values
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="store"></param>
        public bool ProcessNode(BranchData branch)
        {
            IsNodeReadOnly();
            if (state.completed) return true;

            if (state.state == State.Idle)
            {
                state.SetState(State.SettingInputValues);
#if UNITY_EDITOR
                processTimes.Restart();
                processTimes.Start();
#endif
            }

            if (state.state == State.SettingInputValues)
            {
                if (!SetInputValues(branch)) return Leave(branch, false);
                state.SetState(State.Processing);
            }

            if (state.state == State.Processing)
            {
                if (!Process(branch)) return Leave(branch, false);
                CacheOutputValues();
                state.SetState(State.Done);
#if UNITY_EDITOR
                processTimes.Stop();
                SendTimings();
#endif
            }
            return Leave(branch, state.completed);
        }
        private bool Leave(BranchData branch, bool value)
        {
            if (branch.treeData.ForceToComplete && !state.completed)
            {
                Debug.LogErrorFormat("Something went wrong with {0} : {1}. It should be completed but it's not. Stuck at {2}:{3}", guid, this.GetType(), state.state, state.SubState);
            }
            return value;
        }
#if UNITY_EDITOR
        private void SendTimings()
        {
            var node = Graph.GetNodeFromGUID(guid);
            node.timings.Add(processTimes.ElapsedTicks);
        }
#endif
        public float RetriveTimings()
        {
#if UNITY_EDITOR
            if (timings.Count <= 0) return 0;
            timings.Sort();
            int elementsToRemove = Mathf.FloorToInt(timings.Count * 0.1f);

            float currentAverage = 0;
            int totalElements = timings.Count - elementsToRemove * 2;
            if(totalElements < 1) return timings.Average();

            for (int i = elementsToRemove; i < timings.Count - elementsToRemove; i++)
            {
                currentAverage += timings[i];
            }
            return currentAverage / totalElements;
#else
            return 0;
#endif
        }
        /// <summary>
        /// Override to define a custom workflow to set the input values. This method ensures that all structs with the Input attribute have data attached to them before calling the process.
        /// </summary>
        /// <param name="branch"></param>
        protected virtual bool SetInputValues(BranchData branch)
        {
            if (cacheInputs.Length > 0)
                Debug.LogWarningFormat("No SetInputValues method was implemented for {0}", this.GetType());
            return true;
        }

        /// <summary>
        /// Override to transform data from input to output. The target of this method is to set a value inside the Output fields
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="store"></param>
        protected virtual bool Process(BranchData branch)
        {
            Debug.LogWarningFormat("No Process method was implemented for {0}", this.GetType());
            return true;
        }
        /// <summary>
        ///  Override to define a custom workflow to store the output values. Ensures that all data created during the Process method is stored inside the generation branch object for later use by other nodes.
        /// </summary>
        /// <param name="branch"></param>
        protected virtual void CacheOutputValues(){
            if(cacheOutputs.Length > 0)
                Debug.LogWarningFormat("No CacheOutputValues method was implemented for {0}", this.GetType());
                
        }
    }
}