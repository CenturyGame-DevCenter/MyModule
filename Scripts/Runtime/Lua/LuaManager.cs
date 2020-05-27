//using XLua;

namespace CenturyGame.Framework
{
    public class LuaManager : FrameworkModule
    {
        //internal LuaEnv luaEnv = new LuaEnv();

        static readonly float GCInterval = 10.0f;
        float gcTime;

        public override void Init()
        {
        }

        public override void Update(float elapseTime, float realElapseTime)
        {
            gcTime += elapseTime;
            if (gcTime >= GCInterval)
            {
                //luaEnv.Tick();
                gcTime = 0;
            }
        }

        public override void LateUpdate()
        {
        }

        public override void Shutdown()
        {
        }
    }
}