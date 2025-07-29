using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace sapra.InfiniteLands
{    
    [Serializable]
    public partial class InfiniteLandsNode
    {
        public readonly struct CachedPort
        {
            public readonly PortData originPort;
            public readonly PortData localPort;

            public readonly InfiniteLandsNode node;
            public CachedPort(PortData originPort, PortData localPort, InfiniteLandsNode node)
            {
                this.originPort = originPort;
                this.node = node;
                this.localPort = localPort;
            }
        }
        public string guid;
        public int small_index{ get; private set; }
        enum FieldMode{Single, Array, List};
        [HideInInspector] public string name;
        [HideInInspector] public bool expanded = true;
        [HideInInspector] public Vector2 position;
        [HideInInspector] public PortData previewPort;

        #region Caches for performance
        public bool isValid;

        [NonSerialized] private FieldInfo[] cacheOutputs;
        [NonSerialized] private FieldInfo[] cacheInputs;

        [NonSerialized] private Dictionary<string, CachedPort[]> PortsToInputWithNode = new();
        [NonSerialized] private Dictionary<string, InfiniteLandsNode[]> NodesToInput = new();
        [NonSerialized] private Dictionary<string, InfiniteLandsNode[]> NodesInOutput = new();
        [NonSerialized] private List<PortData> PortsToInputList = new();
        [NonSerialized] private Dictionary<string, FieldMode> InputMode = new();
        [NonSerialized] private List<CachedPort> Dependencies = new();
        #endregion

        [NonSerialized] protected IGraph Graph;
        #region Initalizations
        public void SetupNode(string guid, Vector2 position){
            this.guid = guid;
            this.position = position;
            this.IsReadonly = true;

            name = GetType().Name;
            cacheOutputs = null;
            cacheInputs = null;
        }
        /// <summary>
        /// Called once after the first generation call of the process. Only once in the lifetime of the generation. 
        /// Used for caching or reseting data that might persist between world genrations
        /// </summary>
        /// <param name="graph"></param>
        public virtual void Restart(IGraph graph){
            if (cacheOutputs == null)
                cacheOutputs = GetFields<OutputAttribute>();
            
            if(cacheInputs == null)
                cacheInputs = GetFields<InputAttribute>();
            
            isValid = true;
            Graph = graph;
            small_index = graph.GetUniqueIndex();
        }
        
#if UNITY_EDITOR
        public virtual void OnDeleteNode() { }
#endif        

        public void SetUpAndValidateConnections(IEnumerable<EdgeConnection> edgeConnections)
        { //Getting and validating the connections
            InputMode.Clear();

            PortsToInputWithNode.Clear();
            PortsToInputList.Clear();
            Dependencies.Clear();
            NodesToInput.Clear();
            NodesInOutput.Clear();

            if (!isValid)
                return;
            if (cacheInputs == null)
                return;

            foreach (FieldInfo field in cacheInputs)
                {
                    var attribute = field.GetCustomAttribute<InputAttribute>();
                    var connectionsIntoTheField = edgeConnections.Where(a => a.inputPort.fieldName.Equals(field.Name));

                    var connections = connectionsIntoTheField.ToArray();
                    CachedPort[] cachedPorts = new CachedPort[connections.Length];
                    for (int i = 0; i < connections.Length; i++)
                    {
                        var outputPort = connections[i].outputPort;
                        var inputPort = connections[i].inputPort;
                        var portNode = Graph.GetNodeFromGUID(outputPort.nodeGuid);
                        CachedPort prt = new CachedPort(outputPort, inputPort, portNode);
                        cachedPorts[i] = prt;
                        Dependencies.Add(prt);
                        PortsToInputList.Add(outputPort);
                    }
                    PortsToInputWithNode.Add(field.Name, cachedPorts);
                    NodesToInput.Add(field.Name, cachedPorts.Select(a => a.node).ToArray());

                    Type fieldType = field.FieldType;
                    FieldMode mode;
                    if (fieldType.IsArray)
                        mode = FieldMode.Array;
                    else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)) // Handle Lists
                        mode = FieldMode.List;
                    else
                        mode = FieldMode.Single;

                    InputMode.Add(field.Name, mode);

                    if (connections.Length <= 0)
                    { //No connection into that port
                        var optional = field.GetCustomAttribute<OptionalAttribute>();
                        if (optional == null)
                        {
                            SetInvalid();
                            return;
                        }
                    }

                    foreach (var cn in cachedPorts)
                    {
                        var nd = cn.node;
                        if (nd == null || !nd.isValid)
                        {
                            SetInvalid();
                            return;
                        }
                    }
                }

            foreach (FieldInfo field in cacheOutputs)
            {
                var connectionsFromOutput = Graph.GetAllEdges().Where(a => a.outputPort.nodeGuid.Equals(guid) && a.outputPort.fieldName.Equals(field.Name));
                var inputs = connectionsFromOutput.Select(a => a.inputPort).ToArray();
                NodesInOutput.Add(field.Name, Graph.GetNodesFromGUID(inputs.Select(a => a.nodeGuid)).ToArray());
            }


            if (!ExtraValidations())
                SetInvalid();
        }

        /// <summary>
        /// Called after all validations have run. Required in case some fields need to be checked to determine of the node is valid or not
        /// </summary>
        /// <param name="graph"></param>
        public virtual bool ExtraValidations(){return true;}

        private void SetInvalid(){
            if(!isValid)
                return;

            isValid = false;
            var outputConnections = Graph.GetAllEdges().Where(a => a.outputPort.nodeGuid.Equals(guid));
            foreach(var output in outputConnections){
                var node = Graph.GetNodeFromGUID(output.inputPort.nodeGuid);
                if(node != null)
                    node.SetInvalid();
            }

        }
        #endregion

        #region Ports
        public FieldInfo[] GetOutputFields() => cacheOutputs;
        public FieldInfo[] GetInputFields() => cacheInputs;

        public List<PortData> GetPortsToInput() => PortsToInputList;
        public CachedPort[] GetPortsToInput(string fieldName) => PortsToInputWithNode[fieldName];

        private FieldInfo[] GetFields<T>() where T : PropertyAttribute{
            return this.GetType().GetInputOutputFields<T>();
        }
        
        /// <summary>
        /// Returns all the nodes connected to the port with that name.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public InfiniteLandsNode[] GetNodesInInput(string fieldName)
        {
            return NodesToInput[fieldName];
        }
        
        /// <summary>
        /// Returns the amount of nodes connected to that port. Faster than getting all nodes and then counting them.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public int GetCountOfNodesInInput(string fieldName){
            if (NodesToInput.TryGetValue(fieldName, out var results))
                return results.Length;
            else
                return 0;
        }
        
        public int GetCountOfNodesInOutput(string fieldName){

            if (NodesInOutput.TryGetValue(fieldName, out var results))
                return results.Length;
            else
                return 0;
        }
        /// <summary>
        /// Is there any node connected to that port?
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        protected bool IsAssigned(string fieldName)
        {
            return GetCountOfNodesInInput(fieldName) > 0;
        }

/*         /// <summary>
        /// Returns the string representing a certain input in the node.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool ExtractNameNode(BranchData branch, string inputFieldName, out string nodeOutputName, out InfiniteLandsNode ogWritableNode)
        {
            FieldMode mode = InputMode[inputFieldName];
            if (mode != FieldMode.Single)
                Debug.LogError("Method wrongly used!");
            CachedPort[] data = PortsToInputWithNode[inputFieldName];
            if (data.Length <= 0)
                Debug.LogErrorFormat("{0} not assigned", inputFieldName);

            var target = data[0];
            ogWritableNode = branch.GetWriteableNode(target.node);
            if (target.node != null)
            {
                nodeOutputName = target.originPort.fieldName;
                return true;
            }
            else
                Debug.LogFormat("Missing {0}", target.originPort.nodeGuid);
            nodeOutputName = "";
            return false;
        } */
        #endregion
        
        /// <summary>
        /// Extract a random value only used by that node.
        /// </summary>
        /// <returns></returns>
        public int GetRandomIndex(){
            return guid.GetHashCode();
        }
    }
}