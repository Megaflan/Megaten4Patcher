using Newtonsoft.Json.Linq;
using SceneGate.Lemon;
using SceneGate.Lemon.Containers.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Yarhl.FileSystem;
using Yarhl.IO;
using PleOps.XdeltaSharp.Decoder;

namespace Megaten4Patcher.Services
{
    public class Searcher
    {
        public static Dictionary<string, string> hashMap;
        public static void PatchDLC(string path)
        {
            hashMap = GenerateHashMap("./Data/DLC/dlc_data.json");
            Node cia = NodeFactory.FromFile(path, "root").TransformWith<BinaryCia2NodeContainer>();
            var content = Navigator.SearchNode(cia, "/root/content");
            foreach (var node in Navigator.IterateNodes(content, NavigationMode.BreadthFirst))
            {
                if (!node.Name.Contains("rom") & !node.Name.Contains("system"))
                {
                    node.TransformWith<Binary2Ncch>();
                    foreach (var c in node.Children)
                    {
                        using (var md5 = MD5.Create())
                        {
                            var hash = Convert.ToBase64String(md5.ComputeHash(c.Stream));
                            if (hashMap.ContainsKey(hash))
                            {
                                var patched = new BinaryFormat();
                                var xdelta = new FileStream($"./Data/DLC/{hashMap[hash]}", FileMode.Open);
                                var decoder = new Decoder(c.Stream, xdelta, patched.Stream);
                                decoder.Run();

                                c.ChangeFormat(patched);
                            }
                        }
                    }
                    node.TransformWith<Ncch2Binary>();
                }
            }
            cia.TransformWith<NodeContainer2BinaryCia>().Stream.WriteTo($"{Path.GetDirectoryName(path)}{Path.DirectorySeparatorChar}ShinMegamiTenseiIV_DLC_esp.cia");
        }

