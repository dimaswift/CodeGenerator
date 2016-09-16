using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
namespace CodeGenerator
{
    public abstract class Member
    {
        public string protectionLevel = "";
        public string type;
        public string name;
        public string parentRegion;
        public string prefix;

        public Member(string type, string name, string protectionLevel = "", string parentRegion = "")
        {
            this.protectionLevel = protectionLevel;
            this.type = type;
            this.name = name;
            this.parentRegion = parentRegion;
        }

        public const string INDENT_0 = "";
        public const string INDENT_1 = "    ";
        public const string INDENT_2 = "        ";
        public const string INDENT_3 = "            ";

        public static string GetIndentLevel(int level)
        {
            switch (level)
            {
                case 0:
                    return INDENT_0;
                case 1:
                    return INDENT_1;
                case 2:
                    return INDENT_2;
                case 3:
                    return INDENT_3;
                default:
                    return INDENT_0;
            }
        }

        public virtual string ToString(int indentLevel)
        {
            return GetIndentLevel(indentLevel) + ToString();
        }

        public static string WithSemicolon(string value)
        {
            return value.Contains(";") 
                || value == @"\n" 
                || value == " " 
                || value.Length < 2
                ? value : value + ";";
        }

    }



    public class Method : Member
    {
        List<Parameter> parameters;
        List<string> lines;
        public string returnValue;

        public Method(string protectionLevel, 
            string prefix, 
            string type, 
            string name, 
            string region = "Methods", 
            params Parameter[] parameters) : 
            base(protectionLevel, type, name, region)
        {
            this.prefix = prefix;
            this.parameters = new List<Parameter>(parameters);
            this.returnValue = type == "void" ? "" : string.Format("default({0});", type);
            lines = new List<string>();
        }

        public Method(string prefix,
            string type, 
            string name, 
            params Parameter[] parameters) : 
            this("", prefix, type, name) { }

        public Method(string type, 
            string name, 
            params Parameter[] parameters) : 
            this("", "", type, name) { }

        public Method() :
         this("", "", "", "")
        { }

        public class Parameter
        {
            public string type;
            public string name;
            public string defaultValue;

            public Parameter(string type, string name, string defaultValue = null)
            {
                this.type = type;
                this.name = name;
                this.defaultValue = string.IsNullOrEmpty(defaultValue) ? "" : " = " + defaultValue;
                if (type == "string" && !string.IsNullOrEmpty(defaultValue) && !defaultValue.StartsWith("\""))
                    this.defaultValue = string.Format(" = \"{0}\"", defaultValue);
            }

            public override string ToString()
            {
                return string.Format("{0} {1}{2}", type, name, defaultValue);
            }
        }

        public Method AddReturnValue(string returnValue)
        {
            this.returnValue = WithSemicolon(returnValue);
            return this;
        }

        public string GetParams()
        {
            string res = "";
            for (int i = 0; i < parameters.Count; i++)
            {
                res += parameters[i].ToString();
                if (i < parameters.Count - 1)
                    res += ", ";
                   
            }
            return res;
        }

        public Method AddParameter(Parameter parameter)
        {
            parameters.Add(parameter);
            return this;
        }

        public Method AddLine(string line)
        {
            lines.Add(WithSemicolon(line));
            return this;
        }

        public string GetFirstLine()
        {
            return string.Format("{0}{1} {2} ({3})", 
                protectionLevel, 
                type, 
                name, 
                GetParams());
        }

        string GetBody(string indent)
        {
            string body = "";
            for (int i = 0; i < lines.Count; i++)
            {
                body += indent + lines[i];
                if (i < lines.Count - 1)
                    body += indent + "\n";
            }
            body += returnValue.Length > 0 ? "\n" + indent + "return " + returnValue : "";
            return body;
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public override string ToString(int indentLevel)
        {
            var indent = GetIndentLevel(indentLevel);
            var p = protectionLevel.Length > 0 ? protectionLevel + " " : "";
            return string.Format("{4}{0}{1} {2} ({3})\n{4}{{\n{5}\n{4}}}",
                p, 
                type, 
                name, 
                GetParams(), 
                indent, 
                GetBody(indent + INDENT_1));
        }
    }

    public class Field : Member
    {
        protected string defaultFieldValue;
        public Field(string protectionLevel, 
            string type, 
            string name,
            string defaultFieldValue = null,
            string region = "Fields") : 
            base(protectionLevel, type, name, region)
        {
            if (!string.IsNullOrEmpty(defaultFieldValue))
            {
                this.defaultFieldValue = string.Format(" = {0}", defaultFieldValue);
            }
        }

