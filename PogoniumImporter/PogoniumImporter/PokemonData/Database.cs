using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.Net.Async;
using SQLite.Net;
using SQLite.Net.Interop;
using System.IO;
using System.Net.Http;
using System.Json;
using System.Text.RegularExpressions;
using PogoniumImporter.PokemonData;

namespace PogoniumImporter.PokemonDatabase
{
    public class Database
    {
        private const string RetrieveUrl = "http://pogonium.com/GAME_MASTER.json";
        private const string DatabaseFile = "game_master.db3";

        private static string path = "";
        private static ISQLitePlatform sqlitePlatform = null;

        private static SQLiteAsyncConnection db = null;

        public static SQLiteAsyncConnection GetConnection()
        {
            if (db == null)
                db = new SQLiteAsyncConnection(
                    new Func<SQLiteConnectionWithLock>(() => new SQLiteConnectionWithLock(
                        sqlitePlatform,
                        new SQLiteConnectionString(Path.Combine(path, DatabaseFile), storeDateTimeAsTicks: false)
                        )
                    )
                );

            return db;
        }

        public static void SetPlatform(ISQLitePlatform sqlitePlatform)
        {
            Database.sqlitePlatform = sqlitePlatform;
        }

        public static void SetPath(string path)
        {
            Database.path = path;
        }

        public static async Task Initialize()
        {
            SQLiteAsyncConnection connection = GetConnection();
            await connection.CreateTableAsync<Timestamp>().ConfigureAwait(false);
            await connection.CreateTableAsync<Pokemon>().ConfigureAwait(false);
            await connection.CreateTableAsync<Move>().ConfigureAwait(false);
        }

        public static async Task RefreshData(IProgress<int> progress = null)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(RetrieveUrl).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            JsonValue gameMaster = JsonValue.Parse(json);

            SQLiteAsyncConnection db = GetConnection();
            Timestamp t = await db.Table<Timestamp>().OrderByDescending(timestamp => timestamp.TimestampMs).FirstOrDefaultAsync().ConfigureAwait(false);
            long latest = t == null ? 0 : t.TimestampMs;
            long gameMasterTimestamp = long.Parse(gameMaster["timestampMs"]);

            if (latest == 0 || latest < gameMasterTimestamp)
            {
                List<Move> moveList = new List<Move>();
                List<Pokemon> pokeList = new List<Pokemon>();

                JsonArray itemTemplates = (JsonArray)gameMaster["itemTemplates"];

                int current = 0;
                int total = itemTemplates.Cast<JsonValue>().Count();

                if (progress != null)
                    progress.Report(total);

                // parse moves and pokemon
                foreach (JsonValue item in itemTemplates.Cast<JsonValue>())
                {
                    if (progress != null)
                        progress.Report(++current);

                    if (item.ContainsKey("moveSettings") && item["moveSettings"] != null)
                    {
                        JsonValue moveJson = item["moveSettings"];
                        moveList.Add(new Move
                        {
                            Name = moveJson["movementId"]
                        });
                    }
                    else if (item.ContainsKey("pokemonSettings") && item["pokemonSettings"] != null)
                    {
                        string templateId = item["templateId"];
                        JsonValue pokemonJson = item["pokemonSettings"];
                        string pokemonName = pokemonJson["pokemonId"];
                        string idString = Regex.Replace(templateId, @"\D+", "");
                        int id = int.Parse(idString);

                        string[] qMoveStrings = ((JsonArray)pokemonJson["quickMoves"]).Select(x => (string)x).ToArray();
                        string[] cMoveStrings = ((JsonArray)pokemonJson["cinematicMoves"]).Select(x => (string)x).ToArray();

                        pokeList.Add(new Pokemon()
                        {
                            Name = pokemonName,
                            PokedexNumber = id,

                            BaseAttack = pokemonJson["stats"]["baseAttack"],
                            BaseDefense = pokemonJson["stats"]["baseDefense"],
                            BaseStamina = pokemonJson["stats"]["baseStamina"],

                            SerializedQuickMoves = string.Join(",", qMoveStrings),
                            SerializedChargeMoves = string.Join(",", cMoveStrings)
                        });
                    }
                }

                if (progress != null)
                    progress.Report(++current);

                Timestamp newTimeStamp = new Timestamp()
                {
                    TimestampMs = gameMasterTimestamp
                };

                await db.DeleteAllAsync<Pokemon>().ConfigureAwait(false);
                await db.DeleteAllAsync<Move>().ConfigureAwait(false);

                await db.InsertAllAsync(moveList).ConfigureAwait(false);
                await db.InsertAllAsync(pokeList).ConfigureAwait(false);
                await db.InsertAsync(newTimeStamp).ConfigureAwait(false);

                if (progress != null)
                    progress.Report(++current);
            }
        }
    }
}
