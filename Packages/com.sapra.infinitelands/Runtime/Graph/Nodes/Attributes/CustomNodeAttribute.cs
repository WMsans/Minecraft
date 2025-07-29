using UnityEngine;
using System;
using System.Linq;

namespace sapra.InfiniteLands
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomNodeAttribute : PropertyAttribute
    {
        public string name;
        public string[] synonims = new string[0];

        public string customType = "";
        public string docs = "";
        public bool canCreate = true;
        public bool canDelete = true;
        public bool startCollapsed = false;
        public bool singleInstance = false;

        private Type[] ValidOnlyIn;

        public bool IsValidInTree(Type treeTyp){
            if (ValidOnlyIn.Length <= 0)
            {
                return true;
            }

            return ValidOnlyIn.Contains(treeTyp);    
        }

        public bool IsAlwaysValid(){
            return ValidOnlyIn.Length <= 0;
        }

        public CustomNodeAttribute(string name, params Type[] ValidOnlyIn)
        {
            this.name = name;
            this.ValidOnlyIn = ValidOnlyIn;
        }
        public CustomNodeAttribute(string name)
        {
            this.name = name;
            this.ValidOnlyIn = new Type[0];
        }
    }
}