        public Field(string protectionLevel,
           string type,
           string name) :
           this(protectionLevel, type, name, null, "Fields") { }

        public Field(string type,
           string name) :
           this("", type, name, null, "Fields")  { }

        public override string ToString(int indent)
        {
            return string.Format("{3}{0}{1} {2}{4};",
                protectionLevel,
                type, 
                name,
                GetIndentLevel(indent),
                defaultFieldValue);
        }
    }

    public class ConstField : Member
    {
        protected string constValue;
        public ConstField(string protectionLevel,
          string type,
          string name,
          string constValue,
          string region = "Fields") :
            base(protectionLevel, type, name, region)
        {
            this.constValue = constValue;
        }

        public ConstField(string protectionLevel,
            string type,
            string name,
            string constValue) :
          this(protectionLevel, type, name, constValue, "Constants")  { }

        public ConstField(string type,
           string name,
           string constValue) :
        this("", type, name, constValue, "Constants")  { }

        public override string ToString(int indent)
        {
            return string.Format("{3}{0}const {1} {2}{4};",
                protectionLevel,
                type,
                name,
                GetIndentLevel(indent),
                constValue);
        }
    }

    public class ReadonlyField : Field
    {
        public ReadonlyField(string protectionLevel,
            string type,
            string name,
            string defaultFieldValue,
            string region = "Fields") :
            base(protectionLevel, type, name, defaultFieldValue, region) { }

        public ReadonlyField(string protectionLevel,
            string type,
            string name,
            string defaultFieldValue) :
            this(protectionLevel, type, name, defaultFieldValue, "Readonly") { }

        public ReadonlyField(string type,
          string name,
          string defaultFieldValue) :
          this("", type, name, defaultFieldValue, "Readonly") { }

        public override string ToString(int indent)
        {
            return string.Format("{3}{0}readonly {1} {2}{4};",
                protectionLevel,
                type,
                name,
                GetIndentLevel(indent),
                defaultFieldValue);
        }
    }

    public class FieldPropertyPair : Property
    {
        protected string defaultFieldValue;

        public FieldPropertyPair(string protectionLevel,
            string type,
            string name,
            string defaultFieldValue,
            string region,
            string setterProtection) : 
            base(protectionLevel, type, name, region, "m_" + name.ToLower(), setterProtection)
        {
            if(!string.IsNullOrEmpty(defaultFieldValue))
            {
                this.defaultFieldValue = string.Format(" = {0}", defaultFieldValue);
            }
        }

        public FieldPropertyPair(string protectionLevel,
            string type,
            string name,
            string defaultFieldValue,
            string region) :
            this(protectionLevel, type, name, defaultFieldValue, region, "")  { }

        public FieldPropertyPair(string protectionLevel,
            string type,
            string name,
            string defaultFieldValue) :
            this(protectionLevel, type, name, defaultFieldValue, "Properties") { }

        public FieldPropertyPair(string protectionLevel,
            string type,
            string name) :
            this(protectionLevel, type, name, null)   { }

        public override string ToString(int indentLevel)
        {
            var indent = GetIndentLevel(indentLevel);
            return string.Format(@"
{5}{4}{1} {3}{6};
{5}{0}{1} {2} 
{5}{{ 
    {5}get 
    {5}{{
        {5}return {3};
    {5}}}
    {5}{4}set 
    {5}{{
        {5}{3} = value;
    {5}}}
{5}}}"
            , protectionLevel,
            type,
            name,
            fieldName,
            setterProtection,
            indent,
            defaultFieldValue);
        }
    }

    public class FieldPropertyPairReadonly : FieldPropertyPair
    {
        public FieldPropertyPairReadonly(string protectionLevel,
            string type,
            string name,
            string defaultFieldValue,
            string region,
            string setterProtection) : 
            base(protectionLevel, type, name, defaultFieldValue, region, setterProtection)
        { }

        public FieldPropertyPairReadonly(string protectionLevel,
            string type,
            string name,
            string defaultFieldValue,
            string region) :
            this(protectionLevel, type, name, defaultFieldValue, region, "")
        { }

        public FieldPropertyPairReadonly(string protectionLevel,
            string type,
            string name,
            string defaultFieldValue) :
         this(protectionLevel, type, name, defaultFieldValue, "Properties")
        { }

