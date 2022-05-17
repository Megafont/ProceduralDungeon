

namespace ProceduralDungeon.DungeonGeneration.MissionStructureGeneration
{
    /// <summary>
    /// This class stores a generative grammar replacement rule.
    /// </summary>
    public class GrammarReplacementRule
    {
        public string Category; // The category this rule belongs to.
        public string Name; // A unique name to identify this rule.
        public MissionStructureGraph LeftSide; // The left side of the rule defines the node structure this rule will replace in the mission structure graph.
        public MissionStructureGraph RightSide; // The right side of the rule defines the node structure that will replace instances of the one defined by the left side of the replacement rule.



        public GrammarReplacementRule(string category, string name)
        {
            Category = category;
            Name = name;

            LeftSide = new MissionStructureGraph();
            RightSide = new MissionStructureGraph();
        }


    }

}