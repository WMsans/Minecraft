
using UnityEngine;

namespace sapra.InfiniteLands
{
    public static class Traveller
    {
        public static int Depth;
        public static int Limit = 1000;
        public static CheckPoint CurrentCheckpoint;

        public static bool Block{ get; private set; }
        private static bool ForceFinish;

        public static void DisableTraveller(bool state)
        {
            ForceFinish = state;
            CurrentCheckpoint = null;
            Block = false;
        }

        public static bool ProcessCheckpoints()
        {
            Block = false;
            if (CurrentCheckpoint == null) return true;

            bool isFinished = CurrentCheckpoint.ProcessNode();
            if (isFinished && CurrentCheckpoint == null)
                return true;
            else
                return false;
        }

        public static void NewCheckpoint(InfiniteLandsNode node, BranchData branchData)
        {
            var newCheckpoint = GenericPoolLight<CheckPoint>.Get();
            newCheckpoint.Reuse(CurrentCheckpoint, branchData, node);
            SwapCheckpoint(newCheckpoint, null);
        }

        public static void SwapCheckpoint(CheckPoint checkPoint, CheckPoint toRelease)
        {
            CurrentCheckpoint = checkPoint;
            if (toRelease != null)
            {
                GenericPoolLight.Release(toRelease);
            }
        }

        public static bool IncreaseAndCreateCheckpoint(InfiniteLandsNode node)
        {
            if (ForceFinish) return false;
            if (Limit < 0) return false;
            if (CurrentCheckpoint != null && CurrentCheckpoint.StartAtNode == node) return false;

            Depth++;
            if (Depth > Limit)
            {
                Depth = 0;
                Block = true;
                return true;
            }
            else
                return false;

        }
    }
}