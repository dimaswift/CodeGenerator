using UnityEngine;
using System.Collections;
using CodeGenerator;

public class CodeGeneratorTest : MonoBehaviour
{
    void Start()
    {
        GenerateCode();
    }

    void GenerateCode()
    {
        string[] directives = new string[] { "UnityEngine", "DynamicUI", "UnityEngine.UI" };
        string[] regions = new string[] { "Elements" };
        string[] inherited = new string[] { "DUIPanel" };

        var script = System.IO.File.CreateText(Application.dataPath + "/Scripts/" + name + ".cs");
        var cls = new Class(Member.ProtectionLevel.Public, Class.ClassType.Sealed, "Bae", inherited, directives, regions);
        cls.AddMember(new Class("Class1"));
        cls.AddMember(new Class(Member.ProtectionLevel.Public, "Class2").AddRegion("Ass"));
        cls.AddMember(new Class(Member.ProtectionLevel.Public, Class.ClassType.Partial, "Class3").AddInherited("Class1"));
        cls.AddMember(new Class(Member.ProtectionLevel.Public, Class.ClassType.Partial, "Class4", inherited, null, null, "Cool").AddRegion("Cool").AddMember(new Field(Member.ProtectionLevel.None, "string", "assh", null, "Cool")));
        script.Write(cls.ToString(0));
        script.Close();
    }
}
