namespace CenturyGame.Framework
{
    public abstract class FrameworkModule
    {
        public abstract void Init();

        public abstract void Update(float elapseTime, float realElapseTime);

        public abstract void LateUpdate(); 

        public abstract void Shutdown();
    }
}