using System;
using System.Collections.Generic;
using MCGalaxy.Commands;
using MCGalaxy.Drawing.Brushes;
using MCGalaxy.Drawing.Ops;
using MCGalaxy.Network;
using MCGalaxy.Maths;
using BlockID = System.UInt16;

namespace MCGalaxy
{
    
    public class CmdReplaceVars : Command2
    {
        public override string name { get { return "ReplaceVars"; } }
        public override string shortcut { get { return "rv"; } }
        public override bool MessageBlockRestricted { get { return false; } }
        public override string type { get { return "other"; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        
        public override void Use(Player p, string message, CommandData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces();
            if (args.Length < 1) { p.Message("You must give a block to be replaced and a optionally block to replace with."); return; }
            
            BlockID target;
            BlockID output = Block.Invalid;
            
            if (!CommandParser.GetBlockIfAllowed(p, args[0], "draw with", out target)) { return; }
            if (args.Length > 1 && !CommandParser.GetBlockIfAllowed(p, args[1], "draw with", out output)) { return; }
            
            var rvArgs = new RvArgs(target, output);
            
            p.MakeSelection(2, "&fSelection region for &SReplaceVar", rvArgs, Callback);
            p.Message("Place or break two blocks to determine the edges.");
        }
        class RvArgs {
            public BlockID target, output;
            public RvArgs(BlockID target, BlockID output) {
                this.target = target; this.output = output;
            }
        }
        static bool Callback(Player p, Vec3S32[] marks, object state, BlockID block) {
            RvArgs rvArgs = (RvArgs)state;
            BlockID target = rvArgs.target;
            BlockID output = rvArgs.output == Block.Invalid ? block : rvArgs.output;
            
            Variant variant = new Variant(p, target, output);
            ReplaceVarOp op = ReplaceVarOp.Create(p, variant);
            
            if (op == null) { return false; }
            DrawOpPerformer.Do(op, null, p, marks);
            return true;
        }
        
        public override void Help(Player p) {
            p.Message("&T/ReplaceVars [block] <other>");
            p.Message("&HReplaces [block] and its stairs, slabs, etc, with <other> and its stairs, slabs, etc.");
            p.Message("&HIf <other> is not provided, uses your currently held block");

        }
    }
    
    public class Variant {
        Dictionary<BlockID, BlockID> outputBlock = new Dictionary<BlockID, BlockID>();
        
        public Variant(Player p, BlockID target, BlockID output) {
            outputBlock[target] = output;
            
            //no spaces are in this
            
            string targetName = GetPrefix(Block.GetName(p, target));
            string outputName = GetPrefix(Block.GetName(p, output));
            
            var targets = new List<BlockDefinition>();
            var outputs = new List<BlockDefinition>();
            
            foreach (var def in p.level.CustomBlockDefs) {
                if (def == null) { continue; }
                PopulateDict(p, targetName, def, targets);
                PopulateDict(p, outputName, def, outputs);
            }
            
            
            foreach (var targetDef in targets) {
                
                string targetSuffix = GetSuffix(targetDef);
                
                if (targetSuffix == null) { continue; }
                
                foreach (var outputDef in outputs) {
                    string outputSuffix = GetSuffix(outputDef);
                    
                    if (outputSuffix.CaselessEq(targetSuffix)) {
                        outputBlock[targetDef.GetBlock()] = outputDef.GetBlock();
                    }
                }
            }
        }
        
        //If a var contains another var, it must be placed earlier in the list than the one it contains.
        //e.g. walls must come before wall otherwise "walls" will become "s"
        static string[] varNames = new string[] { " slab", " walls", " wall", " stair", " corner" };
        
        static void PopulateDict(Player p, string groupName, BlockDefinition def, List<BlockDefinition> list) {
            if (def.Name.IndexOf('-') == -1) { return; }
            
            string defName = GetPrefix(def.Name);

            foreach (string varName in varNames) {
                defName = defName.ToLower().Replace(varName, "");
            }
            
            if (defName.Replace(" ", "").CaselessEq(groupName)) { list.Add(def); }
        }
        static string GetSuffix(BlockDefinition def) {
            int dashIndex = def.Name.IndexOf('-'); if (dashIndex == -1) { return null; }
            return def.Name.Substring(dashIndex+1);
        }
        static string GetPrefix(string input) {
            int dashIndex = input.IndexOf('-'); if (dashIndex == -1) { return input; }
            return input.Substring(0, dashIndex);
        }
        
        public BlockID this[int index]
        {
            get { return outputBlock.ContainsKey((BlockID)index) ? outputBlock[(BlockID)index] : Block.Invalid; }
        }
    }
    
    public class ReplaceVarOp : DrawOp {
        
        public static ReplaceVarOp Create(Player p, Variant variant) {
            return new ReplaceVarOp(p, variant);
        }
        public override string Name { get { return "ReplaceVariants"; } }
        public override long BlocksAffected(Level lvl, Vec3S32[] marks) { return SizeX * SizeY * SizeZ; }
        
        Player p;
        Level level;
        Variant variant;
        
        private ReplaceVarOp(Player p, Variant variant) {
            this.Player = p;
            this.p = p;
            level = p.level;
            Level = p.level;
            this.variant = variant;
        }
        DrawOpOutput output;
        
        public override void Perform(Vec3S32[] marks, Brush brush, DrawOpOutput output) {
            this.output = output;
            
            Vec3U16 p1 = Clamp(Min), p2 = Clamp(Max);
            for (ushort y = p1.Y; y <= p2.Y; y++)
                for (ushort z = p1.Z; z <= p2.Z; z++)
                    for (ushort x = p1.X; x <= p2.X; x++)
            {
                BlockID toPlace = variant[level.GetBlock(x, y, z)];
                output(Place(x, y, z, toPlace));
            }
        }
        
    }
}