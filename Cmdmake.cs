using System;
using MCGalaxy.Commands;
using System.Collections.Generic;
using MCGalaxy.Blocks;
using RawID = System.UInt16;
using MakeType = System.Action<MCGalaxy.Player, int, string, System.UInt16>;

namespace MCGalaxy {
	public class CmdMake : Command2 {
		public override string name { get { return "Make"; } }
		public override string shortcut { get { return ""; } }
		public override string type { get { return "other"; } }
		public override bool museumUsable { get { return false; } }
		public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        
        static bool hooked = false;
        static string TYPES = "";
        static Dictionary<string, MakeType> makeActions = new Dictionary<string, MakeType>();
        static Command lb;
        
        static void Load() {
            makeActions["slabs"] = MakeSlabs;
            makeActions["walls"] = MakeWalls;
            makeActions["stairs"] = MakeStairs;
            makeActions["flatstairs"] = MakeFlatStairs;
            makeActions["corners"] = MakeCorners;
            makeActions["eighths"] = MakeEighths;
            makeActions["panes"] = MakePanes;
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, MakeType> pair in makeActions) {
                sb.Append(pair.Key + ", ");
            }
            sb.Remove(sb.Length-2, 2); //remove last comma and space
            TYPES = sb.ToString();
            lb = Command.Find("levelblock");
        }
        
		static bool AreEnoughLBspacesFree(Player p, int amountRequested, out int blockID, RawID requestedSlot) {
			blockID = 0;
			int amountFound = 0;
			
			BlockDefinition[] defs = p.level.CustomBlockDefs;
			for (RawID b = requestedSlot; b >= Block.CPE_COUNT; b--) {
				RawID cur = Block.FromRaw(b);
				//found free slot
				if (defs[cur] == null) {
					//p.Message("found free slot at {0}", b);
					if (blockID == 0) {
						//p.Message("setting starting id to {0}", b);
						blockID = b;
					}
					amountFound++;
					//p.Message("Amount found is now {0}", amountFound);
					if (amountFound == amountRequested) {
						return true;
					}
				} else if (blockID != 0) {
					//if a starting point was already found and we run into a used slot before finding the amount requested, stop
					break;
				}
			}
			
			p.Message("%WThere are not enough free sequential block IDs to perform this command.");
			string start = (blockID != 0) ? " starting at " + blockID : "";
			p.Message("(found {0} free IDs{1}, but needed {2})", amountFound, start, amountRequested);
			return false;
		}
		
        public static string GetBlockName(Player p, RawID blockID) {
			blockID = Block.FromRaw(blockID);
			
            if (Block.IsPhysicsType(blockID)) return "Physics block";
            
            BlockDefinition def = null;
            if (!p.IsSuper) {
                def = p.level.GetBlockDef(blockID);
            } else {
                def = BlockDefinition.GlobalDefs[blockID];
            }
            if (def != null) { return def.Name; }
            
            return "Unknown";
        }
		
		
		public override void Use(Player p, string message, CommandData data) {
			bool canUse = false;
			if (LevelInfo.IsRealmOwner(p.name, p.level.name)) canUse = true;
			if (p.group.Permission >= LevelPermission.Operator && p.group.Permission >= p.level.BuildAccess.Min) { canUse = true; }
			if (!canUse) {
				p.Message("&cYou can only use this command on your own maps."); return;
			}
            if (!hooked) {
                Load();
            }
            
			
			if (message == "") { Help(p); return; }
			string[] args = message.SplitSpaces(3);
			if (args.Length < 2) { Help(p); return; }
			
			
			string type = args[0].ToLower();
			RawID sourceID;
			if (!CommandParser.GetBlock(p, args[1], out sourceID)) { return; }
            sourceId = Block.Convert(sourceId); // convert physics blocks to their visual form
            sourceId = Block.ToRaw(sourceId);   // convert server block IDs to client block IDs
			
			string name = GetBlockName(p, (RawID)sourceID);
			int requestedSlot = Block.MaxRaw;
			if (args.Length > 2) {
				if (!CommandParser.GetInt(p, args[2], "starting ID", ref requestedSlot, Block.CPE_COUNT, Block.MaxRaw)) { return; } 
			}
			
            if (makeActions.ContainsKey(type)) {
                makeActions[type](p, sourceID, name, (RawID)requestedSlot);
            } else {
                p.Message("%WInvalid type \"{0}\".", type);
                p.Message("Types can be: %b{0}", TYPES);
            }
		}
		
