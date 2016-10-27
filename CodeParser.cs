using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeGenerator
{
    public class Parser
    {
        protected const int SPACES_PER_TAB = 4;
        protected const string ALL_KEYWORDS = "const|event|delegate|public|static|class|private|protected|sealed|abstract|partial|set|get|override|readonly";
        protected const string PREFIXES = "const|sealed|virtual|abstract|internal|static|override|readonly|delegate|event";
        protected const string PROTECTION_LEVELS = "public|private|protected|internal";

        protected string GetProtectionLevel(string line)
        {
            var match = Regex.Match(line, PROTECTION_LEVELS);
            return match.Value;
        }

        protected string GetPrefix(string line)
        {
            var match = Regex.Match(line, PREFIXES);
            return match.Value;
        }

        protected IEnumerable<string> GetRegions(string body, int indent)
        {
            var pattern = string.Format(GetIndent(indent) + @"\#region\s.*", indent);
            var matches = Regex.Matches(body, pattern);
            for (int i = 0; i < matches.Count; i++)
            {
                var v = matches[i].Value;
                yield return v.Remove(0, 8);
            }
        }


        protected IEnumerable<string> GetDirectives(string body)
        {
            var pattern = @"using\s(.*);";
            var matches = Regex.Matches(body, pattern);

            for (int i = 0; i < matches.Count; i++)
            {
                var groups = matches[i].Groups;
                if (groups.Count > 1)
                    yield return groups[1].Value;
            }
        }

        protected string GetIndent(int indentLevel)
        {
            return indentLevel > 0 ? "^[ ]{" + indentLevel * SPACES_PER_TAB + "}" : "^";
        }

        protected bool IsMethod(string line)
        {
            var pattern = @"^[ ]*?((\w+.*\(.*\)\s?\n)|(\w+.*\(.*\)\n))";
            var match = Regex.Match(line, pattern);
            return match.Length > 0;
        }

        protected bool IsMethod(string line, int indent)
        {
            var pattern = GetIndent(indent) + @"((\w+.*\(.*\)\s?\n)|(\w+.*\(.*\)\n?))";
            var match = Regex.Match(line, pattern);
            return match.Success;
        }

        protected bool IsAttribute(string line, int indent = 0)
        {
            var pattern = GetIndent(indent) + @"\[.*\]";
            var match = Regex.Match(line, pattern);
            return match.Success;
        }

        protected bool IsField(string line, int indent = 0)
        {
            if (IsAutoProperty(line, indent)) return false;
            var pattern = GetIndent(indent) + @"\w+\s.*;";
            var match = Regex.Match(line, pattern);
            return match.Success;
        }

        protected bool IsAutoProperty(string line, int indent = 0)
        {
            var pattern = GetIndent(indent) + @"\w+.*\{\s?(set|get);";
            var match = Regex.Match(line, pattern);
            return match.Success;
        }

        protected bool IsProperty(string line, int indent = 0)
        {
            if (IsClass(line, indent)) return false;
            var pattern = GetIndent(indent) + @"\w+.*\w+\s*$";
            var match = Regex.Match(line, pattern);
            return match.Success;
        }

        protected bool IsOneLineProperty(string line, int indent = 0)
        {
            var pattern = GetIndent(indent) + @"\w+.*\w+\s+{\s*get\s*{\s*.*}\s*$";
            var match = Regex.Match(line, pattern);
            return match.Success;
        }

        protected bool IsKeyword(string word)
        {
            return Regex.IsMatch(word, @"\s?(" + ALL_KEYWORDS + @")\s?");
        }

        protected bool IsPrefix(string word)
        {
            return Regex.IsMatch(word, @"\s?(" + PREFIXES + @")\s?");
        }

        protected bool IsProtectionKeyword(string word)
        {
            return Regex.IsMatch(word, @"\s?(" + PROTECTION_LEVELS + @")\s?");
        }

        protected bool IsClass(string line)
        {
            var pattern = @".*\s*?class\s.*$";
            var match = Regex.Match(line, pattern);
            return match.Success;
        }

        protected bool IsClass(string line, int indent)
        {
            var pattern = GetIndent(indent) + @".*\s?class\s.*$";
            var match = Regex.Match(line, pattern);
            return match.Success;
        }

        protected IEnumerable<string> GetClosure(string body, string startLine, int indentLevel)
        {
            var indent = GetIndent(indentLevel);
            var lines = body.Split('\n');
            bool opened = false, foundFirstLine = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line == startLine)
                {
                    foundFirstLine = true;
                }
                if (foundFirstLine)
                {
                    if (!opened)
                    {
                        if (Regex.IsMatch(line, indent + @"\{.*$"))
                        {
                            opened = true;
                            continue;
                        }
                    }
                    else
                    {
                        if (Regex.IsMatch(line, indent + @"\}.*$"))
                        {
                            yield break;
                        }
                    }
                    yield return line;
                }
            }
        }

        protected string GetClosureString(string body, string startLine, int indentLevel)
        {
            StringBuilder sb = new StringBuilder(body.Length);
            var indent = GetIndent(indentLevel);
            var lines = body.Split('\n');
            bool opened = false, foundFirstLine = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line == startLine)
                {
                    foundFirstLine = true;
                    int p = i;
                    if (p > 0)
                    {
                        var prev = lines[--p];
                        while (IsAttribute(prev, indentLevel))
                        {
                            sb.Insert(0, prev + '\n');
                            prev = lines[--p];
                            if (p <= 0)
                                break;
                        }
                    }

                }
                if (foundFirstLine)
                {
                    if (!opened)
                    {
                        if (Regex.IsMatch(line, indent + @"\{.*$"))
                        {
                            opened = true;
                        }
                    }
                    else
                    {
                        if (Regex.IsMatch(line, indent + @"\}.*$"))
                        {
                            sb.AppendLine(line);
                            break;
                        }
                    }
                    sb.AppendLine(line);
                }
            }
            return sb.ToString();
        }

        public static string Filter(string line, string pattern, int groupIndex)
        {
            var groups = Regex.Match(line, pattern).Groups;
            if (groups.Count > groupIndex + 1)
                return groups[groupIndex + 1].Value;
            return string.Empty;
        }
    }

    public class FieldParser : Parser
    {
        public Field Parse(string line)
        {
            string type = null;
            string name = null;
            string prot = null;
            string prefix = null;
            line = Regex.Replace(line, @"^\s+", "");
            var words = line.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if (i < 2)
                {
                    if (IsProtectionKeyword(word))
                    {
                        prot = word;
                    }
                    else if (IsPrefix(word))
                    {
                        prefix = word;
                    }
                }
            }
            if (prot != null && prefix != null)
            {
                type = words[2];
                name = words[3];
            }
            else
            {
                if (prot != null || prefix != null)
                {
                    int t = 1;
                    type = words[t];
                    name = words[t + 1];
                }
                else
                {
                    type = words[0];
                    name = words[1];
                }
            }
            name = name.Trim();
            if (name.EndsWith(";"))
                name = name.Remove(name.Length - 1, 1);
            var filed = new Field(type, name, prot, prefix, Filter(line, @"^.*=\s?(.*);", 0));
            return filed;
        }

        protected string GetType(string line)
        {
            return Filter(line, @".*?(\w+\[?,?\]?(<.*>)?)\s\w+\s?\(.*\)", 0);
        }

        protected string GetName(string line)
        {
            return Filter(line, @".*?(\w+\[?,?\]?(<.*>)?)\s\w+\s?\(.*\)", 0);
        }
    }

    public class PropertyParser : Parser
    {
        public AutoProperty ParseAutoProp(string line)
        {
            string type = null;
            string name = null;
            string prot = null;
            string prefix = null;
            string setter = Filter(line, @"get;\s?(.*)\sset;\s?\}", 0);
            line = Regex.Replace(line, @"^\s+", "");
            var words = line.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if (i < 2)
                {
                    if (IsProtectionKeyword(word))
                    {
                        prot = word;
                    }
                    else if (IsPrefix(word))
                    {
                        prefix = word;
                    }
                }
            }
            if (prot != null && prefix != null)
            {
                type = words[2];
                name = words[3];
            }
            else
            {
                if (prot != null || prefix != null)
                {
                    int t = 1;
                    type = words[t];
                    name = words[t + 1];
                }
                else
                {
                    type = words[0];
                    name = words[1];
                }
            }
            var prop = new AutoProperty(type, name, prot, setter, prefix);
            return prop;
        }

        public Property ParseProp(string body, string startLine, int indentLevel)
        {
            string indent = GetIndent(indentLevel);
            var auto = ParseAutoProp(startLine);
            string fieldName = auto.name.ToLower();
            int lineIndex = 0;
            var prop = new Property(auto.type, auto.name, auto.protectionLevel, fieldName, auto.prefix);
            bool hasSetter = false;
           
            foreach (var line in GetClosure(body, startLine, indentLevel))
            {
                if (lineIndex > 1)
                {
                    if (Regex.IsMatch(line, @"set(\s|\n)"))
                        hasSetter = true;
                    if (Regex.IsMatch(line, @"return\s.*;"))
                        prop.SetFieldName(Filter(line, @"return\s(.*);", 0));
                    if (!Regex.IsMatch(line, @"(return\s|set(\s|\n)|get(\s|\n))")
                        && !Regex.IsMatch(line, GetIndent(indentLevel + 1) + @"(\{|\})"))
                    {
                        if (!hasSetter)
                            prop.AddGetterBodyLine(Regex.Replace(line, indent, ""));
                        else prop.AddSetterBodyLine(Regex.Replace(line, indent, ""));
                    }
                }
                lineIndex++;
            }
         
            prop.SetReadonly(!hasSetter);
            return prop;
        }
    }

    public class MethodParameterParser : Parser
    {
        public Method.Parameter Parse(string line)
        {
            var type = Filter(line, @"^(\S+)\s", 0);
            var name = Filter(line, @"^\s?\S+\s(\w+)", 0);
            var def = Filter(line, @"^\s?.*\s\w+\s?=\s?(.*)", 0);
            return new Method.Parameter(type, name, def);
        }
    }

    public class MethodParser : Parser
    {
        public Method Parse(string body, string startLine, int indentLevel)
        {
            var method = new Method();
            int lineIndex = 0;
            foreach (var line in GetClosure(body, startLine, indentLevel))
            {
                if (lineIndex == 0)
                {
                    method.name = GetName(line);
                    method.type = GetType(line);
                    method.protectionLevel = GetProtectionLevel(line);
                    method.prefix = GetPrefix(line);
                    method.SetReturnValue(GetReturnValue(line));
                    foreach (var p in GetParameters(line))
                    {
                        var parameter = new MethodParameterParser().Parse(p);
                        method.AddParameters(parameter);
                    }
                }
                else
                {
                    if (line.Length > 2 && line != @" \n" && line != " ")
                        method.AddLine(Regex.Replace(line, @"^\s+", ""));
                }
                lineIndex++;
            }
            return method;
        }

        protected string GetType(string line)
        {
            return Filter(line, @".*?(\w+\[?,?\]?(<.*>)?)\s\w+\s?\(.*\)", 0);
        }

        protected string GetReturnValue(string line)
        {
            return Filter(line, @"return\s(\S*)", 0);
        }

        protected string GetName(string line)
        {
            return Filter(line, @".*\s(\w+)\s?\(.*\)", 0);
        }

        protected IEnumerable<string> GetParameters(string line)
        {
            var pattern = @".*\s\w+\s*\((.*)\)";
            foreach (var p in Filter(line, pattern, 0).Split(','))
            {
                yield return Regex.Replace(p, @"^\s", "");
            }
        }
    }

    public class ClassParser : Parser
    {
        protected string GetName(string line)
        {
            return Filter(line, @"^.*\s?class\s(\w+<?.*?>?)\s", 0);
        }

        protected IEnumerable<string> GetInheritance(string line)
        {
            var pattern = string.Format(@"\b(?!{0})\b\w+", ALL_KEYWORDS);
            var matches = Regex.Matches(line, pattern);
            if (matches.Count > 1)
            {
                for (int i = 1; i < matches.Count; i++)
                {
                    var v = matches[i].Value;
                    yield return v;
                }
            }
        }

        protected IEnumerable<string> GetAutoProperties(string body, int indent)
        {
            var pattern = GetIndent(indent) + @"\w+.*\}$";
            var matches = Regex.Matches(body, pattern);
            for (int i = 0; i < matches.Count; i++)
            {
                var v = matches[i].Value;
                yield return v;
            }
        } 

        protected IEnumerable<string> GetSubclasses(int indent, string body)
        {
            var pattern = string.Format(@"(^\s{{0}}\w*\sclass\s\w*$)", indent * 4);
            var matches = Regex.Matches(body, pattern);
            for (int i = 0; i < matches.Count; i++)
            {
                var v = matches[i].Value;
                yield return v;
            }
        }

        public Class Parse(string source, int indent = 0)
        {
            var lines = source.Split('\n');
            string mainLine = null;

            foreach (var line in lines)
            {
                if (IsClass(line, indent))
                {
                    mainLine = line;
                    break;
                }
            }

            if (mainLine != null)
            {
                var name = GetName(mainLine);
                var protectionLevel = GetProtectionLevel(mainLine);
                var prefix = GetPrefix(mainLine);
                var cls = new Class(name, protectionLevel, prefix);
                var methodParser = new MethodParser();
                var fieldParser = new FieldParser();
                var propParser = new PropertyParser();

                foreach (var d in GetDirectives(source))
                {
                    cls.AddDirective(d);
                }
                foreach (var inh in GetInheritance(mainLine))
                {
                    cls.AddInherited(inh);
                }
                foreach (var r in GetRegions(source, 0))
                {
                    cls.AddRegion(r);
                }
                List<string> attributes = new List<string>(3);
                foreach (var line in lines)
                {
                    if (IsMethod(line, indent + 1))
                    {
                        var method = methodParser.Parse(source, line, indent + 1);
                        if (attributes.Count > 0)
                        {
                            method.AddAttributes(attributes.ToArray());
                            attributes.Clear();
                        }
                        cls.AddMember(method);
                    }
                    else if (IsClass(line, indent + 1))
                    {
                        cls.AddMember(Parse(GetClosureString(source, line, indent + 1), indent + 1));
                    }
                    else if (IsAttribute(line, indent))
                    {
                        var a = Regex.Replace(line, @"\s*[\[\]]", "");
                        cls.AddAttribute(a);
                    }
                    else if (IsAttribute(line, indent + 1))
                    {
                        var a = Regex.Replace(line, @"\s*[\[\]]", "");
                        attributes.Add(a);
                    }
                    else if (IsField(line, indent + 1))
                    {
                        var field = fieldParser.Parse(line);
                        if (attributes.Count > 0)
                        {
                            field.AddAttributes(attributes.ToArray());
                            attributes.Clear();
                        }
                        cls.AddMember(field);
                    }
                    else if (IsOneLineProperty(line, indent + 1))
                    {
                        var prop = propParser.ParseProp(source, line, indent + 1).SetOneLine(true);
                        if (attributes.Count > 0)
                        {
                            prop.AddAttributes(attributes.ToArray());
                            attributes.Clear();
                        }
                        cls.AddMember(prop);
                    }
                    else if (IsProperty(line, indent + 1))
                    {
                        var prop = propParser.ParseProp(source, line, indent + 1);
                        if (attributes.Count > 0)
                        {
                            prop.AddAttributes(attributes.ToArray());
                            attributes.Clear();
                        }
                        cls.AddMember(prop);
                    }
                    else if (IsAutoProperty(line, indent + 1))
                    {
                        var prop = propParser.ParseAutoProp(line);
                        if (attributes.Count > 0)
                        {
                            prop.AddAttributes(attributes.ToArray());
                            attributes.Clear();
                        }
                        cls.AddMember(prop);
                    }
                }
                return cls;
            }
            return null;
        }
    }
}
