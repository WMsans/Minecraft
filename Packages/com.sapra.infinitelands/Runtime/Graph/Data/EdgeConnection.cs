using UnityEngine;
using System;

namespace sapra.InfiniteLands
{
    [Serializable]
    public struct PortData{
        public string nodeGuid;
        public string fieldName;

        public int listIndex;
        public bool itemIsRearrangable;
        public bool isAddItem;

        public PortData(PortData parent){
            this.nodeGuid = parent.nodeGuid;
            this.fieldName = parent.fieldName;
            this.listIndex = parent.listIndex;
            this.isAddItem = parent.isAddItem;
            this.itemIsRearrangable = parent.itemIsRearrangable;
        }
        public PortData(string nodeGuid, string fieldName){
            this.nodeGuid = nodeGuid;
            this.fieldName = fieldName;

            listIndex = -1;
            isAddItem = false;
            itemIsRearrangable = false;
        }
        public override bool Equals(object obj)
        {
            var compared = (PortData)obj;
            return nodeGuid == compared.nodeGuid 
                && fieldName == compared.fieldName
                && listIndex == compared.listIndex
                && itemIsRearrangable == compared.itemIsRearrangable;
        }
        public override int GetHashCode()
        {
            return string.Format("{0}/{1},{2},{3}",nodeGuid,fieldName,listIndex, itemIsRearrangable).GetHashCode();
        }
    }

    [Serializable]
    public class EdgeConnection
    {
        public PortData outputPort;
        public PortData inputPort;

        public EdgeConnection(PortData outputPort, PortData inputPort){
            this.outputPort = outputPort;
            this.inputPort = inputPort;
        }

        public EdgeConnection(EdgeConnection edge){
            outputPort = new PortData(edge.outputPort);
            inputPort = new PortData(edge.inputPort);
        }

        public PortData GetPort(bool input) => input? inputPort : outputPort;
        public void SetPort(bool input, PortData data){
            if(input)
                inputPort = data;
            else
                outputPort = data;
        }

    }
}