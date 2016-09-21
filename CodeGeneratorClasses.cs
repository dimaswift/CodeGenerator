using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
namespace CodeGenerator
{
    public abstract class Member
    {
        public string protectionLevel { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public string parentRegion { get; set; }
        public string prefix { get; set; }

        protected List<string> m_attributes = new List<string>();

        public List<string> attributes { get { return m_attributes; } }

        protected Member(string type, string name, string protectionLevel = "", string parentRegion = "")
        {
            this.protectionLevel = protectionLevel;
            this.type = type;
            this.name = name;
            this.parentRegion = parentRegion;
        }

        const string INDENT_0 = "";
        const string INDENT_1 = "    ";
        const string INDENT_2 = "        ";
        const string INDENT_3 = "            ";

        public virtual string ToString(int indentLevel)
        {
            return GetIndentLevel(indentLevel) + ToString();
        }

        protected static string WithSemicolon(string value)
        {
            return value.Contains(";")
                || value == @"\n"
                || value == " "
                || value.Length < 2
                ? value : value + ";";
        }

        protected string GetIndentLevel(int level)
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

        protected string GetProtectionLevel()
        {
            return string.IsNullOrEmpty(protectionLevel) ? "" : protectionLevel + " ";
        }

        protected string GetName()
        {
            return Regex.Replace(name, @"[\s*\(\)\[\]-]", "");
        }

        protected string GetPrefix()
        {
            return string.IsNullOrEmpty(prefix) ? "" : prefix + " ";
        }

        protected string GetAttributes(int indentLevel)
        {
            string atrbs = string.Empty;
            if (m_attributes.Count == 0)
                return atrbs;
            string indent = GetIndentLevel(indentLevel);
            foreach (var a in m_attributes)
            {
                atrbs += string.Format("{0}[{1}]", indent, a);
                atrbs += '\n';
            }
            return atrbs;
        }
    }

    public class Method : Member
    {
        List<Parameter> m_parameters;
        List<string> m_lines;
        string m_returnValue;

        public List<Parameter> parameters { get { return m_parameters; } }
        public List<string> lines { get { return m_lines; } }
        public string returnValue { get { return m_returnValue; } }

        public Method(string type,
            string name,
            string prefix,
            string protectionLevel, 
            string region = "Methods", 
            params Parameter[] parameters) : 
            base(type, name, protectionLevel, region)
        {
            this.prefix = prefix;
            this.m_parameters = new List<Parameter>(parameters);
            this.m_returnValue = type == "void" ? "" : string.Format("default({0});", type);
            m_lines = new List<string>();
        }

        public Method( string type, 
            string name,
            string prefix,
            params Parameter[] parameters) : 
            this(type, name, prefix, "")
        { }

        public Method(string type, 
            string name, 
            params Parameter[] parameters) : 
            this(type, name, "", "", "", parameters)
        { }

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

        public Method SetReturnValue(string returnValue)
        {
            this.m_returnValue = WithSemicolon(returnValue);
            return this;
        }

        public Method AddAttributes(params string[] attributes)
        {
            foreach (var a in attributes)
            {
               this.m_attributes.Add(a);
            }
            return this;
        }

        public Method AddParameters(params Parameter[] parameter)
        {
            foreach (var p in m_parameters)
            {
                this.m_parameters.Add(p);
            }
            return this;
        }

        public Method AddLine(params string[] lines)
        {
            foreach (var l in lines)
            {
                this.m_lines.Add(WithSemicolon(l));
            }
            return this;
        }

        public Method SetPrefix(string prefix)
        {
            this.prefix = prefix;
            return this;
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public override string ToString(int indentLevel)
        {
            var indent = GetIndentLevel(indentLevel);
            return string.Format("{7}{4}{0}{6}{1} {2} ({3})\n{4}{{\n{5}\n{4}}}",
                GetProtectionLevel(),
                type,
                GetName(),
                GetParams(),
                indent,
                GetBody(indentLevel + 1),
                GetPrefix(),
                GetAttributes(indentLevel));
        }

        string GetParams()
        {
            string res = "";
            for (int i = 0; i < m_parameters.Count; i++)
            {
                res += m_parameters[i].ToString();
                if (i < m_parameters.Count - 1)
                    res += ", ";
                   
            }
            return res;
        }

        string GetBody(int indentLevel)
        {
            var indent = GetIndentLevel(indentLevel);
            string body = "";
            for (int i = 0; i < m_lines.Count; i++)
            {
                body += indent + m_lines[i];
                if (i < m_lines.Count - 1)
                    body += indent + "\n";
            }
            body += m_returnValue.Length > 0 ? "\n" + indent + "return " + m_returnValue : "";
            return body;
        }
    }