		static void MakePanes(Player p, int origin, string name, RawID requestedSlot) {
			int dest;
			if (!AreEnoughLBspacesFree(p, 2, out dest, requestedSlot)) { return; }
			
			//WE  (0, 0, 6) to (16, 16, 10)
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-WE");
			lb.Use(p, "edit " + dest + " min 0 0 6");
			lb.Use(p, "edit " + dest + " max 16 16 10");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//NS (6, 0, 0) to (10, 16, 16)
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-NS");
			lb.Use(p, "edit " + dest + " min 6 0 0");
			lb.Use(p, "edit " + dest + " max 10 16 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
		}
        
		static void MakeSlabs(Player p, int origin, string name, RawID requestedSlot) {
			int dest;
			if (!AreEnoughLBspacesFree(p, 2, out dest, requestedSlot)) { return; }
			
			//down
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-D");
			lb.Use(p, "edit " + dest + " min 0 0 0");
			lb.Use(p, "edit " + dest + " max 16 8 16");
			lb.Use(p, "edit " + dest + " blockslight 1");
			dest -=1;
			//up
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-U");
			lb.Use(p, "edit " + dest + " min 0 8 0");
			lb.Use(p, "edit " + dest + " max 16 16 16");
			lb.Use(p, "edit " + dest + " blockslight 1");
			Command.Find("blockproperties").Use(p, "level "+(dest+1)+" stackblock "+origin);
		}
		static void MakeWalls(Player p, int origin, string name, RawID requestedSlot) {
			int dest;
			if (!AreEnoughLBspacesFree(p, 4, out dest, requestedSlot)) { return; }
			
			int height = 16;
			//north
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-N");
			lb.Use(p, "edit " + dest + " min 0 0 0");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 8");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//south
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-S");
			lb.Use(p, "edit " + dest + " min 0 0 8");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//west
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-W");
			lb.Use(p, "edit " + dest + " min 0 0 0");
			lb.Use(p, "edit " + dest + " max 8 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//east
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-E");
			lb.Use(p, "edit " + dest + " min 8 0 0");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
		}
		static void MakeStairs(Player p, int origin, string name, RawID requestedSlot) {
			int dest;
			if (!AreEnoughLBspacesFree(p, 8, out dest, requestedSlot)) { return; }
			
			
			int height = 8;
			//north
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-D-N");
			lb.Use(p, "edit " + dest + " min 0 0 0");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 8");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//south
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-D-S");
			lb.Use(p, "edit " + dest + " min 0 0 8");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//west
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-D-W");
			lb.Use(p, "edit " + dest + " min 0 0 0");
			lb.Use(p, "edit " + dest + " max 8 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//east
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-D-E");
			lb.Use(p, "edit " + dest + " min 8 0 0");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			
			//--------------------------------------------------------------------------------upper
			height = 16;
			
			//north
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-U-N");
			lb.Use(p, "edit " + dest + " min 0 8 0");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 8");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//south
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-U-S");
			lb.Use(p, "edit " + dest + " min 0 8 8");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//west
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-U-W");
			lb.Use(p, "edit " + dest + " min 0 8 0");
			lb.Use(p, "edit " + dest + " max 8 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//east
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-U-E");
			lb.Use(p, "edit " + dest + " min 8 8 0");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
		}
		static void MakeFlatStairs(Player p, int origin, string name, RawID requestedSlot) {
			int dest;
			if (!AreEnoughLBspacesFree(p, 8, out dest, requestedSlot)) { return; }
			
			
			int height = 8;
			//north
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-D-N");
			lb.Use(p, "edit " + dest + " min 0 0 0");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 1");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//south
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-D-S");
			lb.Use(p, "edit " + dest + " min 0 0 15");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//west
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-D-W");
			lb.Use(p, "edit " + dest + " min 0 0 0");
			lb.Use(p, "edit " + dest + " max 1 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//east
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-D-E");
			lb.Use(p, "edit " + dest + " min 15 0 0");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			
			//--------------------------------------------------------------------------------upper
			height = 16;
			
			//north
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-U-N");
			lb.Use(p, "edit " + dest + " min 0 8 0");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 1");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//south
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-U-S");
			lb.Use(p, "edit " + dest + " min 0 8 15");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//west
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-U-W");
			lb.Use(p, "edit " + dest + " min 0 8 0");
			lb.Use(p, "edit " + dest + " max 1 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//east
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-U-E");
			lb.Use(p, "edit " + dest + " min 15 8 0");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
		}
		static void MakeCorners(Player p, int origin, string name, RawID requestedSlot) {
			int dest;
			if (!AreEnoughLBspacesFree(p, 4, out dest, requestedSlot)) { return; }
			
			int height = 16;
			//north
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-NW");
			lb.Use(p, "edit " + dest + " min 0 0 0");
			lb.Use(p, "edit " + dest + " max 8 "+height+" 8");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//south
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-SE");
			lb.Use(p, "edit " + dest + " min 8 0 8");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//west
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-SW");
			lb.Use(p, "edit " + dest + " min 0 0 8");
			lb.Use(p, "edit " + dest + " max 8 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//east
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-NE");
			lb.Use(p, "edit " + dest + " min 8 0 0");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 8");
			lb.Use(p, "edit " + dest + " blockslight 0");
		}
		static void MakeEighths(Player p, int origin, string name, RawID requestedSlot) {
			int dest;
			if (!AreEnoughLBspacesFree(p, 8, out dest, requestedSlot)) { return; }
			
			int height = 8;
			//north
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-D-NW");
			lb.Use(p, "edit " + dest + " min 0 0 0");
			lb.Use(p, "edit " + dest + " max 8 "+height+" 8");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//south
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-D-SE");
			lb.Use(p, "edit " + dest + " min 8 0 8");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//west
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-D-SW");
			lb.Use(p, "edit " + dest + " min 0 0 8");
			lb.Use(p, "edit " + dest + " max 8 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//east
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-D-NE");
			lb.Use(p, "edit " + dest + " min 8 0 0");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 8");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			
			
			height = 16;
			//north
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-U-NW");
			lb.Use(p, "edit " + dest + " min 0 8 0");
			lb.Use(p, "edit " + dest + " max 8 "+height+" 8");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//south
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-U-SE");
			lb.Use(p, "edit " + dest + " min 8 8 8");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//west
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-U-SW");
			lb.Use(p, "edit " + dest + " min 0 8 8");
			lb.Use(p, "edit " + dest + " max 8 "+height+" 16");
			lb.Use(p, "edit " + dest + " blockslight 0");
			dest -=1;
			//east
			lb.Use(p, "copy " + origin + " " + dest);
			lb.Use(p, "edit " + dest + " name " + name + "-U-NE");
			lb.Use(p, "edit " + dest + " min 8 8 0");
			lb.Use(p, "edit " + dest + " max 16 "+height+" 8");
			lb.Use(p, "edit " + dest + " blockslight 0");
		}
		
		public override void Help(Player p)
		{
            if (!hooked) {
                Load();
            }
			p.Message("%T/Make [type] [block] <optional starting ID>");
			p.Message("%HCreates block variants out of [block] based on [type].");
			p.Message("%H[type] can be: %b{0}", TYPES);
			p.Message("%HFor example, %T/make eighths stone %Hwould give you 8 new stone eighth piece blocks.");
		}
	}
}