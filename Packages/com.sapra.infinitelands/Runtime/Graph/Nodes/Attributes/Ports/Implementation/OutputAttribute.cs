using UnityEngine;
using System;

namespace sapra.InfiniteLands
{
    [AttributeUsage(AttributeTargets.Field)]
    public class OutputAttribute : PropertyAttribute, IMatchList, IMatchInputType, ICanBeRenamed
    {
        public Type type;
        public string matchingList{get; private set;}
        public string matchingType{get; private set;}
        public string name_field{get; private set;}

        public OutputAttribute(string match_list_name = "", string match_type_name = "", string namefield = "")
        {
            matchingList = match_list_name;
            matchingType = match_type_name;
            name_field = namefield;
        }
    }
}