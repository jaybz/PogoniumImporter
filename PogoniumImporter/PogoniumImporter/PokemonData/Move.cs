using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using SQLite.Net.Async;

namespace PogoniumImporter.PokemonData
{
    public class Move
    {
        [PrimaryKey,AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        [Ignore]
        public string CommonName
        {
            get
            {
                return Regex.Replace(Name, "_FAST$", "");
            }
        }

        [Ignore]
        public string FriendlyName {
            get {
                return Util.GetFriendlyName(Name);
            }
        }
    }
}
