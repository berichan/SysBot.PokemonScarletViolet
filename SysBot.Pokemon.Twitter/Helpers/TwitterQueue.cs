using PKHeX.Core;

namespace SysBot.Pokemon.Twitter
{
    public class TwitterQueue
    {
        public PK8 Pokemon { get; }
        public PokeTradeTrainerInfo Trainer { get; }
        public string UserName { get; }
        public string DisplayName => Trainer.TrainerName;
        public ulong MessageId { get; } // The dm sent to ask which trade code the user would like
        public int CheckCount { get; private set; } = 0;

        public TwitterQueue(PK8 pkm, PokeTradeTrainerInfo trainer, string username)
        {
            Pokemon = pkm;
            Trainer = trainer;
            UserName = username;
        }

        public void IncrementCheck()
        {
            CheckCount++;
        }
    }
}
