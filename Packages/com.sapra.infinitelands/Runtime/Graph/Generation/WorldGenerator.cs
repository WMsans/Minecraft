using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public class WorldGenerator 
    {        
        public InfiniteLandsNode[] nodes;
        public IGraph graph;

        public HeightOutputNode output;
        public InfiniteLandsNode[] mainBranchNodes;
        public InfiniteLandsNode[] separateBranchNodes;

        public HeightToWorldNode heightToWorldNode;
        public StringObjectStore<object> store;
        public HeightMapManager heightMapManager;
        public ReturnableManager returnableManager;
        public PointManager pointManager;
        public CompletitionToken token;
        
        public bool ValidGenerator{ get; private set; }
        public bool SepartedOutputs{ get; private set; }

        public WorldGenerator(IGraph generator, bool separateOutputs)
        {
            graph = generator;
            if (generator == null)
                return;
            ReCollectItems();
            InitializeComponents();

            heightToWorldNode = generator.GetAllNodes().OfType<HeightToWorldNode>().FirstOrDefault();
            bool validPreviewNode = GraphSettingsController.ValidateNode(graph, out InfiniteLandsNode previewNode);
            if (separateOutputs)
            {
                List<InfiniteLandsNode> mainNodes = new();
                if (heightToWorldNode.isValid)
                    mainNodes.Add(heightToWorldNode);
                if (validPreviewNode)
                    mainNodes.Add(previewNode);

                mainBranchNodes = mainNodes.ToArray();

                IEnumerable<InfiniteLandsNode> outputs = nodes.Where(a => typeof(IOutput).IsAssignableFrom(a.GetType()))
                    .Except(mainBranchNodes)
                    .Where(a => a.isValid);
                separateBranchNodes = outputs.ToArray();
            }
            else
            {
                IEnumerable<InfiniteLandsNode> outputs = nodes.Where(a => typeof(IOutput).IsAssignableFrom(a.GetType()))
                    .Where(a => a.isValid);
                if (validPreviewNode)
                {
                    outputs = outputs.Append(previewNode);
                }

                mainBranchNodes = outputs.ToArray();
                separateBranchNodes = null;
            }

            SepartedOutputs = separateOutputs && separateBranchNodes != null && separateBranchNodes.Length > 0;

            if (mainBranchNodes.Length <= 0 || !heightToWorldNode.isValid)
            {
                Debug.LogErrorFormat("There's no valid node in {0}. Ensure the Height Output Node has a valid connection", graph.name);
                ValidGenerator = false;
            }
            else
                ValidGenerator = true;
        }
        
        public void ReCollectItems()
        {
            nodes = graph.GetAllNodes().ToArray();
            output = graph.GetOutputNode();
        }
        private void InitializeComponents(){
            store = new StringObjectStore<object>();
            returnableManager = new ReturnableManager(store); 
            heightMapManager = new HeightMapManager(graph);
            pointManager = new PointManager(store);
            token = new CompletitionToken();

            store.AddData(returnableManager);
            store.AddData(heightMapManager);
            store.AddData(pointManager);
            store.AddData(token);
        }

        public void Dispose(JobHandle job){
            store.Dispose(job);
        }

        public void DisposeReturned(){
            store.DisposeReturned();
        }
    }
}