using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldRecords.Entities
{
    [Serializable]
    class Record
    {
        public string id;
        public string game;
        public string category;
        public string subcats;

        public Record(string id, string game, string category, string subcats)
        {
            this.id = id;
            this.game = game;
            this.category = category;
            this.subcats = subcats;
        }

        public bool IsSameCategory(Record record)
        {
            return game == record.game
                && category == record.category
                && subcats == record.subcats;
        }

        public bool Equals(Record record)
        {
            return id == record.id
                && game == record.game
                && category == record.category
                && subcats == record.subcats;
        }
    }
}
