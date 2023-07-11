using System;
using System.Threading;
using MCGalaxy.Commands;
using MCGalaxy.Network;
using MCGalaxy.Maths;

namespace MCGalaxy
{
    public class CmdBoost : Command2
    {
        public override string name { get { return "boost"; } }
        
        public override string shortcut { get { return ""; } }
        
        public override bool MessageBlockRestricted { get { return false; } }

        public override string type { get { return "other"; } }
        
        public override bool museumUsable { get { return false; } }
        
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }
        
        public override void Use(Player p, string message, CommandData data)
        {
            if (!(p.group.Permission >= LevelPermission.Operator)) {
                if (!Hacks.CanUseHacks(p)) {
                    if (data.Context != CommandContext.MessageBlock) {
                        p.Message("%cYou cannot use this command manually when hacks are disabled.");
                        return;
                    }
                }
            }
            if (message == "") { Help(p); return; }
            string[] words = message.Split(' ');
            if (words.Length < 6) {
                p.Message("%cYou need to provide x, y, z, xMode, yMode, and zMode.");
                return;
            }
            
            float x = 0, y = 0, z = 0;
            int xMode = 0, yMode = 0, zMode = 0;
            int delay = 0;
            bool allowRepeat = true;
            const float max = 1024;
            if (!CommandParser.GetReal(p, words[0], "x", ref x, -max, max)) { return; }
            if (!CommandParser.GetReal(p, words[1], "y", ref y, -max, max)) { return; }
            if (!CommandParser.GetReal(p, words[2], "z", ref z, -max, max)) { return; }
            
            if (!CommandParser.GetInt(p, words[3], "xMode", ref xMode, 0, 1)) { return; }
            if (!CommandParser.GetInt(p, words[4], "yMode", ref yMode, 0, 1)) { return; }
            if (!CommandParser.GetInt(p, words[5], "zMode", ref zMode, 0, 1)) { return; }
            if (words.Length >= 7) {
                if (!CommandParser.GetInt(p, words[6], "stun milliseconds", ref delay, 0, 10000)) { return; }
            }
            if (words.Length >= 8) {
                if (!CommandParser.GetBool(p, words[7], ref allowRepeat)) { return; }
            }
            
            
            if (p.Supports(CpeExt.VelocityControl)) {
                if (delay > 0) {
                    p.Send(Packet.Motd(p, "-hax horspeed=0.000001 jumps=0 -push"));
                    p.Send(Packet.VelocityControl(x, y, z, (byte)xMode, (byte)yMode, (byte)zMode));
                    Thread.Sleep(delay);
                    p.SendMapMotd();
                    //p.Message("unfrozen");
                } else {
                    p.Send(Packet.VelocityControl(x, y, z, (byte)xMode, (byte)yMode, (byte)zMode));
                    Thread.Sleep(100);
                }
                if (allowRepeat) {
                    p.prevMsg = "";
                }
            } else {
                p.SendMapMotd();
                p.Message("%cYour client does not support VelocityControl.");
                p.Message("%bPlease update to the latest (DEV) build.");
            }
        }
        public override void Help(Player p)
        {
            p.Message("%T/Boost [x y z] [xMode yMode zMode] <milliseconds> <repeat>");
            p.Message("%HGives you a velocity based on [x y z]. Mode can be 0 or 1.");
            p.Message("%HMode 0 means your current velocity is added to,");
            p.Message("%HMode 1 means your current velocity is replaced.");
            p.Message("%HA y velocity of 1.233 is the same as a regular jump.");
            p.Message("%HThe arguments with < > brackets are optional.");
            p.Message("%HUse %T/help boost options %Hfor further explanation.");
        }
        public override void Help(Player p, string message)
        {
            if (!message.CaselessEq("options")) { Help(p); return; }
            
            p.Message("%H<milliseconds> determines how long player influence on trajectory will be disabled.");
            p.Message("%HThis is useful to force you to land in a very specific spot.");
            p.Message("%HThe default is 0.");
            p.Message("%H<repeat> can be true or false.");
            p.Message("%HIt allows /boost in message blocks to be repeated if walked-through, without having to touch another message block first.");
            p.Message("%HThe default is true.");
        }
    }
}