        public static void PatchGame(string path, bool jpnDub)
        {
            try
            {
                Node cia = NodeFactory.FromFile(path, "root").TransformWith<BinaryCia2NodeContainer>();
                var content = Navigator.SearchNode(cia, "/root/content/program");
                content.TransformWith<Binary2Ncch>();
                var c = content.Children["rom"];

                //Xdelta to RomFS
                var patched = new BinaryFormat();
                var xdelta = new FileStream("./Data/RomFS/romfs.xdelta", FileMode.Open);
                var decoder = new Decoder(c.Stream, xdelta, patched.Stream);
                Console.WriteLine("LOG: Parcheando RomFS...");
                decoder.Run();

                c.ChangeFormat(patched);

                //Xdelta to Videos
                c.Stream.Position = 0;
                c.TransformWith<BinaryIvfc2NodeContainer>();
                var moflexNode = Navigator.SearchNode(c, "moflex");
                switch (jpnDub)
                {
                    case true:
                        Dictionary<string, string> deltaJPNMap = new Dictionary<string, string>()
                        {
                            {$"./Data/RomFS/Moflex_JPN{Path.DirectorySeparatorChar}chaos_end(3d)_ES_JPN.xdelta", "chaos_end(3d).moflex"},
                            {$"./Data/RomFS/Moflex_JPN{Path.DirectorySeparatorChar}cut_anim001(3d)_ES_JPN.xdelta", "cut_anim001(3d).moflex"},
                            {$"./Data/RomFS/Moflex_JPN{Path.DirectorySeparatorChar}cut_anim003c(3d)_ES_JPN.xdelta", "cut_anim003c(3d).moflex"},
                            {$"./Data/RomFS/Moflex_JPN{Path.DirectorySeparatorChar}cut_anim004(3d)_ES_JPN.xdelta", "cut_anim004(3d).moflex"},
                            {$"./Data/RomFS/Moflex_JPN{Path.DirectorySeparatorChar}cut_anim006(3d)_ES_JPN.xdelta", "cut_anim006(3d).moflex"},
                            {$"./Data/RomFS/Moflex_JPN{Path.DirectorySeparatorChar}cut_anim007(3d)_ES_JPN.xdelta", "cut_anim007(3d).moflex"},
                            {$"./Data/RomFS/Moflex_JPN{Path.DirectorySeparatorChar}cut_anim008(3d)_ES_JPN.xdelta", "cut_anim008(3d).moflex"},
                            {$"./Data/RomFS/Moflex_JPN{Path.DirectorySeparatorChar}cut_anim009A(3d)_ES_JPN.xdelta", "cut_anim009A(3d).moflex"},
                            {$"./Data/RomFS/Moflex_JPN{Path.DirectorySeparatorChar}cut_anim010B(3d)_ES_JPN.xdelta", "cut_anim010B(3d).moflex"},
                            {$"./Data/RomFS/Moflex_JPN{Path.DirectorySeparatorChar}cut_anim016(3d)_ES_JPN.xdelta", "cut_anim016(3d).moflex"},
                            {$"./Data/RomFS/Moflex_JPN{Path.DirectorySeparatorChar}law_end(3d)_ES_JPN.xdelta", "law_end(3d).moflex"},
                            {$"./Data/RomFS/Moflex_JPN{Path.DirectorySeparatorChar}neutral_end(3d)_ES_JPN.xdelta", "neutral_end(3d).moflex"},
                        };
                        foreach (var xdeltaFile in Directory.EnumerateFiles("./Data/RomFS/Moflex_JPN"))
                        {
                            var moflex = Navigator.SearchNode(moflexNode, deltaJPNMap[xdeltaFile]);
                            patched = new BinaryFormat();
                            xdelta = new FileStream(xdeltaFile, FileMode.Open);
                            decoder = new Decoder(moflex.Stream, xdelta, patched.Stream);
                            Console.WriteLine($"LOG: Parcheando {moflex.Name}");
                            decoder.Run();

                            moflex.ChangeFormat(patched);
                        }
                        break;
                    case false:
                        Dictionary<string, string> deltaENGMap = new Dictionary<string, string>()
                        {
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}chaos_end(3d)_ES_ENG.xdelta", "chaos_end(3d).moflex"},
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}cut_anim001(3d)_ES_ENG.xdelta", "cut_anim001(3d).moflex"},
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}cut_anim003c(3d)_ES_ENG.xdelta", "cut_anim003c(3d).moflex"},
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}cut_anim004(3d)_ES_ENG.xdelta", "cut_anim004(3d).moflex"},
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}cut_anim006(3d)_ES_ENG.xdelta", "cut_anim006(3d).moflex"},
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}cut_anim007(3d)_ES_ENG.xdelta", "cut_anim007(3d).moflex"},
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}cut_anim008(3d)_ES_ENG.xdelta", "cut_anim008(3d).moflex"},
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}cut_anim009A(3d)_ES_ENG.xdelta", "cut_anim009A(3d).moflex"},
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}cut_anim010B(3d)_ES_ENG.xdelta", "cut_anim010B(3d).moflex"},
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}cut_anim016(3d)_ES_ENG.xdelta", "cut_anim016(3d).moflex"},
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}law_end(3d)_ES_ENG.xdelta", "law_end(3d).moflex"},
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}neutral_end(3d)_ES_ENG.xdelta", "neutral_end(3d).moflex"},
                        };
                        foreach (var xdeltaFile in Directory.EnumerateFiles("./Data/RomFS/Moflex_USA"))
                        {
                            var moflex = Navigator.SearchNode(moflexNode, deltaENGMap[xdeltaFile]);
                            patched = new BinaryFormat();
                            xdelta = new FileStream(xdeltaFile, FileMode.Open);
                            decoder = new Decoder(moflex.Stream, xdelta, patched.Stream);
                            Console.WriteLine($"LOG: Parcheando {moflex.Name}");
                            decoder.Run();

                            moflex.ChangeFormat(patched);
                        }
                        break;
                }
                c.TransformWith<NodeContainer2BinaryIvfc>();
                content.TransformWith<Ncch2Binary>();

                Console.WriteLine("LOG: Generando CIA...");
                cia.TransformWith<NodeContainer2BinaryCia>().Stream.WriteTo($"{Path.GetDirectoryName(path)}{Path.DirectorySeparatorChar}ShinMegamiTenseiIV_esp.cia");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        public static Dictionary<string, string> GenerateHashMap(string path)
        {
            Dictionary<string, string> hashMap = new Dictionary<string, string>();
            JObject o1 = JObject.Parse(File.ReadAllText(path));
            foreach (var x in o1["dlc"])
            {
                string name = x["fileName"].ToString();
                string hash = x["targetHash"].ToString();
                hashMap.Add(hash, name);
            }
            return hashMap;
        }
    }    
}
