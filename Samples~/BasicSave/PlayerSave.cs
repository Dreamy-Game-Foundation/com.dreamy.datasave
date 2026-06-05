using System;
using System.Collections.Generic;
using Dreamy.Datasave;

namespace Dreamy.Datasave.Samples
{
    [Serializable]
    public sealed class PlayerSave : SaveData
    {
        public int Coins;
        public Dictionary<string, int> Items = new();
    }
}
