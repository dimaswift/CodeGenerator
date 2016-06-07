using System.Collections.Generic;

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

        public Member(ProtectionLevel protectionLevel, string type, string name)
        {
            this.protectionLevel = protectionLevel;
            this.type = type;
            this.name = name;
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

    }



    public class Method : Member
    {
        List<Parameter> parameters;
        List<string> lines;
        public int indentLevel;
        string returnValue;
        public Method(ProtectionLevel protectionLevel, string prefix, string type, string name, params Parameter[] parameters) : base(protectionLevel, type, name)
        {
            this.parameters = new List<Parameter>(parameters);
            this.returnValue = type == "void" ? "" : string.Format("default({0});", type);
            lines = new List<string>();
        }
        public Method(string type, string prefix, string name, params Parameter[] parameters) : this(ProtectionLevel.None, prefix, type, name) { }

        public Method(string type, string name, params Parameter[] parameters) : this(ProtectionLevel.None, "", type, name) { }

        public Method(ProtectionLevel protectionLevel, string type, string name, params Parameter[] parameters) :  this(protectionLevel, "", type, name) { }

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
                if (type == "string" && !string.IsNullOrEmpty(defaultValue)) this.defaultValue = string.Format(" = \"{0}\"", defaultValue);
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
            return string.Format("{0}{1} {2} ({3})", protectionLevel, type, name, GetParams());
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
            return string.Format(@"{0}{1} {2} ({3}) 
                {{
                    {4}
                }}", protectionLevel, type, name, GetParams(), GetBody(INDENT_1));
        }

        public override string ToString(int indentLevel)
        {
            var indent = GetIndentLevel(indentLevel);
            return string.Format("{4}{0}{1} {2} ({3})\n{4}{{\n{5}\n{4}}}", protectionLevel, type, name, GetParams(), indent, GetBody(indent + INDENT_1));
        }
    }

    public class Field : Member
    {
        public Field(ProtectionLevel protectionLevel, string type, string name) : base(protectionLevel, type, name)
        {

        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2};", protectionLevel, type, name);
        }
    }

    public class AutoProperty : Member
    {
        public string setterProtection;

        public AutoProperty(ProtectionLevel protectionLevel, string type, string name, string setterProtection = null) : base(protectionLevel, type, name)
        {
            this.setterProtection = string.IsNullOrEmpty(setterProtection) ? string.Empty : setterProtection + " ";
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {{ get; {3}set; }}", protectionLevel, type, name, setterProtection);
        }
    }

    public class Property : AutoProperty
    {
        public string fieldName;

        public Property(ProtectionLevel protectionLevel, string type, string name, string fieldName = null, string setterProtection = null) : base(protectionLevel, type, name, setterProtection)
        {
            this.fieldName = string.IsNullOrEmpty(fieldName) ? "_" + name.ToLower() : fieldName;
        }

        public override string ToString()
        {
            return string.Format(@"{0} {1} {2} 
{{ 
    get 
    {{
        return {3};
    }}
    {4}set 
    {{
        {3} = value;
    }}
}}", protectionLevel, type, name, fieldName.ToLower(), setterProtection);

        }

        public override string ToString(int indentLevel)
        {
            var indent = GetIndentLevel(indentLevel);
            return string.Format(@"{5}{0} {1} {2} 
{5}{{ 
    {5}get 
    {5}{{
        {5}return {3};
    {5}}}
    {5}{4}set 
    {5}{{
        {5}{3} = value;
    {5}}}
{5}}}", protectionLevel, type, name, fieldName.ToLower(), string.IsNullOrEmpty(setterProtection) ? string.Empty : setterProtection + " ", indent);
        }
    }


    public class Class : Member
    {
        ClassType classType;
        string[] directives;
        string[] regions;
        List<Method> methods = new List<Method>();
        List<AutoProperty> properties = new List<AutoProperty>();
        List<Field> fields = new List<Field>();
        const string NEWLINE = "\n";
        const string REGION = "    #region ";
        const string ENDREGION = "    #endregion ";
        const string OPEN_PARENTESIS = "{";
        const string CLOSE_PARENTESIS = "}";
        public enum ClassType { Static, Sealed, Astract, Partial, None }
      
        public Class(ProtectionLevel protectionLevel, string name, string[] directives, string[] regions, ClassType classType = ClassType.None) : base(protectionLevel, "class", name)
        {
            this.classType = classType;
            this.regions = regions;
            this.directives = directives;
        }
        public Class AddMethod(Method method)
        {
            methods.Add(method);
            return this;
        }
        public override string ToString()
        {
            var body = "";
            foreach (var dir in directives)
            {
                body += "using " + dir + ";" + NEWLINE;
            }
            body += NEWLINE;
            body += string.Format("{0} {1} {2} \n{{", protectionLevel, type, name);
            body += NEWLINE;
            foreach (var reg in regions)
            {
                body += REGION + reg;
                body += NEWLINE;
                body += NEWLINE;
                body += NEWLINE;
                body += ENDREGION + reg;
                body += NEWLINE;
                body += NEWLINE;
            }
            
            body += CLOSE_PARENTESIS;
            return body;
        }

        public override string ToString(int indentLevel)
        {
            return ToString();
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
            if (propLine > 0)
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
                    lines.Insert(methodLineIndex, Member.GetIndentLevel(method.indentLevel + 1) + Member.WithSemicolon(newLines[i]));
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
         
            if (propLine > 0)
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
