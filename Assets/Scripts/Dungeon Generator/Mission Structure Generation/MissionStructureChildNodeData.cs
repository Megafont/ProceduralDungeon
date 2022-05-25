

namespace ProceduralDungeon.DungeonGeneration.MissionStructureGeneration
{
    public class MissionStructureChildNodeData
    {
        public readonly MissionStructureGraphNode ChildNode;
        public bool IsTightlyCoupled = false; // Indicates whether this node is tightly coupled to its parent. If so, it means it the player won't be able to access it until the clear the previous component of the dungeon.


        public MissionStructureChildNodeData(MissionStructureGraphNode childNode)
        {
            ChildNode = childNode;
        }

        public MissionStructureChildNodeData(MissionStructureGraphNode childNode, bool isTightlyCoupled)
            : this(childNode)
        {
            IsTightlyCoupled = isTightlyCoupled;
        }


    }

}
