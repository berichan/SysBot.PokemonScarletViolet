using System;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Z3;
using SysBot.Pokemon.ConsoleApp;

namespace SysBot.Pokemon.WindowsService
{
    public class Worker : BackgroundService
    {
        public static readonly string ServiceName = "SysBot.Pokemon.WindowsService";
        private static readonly string ConfigPath = @"config.json";

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var executingLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var configPath = executingLocation == null ? ConfigPath : Path.Combine(executingLocation, ConfigPath);

            _logger.LogInformation($"{ServiceName} starting up at: {DateTimeOffset.Now}");

            if (!File.Exists(configPath))
            {
                ExitNoConfig();
                return;
            }

            IPokeBotRunner? env = null;
            try
            {
                _logger.LogInformation($"{ServiceName} reading config file");
                var lines = File.ReadAllText(configPath);

                var cfg = JsonConvert.DeserializeObject<ProgramConfig>(lines, GetSettings()) ?? new ProgramConfig();

                PokeTradeBot.SeedChecker = new Z3SeedSearchHandler<PK8>();
                env = BotContainer.RunBots(cfg, _logger);
            }
            catch
            {
                _logger.LogError($"{ServiceName} unable to start bots with saved config file. Please copy your config from the WinForms project or delete it and reconfigure.");
            }


            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(10000, stoppingToken);
            }

            if(env != null)
            {
                env.StopAll();

            }
        }

        private static void ExitNoConfig()
        {
            var bot = new PokeBotState { Connection = new SwitchConnectionConfig { IP = "192.168.0.1", Port = 6000 }, InitialRoutine = PokeRoutineType.FlexTrade };
            var cfg = new ProgramConfig { Bots = new[] { bot } };
            var created = JsonConvert.SerializeObject(cfg, GetSettings());
            File.WriteAllText(ConfigPath, created);
            Console.WriteLine("Created new config file since none was found in the program's path. Please configure it and restart the program.");
            Console.WriteLine("It is suggested to configure this config file using the GUI project if possible, as it will help you assign values correctly.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static JsonSerializerSettings GetSettings() => new()
        {
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.Include,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new SerializableExpandableContractResolver(),
        };


        // https://stackoverflow.com/a/36643545
        private sealed class SerializableExpandableContractResolver : DefaultContractResolver
        {
            protected override JsonContract CreateContract(Type objectType)
            {
                if (TypeDescriptor.GetAttributes(objectType).Contains(new TypeConverterAttribute(typeof(ExpandableObjectConverter))))
                    return CreateObjectContract(objectType);
                return base.CreateContract(objectType);
            }
        }
    }

    public static class BotContainer
    {
        public static IPokeBotRunner RunBots(ProgramConfig prog, ILogger<Worker> _logger)
        {
            IPokeBotRunner env = GetRunner(prog);
            foreach (var bot in prog.Bots)
            {
                bot.Initialize();
                if (!AddBot(env, bot, prog.Mode))
                {
                    _logger.LogInformation($"Failed to add bot: {bot}");
                }
                    
            }

            LogUtil.Forwarders.Add((msg, ident) => Console.WriteLine($"{ident}: {msg}"));
            env.StartAll();
            _logger.LogInformation($"Started all bots (Count: {prog.Bots.Length}.");

            return env;
        }

        private static IPokeBotRunner GetRunner(ProgramConfig prog) => prog.Mode switch
        {
            ProgramMode.SWSH => new PokeBotRunnerImpl<PK8>(prog.Hub, new BotFactory8()),
            ProgramMode.BDSP => new PokeBotRunnerImpl<PB8>(prog.Hub, new BotFactory8BS()),
            ProgramMode.LA => new PokeBotRunnerImpl<PA8>(prog.Hub, new BotFactory8LA()),
            ProgramMode.SCVI => new PokeBotRunnerImpl<PK9>(prog.Hub, new BotFactory9SV()),
            _ => throw new IndexOutOfRangeException("Unsupported mode."),
        };

        private static bool AddBot(IPokeBotRunner env, PokeBotState cfg, ProgramMode mode)
        {
            if (!cfg.IsValid())
            {
                Console.WriteLine($"{cfg}'s config is not valid.");
                return false;
            }

            PokeRoutineExecutorBase newBot;
            try
            {
                newBot = env.CreateBotFromConfig(cfg);
            }
            catch
            {
                Console.WriteLine($"Current Mode ({mode}) does not support this type of bot ({cfg.CurrentRoutineType}).");
                return false;
            }
            try
            {
                env.Add(newBot);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            Console.WriteLine($"Added: {cfg}: {cfg.InitialRoutine}");
            return true;
        }
    }

}