        public FieldPropertyPairReadonly(string protectionLevel,
            string type,
            string name) :
            this(protectionLevel, type, name, null)
        { }

        public override string ToString(int indentLevel)
        {
            var indent = GetIndentLevel(indentLevel);
            return string.Format(@"
{5}{4}{1} {3}{6};
{5}{0}{1} {2} 
{5}{{ 
    {5}get 
    {5}{{
        {5}return {3};
    {5}}}
{5}}}"
            , protectionLevel,
            type,
            name,
            fieldName,
            setterProtection,
            indent,
            defaultFieldValue);
        }
    }

    public class AutoProperty : Member
    {
        public string setterProtection;

        public AutoProperty(string protectionLevel, 
            string type, 
            string name, 
            string region,
            string setterProtection) : 
            base(protectionLevel, type, name)
        {
            this.parentRegion = region;
            this.setterProtection = setterProtection;
        }

        public AutoProperty(string protectionLevel,
           string type,
           string name,
           string region) :
           this(protectionLevel, type, name, region, "")
        { }

        public AutoProperty(string protectionLevel,
            string type,
            string name) :
            this(protectionLevel, type, name, "Properties")
        { }

        public override string ToString()
        {
            return string.Format("{0}{1} {2} {{ get; {3}set; }}",
                protectionLevel,
                type, 
                name,
                setterProtection);
        }

        public override string ToString(int indent)
        {
            return string.Format("{4}{0}{1} {2} {{ get; {3}set; }}",
                protectionLevel,
                type, 
                name,
                setterProtection, 
                GetIndentLevel(indent));
        }
    }

    public class Property : AutoProperty
    {
        public string fieldName;

        public Property(string protectionLevel, 
            string type,
            string name,
            string fieldName,
            string region,
            string setterProtection) : 
            base(protectionLevel, type, name, region, setterProtection)
        {
            this.fieldName = string.IsNullOrEmpty(fieldName) ? "m_" + name.ToLower() : fieldName;
        }

        public Property(string protectionLevel,
            string type,
            string name,
            string fieldName,
            string region) :
            this(protectionLevel, type, name, region, fieldName, "")
        { }

        public Property(string protectionLevel,
            string type,
            string name,
            string fieldName) :
            this(protectionLevel, type, name, fieldName, "Properties")
        { }

        public override string ToString(int indentLevel)
        {
            var indent = GetIndentLevel(indentLevel);
            return string.Format(@"{5}{0}{1} {2} 
{5}{{ 
    {5}get 
    {5}{{
        {5}return {3};
    {5}}}
    {5}{4}set 
    {5}{{
        {5}{3} = value;
    {5}}}
{5}}}"
            , protectionLevel,
            type, 
            name, 
            fieldName.ToLower(),
            setterProtection,
            indent);
        }
    }

    public class Class : Member
    {
        List<string> directives = new List<string>();
        List<string> regions = new List<string>();
        List<string> inherited = new List<string>();
        List<Member> members = new List<Member>();

        const string REGION = "#region ";
        const string ENDREGION = "#endregion ";
        const string OPEN_PARENTESIS = "{";
        const string CLOSE_PARENTESIS = "}";

        string indent;
        string prefix = " ";
        int indentLevel;
        int builderCapacity = 10000;

        public Class(string name) : base("class", name)
        { 

        }

        public Class(string name, string protectionLevel) :
            base("class", name, protectionLevel)
        {

        }

        public Class(string name, string protectionLevel, string prefix) :
            this(name, protectionLevel)
        {
            SetPrefix(prefix);
        }

        public Class SetIndentLevel(int indentLevel)
        {
            this.indentLevel = indentLevel;
            return this;
        }

        public Class AddMember(Member member)
        {
            members.Add(member);
            return this;
        }

        public Class SetPrefix(string prefix)
        {
            this.prefix = prefix;
            return this;
        }

        public Class AddDirective(string dir)
        {
            directives.Add(dir);
            return this;
        }

        public Class AddInherited(string inh)
        {
            inherited.Add(inh);
            return this;
        }

        public Class AddRegion(string reg)
        {
            regions.Add(reg);
            return this;
        }

        public void SetBuilderCapaciy(int cap)
        {
            builderCapacity = cap;
        }

