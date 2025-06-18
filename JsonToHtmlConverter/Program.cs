using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

class Program
{
    public class JsonHtmlConverter
    {
        public string ConvertJsonToHtml(string json)
        {
            var root = JsonNode.Parse(json)?.AsObject( );
            var sb = new StringBuilder();

            // za !DOCTYPE html
            if (root?["doctype"]?.ToString() == "html")
                sb.AppendLine("<!DOCTYPE html>");

            // language
            string language = root?["language"]?.ToString() ?? "en";
            sb.AppendLine($"<html lang=\"{language}\">");

            // head
            if (root != null && root.ContainsKey("head"))
            {
                sb.AppendLine(Indent(1) + "<head>");
                sb.Append(HtmlHead(root["head"]!, 2));
                sb.AppendLine(Indent(1) + "</head>");
            }

            // body
            if (root != null && root.ContainsKey("body"))
            {
                sb.Append(HtmlBody(root["body"]!, 1));
            }

            sb.AppendLine("</html>"); // zaključek html tag-a
            return sb.ToString();   // celoten HTML vrnjen kot string
        }

        private string HtmlHead(JsonNode node, int indent) // metoda za head del 
        {
            var sb = new StringBuilder();
            var obj = node.AsObject();
            
            foreach (var kvp in obj) // zanka po elementih v head
            {
                switch (kvp.Key)
                {
                    case "meta":
                        var metas = kvp.Value!.AsObject();
                        foreach (var meta in metas)
                        {
                            if (meta.Key == "charset")
                                sb.AppendLine($"{Indent(indent)}<meta charset=\"{meta.Value}\">");
                            else
                                sb.AppendLine($"{Indent(indent)}<meta name=\"{meta.Key}\" content=\"{meta.Value}\">");
                        }
                        break;

                    case "link":
                        if (kvp.Value is JsonArray links)
                        {
                            foreach (var linkNode in links)
                            {
                                var attrs = linkNode?.AsObject();
                                if (attrs != null)
                                {
                                    var attrString = string.Join(" ", attrs.Select(attr => $"{attr.Key}=\"{attr.Value}\""));
                                    sb.AppendLine($"{Indent(indent)}<link {attrString}>");
                                }
                            }
                        }
                        break;

                    case "title":
                        sb.AppendLine($"{Indent(indent)}<title>{Escape(kvp.Value?.ToString())}</title>");
                        break;
                }
            }
            return sb.ToString();
        }

        private string HtmlBody(JsonNode node, int indent) // metoda za body del
        {
            var sb = new StringBuilder();
            var obj = node.AsObject();

            // atributi v body
            string attrs = "";
            if (obj.TryGetPropertyValue("attributes", out JsonNode? attrNode))
            {
                var attrObj = attrNode!.AsObject();
                var attrList = new List<string>();

                foreach (var attr in attrObj)
                {
                    if (attr.Key == "style")
                    {
                        var styles = attr.Value!.AsObject();
                        var styleString = string.Join(";", styles.Select(s => $"{s.Key}:{s.Value}"));
                        attrList.Add($"style=\"{styleString}\"");
                    }
                    else
                    {
                        attrList.Add($"{attr.Key}=\"{attr.Value}\"");
                    }
                }

                attrs = " " + string.Join(" ", attrList);
                obj.Remove("attributes"); // izločimo atribute (body tag) 
            }

            sb.AppendLine($"{Indent(indent)}<body{attrs}>");

            foreach (var kvp in obj)
            {
                sb.Append(RenderElement(kvp.Key, kvp.Value!, indent + 1));
            }

            sb.AppendLine($"{Indent(indent)}</body>");
            return sb.ToString();
        }

        private string RenderElement(string tag, JsonNode value, int indent)
        {
            var sb = new StringBuilder();

            if (value is JsonObject obj)
            {
                string attrs = "";

                if (obj.TryGetPropertyValue("attributes", out var attrNode))
                {
                    var attrObj = attrNode!.AsObject();
                    var attrList = new List<string>();

                    foreach (var attr in attrObj)
                    {
                        attrList.Add($"{attr.Key}=\"{attr.Value}\"");
                    }

                    attrs = " " + string.Join(" ", attrList);
                    obj.Remove("attributes");
                }

                sb.AppendLine($"{Indent(indent)}<{tag}{attrs}>");
                foreach (var child in obj)
                {
                    sb.Append(RenderElement(child.Key, child.Value!, indent + 1));
                }
                sb.AppendLine($"{Indent(indent)}</{tag}>");
            }
            else if (value is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    sb.Append(RenderElement(tag, item!, indent));
                }
            }
            else
            {
                sb.AppendLine($"{Indent(indent)}<{tag}>{Escape(value.ToString())}</{tag}>");
            }

            return sb.ToString();
        }

        private string Indent(int level) => new string(' ', level * 4);

        private string Escape(string? input) => System.Net.WebUtility.HtmlEncode(input ?? "");
    }

    static void Main()
    {
        while (true)
        {
            Console.WriteLine("Izberite možnost:");
            Console.WriteLine("1. Pretvori JSON v HTML");
            Console.WriteLine("2. Izhod");
            string choice = Console.ReadLine()?.Trim() ?? "";
            if (choice == "1")
            {
                Console.Clear();
                Console.WriteLine("Vnesite ime JSON datoteke: ");
                string fileName = Console.ReadLine()?.Trim() ?? "";
                do
                {
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        Console.WriteLine("Ime datoteke ne sme biti prazno.");
                        break;
                    }

                    if (!File.Exists(fileName))
                    {
                        Console.WriteLine($"Datoteka {fileName} ne obstaja.");
                        break;
                    }
                } while (!File.Exists(fileName)); // preveri, če datoteka obstaja

                try
                {
                    string json = File.ReadAllText(fileName);
                    var converter = new JsonHtmlConverter();
                    string html = converter.ConvertJsonToHtml(json);
                    string htmlFile = Path.ChangeExtension(fileName, ".html");
                    File.WriteAllText(htmlFile, html);
                    Console.WriteLine($"HTML datoteka '{htmlFile}' je bila uspešno ustvarjena.");
                    Task.Delay(2000).Wait();
                    Console.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Podrobnosti: {ex.Message}");
                    Console.WriteLine("");
                    Task.Delay(2000).Wait();
                    Console.Clear();
                }
            }
            else if (choice == "2")
            {
                
                Console.WriteLine("Izhod iz programa.");
                Task.Delay(2000).Wait();
                System.Environment.Exit(0); // zapremo okno
            }
            else
            {
                Console.WriteLine("Neveljavna izbira, poskusite znova.");
            }
        }
    }
}