    public class Field : Member
    {
        string m_defaultFieldValue;

        public string defaultFieldValue { get { return m_defaultFieldValue; } }

        public Field(string type, 
            string name,
            string protectionLevel,
            string prefix,
            string defaultFieldValue,
            string region = "Fields") : 
            base(type, name, protectionLevel, region)
        {
            this.prefix = prefix;
            if (!string.IsNullOrEmpty(defaultFieldValue))
            {
                this.m_defaultFieldValue = defaultFieldValue.Contains("=") ?
                    defaultFieldValue : string.Format(" = {0}", defaultFieldValue);
            }
        }

        public Field(string type,
            string name,
            string protectionLevel,
            string prefix) :
            this(type, name, protectionLevel, prefix, null, "Fields")
        { }

        public Field(string type,
           string name,
           string protectionLevel) :
           this(type, name, protectionLevel, null, null, "Fields") { }

        public Field(string type,
           string name) :
           this(type, name, null, null, null, "Fields")  { }

        public Field() :
            this(null, null, null, null, null, "Fields")
        { }

        public Field AddAttributes(params string[] attrbs)
        {
            foreach (var a in attrbs)
            {
                m_attributes.Add(a);
            }
            return this;
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public override string ToString(int indent)
        {
            return string.Format(@"{6}{3}{0}{5}{1} {2}{4};",
                GetProtectionLevel(),
                type,
                GetName(),
                GetIndentLevel(indent),
                m_defaultFieldValue,
                GetPrefix(),
                GetAttributes(indent));
        }
    }

    public class AutoProperty : Member
    {
        protected string m_setterProtection;

        public string setterProtection { get { return m_setterProtection; } }

        public AutoProperty(string type, 
            string name,
            string protectionLevel,
            string setterProtection,
            string prefix,
            string region
           ) : 
            base(type, name, protectionLevel, region)
        {
            this.prefix = prefix;
            this.m_setterProtection = setterProtection;
        }

        public AutoProperty(string type,
           string name,
           string protectionLevel,
           string setterProtection,
           string prefix) :
           this(type, name, protectionLevel, setterProtection, prefix, "Properties")
        { }

        public AutoProperty(string type,
            string name,
            string protectionLevel) :
            this(type, name, protectionLevel, null, "Properties")
        { }

        public AutoProperty() : this(null, null, null)
        {

        }

        public AutoProperty AddAttributes(params string[] attrbs)
        {
            foreach (var a in attrbs)
            {
                m_attributes.Add(a);
            }
            return this;
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public override string ToString(int indent)
        {
            return string.Format("{6}{4}{0}{5}{1} {2} {{ get; {3}set; }}",
                GetProtectionLevel(),
                type,
                GetName(),
                GetSetter(),
                GetIndentLevel(indent),
                GetPrefix(),
                GetAttributes(indent));
        }

        protected string GetSetter()
        {
            return string.IsNullOrEmpty(m_setterProtection) ? "" : m_setterProtection + " ";
        }
    }

    public class Property : AutoProperty
    {
        string m_fieldName;
        bool m_readOnly;
        List<string> m_setterBody = new List<string>();
        List<string> m_getterBody = new List<string>();
   
        public string fieldName { get { return m_fieldName; } }
        public bool readOnly { get { return m_readOnly; } }
        public List<string> getterBody { get { return m_getterBody; } }
        public List<string> setterBody { get { return m_setterBody; } }

        public Property(string type,
            string name,
            string protectionLevel,
            string fieldName,
            string prefix,
            string setterProtection,
            string region) : 
            base(type, name, protectionLevel, setterProtection, prefix, region)
        {
            this.m_fieldName = string.IsNullOrEmpty(fieldName) ? "m_" + name.ToLower() : fieldName;
        }

        public Property(string type,
            string name,
            string protectionLevel,
            string fieldName,
            string prefix,
            string setterProtection) :
            this(type, name, protectionLevel, fieldName, prefix, setterProtection, "Properties")
        { }

        public Property(string type,
            string name,
            string protectionLevel,
            string fieldName,
            string prefix) :
            this(type, name, protectionLevel, fieldName, prefix, "")
        { }

        public Property(string type,
          string name) :
          this(type, name, "", "", "")
        { }

        public Property() :
         this("", "", "", "", "")
        { }

        public Property AddSetterBodyLine(string line)
        {
            m_setterBody.Add(line);
            return this;
        }

