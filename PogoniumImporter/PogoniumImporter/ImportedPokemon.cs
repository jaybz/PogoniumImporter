using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PogoniumImporter.PokemonData;

namespace PogoniumImporter
{
    public class ImportedPokemon
    {
        private const string RetrieveUrl = "http://pogonium.com/_user/{0}.json";
        private const string SaveUrl = "http://pogonium.com/myPokemon.php";

        private JsonValue jsonValue;

        public string Key { get; private set; }
        public string Name { get; set; }
        public float? Level { get; set; }
        public int? Id { get; set; }
        public int? Attack { get; set; }
        public int? Defense { get; set; }
        public int? Stamina { get; set; }

        public int? CombatPower { get; set; }
        public int? HitPoints { get; set; }

        public PokemonMove? QuickMove { get; set; }
        public PokemonMove? ChargeMove { get; set; }

        private JsonValue storedPokemon;
        private JsonValue existingPokemon;

        public int? Percent
        {
            get
            {
                if (Attack == null || Defense == null || Stamina == null)
                    return null;

                return (int)Math.Round(((float)(Attack + Defense + Stamina) / 45.0f) * 100.0f);
            }
        }

        public string IdString
        {
            get
            {
                if (Id == null)
                    return null;
                return string.Format("{0:000}", Id);
            }
        }

        public PokemonId? PokemonId
        {
            get
            {
                if (Id == null)
                    return null;
                return (PokemonId)Id;
            }
        }
        
        private List<IvCombination> ivCombinations = new List<IvCombination>();

        private ImportedPokemon(string input)
        {
            this.jsonValue = JsonValue.Parse(input);

            if (jsonValue.ContainsKey("PokemonId"))
            {
                if (jsonValue["PokemonId"].JsonType == JsonType.Number)
                {
                    this.Id = jsonValue["PokemonId"];

                    PokemonId me = (PokemonId)Id;
                    this.Name = me.ToString();
                }
                else
                {
                    throw new ArgumentException("PokemonId is not a number");
                }
            }
            else
            {
                throw new ArgumentException("PokemonId does not exist");
            }

            if (jsonValue.ContainsKey("estimatedPokemonLevel"))
            {
                if (jsonValue["estimatedPokemonLevel"].JsonType == JsonType.Number)
                {
                    this.Level = jsonValue["estimatedPokemonLevel"];
                }
                else
                {
                    throw new ArgumentException("estimatedPokemonLevel is not a number");
                }
            }
            else
            {
                throw new ArgumentException("estimatedPokemonLevel does not exist");
            }

            if (jsonValue.ContainsKey("uniquePokemon"))
            {
                if (jsonValue["uniquePokemon"].JsonType == JsonType.String)
                {
                    this.Key = jsonValue["uniquePokemon"];
                }
                else
                {
                    throw new ArgumentException("uniquePokemon is not a string");
                }
            }

            if (jsonValue.ContainsKey("ivCombinations"))
            {
                if (jsonValue["ivCombinations"].JsonType == JsonType.Array && jsonValue["ivCombinations"].Count > 0)
                {
                    List<IvCombination> unsortedIvCombinations = new List<IvCombination>();
                    foreach (JsonValue i in jsonValue["ivCombinations"])
                    {
                        if (i.ContainsKey("Atk") && i["Atk"].JsonType == JsonType.Number &&
                            i.ContainsKey("Def") && i["Def"].JsonType == JsonType.Number &&
                            i.ContainsKey("Stam") && i["Stam"].JsonType == JsonType.Number &&
                            i.ContainsKey("Percent") && i["Percent"].JsonType == JsonType.Number)
                        {
                            unsortedIvCombinations.Add(new IvCombination
                            {
                                Attack = i["Atk"],
                                Defense = i["Def"],
                                Stamina = i["Stam"],
                                Percent = i["Percent"]
                            });
                        }
                        else
                        {
                            throw new ArgumentException("ivCombinations is not an array");
                        }

                        ivCombinations = unsortedIvCombinations.OrderBy(o => o.Percent).ToList();
                        IvCombination lowestIvCombination = ivCombinations[0];
                        this.Attack = lowestIvCombination.Attack;
                        this.Defense = lowestIvCombination.Defense;
                        this.Stamina = lowestIvCombination.Stamina;
                    }
                }

            }
            else if (jsonValue.ContainsKey("AtkMin") && jsonValue["AtkMin"].JsonType == JsonType.Number &&
                     jsonValue.ContainsKey("DefMin") && jsonValue["DefMin"].JsonType == JsonType.Number &&
                     jsonValue.ContainsKey("StamMin") && jsonValue["StamMin"].JsonType == JsonType.Number)
            {
                this.Attack = jsonValue["AtkMin"];
                this.Defense = jsonValue["DefMin"];
                this.Stamina = jsonValue["StamMin"];
            }
            else
            {
                throw new ArgumentException("ivCombinations is not an array");
            }
        }

