using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class TransformInGrid
    {
        public Transform Transform{get; private set;}
        public HashSet<Vector2Int> VisibleChunks{get; private set;}
        private HashSet<Vector2Int> CurrentChunks = new();


        #region Parameters
        private int VisibleChunksCount;
        private Vector2Int LastValidPosition;
        private IControlTerrain infiniteLandsController;
        private Vector2 Offset;
        #endregion

        #region Constructors
        public TransformInGrid(Transform transform, IControlTerrain infiniteLandsController, Vector2 offset){
            
            this.Transform = transform;
            this.infiniteLandsController = infiniteLandsController;
            VisibleChunks = new HashSet<Vector2Int>();
            VisibleChunksCount = -1;
            Offset = offset;
        }
        #endregion

        #region Chunks Around
        public void UpdateChunksAround(ref List<Vector2Int> toEnable, ref List<Vector2Int> toDisable, float size, int visibleChunks){

            if(Transform == null){
                return;
            }
           
            var position = infiniteLandsController.WorldToLocalPoint(Transform.position);
            Vector2Int positionInGrid = Vector2Int.RoundToInt((new Vector2(position.x, position.z)-Offset)/size);
            if (!positionInGrid.Equals(LastValidPosition) || VisibleChunksCount != visibleChunks)
            {
                RetrieveChunksAround(positionInGrid, visibleChunks, ref toEnable, ref toDisable);
                return;
            }
        }

       /*  private void RetrieveChunksAround(Vector2Int posInGrid, int visibleChunks, out IEnumerable<Vector2Int> toEnable, out IEnumerable<Vector2Int> toDisable){
            Vector2Int[] CurrentChunks = new Vector2Int[(2*visibleChunks+1)*(2*visibleChunks+1)];
            
            int i = 0;
            for (int yOffset = -visibleChunks; yOffset <= visibleChunks; yOffset++)
            {
                for (int xOffset = -visibleChunks; xOffset <= visibleChunks; xOffset++)
                {
                    Vector2Int currentChunkID = posInGrid+new Vector2Int(xOffset, yOffset);
                    CurrentChunks[i] = currentChunkID;
                    i++;
                }
            }

            toEnable = CurrentChunks.Except(VisibleChunks);
            toDisable = VisibleChunks.Except(CurrentChunks);

            VisibleChunks = CurrentChunks;
            VisibleChunksCount = visibleChunks;
            LastValidPosition = posInGrid;
        }
 */

        private void RetrieveChunksAround(Vector2Int posInGrid, int visibleChunks, ref List<Vector2Int> toEnable, ref List<Vector2Int> toDisable)
        {
            // Clear the reusable lists
            CurrentChunks.Clear();
            // Populate CurrentChunks with the new visible chunks
            for (int yOffset = -visibleChunks; yOffset <= visibleChunks; yOffset++)
            {
                for (int xOffset = -visibleChunks; xOffset <= visibleChunks; xOffset++)
                {
                    CurrentChunks.Add(posInGrid + new Vector2Int(xOffset, yOffset));
                }
            }

            // Determine chunks to enable
            foreach (var chunk in CurrentChunks)
            {
                if (!VisibleChunks.Contains(chunk))
                {
                    toEnable.Add(chunk);
                }
            }

            // Determine chunks to disable
            foreach (var chunk in VisibleChunks)
            {
                if (!CurrentChunks.Contains(chunk))
                {
                    toDisable.Add(chunk);
                }
            }

            // Update the VisibleChunks hash set
            (VisibleChunks, CurrentChunks) = (CurrentChunks, VisibleChunks);
            VisibleChunksCount = visibleChunks;
            LastValidPosition = posInGrid;
        }
        #endregion
    }
}