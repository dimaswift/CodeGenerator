using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using CodeGenerator;

public class CodeBuilder : EditorWindow
{
    [MenuItem("Tools/Code Builder")]
    public static void ShowWindow()
    {
        var w = GetWindow<CodeBuilder>(true, "Code Builder", true);
        w.Show(true);
    }

    public void Write(string target, string path)
    {
        var writer = File.CreateText(path);
        writer.Write(target);
        writer.Close();
    }



    public ClassBuilder Open(string path)
    {
        var writer = File.OpenText(path);
        var builder = new ClassBuilder(writer.ReadToEnd());
        writer.Close();
        return builder;
    }

    void OnGUI()
    {
        //var rect = EditorGUILayout.GetControlRect();
        //rect.height = 50;
        //rect.y += 50;
        //var cls = new Class(Member.ProtectionLevel.Public, "Test : MonoBehaviour", new string[] { "UnityEngine", "System.Collections", "UnityEngine.UI" }, new string[] { "Props", "Methods" });
        //cls.AddMethod(new Method("public", "void", "Init"));
        //if(GUI.Button(rect, "Generate"))
        //{
        //    Write(cls.ToString(), Application.dataPath + "/Test.cs");
        //    Debug.Log(string.Format("{0}", Application.dataPath + "/Test.cs")); 
        //}
        //rect.y += 70;
        //if (GUI.Button(rect, "Open"))
        //{
        //    var builder = Open(Application.dataPath + "/Test.cs");
        //    builder.AddMember(new AutoProperty(Member.ProtectionLevel.Public, "string", "pip2"), "Props");
        //    var method = new Method("public", "int", "Kill").
        //        AddLine("var kill = 0").
        //        AddLine("kill++").
        //        AddParameter(new Method.Parameter("string", "name")).AddParameter(new Method.Parameter("int", "count", "5"));

        //    builder.AddMember(method, "Methods", 1);
        //    builder.AppendLineToMethod(method, "kill = -100");
        //    Write(builder.ToString(), Application.dataPath + "/Test.cs");
        //}
    }

}
