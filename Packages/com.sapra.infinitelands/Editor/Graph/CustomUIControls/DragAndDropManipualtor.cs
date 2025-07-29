using UnityEditor;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    // DragAndDropManipulator is a manipulator that stores pointer-related callbacks, so it inherits from
    // PointerManipulator.
    class DragAndDropManipulator : PointerManipulator
    {
        InfiniteLandsGraphView view;
        public DragAndDropManipulator(InfiniteLandsGraphView view)
        {
            // The target of the manipulator, the object to which to register all callbacks, is the drop area.
            target = view;
            this.view = view;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            // Register callbacks for various stages in the drag process.
            target.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            target.RegisterCallback<DragPerformEvent>(OnDragPerform);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            // Unregister all callbacks that you registered in RegisterCallbacksOnTarget().
            target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
            target.UnregisterCallback<DragPerformEvent>(OnDragPerform);
        }

        // This method runs every frame while a drag is in progress.
        void OnDragUpdate(DragUpdatedEvent _)
        {
            var selectedObject = DragAndDrop.objectReferences[0];
            if(typeof(InfiniteLandsAsset).IsAssignableFrom(selectedObject.GetType())){
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            }
        }

        // This method runs when a user drops a dragged object onto the target.
        void OnDragPerform(DragPerformEvent a)
        {
            var selectedObject = DragAndDrop.objectReferences[0];
            if(typeof(InfiniteLandsAsset).IsAssignableFrom(selectedObject.GetType())){
                view.OnAssetDropped(selectedObject as InfiniteLandsAsset, a.mousePosition);
            }
        }
    }
}