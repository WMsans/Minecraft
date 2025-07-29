using NUnit.Framework;

namespace sapra.InfiniteLands.Tests
{
    public class GraphToolsTests {
        [Test]
        public void CopyStaysTheSame()
        {
            PerlinNoiseNode node = new PerlinNoiseNode();
            node.guid = "NEW GUID";
            node.TileSize = 100;
            var copy = (PerlinNoiseNode)GraphTools.GetWriteableNode(node);
            Assert.AreEqual(node.guid, copy.guid);
            Assert.AreEqual(node.TileSize, copy.TileSize);
            node.TileSize = 300;
            Assert.AreNotEqual(node.TileSize, copy.TileSize);
            GraphTools.ReturnNode(copy);
            copy = (PerlinNoiseNode)GraphTools.GetWriteableNode(node);
            Assert.AreNotEqual(node.TileSize, copy.TileSize);
            GraphTools.MarkNodeAsInvalid(node.guid);
            copy = (PerlinNoiseNode)GraphTools.GetWriteableNode(node);
            Assert.AreEqual(node.guid, copy.guid);
            Assert.AreEqual(node.TileSize, copy.TileSize);
        }
    }

}