using System;

namespace MCGalaxy
{
    public class CmdAdventure : Command
    {   
        public override string name { get { return "Adventure"; } }
        public override string shortcut { get { return "ad"; } }
        public override string type { get { return "world"; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
        public override void Use(Player p, string message)
        {
            if (p.group.Permission >= LevelPermission.Operator) {
                Command.Find("map").Use(p, "buildable");
                Command.Find("map").Use(p, "deletable");
                return;
            }
            
            if (!LevelInfo.IsRealmOwner(p.name, p.level.name)) { 
                p.Message("%cYou do not have permission to use /Adventure in this map."); return;
            }
            
            Command.Find("overseer").Use(p, "map buildable");
            Command.Find("overseer").Use(p, "map deletable");
        }
        public override void Help(Player p) {
            p.Message("%T/Adventure");
            p.Message("%HInstantly toggles buildable and deletable, to turn your map into an \"adventure\" map with unbreakable blocks.");
        }
    }
}
