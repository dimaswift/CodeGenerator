using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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

        public static string RemoveNewLines(string str)
        {
            return Regex.Replace(str, @"\t|\n|\r", "");
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
                var atr = RemoveNewLines(a);
                atrbs += string.Format("{0}[{1}]", indent, atr);
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
                if (string.IsNullOrEmpty(name)) return "";
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
            foreach (var p in parameter)
            {
                this.m_parameters.Add(p);
            }
            return this;
        }

        public Method AddLine(params string[] lines)
        {
            foreach (var l in lines)
            {
                this.m_lines.Add(l);
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
            return string.Format("{7}{4}{0}{6}{1} {2}({3})\n{4}{{\n{5}\n{4}}}",
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
            if (parameters.Count == 0) return "";
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
                var line = RemoveNewLines(m_lines[i]);
                body += indent + line;
                if (i < m_lines.Count - 1)
                    body += "\n";
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
            this(type, name, protectionLevel, prefix, "", "Fields")
        { }

        public Field(string type,
           string name,
           string protectionLevel) :
           this(type, name, protectionLevel, "", "", "Fields") { }

        public Field(string type,
           string name) :
           this(type, name, "", "", "", "Fields")  { }

        public Field() :
            this("", "", "", "", "", "Fields")
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

    public class Comment : Member
    {
        string m_comment;
        public Comment(string commentString) : 
            base("", "", "", "")
        {
            m_comment = commentString;
        }
        
        public override string ToString(int indentLevel)
        {
            var indent = GetIndentLevel(indentLevel);
            return string.Format("//{0}{1}", indent, m_comment);
        }
    }

    public class Property : AutoProperty
    {
        string m_fieldName;
        bool m_readOnly;
        bool m_useOneLine;
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

        public Property SetOneLine(bool oneLine)
        {
            m_useOneLine = oneLine;
            return this;
        }

        string GetFormat()
        {
            if(m_useOneLine)
            {
                return m_readOnly ?
@"{8}{5}{0}{6}{1} {2} {{ get {{{7} return {3}; }} }}"
:
@"{9}{5}{0}{6}{1} {2} {{ get {{{7} return {3}; }} {4}set {{{8} {3} = value; }} }}";
            }
            else
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

        public string nameSpace = "";
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

        public Class InsertMember(params Member[] member)
        {
            foreach (var m in member)
            {
                members.Insert(0, m);
            }
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
            StringBuilder builder = new StringBuilder(m_builderCapacity);
            this.m_indentLevel = indentLevel;
            if (!string.IsNullOrEmpty(nameSpace))
            {
                m_indentLevel++;
                builder.AppendLine("namespace " + nameSpace);
                builder.AppendLine("{");
            }
           
            m_indent = GetIndentLevel(m_indentLevel);
            AppendDirectives(builder);
            builder.AppendLine();
            AppentAttributes(builder);
            AppendClassName(builder);
            AppendInheritance(builder);
            builder.AppendLine();
            AppendOpenBracket(builder);
            AppendMembers(builder);
            AppendCloseBracket(builder);
            if (!string.IsNullOrEmpty(nameSpace))
            {
                builder.AppendLine("}");
            }
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
                var a = RemoveNewLines(at);
                builder.Append(GetIndentLevel(m_indentLevel));
                if (!a.Contains("["))
                {
                    builder.Append("[");
                    builder.Append(a);
                    builder.Append("]\n");
                } 
                else builder.AppendLine(a);
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
                    builder.AppendLine();
                }
            }
            return builder;
        }
    }
}
