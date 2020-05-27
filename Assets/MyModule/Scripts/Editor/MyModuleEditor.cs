using UnityEditor;
using MyModule;

namespace MyModule.Editor
{
    public class MyModuleEditor
    {
        [MenuItem("MyModuleTool/Test")]
        public static void MyModuleTool()
        {
            MyModuleTest t = new MyModuleTest();
            t.Start();
            t = null;
        }
    }
}