        public Property SetFieldName(string fieldName)
        {
            this.m_fieldName = fieldName;
            return this;
        }

        public Property AddGetterBodyLine(string line)
        {
            m_getterBody.Add(line);
            return this;
        }

        public Property SetReadonly(bool ro)
        {
            m_readOnly = ro;
            return this;
        }

        new public Property AddAttributes(params string[] attrbs)
        {
            foreach (var a in attrbs)
            {
                m_attributes.Add(a);
            }
            return this;
        }

        public override string ToString(int indentLevel)
        {
            var indent = GetIndentLevel(indentLevel);
            return m_readOnly ? string.Format(
                GetFormat(),
                GetProtectionLevel(),
                type,
                GetName(),
                GetFieldName(),
                GetSetter(),
                indent,
                GetPrefix(),
                GetSetterBody(indentLevel),
                GetAttributes(indentLevel))
            :
            string.Format(
                GetFormat(),
                GetProtectionLevel(),
                type,
                GetName(),
                GetFieldName(),
                GetSetter(),
                indent,
                GetPrefix(),
                GetGetterBody(indentLevel),
                GetSetterBody(indentLevel),
                GetAttributes(indentLevel));
        }

        protected string GetSetterBody(int indentLevel)
        {
            if (m_setterBody.Count == 0) return string.Empty;
            StringBuilder s = new StringBuilder(m_setterBody.Count);
            s.AppendLine();
            foreach (var line in m_setterBody)
            {
                s.Append(GetIndentLevel(indentLevel));
                s.AppendLine(line);
            }
            return s.ToString();
        }

        protected string GetGetterBody(int indentLevel)
        {
            if (m_getterBody.Count == 0) return string.Empty;
            StringBuilder s = new StringBuilder(m_getterBody.Count);
            s.AppendLine();
            foreach (var line in m_getterBody)
            {
                s.Append(GetIndentLevel(indentLevel));
                s.AppendLine(line);
            }
            return s.ToString();
        }

        string GetFieldName()
        {
            return Regex.Replace(m_fieldName, @"[\s*\(\)]", "");
        }

        string GetFormat()
        {
            return m_readOnly ?
@"{8}{5}{0}{6}{1} {2} 
{5}{{ 
    {5}get 
    {5}{{{7}
        {5}return {3};
    {5}}}
{5}}}"
:
@"{9}{5}{0}{6}{1} {2} 
{5}{{ 
    {5}get 
    {5}{{{7}
        {5}return {3};
    {5}}}
    {5}{4}set 
    {5}{{{8}
        {5}{3} = value;
    {5}}}
{5}}}";

        }
    }

    public class Class : Member
    {
        List<string> m_directives = new List<string>();
        List<string> m_regions = new List<string>();
        List<string> m_inherited = new List<string>();
        List<Member> m_members = new List<Member>();

        const string REGION = "#region ";
        const string ENDREGION = "#endregion ";

        string m_indent;
        int m_indentLevel;
        int m_builderCapacity = 10000;

        public List<Member> members { get { return m_members; } }
        public List<string> directives { get { return m_directives; } }
        public List<string> regions { get { return m_regions; } }
        public List<string> inherited { get { return m_inherited; } }

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
            this.prefix = prefix;
        }

        public Class SetIndentLevel(int indentLevel)
        {
            this.m_indentLevel = indentLevel;
            return this;
        }

        public Class AddMember(params Member[] member)
        {
            foreach (var m in member)
            {
                members.Add(m);
            }
            return this;
        }

        public Class AddAttribute(params string[] attributes)
        {
            foreach (var item in attributes)
            {
                this.m_attributes.Add(item);
            }
            return this;
        }

        public Class SetPrefix(string prefix)
        {
            this.prefix = prefix;
            return this;
        }

        public Class SetParentRegion(string parentRegion)
        {
            this.parentRegion = parentRegion;
            return this;
        }

        public Class AddDirective(params string[] directives)
        {
            foreach (var item in directives)
            {
                this.m_directives.Add(item);
            }
            return this;
        }

        public Class AddInherited(params string[] inh)
        {
            foreach (var item in inh)
            {
                m_inherited.Add(item);
            }
            return this;
        }

        public Class AddRegion(params string[] region)
        {
            foreach (var item in region)
            {
                m_regions.Add(item);
            }
            return this;
        }

        public void SetBuilderCapaciy(int cap)
        {
            m_builderCapacity = cap;
        }

