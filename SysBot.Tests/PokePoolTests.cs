using FluentAssertions;
using PKHeX.Core;
using SysBot.Pokemon;
using Xunit;

namespace SysBot.Tests
{
    public class PokePoolTests
    {
        [Fact]
        public void TestPool() => Test<PK8>();

        private static void Test<T>() where T : PKM, new()
        {
            // Ensure that we can get more than one Pokémon out of the pool.
            var pool = new PokemonPool<T>(new PokeTradeHubConfig());
            var a = new T { Species = 5 };
            var b = new T { Species = 12 };
            pool.Add(a);
            pool.Add(b);

            pool.Count.Should().BeGreaterOrEqualTo(2);

            while (true) { if (ReferenceEquals(pool.GetRandomPoke(), a)) break; }
            while (true) { if (ReferenceEquals(pool.GetRandomPoke(), b)) break; }
            while (true) { if (ReferenceEquals(pool.GetRandomPoke(), a)) break; }

            true.Should().BeTrue();
        }

        /*[Fact]
        public void TestRPool() => TestR<PK9>();

        private static void TestR<T>() where T : PKM, new()
        {
            var pool = new PokemonPool<T>(new PokeTradeHubConfig());
            var pk = RequestPoolUtil<T>.GenerateFromPool(pool, "Pichu @ Nugget");
            var pk2 = RequestPoolUtil<T>.GenerateFromPool(pool, "Fuecoco @ Nugget\r\nAdamantNature");

            pk.Should().NotBeNull();
            pk2.Should().NotBeNull();

            pk.HeldItem.Should().Be(92);
            pk2.HeldItem.Should().Be(92);
        }*/
    }
}
