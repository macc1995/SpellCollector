using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Xml.Serialization;

namespace SpellCollector
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ////uncomment this method to download the html pages. 
                ////(you will have to provide the start page html from: http://dnd5e.wikidot.com/spells
                
                //InitRepository();

                var spells = DoSpells(); //This will get the htmls and create the spell object from them.

                ////Uncomment to serialize into json file
                //WriteToJson(spells);
                ////Uncomment to serialize into xml file
                //WriteToXml(spells);



            }
            catch (Exception e )
            {

                Console.WriteLine(e.Message);
            }
           
        }

        private static void WriteToXml(IEnumerable<Spell> spells)
        {
            var xmlSerializer = new XmlSerializer(typeof(List<Spell>));
            xmlSerializer.Serialize(File.Open("D:\\Roleplay\\D&D\\Spells\\Spellxml.xml", FileMode.OpenOrCreate), spells.ToList());

        }

        private static void WriteToJson(IEnumerable<Spell> spells)
        {
            var json = JsonConvert.SerializeObject(spells,Formatting.Indented,new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore});
            File.WriteAllText("D:\\Roleplay\\D&D\\Spells\\SpelJson.json", json);
        }

        private static void InitRepository()
        {
            
            var savedPages = Directory.EnumerateFiles(@"D:\Roleplay\D&D\Spells").Where(x => x.Contains(".html"));
            var linkList = GetLinks(savedPages).ToList();
            Console.WriteLine($"got {linkList.Count} links");
            var enabled = false;
            foreach (var link in linkList)
            {
                // ---------------------------Protection from ballistics is a broken site, remove it from the links in the start html or uncomment this section
                //if (link.Contains("protection-from-ballistics"))
                //{
                //    enabled = true;
                //    continue;
                //}
                //if (!enabled)
                //{
                //    continue;
                //}
                // ---------------------------Here the individual sites' html is downloaded
                using (var client = new HttpClient())
                {
                    var html = client.GetStringAsync(link).Result;
                    var spellName = link.Split('/').Last().Replace("spell:", "");
                    Console.WriteLine($"processing {spellName}");
                    File.WriteAllText($"D:\\Roleplay\\D&D\\Spells\\html\\{spellName}.txt", html);
                }
            }
        }

        private static IEnumerable<Spell> DoSpells()
        {
            var list = new List<Spell>();
            var linkList = Directory.EnumerateFiles("D:\\Roleplay\\D&D\\Spells\\html");
            
            foreach (var item in linkList)
            {
                
                try
                {

                    Console.WriteLine($"Collecting spell from {item}");

                    
                        var spellSite = File.ReadAllText(item);
                        var doc = new HtmlDocument();
                        doc.LoadHtml(spellSite);
                        var node = doc.DocumentNode;

                        var spellName = GetNodes(node, "div", "class", "main-content");
                        var fullText = spellName.First().InnerText;


                        var spell = ParseSpell(fullText);
                        var sb = new StringBuilder();
                        Console.WriteLine($"Processing {spell.Name}", ConsoleColor.Green);
                        sb.AppendLine($"NAME;{spell.Name.Trim()}");
                        sb.AppendLine($"SOURCE;{spell.Source.Trim()}");
                        sb.AppendLine($"LEVEL;{spell.Level.Replace("rd- ","").Trim()}");
                        sb.AppendLine($"SCHOOL;{spell.School.Trim()}");
                        sb.AppendLine($"CASTINGTIME;{spell.CastingTime.Replace("Casting Time: ","").Trim()}");
                        sb.AppendLine($"RANGE;{spell.Range.Replace("Range: ","").Trim()}");
                        sb.AppendLine($"DURATION;{spell.Duration.Replace(",", "").Replace("Duration:", "").Trim()}");
                        sb.AppendLine($"ISCONCENTRATION;{spell.IsConcentration}");
                        sb.AppendLine($"ISRITUAL;{spell.IsRitual}");
                        sb.AppendLine($"ISSOMATIC;{spell.IsSomatic}");
                        sb.AppendLine($"ISVERBAL;{spell.IsVebal}");
                        sb.AppendLine($"ISMATERIAL;{spell.IsMaterial}");
                        sb.AppendLine($"MATERIALS;{spell.Materials.Trim()}");
                        sb.AppendLine($"DESCRIPTION;{spell.Description.Trim()}");
                        sb.AppendLine($"HIGHERLEVELS;{spell.AtHigherlevels.Trim()}");
                        sb.AppendLine();
                        sb.AppendLine();
                        sb.AppendLine();
                        sb.AppendLine();
                        File.AppendAllText(@"D:\Roleplay\D&D\Spells\DoneSpell.txt", sb.ToString());
                        sb.Clear();
                        list.Add( spell); 

                    
                }

                catch (Exception e)
                {

                    Console.WriteLine(e.Message);
                }
            }
            return list;
        }

        private static Spell ParseSpell(string fullText)
        {
            var spell = new Spell()
            {
                AtHigherlevels="",
                CastingTime="",
                Description="",
                Duration = "",
                Level = "",
                Materials = "",
                Name = "",
                Range = "",
                School = "",
                Source = ""

            };
            var lines = fullText.Split('\n', StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
            spell.Name = lines[0];
            spell.Source = lines[1].ToLowerInvariant().Replace("source:","");
            spell.Level = GetSpellLevel(lines[2]);
            spell.School = GetSpellSchool(lines[2]);
            spell.IsRitual = lines[2].Contains("(ritual)");
            spell.CastingTime = lines[3];            
            spell.Range = lines[4];
            SetComponents(lines[5], spell);
            spell.Duration=lines[6].Replace("Concentration", "");
            if (lines[6].Contains("entration"))
            {
                spell.IsConcentration = true;
            }
            for (int i = 7; i < lines.Count()-1; i++)
            {
                var item = lines[i];
                if (item.ToLowerInvariant().StartsWith("at higher levels."))
                {
                    spell.AtHigherlevels = item.ToLowerInvariant().Replace("at higher levels.", "");
                }
                else if (item.ToLowerInvariant().StartsWith("spell lists."))
                {
                    spell.UsedBy = item.ToLowerInvariant().Replace("spell lists.", "");
                }
                else
                {
                    spell.Description += item + "\r\n";
                }
            }
            spell.Description = spell.Description.Trim();
            return spell;

        }

        private static void SetComponents(string components,Spell spell)
        {
            var componentos = Regex.Match(components, @": ([^\(]+)").Groups[0].Value;
            if (componentos.Contains('S'))
            {
                spell.IsSomatic = true;
            }
            if(componentos.Contains('V'))
            {
                spell.IsVebal = true;
            }
            if(componentos.Contains('M'))
            {
                spell.IsMaterial = true;
                if (Regex.IsMatch(components, "\\(([^)]+)\\)"))
                {
                    var exactComps = Regex.Match(components, "\\(([^)]+)\\)").Groups[0].Value;
                    spell.Materials = exactComps;
                }
            }
        }

        private static string GetSpellSchool(string v)
        {
            var str = v.ToLowerInvariant()
                .Replace("cantrip", "")
                .Replace("th-","")
                .Replace("st-","")
                .Replace("rd-","")
                .Replace("nd-","")
                .Replace("level","")
                .Replace("1", "")
                .Replace("2", "")
                .Replace("3", "")
                .Replace("4", "")
                .Replace("5", "")
                .Replace("6", "")
                .Replace("7", "")
                .Replace("8", "")
                .Replace("9", "")
                .Replace("(ritual)","").Trim();
            return str;
        }

        private static string GetSpellLevel(string v)
        {
            if (v.Contains("cantrip"))
            {
                return "cantrip";

            }
            switch (v.ToLowerInvariant()) 
            {
                case string a when v.Contains("cantrip"):
                    return "0";
                case string b when v.Contains("1"):
                    return "1";
                case string c when v.Contains("2"):
                    return "2";
                case string d when v.Contains("3"):
                    return "3";
                case string e when v.Contains("4"):
                    return "4";
                case string f when v.Contains("5"):
                    return "5";
                case string g when v.Contains("6"):
                    return "6";
                case string h when v.Contains("7"):
                    return "7";
                case string i when v.Contains("8"):
                    return "8";
                case string j when v.Contains("9"):
                    return "9";
                    default:
                    return "";
            }
        }

        private static IEnumerable<string> GetLinks(IEnumerable<string> savedPages)
        {
            
            var docText = File.ReadAllText(savedPages.First());
            var doc = new HtmlDocument();
            doc.LoadHtml(docText);
            var node = doc.DocumentNode;
            var table = GetNodes(node, "table", "class", "wiki-content-table");
            var links = new List<string>();
            foreach (var link in table)
            {
                var rows = link.Descendants().Where(x => x.Name == "tr");
                foreach (var row in rows.Skip(1))
                {
                    var linkNode = row.Descendants().FirstOrDefault(x => x.Name == "a");
                    if (linkNode != null && linkNode.Attributes.Contains("href"))
                    {
                       links.Add( linkNode.Attributes["href"].Value);
                    }
                }
            }
            return links;
            
        }

        private static IEnumerable<HtmlNode> GetNodes(HtmlNode node, string elementType, string filterAttribute, string filter)
        {
            return node.Descendants().Where(x => (x.Name == elementType) && x.HasAttributes && (x.GetAttributeValue(filterAttribute, "") == filter))
               .ToList();
        }


    }
}