        public override string ToString(int indentLevel)
        {
            this.m_indentLevel = indentLevel;
            StringBuilder builder = new StringBuilder(10000);
            m_indent = GetIndentLevel(indentLevel);
            AppendDirectives(builder);
            builder.AppendLine();
            AppentAttributes(builder);
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
            if (m_directives != null)
            {
                foreach (var dir in m_directives)
                {
                    builder.Append(m_indent);
                    builder.AppendLine("using " + dir + ";");
                }
            }
            return builder;
        }

        StringBuilder AppendInheritance(StringBuilder builder)
        {
            if (m_inherited != null && m_inherited.Count > 0)
            {
                builder.Append(" : ");
                for (int i = 0; i < m_inherited.Count; i++)
                {
                    var inh = m_inherited[i];
                    builder.Append(inh);
                    if (i < m_inherited.Count - 1)
                        builder.Append(", ");
                }
            }
            return builder;
        }

        StringBuilder AppentAttributes(StringBuilder builder)
        {
            foreach (var at in m_attributes)
            {
                builder.Append(GetIndentLevel(m_indentLevel));
                if (!at.Contains("["))
                {
                    builder.Append("[");
                    builder.Append(at);
                    builder.Append("]\n");
                } 
                else builder.AppendLine(at);
            }
            return builder;
        }

        StringBuilder AppendClassName(StringBuilder builder)
        {
            builder.Append(m_indent);
            var p = prefix;
            if (!string.IsNullOrEmpty(p))
                p += " ";
            builder.AppendFormat("{0}{3}{1} {2}", GetProtectionLevel(), type, name, p);
            return builder;
        }

        StringBuilder AppendOpenBracket(StringBuilder builder)
        {
            builder.Append(m_indent);
            builder.Append("{");
            builder.AppendLine();
            return builder;
        }

        StringBuilder AppendCloseBracket(StringBuilder builder)
        {
            builder.Append(m_indent);
            builder.Append("}");
            builder.AppendLine();
            return builder;
        }

        StringBuilder AppendMembers(StringBuilder builder)
        {
            if (m_regions != null && m_regions.Count > 0)
            {
                var nextIndent = GetIndentLevel(m_indentLevel + 1);
                bool appendedMemberWithNoRegions = false;
                foreach (var member in members)
                {
                    bool hasRegion = false;
                    foreach (var reg in m_regions)
                    {
                        if (member.parentRegion == reg)
                        {
                            hasRegion = true;
                        }
                    }
                    if(!hasRegion)
                    {
                        appendedMemberWithNoRegions = true;
                        builder.AppendLine(member.ToString(m_indentLevel + 1));
                    }
                }
                if(appendedMemberWithNoRegions)
                    builder.AppendLine();
                foreach (var reg in m_regions)
                {
                    builder.Append(nextIndent);
                    builder.Append(REGION);
                    builder.AppendLine(reg);
                    builder.AppendLine();
                    foreach (var member in members)
                    {
                        if (member.parentRegion == reg)
                        {
                            builder.AppendLine(member.ToString(m_indentLevel + 1));
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
                    builder.AppendLine(member.ToString(m_indentLevel + 1));
                }
            }
            return builder;
        }
    }

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
                if(line == startLine)
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
                    if(p > 0)
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
                    if(IsProtectionKeyword(word))
                    {
                        prot = word;
                    }
                    else if (IsPrefix(word))
                    {
                        prefix = word;
                    }
                }
            }
            if(prot != null && prefix != null)
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
                    name = words[t+1];
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
                if(lineIndex > 1)
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
                        cls.AddAttribute(Regex.Replace(line, @"\s*[\[\]]", ""));
                    }
                    else if (IsAttribute(line, indent + 1))
                    {
                        attributes.Add(Regex.Replace(line, @"\s*[\[\]]", ""));
                    }
                    else if (IsField(line, indent + 1))
                    {
                        var field = fieldParser.Parse(line);
                        if(attributes.Count > 0)
                        {
                            field.AddAttributes(attributes.ToArray());
                            attributes.Clear();
                        }
                        cls.AddMember(field);
                    }
                    else if(IsProperty(line, indent + 1))
                    {
                        var prop = propParser.ParseProp(source, line, indent + 1);
                        if (attributes.Count > 0)
                        {
                            prop.AddAttributes(attributes.ToArray());
                            attributes.Clear();
                        }
                        cls.AddMember(prop);
                    }
                    else if(IsAutoProperty(line, indent + 1))
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
            Debug.LogErrorFormat("{0} - doesn't contain class! Retruning null...", source.Substring(0, 20));
            return null;

        }

    }
 
}