        public override string ToString(int indentLevel)
        {
            this.indentLevel = indentLevel;
            StringBuilder builder = new StringBuilder(10000);
            indent = GetIndentLevel(indentLevel);
            AppendDirectives(builder);
            AppendClassName(builder);
            AppendInheritance(builder);
            builder.AppendLine();
            AppendOpenBracket(builder);
            AppendMembers(builder);
            AppendCloseBracket(builder);
            return builder.ToString();
        }

        public override string ToString()
        {
            return ToString(0);
        }

        StringBuilder AppendDirectives(StringBuilder builder)
        {
            if (directives != null)
            {
                foreach (var dir in directives)
                {
                    builder.Append(indent);
                    builder.AppendLine("using " + dir + ";");
                }
            }
            return builder;
        }

        StringBuilder AppendInheritance(StringBuilder builder)
        {
            if (inherited != null && inherited.Count > 0)
            {
                builder.Append(" : ");
                for (int i = 0; i < inherited.Count; i++)
                {
                    var inh = inherited[i];
                    builder.Append(inh);
                    if (i < inherited.Count - 1)
                        builder.Append(", ");
                }
            }
            return builder;
        }

        StringBuilder AppendClassName(StringBuilder builder)
        {
            builder.AppendLine();
            builder.Append(indent);
            if (!string.IsNullOrEmpty(prefix))
                prefix += " ";
            if (!string.IsNullOrEmpty(protectionLevel))
                protectionLevel += " ";
            builder.AppendFormat("{0}{3}{1} {2}", protectionLevel, type, name, prefix);
            return builder;
        }

        StringBuilder AppendOpenBracket(StringBuilder builder)
        {
            builder.Append(indent);
            builder.Append("{");
            builder.AppendLine();
            return builder;
        }

        StringBuilder AppendCloseBracket(StringBuilder builder)
        {
            builder.Append(indent);
            builder.Append("}");
            builder.AppendLine();
            return builder;
        }

        StringBuilder AppendMembers(StringBuilder builder)
        {
            if (regions != null && regions.Count > 0)
            {
                var nextIndent = GetIndentLevel(indentLevel + 1);
                bool appendedMemberWithNoRegions = false;
                foreach (var member in members)
                {
                    bool hasRegion = false;
                    foreach (var reg in regions)
                    {
                        if (member.parentRegion == reg)
                        {
                            hasRegion = true;
                        }
                    }
                    if(!hasRegion)
                    {
                        appendedMemberWithNoRegions = true;
                        builder.AppendLine(member.ToString(indentLevel + 1));
                    }
                }
                if(appendedMemberWithNoRegions)
                    builder.AppendLine();
                foreach (var reg in regions)
                {
                    builder.Append(nextIndent);
                    builder.Append(REGION);
                    builder.AppendLine(reg);
                    builder.AppendLine();
                    foreach (var member in members)
                    {
                        if (member.parentRegion == reg)
                        {
                            builder.AppendLine(member.ToString(indentLevel + 1));
                        }
                    }
                    builder.AppendLine();
                    builder.Append(nextIndent);
                    builder.Append(ENDREGION);
                    builder.AppendLine(reg);
                    builder.AppendLine();
                }
            }
            else
            {
                if(members.Count == 0)
                    builder.AppendLine();
                foreach (var member in members)
                {
                    builder.AppendLine(member.ToString(indentLevel + 1));
                }
            }
            return builder;
        }

    }

    public class Parser
    {
        public const int SPACES_PER_TAB = 4;
        public const string ALL_KEYWORDS = "const|event|delegate|public|static|class|private|protected|sealed|abstract|partial|set|get|override|readonly";
        public const string PREFIXES = "const|sealed|virtual|abstract|internal|static|override|readonly|delegate|event";
        public const string PROTECTION_LEVELS = "public|private|protected|internal";

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
            var pattern = string.Format(GetIndent(indent) + @"\#region.*", indent);
            var matches = Regex.Matches(body, pattern);
            for (int i = 0; i < matches.Count; i++)
            {
                var v = matches[i].Value;
                yield return v.Remove(0, 8);
            }
        }

        protected string GetRegionName(string line)
        {
            var pattern = @"^[ ]*\#region\s(\w+.*$)";
            var res = Regex.Match(line, pattern);
            return res.Groups.Count > 1 ? res.Groups[1].Value : null;
        }

        protected string GetClassName(string line)
        {
            var pattern = @"^.*\sclass\s(\w+)";
            var res = Regex.Match(line, pattern);
            return res.Groups.Count > 1 ? res.Groups[1].Value : null;
        }

