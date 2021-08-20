using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldRecords.Entities
{
    [Serializable]
    class Game
    {
        public string id;
        public string abbreviation;
        public string name;
        public string hardware;
        public string color;
        public string hoverColor;
        public GameIgnoredVariable[] ignoredVariables;
        public string[] verifiers;
    }

    [Serializable]
    class GameIgnoredVariable
    {
        public string id;
        public string value;
    }
}
