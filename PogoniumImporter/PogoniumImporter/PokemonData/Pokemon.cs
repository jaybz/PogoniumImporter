using SQLite.Net.Async;
using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PogoniumImporter.PokemonDatabase;

namespace PogoniumImporter.PokemonData
{
    public class Pokemon
    {
        [PrimaryKey]
        public string Name { get; set; }

        public int PokedexNumber { get; set; }

        [Ignore]
        public string FriendlyName
        {
            get
            {
                return Util.GetFriendlyName(Name);
            }
        }

        [Ignore]
        public Move[] QuickMoves {
            get
            {
                string[] moveStrings = SerializedQuickMoves.Split(',');
                Move[] moves = null;
                SQLiteAsyncConnection db =  Database.GetConnection();

                Task.Run(async () =>
                {
                    moves = (await db.Table<Move>().Where(m => moveStrings.Contains(m.Name)).ToListAsync()).ToArray();
                }).Wait();

                return moves;
            }
            set
            {
                string[] moveStrings = new string[value.Count()];
                for (int i = 0; i < value.Count(); i++)
                {
                    moveStrings[i] = value[i].Name;
                }
                SerializedQuickMoves = string.Join(",", moveStrings);
            }
        }

        [Ignore]
        public Move[] ChargeMoves {
            get
            {
                string[] moveStrings = SerializedChargeMoves.Split(',');
                Move[] moves = null;
                SQLiteAsyncConnection db = Database.GetConnection();

                Task.Run(async () =>
                {
                    moves = (await db.Table<Move>().Where(m => moveStrings.Contains(m.Name)).ToListAsync()).ToArray();
                }).Wait();

                return moves;
            }
            set
            {
                string[] moveStrings = new string[value.Count()];
                for (int i = 0; i < value.Count(); i++)
                {
                    moveStrings[i] = value[i].Name;
                }
                SerializedChargeMoves = string.Join(",", moveStrings);
            }
        }

        public string SerializedQuickMoves { get; set; }
        public string SerializedChargeMoves { get; set; }

        public int BaseStamina { get; set; }
        public int BaseAttack { get; set; }
        public int BaseDefense { get; set; }
    }
}