        protected IEnumerable<string> GetClassInheritance(string line)
        {
            var pattern = @"^.*\sclass\s\w+\s?:\s?(.*$)";
            var res = Regex.Match(line, pattern);
            if(res.Groups.Count > 1)
            {
                line = res.Groups[1].Value;
                var inh = line.Split(',');
                foreach (var i in inh)
                {
                    yield return Regex.Replace(i, @"[\s*\{]", "");
                }
            }
        }

        protected IEnumerable<string> GetMembers(int indent, string body)
        {
            var pattern = string.Format(@"^[ ]{{0}}((\w+.*\n[ ]{{0}}\{)|(\w+.*\{$))", indent * SPACES_PER_TAB);
            var matches = Regex.Matches(body, pattern);
            for (int i = 0; i < matches.Count; i++)
            {
                var v = matches[i].Value;
                yield return v;
            }
        }

        protected IEnumerable<string> GetFields(int indent, string body)
        {
            var pattern = string.Format(@"^[ ]{{0}}\w+.*\;$", indent * SPACES_PER_TAB);
            var matches = Regex.Matches(body, pattern);
            for (int i = 0; i < matches.Count; i++)
            {
                var v = matches[i].Value;
                yield return v;
            }
        }

        protected bool IsMethod(string line)
        {
            var pattern = @"^[ ]*?((\w+.*\(.*\)\s?\n)|(\w+.*\(.*\)\n$))";
            var match = Regex.Match(line, pattern);
            return match.Length > 0;
        }

        protected string GetIndent(int indentLevel)
        {
            return indentLevel > 0 ? "^[ ]{" + indentLevel * SPACES_PER_TAB + "}" : "^";
        }

        protected bool IsMethod(string line, int indent)
        {
            var pattern = GetIndent(indent) + @"((\w+.*\(.*\)\s?\n)|(\w+.*\(.*\)\n?$))";
            var match = Regex.Match(line, pattern);
            return match.Success;
        }

        protected bool IsClass(string line)
        {
            var pattern = @"\s*class\s\w+.*$";
            var match = Regex.Match(line, pattern);
            return match.Success;
        }

        protected bool IsClass(string line, int indent)
        {
            var pattern = string.Format(@"^[ ]{{0}}\w+?\s?\w+?\s?class\s\w+.*$", indent * SPACES_PER_TAB);
            var match = Regex.Match(line, pattern);
            return match.Success;
        }

        protected IEnumerable<string> GetAutoProperties(int indent, string body)
        {
            var pattern = string.Format(@"^[ ]{{0}}\w+.*\}$", indent * SPACES_PER_TAB);
            var matches = Regex.Matches(body, pattern);
            for (int i = 0; i < matches.Count; i++)
            {
                var v = matches[i].Value;
                yield return v;
            }
        }