        public static ImportedPokemon Parse(string input)
        {
            return new ImportedPokemon(input);
        }

        public async Task<bool> Import(string passcode)
        {
            await RetrieveData(passcode);

            bool updated = false;
            if(existingPokemon != null)
            {
                this.existingPokemon["name"] = this.Name;
                this.existingPokemon["level"] = this.Level;
                this.existingPokemon["attack"] = this.Attack;
                this.existingPokemon["defend"] = this.Defense;
                this.existingPokemon["stamina"] = this.Stamina;

                if (QuickMove.HasValue)
                    this.existingPokemon["quickMove"] = Pokemon.GetMoveCode(QuickMove.Value);

                if (ChargeMove.HasValue)
                    this.existingPokemon["chargeMove"] = Pokemon.GetMoveCode(ChargeMove.Value);

                updated = true;
            }
            else
            {
                JsonValue newPokemon = new JsonObject();
                if(!string.IsNullOrEmpty(this.Key))
                newPokemon["key"] = this.Key;
                newPokemon["pokemon"] = this.IdString;
                newPokemon["name"] = this.Name;
                newPokemon["level"] = this.Level;
                newPokemon["attack"] = this.Attack;
                newPokemon["defend"] = this.Defense;
                newPokemon["stamina"] = this.Stamina;

                if (QuickMove.HasValue)
                    newPokemon["quickMove"] = Pokemon.GetMoveCode(QuickMove.Value);

                if (ChargeMove.HasValue)
                    newPokemon["chargeMove"] = Pokemon.GetMoveCode(ChargeMove.Value);

                ((JsonArray)storedPokemon["myPokemon"]).Add(newPokemon);
            }

            await SaveData(passcode, storedPokemon.ToString());

            return updated;
        }

        public async Task RetrieveData(string passcode)
        {
            if (this.storedPokemon != null) return;

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(string.Format(RetrieveUrl, passcode));
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                JsonValue empty = new JsonObject();
                empty["myPokemon"] = new JsonArray();
                storedPokemon = empty;
            }
            else
            {
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                this.storedPokemon = JsonValue.Parse(json);

                if (!this.storedPokemon.ContainsKey("myPokemon"))
                    this.storedPokemon["myPokemon"] = new JsonArray();

                if (this.storedPokemon["myPokemon"].JsonType != JsonType.Array)
                    throw new HttpRequestException("Malformed JSON received");

                foreach (JsonValue pokemon in this.storedPokemon["myPokemon"])
                {
                    if (pokemon.ContainsKey("key") &&
                        pokemon["key"].JsonType == JsonType.String &&
                        pokemon["key"] == this.Key &&
                        pokemon["pokemon"] == this.IdString)
                    {
                        this.existingPokemon = pokemon;
                        if (pokemon.ContainsKey("name") && pokemon["name"].JsonType == JsonType.String)
                        {
                            this.Name = pokemon["name"];
                        }
                        break;
                    }
                }
            }
        }

        public async Task SaveData(string passcode, string myPokemon)
        {
            JsonValue update = new JsonObject();
            update["passcode"] = passcode;
            update["pokemon"] = myPokemon;

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.PostAsync(SaveUrl, new StringContent(update.ToString()));
            response.EnsureSuccessStatusCode();
        }
    }

    public class IvCombination
    {
        public int? Attack { get; set; }
        public int? Defense { get; set; }
        public int? Stamina { get; set; }
        public int? Percent { get; set; }
    }
}
