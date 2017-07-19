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

        private static readonly double[] CpMultipliers = {
            0.0939999967813492, 0.135137432089339, 0.166397869586945, 0.192650913155325, 0.215732470154762,
            0.236572651424822, 0.255720049142838, 0.273530372106572, 0.290249884128571, 0.306057381389863,
            0.321087598800659, 0.335445031996451, 0.349212676286697, 0.362457736609939, 0.375235587358475,
            0.387592407713878, 0.399567276239395, 0.4111935532161, 0.422500014305115, 0.432926420512509,
            0.443107545375824, 0.453059948165049, 0.46279838681221, 0.472336085311278, 0.481684952974319,
            0.490855807179549, 0.499858438968658, 0.5087017489616, 0.517393946647644, 0.525942516110322,
            0.534354329109192, 0.542635753803599, 0.550792694091797, 0.558830584490385, 0.566754519939423,
            0.57456912814537, 0.582278907299042, 0.589887907888945, 0.597400009632111, 0.604823648665171,
            0.61215728521347, 0.619404107958234, 0.626567125320435, 0.633649178748576, 0.6406529545784,
            0.647580971386554, 0.654435634613037, 0.661219265805859, 0.667934000492096, 0.674581885647492,
            0.681164920330048, 0.687684901255373, 0.694143652915955, 0.700542901033063, 0.706884205341339,
            0.713169074873823, 0.719399094581604, 0.725575586915154, 0.731700003147125, 0.734741038550429,
            0.737769484519958, 0.740785579737136, 0.743789434432983, 0.746781197247765, 0.749761044979095,
            0.752729099732281, 0.75568550825119, 0.758630370209851, 0.761563837528229, 0.76448604959218,
            0.767397165298462, 0.770297293677362, 0.773186504840851, 0.776064947064992, 0.778932750225067,
            0.781790050767666, 0.784636974334717, 0.787473608513275, 0.790300011634827};

        public int? Percent
        {
            get
            {
                if (Attack == null || Defense == null || Stamina == null)
                    return null;

                return ComputeIVPercent(Attack.Value, Defense.Value, Stamina.Value);
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

            List<IvCombination> unsortedIvCombinations = null;
            if (jsonValue.ContainsKey("ivCombinations"))
            {
                if (jsonValue["ivCombinations"].JsonType == JsonType.Array && jsonValue["ivCombinations"].Count > 0)
                {
                    unsortedIvCombinations = new List<IvCombination>();
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
                    }
                }
            }
            else if(jsonValue.ContainsKey("Cp") && jsonValue["Cp"].JsonType == JsonType.Number)
            {
                int hp = 0;
                if (jsonValue.ContainsKey("Hp") && jsonValue["Hp"].JsonType == JsonType.Number)
                    hp = jsonValue["Hp"];
                unsortedIvCombinations = GetIVCombinations(jsonValue["Cp"], hp);
            }

            if(unsortedIvCombinations != null)
            {
                ivCombinations = unsortedIvCombinations.OrderBy(o => o.Percent).ToList();
                IvCombination lowestIvCombination = ivCombinations[0];
                this.Attack = lowestIvCombination.Attack;
                this.Defense = lowestIvCombination.Defense;
                this.Stamina = lowestIvCombination.Stamina;
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

        public List<IvCombination> GetIVCombinations(int targetCp, int targetHp = 0)
        {
            List<IvCombination> combinations = new List<IvCombination>();

            var baseStats = Pokemon.GetBaseStats(this.PokemonId.Value);

            double cpMultiplier = GetCpMultiplier(this.Level.Value);
            double cpMultiplier2 = Math.Pow(cpMultiplier, 2) / 10;

            for (int totalSta = baseStats.BaseStamina; totalSta <= baseStats.BaseStamina + 15; totalSta++)
            {
                int candidateHp = ComputeHP(totalSta, cpMultiplier);

                if (targetHp == 0 || targetHp == candidateHp)
                {
                    double staMultiplier = Math.Sqrt(totalSta) * cpMultiplier2;
                    for (int totalDef = baseStats.BaseDefense; totalDef <= baseStats.BaseDefense + 15; totalDef++)
                        for (int totalAtk = baseStats.BaseAttack; totalAtk <= baseStats.BaseAttack + 15; totalAtk++)
                        {
                            int candidateCp = ComputeCP(totalAtk, totalDef, staMultiplier);
                            if (candidateCp == targetCp)
                            {
                                int atkIv = totalAtk - baseStats.BaseAttack;
                                int defIv = totalDef - baseStats.BaseDefense;
                                int staIv = totalSta - baseStats.BaseStamina;

                                combinations.Add(new IvCombination()
                                {
                                    Attack = atkIv,
                                    Defense = defIv,
                                    Stamina = staIv,
                                    Percent = ComputeIVPercent(atkIv, defIv, staIv)
                                });
                            }
                        }
                }
            }

            return combinations;
        }

        public static int ComputeIVPercent(int atk, int def, int sta)
        {
            return (int)System.Math.Round(((float)(atk + def + sta) * 100.0f) / 45.0f);
        }

        public static int ComputeCP(PokemonId pokemon, int atk, int def, int sta, float level)
        {
            var baseStats = Pokemon.GetBaseStats(pokemon);

            double cpMultiplier = GetCpMultiplier(level);
            double cpMultiplier2 = Math.Pow(cpMultiplier, 2) / 10;
            double staMultiplier = Math.Sqrt(baseStats.BaseStamina + sta) * cpMultiplier2;

            return ComputeCP(baseStats.BaseAttack + atk, baseStats.BaseDefense + def, staMultiplier);
        }

        public static int ComputeCP(int totalAtk, int totalDef, double staMultiplier)
        {
            return Math.Max((int)Math.Floor(totalAtk * Math.Sqrt(totalDef) * staMultiplier), 10);
        }

        public static int ComputeHP(PokemonId pokemon, int sta, float level)
        {
            double cpMultiplier = GetCpMultiplier(level);

            var baseStats = Pokemon.GetBaseStats(pokemon);
            return ComputeHP(baseStats.BaseStamina + sta, cpMultiplier);
        }

        public static int ComputeHP(int totalSta, double cpMultiplier)
        {
            return (int)Math.Max(Math.Floor(totalSta * cpMultiplier), 10);
        }

        public static double GetCpMultiplier(float level)
        {
            return CpMultipliers[(int)((level - 1) * 2)];
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
