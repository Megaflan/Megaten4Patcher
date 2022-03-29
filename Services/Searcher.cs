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

        public static void PatchGame(string path, bool jpnDub, string regionHash, bool generateLayered)
        {
            try
            {
                Node cia = NodeFactory.FromFile(path, "root");

                //Xdelta to Base CIA
                var patchedRom = new BinaryFormat();
                FileStream xdeltaRom = null;
                switch (regionHash)
                {
                    case "oqwIBhSjD+uJnzBxOnRaqA==":
                        Console.WriteLine("LOG: Detectada región USA");
                        xdeltaRom = new FileStream("./Data/baseUS.xdelta", FileMode.Open);
                        break;
                    case "w3wGFSzCcYWOGojwlK+Mlg==":
                        Console.WriteLine("LOG: Detectada región EUR");
                        xdeltaRom = new FileStream("./Data/baseEU.xdelta", FileMode.Open);
                        break;
                }
                var decoderRom = new Decoder(cia.Stream, xdeltaRom, patchedRom.Stream);
                Console.WriteLine("LOG: Parcheando CIA...");
                decoderRom.Run();
                cia.ChangeFormat(patchedRom);
                cia.Stream.Position = 0;

                cia.TransformWith<BinaryCia2NodeContainer>();

                var content = Navigator.SearchNode(cia, "/root/content/program");
                content.TransformWith<Binary2Ncch>();

                //Xdelta to Videos
                var c = content.Children["rom"];
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
                            {$"./Data/RomFS/Moflex_JPN{Path.DirectorySeparatorChar}cut_anim002(3d)_ES.xdelta", "cut_anim002(3d).moflex"},
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
                            patchedRom = new BinaryFormat();
                            xdeltaRom = new FileStream(xdeltaFile, FileMode.Open);
                            decoderRom = new Decoder(moflex.Stream, xdeltaRom, patchedRom.Stream);
                            Console.WriteLine($"LOG: Parcheando {moflex.Name}");
                            decoderRom.Run();

                            moflex.ChangeFormat(patchedRom);
                        }
                        break;
                    case false:
                        Dictionary<string, string> deltaENGMap = new Dictionary<string, string>()
                        {
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}chaos_end(3d)_ES_ENG.xdelta", "chaos_end(3d).moflex"},
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}cut_anim001(3d)_ES_ENG.xdelta", "cut_anim001(3d).moflex"},
                            {$"./Data/RomFS/Moflex_USA{Path.DirectorySeparatorChar}cut_anim002(3d)_ES.xdelta", "cut_anim002(3d).moflex"},
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
                            patchedRom = new BinaryFormat();
                            xdeltaRom = new FileStream(xdeltaFile, FileMode.Open);
                            decoderRom = new Decoder(moflex.Stream, xdeltaRom, patchedRom.Stream);
                            Console.WriteLine($"LOG: Parcheando {moflex.Name}");
                            decoderRom.Run();

                            moflex.ChangeFormat(patchedRom);
                        }
                        break;
                }
                switch (generateLayered)
                {
                    case true:
                        Console.WriteLine("LOG: Generando LayeredFS...");
                        var pathLayered = $"{Path.GetDirectoryName(path)}{Path.DirectorySeparatorChar}ShinMegamiTenseiIV_esp{Path.DirectorySeparatorChar}0004000000141C00{Path.DirectorySeparatorChar}";
                        foreach (var file in File.ReadAllLines("./Data/RomFS/layeredFS.txt"))
                        {
                            var n = Navigator.SearchNode(c, $"{c.Path}/{file}");
                            n.Stream.WriteTo($"{pathLayered}romfs{Path.DirectorySeparatorChar}{n.Path.Replace($"root/content/program/rom/", "")}");
                        }
                        c.TransformWith<NodeContainer2BinaryIvfc>();
                        content.TransformWith<Ncch2Binary>();
                        content = Navigator.SearchNode(cia, "/root/content/program");
                        content.TransformWith<Binary2Ncch>();
                        var e = content.Children["system"];
                        e.TransformWith<BinaryExeFs2NodeContainer>();
                        e.Children[".code"].Stream.WriteTo($"{pathLayered}code.bin");
                        Console.WriteLine("LOG: LayeredFS generado con éxito");
                        break;
                    case false:
                        c.TransformWith<NodeContainer2BinaryIvfc>();
                        content.TransformWith<Ncch2Binary>();
                        Console.WriteLine("LOG: Generando CIA...");
                        cia.TransformWith<NodeContainer2BinaryCia>().Stream.WriteTo($"{Path.GetDirectoryName(path)}{Path.DirectorySeparatorChar}ShinMegamiTenseiIV_esp.cia");
                        Console.WriteLine("LOG: CIA generado con éxito");
                        break;
                }                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        public static string GenerateHash(string path)
        {
            using (var md5 = MD5.Create())
            {
                var node = NodeFactory.FromFile(path, "root").Stream;
                var hash = Convert.ToBase64String(md5.ComputeHash(node));
                node.Dispose();
                return hash;
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
