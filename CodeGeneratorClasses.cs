using System.Collections.Generic;
using System.Text;

namespace CodeGenerator
{
    public abstract class Member
    {
        public enum ProtectionLevel
        {
            Private, Protected, Public, None
        }
        public ProtectionLevel protectionLevel;

        public string type;
        public string name;
        public string parentRegion;

        public Member(ProtectionLevel protectionLevel, string type, string name, string parentRegion = "")
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
            return value.EndsWith(";") ? value : value + ";";
        }

        public string GetProtectionString()
        {
            return protectionLevel == ProtectionLevel.None ? "" : 
                string.Format("{0} ", protectionLevel).ToLower();
        }

        public string GetProtectionString(ProtectionLevel pl)
        {
            return pl == ProtectionLevel.None ? "" :
                string.Format("{0} ", pl).ToLower();
        }
    }



    public class Method : Member
    {
        List<Parameter> parameters;
        List<string> lines;
        string returnValue;

        public Method(ProtectionLevel protectionLevel, 
            string prefix, 
            string type, 
            string name, 
            string region = "Methods", 
            params Parameter[] parameters) : 
            base(protectionLevel, type, name, region)
        {
            this.parameters = new List<Parameter>(parameters);
            this.returnValue = type == "void" ? "" : string.Format("default({0});", type);
            lines = new List<string>();
        }
        public Method(string type, 
            string prefix, 
            string name, 
            params Parameter[] parameters) : 
            this(ProtectionLevel.None, prefix, type, name) { }

        public Method(string type, 
            string name, 
            params Parameter[] parameters) : 
            this(ProtectionLevel.None, "", type, name) { }

        public Method(ProtectionLevel protectionLevel, 
            string type, 
            string name, 
            params Parameter[] parameters) : 
            this(protectionLevel, "", type, name) { }

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
                if (type == "string" && !string.IsNullOrEmpty(defaultValue))
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
                GetProtectionString(), 
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

        public override string ToString(int indentLevel)
        {
            var indent = GetIndentLevel(indentLevel);
            return string.Format("{4}{0}{1} {2} ({3})\n{4}{{\n{5}\n{4}}}", 
                GetProtectionString(), 
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
        public Field(ProtectionLevel protectionLevel, 
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

        public Field(ProtectionLevel protectionLevel,
           string type,
           string name) :
           this(protectionLevel, type, name, null, "Fields") { }

        public Field(string type,
           string name) :
           this(ProtectionLevel.None, type, name, null, "Fields")  { }

        public override string ToString(int indent)
        {
            return string.Format("{3}{0}{1} {2}{4};",
                GetProtectionString(),
                type, 
                name,
                GetIndentLevel(indent),
                defaultFieldValue);
        }
    }

    public class ConstField : Member
    {
        protected string constValue;
        public ConstField(ProtectionLevel protectionLevel,
          string type,
          string name,
          string constValue,
          string region = "Fields") :
            base(protectionLevel, type, name, region)
        {
            this.constValue = constValue;
        }

        public ConstField(ProtectionLevel protectionLevel,
            string type,
            string name,
            string constValue) :
          this(protectionLevel, type, name, constValue, "Constants")  { }

        public ConstField(string type,
           string name,
           string constValue) :
        this(ProtectionLevel.None, type, name, constValue, "Constants")  { }

        public override string ToString(int indent)
        {
            return string.Format("{3}{0}const {1} {2}{4};",
                GetProtectionString(),
                type,
                name,
                GetIndentLevel(indent),
                constValue);
        }
    }

    public class ReadonlyField : Field
    {
        public ReadonlyField(ProtectionLevel protectionLevel,
            string type,
            string name,
            string defaultFieldValue,
            string region = "Fields") :
            base(protectionLevel, type, name, defaultFieldValue, region) { }

        public ReadonlyField(ProtectionLevel protectionLevel,
            string type,
            string name,
            string defaultFieldValue) :
            this(protectionLevel, type, name, defaultFieldValue, "Readonly") { }

        public ReadonlyField(string type,
          string name,
          string defaultFieldValue) :
          this(ProtectionLevel.None, type, name, defaultFieldValue, "Readonly") { }

        public override string ToString(int indent)
        {
            return string.Format("{3}{0}readonly {1} {2}{4};",
                GetProtectionString(),
                type,
                name,
                GetIndentLevel(indent),
                defaultFieldValue);
        }
    }

    public class FieldPropertyPair : Property
    {
        protected string defaultFieldValue;

        public FieldPropertyPair(ProtectionLevel protectionLevel,
            string type,
            string name,
            string defaultFieldValue,
            string region,
            ProtectionLevel setterProtection) : 
            base(protectionLevel, type, name, region, "m_" + name.ToLower(), setterProtection)
        {
            if(!string.IsNullOrEmpty(defaultFieldValue))
            {
                this.defaultFieldValue = string.Format(" = {0}", defaultFieldValue);
            }
        }

        public FieldPropertyPair(ProtectionLevel protectionLevel,
            string type,
            string name,
            string defaultFieldValue,
            string region) :
            this(protectionLevel, type, name, defaultFieldValue, region, ProtectionLevel.None)  { }

        public FieldPropertyPair(ProtectionLevel protectionLevel,
            string type,
            string name,
            string defaultFieldValue) :
            this(protectionLevel, type, name, defaultFieldValue, "Properties") { }

        public FieldPropertyPair(ProtectionLevel protectionLevel,
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
            , GetProtectionString(),
            type,
            name,
            fieldName,
            GetProtectionString(setterProtection),
            indent,
            defaultFieldValue);
        }
    }

    public class FieldPropertyPairReadonly : FieldPropertyPair
    {
        public FieldPropertyPairReadonly(ProtectionLevel protectionLevel,
            string type,
            string name,
            string defaultFieldValue,
            string region,
            ProtectionLevel setterProtection) : 
            base(protectionLevel, type, name, defaultFieldValue, region, setterProtection)
        { }

        public FieldPropertyPairReadonly(ProtectionLevel protectionLevel,
            string type,
            string name,
            string defaultFieldValue,
            string region) :
            this(protectionLevel, type, name, defaultFieldValue, region, ProtectionLevel.None)
        { }

        public FieldPropertyPairReadonly(ProtectionLevel protectionLevel,
            string type,
            string name,
            string defaultFieldValue) :
         this(protectionLevel, type, name, defaultFieldValue, "Properties")
        { }

        public FieldPropertyPairReadonly(ProtectionLevel protectionLevel,
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
            , GetProtectionString(),
            type,
            name,
            fieldName,
            GetProtectionString(setterProtection),
            indent,
            defaultFieldValue);
        }
    }

    public class AutoProperty : Member
    {
        public ProtectionLevel setterProtection;

        public AutoProperty(ProtectionLevel protectionLevel, 
            string type, 
            string name, 
            string region,
            ProtectionLevel setterProtection) : 
            base(protectionLevel, type, name)
        {
            this.parentRegion = region;
            this.setterProtection = setterProtection;
        }

        public AutoProperty(ProtectionLevel protectionLevel,
           string type,
           string name,
           string region) :
           this(protectionLevel, type, name, region, ProtectionLevel.None)
        { }

        public AutoProperty(ProtectionLevel protectionLevel,
            string type,
            string name) :
            this(protectionLevel, type, name, "Properties")
        { }

        public override string ToString()
        {
            return string.Format("{0}{1} {2} {{ get; {3}set; }}", 
                GetProtectionString(),
                type, 
                name,
                GetProtectionString(setterProtection));
        }

        public override string ToString(int indent)
        {
            return string.Format("{4}{0}{1} {2} {{ get; {3}set; }}",
                GetProtectionString(),
                type, 
                name,
                GetProtectionString(setterProtection), 
                GetIndentLevel(indent));
        }
    }

    public class Property : AutoProperty
    {
        public string fieldName;

        public Property(ProtectionLevel protectionLevel, 
            string type,
            string name,
            string fieldName,
            string region,
            ProtectionLevel setterProtection) : 
            base(protectionLevel, type, name, region, setterProtection)
        {
            this.fieldName = string.IsNullOrEmpty(fieldName) ? "m_" + name.ToLower() : fieldName;
        }

        public Property(ProtectionLevel protectionLevel,
            string type,
            string name,
            string fieldName,
            string region) :
            this(protectionLevel, type, name, region, fieldName, ProtectionLevel.None)
        { }

        public Property(ProtectionLevel protectionLevel,
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
            , GetProtectionString(),
            type, 
            name, 
            fieldName.ToLower(),
            GetProtectionString(setterProtection),
            indent);
        }
    }

    public class Class : Member
    {
        public enum ClassType { Static, Sealed, Astract, Partial, None }

        ClassType classType;
        List<string> directives;
        List<string> regions;
        List<string> inherited;
        List<Member> members = new List<Member>();
        const string REGION = "#region ";
        const string ENDREGION = "#endregion ";
        const string OPEN_PARENTESIS = "{";
        const string CLOSE_PARENTESIS = "}";
        string indent;
        int indentLevel;
        int builderCapacity = 10000;

        public Class(ProtectionLevel protectionLevel,
            ClassType classType,
            string name,
            string[] inherited,
            string[] directives, 
            string[] regions, 
            string region) : base(protectionLevel, "class", name, region)
        {
            this.classType = classType;
            this.regions = regions == null ? new List<string>() : new List<string>(regions);
            this.directives = directives == null ? new List<string>() : new List<string>(directives);
            this.inherited = inherited == null ? new List<string>() : new List<string>(inherited);
        }

        public Class(ProtectionLevel protectionLevel,
            ClassType classType,
            string name,
            string[] inherited,
            string[] directives,
            string[] regions) :
            this(protectionLevel, classType, name, inherited, directives, regions, null)
         {  }

        public Class(ProtectionLevel protectionLevel,
            ClassType classType,
            string name,
            string[] inherited,
            string[] directives) :
            this(protectionLevel, classType, name, inherited, directives, new string[0])
        { }

        public Class(ProtectionLevel protectionLevel,
            ClassType classType,
            string name,
            string[] inherited) :
            this(protectionLevel, classType, name, inherited, new string[0])
        { }

        public Class(ProtectionLevel protectionLevel,
            ClassType classType,
            string name) :
            this(protectionLevel, classType, name, new string[0])
        { }


        public Class(ProtectionLevel protectionLevel,
            string name) :
            this(protectionLevel, ClassType.None, name, new string[0])
        { }

        public Class(string name) :
            this(ProtectionLevel.Public, ClassType.None, name)
        { }



        public Class AddMember(Member member)
        {
            members.Add(member);
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
            var ct = classType == ClassType.None  ? "" :string.Format("{0} ", classType).ToLower();
            builder.AppendFormat("{0}{3}{1} {2}", GetProtectionString(), type, name, ct);
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
