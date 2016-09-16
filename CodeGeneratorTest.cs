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
      
      //  script.Write(cls.ToString(0));
        script.Close();
    }
}
