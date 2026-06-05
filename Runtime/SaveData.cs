using System;

namespace Dreamy.Datasave
{
    [Serializable]
    public abstract class SaveData
    {
        public virtual int Version => 1;

        public virtual string SaveKey => GetType().Name;

        public virtual void OnBeforeSave()
        {
        }

        public virtual void OnAfterLoad()
        {
        }

        public virtual void Migrate(int fromVersion)
        {
        }
    }
}
