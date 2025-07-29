using System.Collections.Generic;
using UnityEditor.Searcher;

namespace sapra.InfiniteLands
{

    public static class CustomSearchTreeUtility
    {
        public static List<SearcherItem> CreateFromFlatList(List<SearcherItem> items)
        {
            List<SearcherItem> searchList = new List<SearcherItem>();
            for (int i = 0; i < items.Count; ++i)
            {
                SearcherItem item = items[i];
                string[] pathParts = item.Name.Split('/');
                SearcherItem searchNode = FindNodeByName(searchList, pathParts[0]);
                if (searchNode == null)
                {
                    searchNode = new SearcherItem(pathParts[0]);
                    searchList.Add(searchNode);
                }
                AddItem(searchNode, item, pathParts);
            }
            return searchList;
        }

        private static void AddItem(SearcherItem root, SearcherItem item, string[] pathParts)
        {
            SearcherItem currentSearchNode = root;

            for (int i = 1; i < pathParts.Length; ++i)
            {
                SearcherItem node = FindNodeByName(currentSearchNode.Children, pathParts[i]);
                if (node == null)
                {
                    node = new SearcherItem(pathParts[i]);
                    currentSearchNode.AddChild(node);
                }
                currentSearchNode = node;
            }
            // Set the user data to the final node, which is guaranteed to correspond to the item.
            currentSearchNode.UserData = item.UserData;
            currentSearchNode.Icon = item.Icon;
            currentSearchNode.Synonyms = item.Synonyms;
        }

        private static SearcherItem FindNodeByName(IList<SearcherItem> searchList, string name)
        {
            for (int i = 0; i < searchList.Count; ++i)
            {
                if (searchList[i].Name==name)
                {
                    return searchList[i];
                }
            }
            return null;
        }

    }
}