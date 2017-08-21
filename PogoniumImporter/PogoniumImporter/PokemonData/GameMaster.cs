using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using PogoniumImporter.PokemonData;
using PogoniumImporter;
using System.Linq;
using System.Threading.Tasks;
using PogoniumImporter.PokemonDatabase;

namespace PogoniumImporter.PokemonData
{
    public class GameMaster
    {
        public static async Task<Pokemon> GetPokemon(int id)
        {
            return await Database.GetConnection()
                .Table<Pokemon>()
                .Where(p => p.PokedexNumber == id)
                .FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public static async Task<List<Move>> GetQuickMoves(int id)
        {
            Pokemon pokemon = await GetPokemon(id).ConfigureAwait(false);
            return pokemon != null ? pokemon.QuickMoves.ToList() : null;
        }

        public static async Task<List<Move>> GetChargeMoves(int id)
        {
            Pokemon pokemon = await GetPokemon(id).ConfigureAwait(false);
            return pokemon != null ? pokemon.ChargeMoves.ToList() : null;
        }

        public static async Task<dynamic> GetBaseStats(int id)
        {
            Pokemon pokemon = await GetPokemon(id).ConfigureAwait(false);

            return new {
                BaseAttack = pokemon.BaseAttack,
                BaseDefense = pokemon.BaseDefense,
                BaseStamina = pokemon.BaseStamina
            };
        }
    }
}
