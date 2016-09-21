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

        Debug.Log(string.Format("{0}", new Method("void", "Ass").AddAttributes("Attr1", "attr2").ToString(1))); 
   
    }
}
