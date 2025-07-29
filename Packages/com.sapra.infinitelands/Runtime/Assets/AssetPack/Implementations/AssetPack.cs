using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands{
    [AssetNode(typeof(AssetOutputNode))]
    [CreateAssetMenu(fileName = "Asset Pack", menuName = "Infinite Lands/Assets/Asset Pack")]
    public class AssetPack : InfiniteLandsAsset, IHoldManyAssets, IHaveAssetPreview
    {
        public List<InfiniteLandsAsset> Assets;
        public List<InfiniteLandsAsset> GetAssets() => Assets;

        public VisualElement Preview(bool BigPreview)
        {
            VisualElement container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.flexWrap = Wrap.NoWrap;

            VisualElement top = new VisualElement();
            top.style.alignContent = Align.FlexEnd;
            top.style.flexDirection = FlexDirection.Row;
            top.style.height = Length.Percent(50);
            top.style.borderBottomWidth = 1;
            top.style.alignItems = Align.Center;


            VisualElement bottom = new VisualElement();
            bottom.style.alignContent = Align.FlexStart;
            bottom.style.height = Length.Percent(50);
            bottom.style.flexDirection = FlexDirection.Row;
            bottom.style.borderTopWidth = 1;
            bottom.style.alignItems = Align.Center;


            var previews = Assets.OfType<IHaveAssetPreview>().ToArray();
            for(int i = 0; i < Mathf.Min(previews.Length, 4); i++){                   
                var preview = previews[i].Preview(false);
                preview.style.width = Length.Percent(50);
                if(i == 0 || i == 2)
                    preview.style.borderRightWidth = 1;
                else
                    preview.style.borderLeftWidth = 1;

                if(i < 2)
                    top.Add(preview);
                else
                    bottom.Add(preview);
            }           
            container.Add(top);
            container.Add(bottom);
            return container;
        }
    }
}