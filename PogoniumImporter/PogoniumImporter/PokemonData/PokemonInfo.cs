namespace PogoniumImporter.PokemonData
{
    public class PokemonInfo
    {
        public PokemonId Id { get; set; }

        public int BaseAttack { get; set; }
        public int BaseDefense { get; set; }
        public int BaseStamina { get; set; }

        public PokemonMove[] QuickMoves { get; set; }
        public PokemonMove[] ChargeMoves { get; set; }
    }
}