        protected IEnumerable<string> GetClosure(string body, string startPattern, int indentLevel)
        {
            var indent = GetIndent(indentLevel);
            var lines = body.Split('\n');
            bool opened = false, foundFirstLine = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if(Regex.IsMatch(line, startPattern))
                {
                    foundFirstLine = true;
                }
                if(foundFirstLine)
                {
                    if(!opened)
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

        protected string Filter(string line, string pattern, int groupIndex)
        {
            var groups = Regex.Match(line, pattern).Groups;
            if (groups.Count > groupIndex + 1)
                return groups[groupIndex + 1].Value;
            return string.Empty;
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
        public Method Parse(string body, string pattern, int indentLevel)
        {
            var method = new Method();
            int lineIndex = 0;
            foreach (var line in GetClosure(body, pattern, indentLevel))
            {
                if (lineIndex == 0)
                {
                    method.name = GetName(line);
                    method.type = GetType(line);
                    method.protectionLevel = GetProtectionLevel(line);
                    method.prefix = GetPrefix(line);
                    method.AddReturnValue(GetReturnValue(line));
                    foreach (var p in GetParameters(line))
                    {
                        var parameter = new MethodParameterParser().Parse(p);
                        method.AddParameter(parameter);
                    }
                }
                else
                {
                    if(line.Length > 2 && line != @" \n" && line != " ")
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
            var pattern = @".*\s\w+\s\((.*)\)";
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
            return Filter(line, @"^.*\sclass\s(\w+)\s", 0);
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

        public void Test()
        {
            var body = @"using UnityEngine;
using System.Collections;
using UnityEditor;
using CodeGenerator;
namespace DynamicUI
{
    [CustomEditor(typeof(DUIPanel), true)]
    public class DUIPanelEditor : Editor
    {
        public DUIPanel panel { get { return (DUIPanel) target; } }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

        }

        void GenerateCode()
        {
            string[] directives = new string[] { ""UnityEngine"", ""DynamicUI"", ""UnityEngine.UI"" };
            string[] regions = new string[] { ""Elements"" };
            string[] inherited = new string[] { ""DUIPanel"" };
            var file = Application.dataPath + "" / Scripts/"" + panel.name + "".cs"";

            var script = System.IO.File.OpenText(file);

            var parser = new ClassParser();
            parser.Test();
            var cls = parser.Parse(script.ReadToEnd());
            script.Close();
        }
    }
}
";
            var method = new MethodParser().Parse(body, @"void OnInspectorGUI\(\)$?", 2);
            Debug.Log(string.Format("{0}", method.ToString(1))); 
        }

        public Class Parse(string source)
        {
            var lines = source.Split('\n');
            string name = null;
            string inheritance = null;
            string mainLine = null;
            foreach (var line in lines)
            {
                if (line.Contains("class"))
                {
                    mainLine = line;
                    break;
                }
            }
            if(mainLine != null)
            {
                name = GetName(mainLine);

                var protectionLevel = GetProtectionLevel(mainLine);
                var prefix = GetPrefix(mainLine);
                var cls = new Class(name, protectionLevel, prefix);

                foreach (var inh in GetInheritance(mainLine))
                {
                    cls.AddInherited(inh);
                }

                foreach (var inh in GetRegions(source, 0))
                {
                    cls.AddRegion(inh);
                }

                if (!string.IsNullOrEmpty(inheritance))
                {
                    name += " : " + inheritance;
                }

                foreach (var line in lines)
                {
                    if (line.StartsWith("using"))
                    {
                        var l = line;
                        l = l.Remove(l.Length - 2, 2);
                        var dir = l.Substring(6);
                        cls.AddDirective(dir);
                    }
                }
                return cls;
            }
            Debug.LogErrorFormat("{0} - doesn't contain class! Retruning null...", source.Substring(0, 20)); 
            return null;
       
        }
}

    public class ClassBuilder
    {
        public List<string> lines = new List<string>();

        public ClassBuilder(string file)
        {
            lines = new List<string>(file.Split('\n'));
        }

        public void AddProperty(Property prop)
        {
            var propLine = GetRegionLine("Properties");
            if (propLine >= 0)
            {
                lines.Insert(propLine, prop.ToString(1));
            }
        }

        public ClassBuilder AppendLineToMethod(Method method, params string[] newLines)
        {
            var methodString = method.GetFirstLine();
            int methodLineIndex = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Contains(methodString))
                {
                    methodLineIndex = i;
                    break;
                }
            }
            if(methodLineIndex >= 0)
            {
                var end = "";
                while(!end.Contains("}") && methodLineIndex < lines.Count - 1)
                {
                    methodLineIndex++;
                    end = lines[methodLineIndex];
                }
                //     methodLineIndex-=2;
#if UNITY_EDITOR
                UnityEngine.Debug.Log(string.Format("{0}", methodLineIndex));
#endif
                for (int i = 0; i < newLines.Length; i++)
                {
                    lines.Insert(methodLineIndex, Member.GetIndentLevel(1) + Member.WithSemicolon(newLines[i]));
                }
            }
            return this;
        }

        public ClassBuilder InsertEmptyLine(int index)
        {
            lines.Insert(index, "\n");
            return this;
        }

        public void AddMember(Member member, string region, int indentLevel = 1)
        {
            var propLine = GetRegionLine(region);
         
            if (propLine >= 0)
            {
                lines.Insert(propLine, member.ToString(indentLevel));
            }
        }

        public override string ToString()
        {
            string res = string.Empty;
            for (int i = 0; i < lines.Count; i++)
            {
                res += lines[i] + "\n";
            }
            return res;
        }

        int GetRegionLine(string name)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains("#endregion " + name))
                    return i - 1;    
            }
#if UNITY_EDITOR
            UnityEngine.Debug.LogWarning("Region " + name + " not found in the file. Check to see if your class contains #endregion " + name + ".");
#endif
            return -1;
        }
    }
}
