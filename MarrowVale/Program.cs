using MarrowVale.Business.Contracts;
using MarrowVale.Business.Services;
using MarrowVale.Common.Contracts;
using MarrowVale.Common.Evaluator;
using MarrowVale.Common.Providers;
using MarrowVale.Data.Contracts;
using MarrowVale.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using Neo4jClient;
using System;

namespace MarrowVale
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.Title = "Marrow Vale - The Realm of Dragons";
            Console.ForegroundColor = ConsoleColor.Green;

            //Configure DI
            var serviceCollection = new ServiceCollection();

            using var shutdown = new System.Threading.CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };
            serviceCollection.AddSingleton(typeof(System.Threading.CancellationToken), shutdown.Token);
            ConfigureServices(serviceCollection);
            ConfigureRepositories(serviceCollection);
            ConfigureProviders(serviceCollection);

            using var serviceProvider = serviceCollection.BuildServiceProvider();
            try
            {
                var graph = serviceProvider.GetRequiredService<IGraphClient>();
                await graph.ConnectAsync().WaitAsync(shutdown.Token);
                await serviceProvider.GetRequiredService<IGameService>().Start();
            }
            catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }
            catch (Exception error)
            {
                Console.Error.WriteLine($"Game stopped ({error.GetType().Name}). Check the configured services and connection settings.");
                Environment.ExitCode = 1;
            }
        }

        #region Service Life Times
        /*        
        Transient: services are created every time they are injected or requested.

        Scoped: services are created per scope.In a web application, every web request 
                creates a new separated service scope.That means scoped services are generally created per web request.

        Singleton: services are created per DI container. That generally means that they are created only one time 
                   per application and then used for whole the application life time.

        Good Practices:

        1.    Register your services as transient wherever possible.Because it’s simple to design transient services.
                You generally don’t care about multi-threading and memory leaks and you know the service has a short life.

        2.    Use scoped service lifetime carefully since it can be tricky if you create child service scopes or use these 
                services from a non-web application.

        3.    Use singleton lifetime carefully since then you need to deal with multi-threading and potential memory leak problems.

        4.    Do not depend on a transient or scoped service from a singleton service.Because the transient service becomes a 
              singleton instance when a singleton service injects it and that may cause problems if the transient service is 
              not designed to support such a scenario.ASP.NET Core’s default DI container already throws exceptions in such cases.*/
        #endregion

        private static void ConfigureServices(IServiceCollection services)
        {
#if DEBUG || DEVELOPMENT
            const LogLevel logLevel = LogLevel.Information;
#else
            const LogLevel logLevel = LogLevel.Error;
#endif

            //add services to service collection
            services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(logLevel))
                .AddTransient<ICombatService, CombatService>()
                .AddTransient<ICharacterService, CharacterService>()
                .AddTransient<IInputProcessingService, InputProcessingService>()
                .AddTransient<IPrintService, PrintService>()
                .AddTransient<IGameSetupService, GameSetupService>()
                .AddTransient<IEntityGenerator, EntityGenerator>()
                .AddTransient<ITextGenerator, TextGenerator>()
                .AddTransient<ITimeService, TimeService>()
                .AddSingleton<IGameService, GameService>()
                .AddTransient<IDrawingService, DrawingService>()
                .AddTransient<IDialogueService, DialogueService>()
                .AddSingleton<IWorldContextService, WorldContextService>()
                .AddTransient<ICommandProcessingService, CommandProcessingService>()
                .AddTransient<IPromptService, PromptService>()
                .AddTransient<INpcActionService, NpcActionService>()
                .AddTransient<IDivineInterventionService, DivineInterventionService>()
                .AddTransient<IAiService, AiService>()
                .AddTransient<IAiEvaluationService, AiEvaluationService>()
                .AddTransient<IEnviornmentalInteractionService, EnviornmentalInteractionService>()
                .AddTransient<INavigationService, NavigationService>();
        }

        private static void ConfigureRepositories(IServiceCollection services)
        {
            //add repos to service collection
            //use dbcontextscope?
            services.AddTransient<IDrawingRepository, DrawingRepository>()
                .AddTransient<ISoundRepository, SoundRepository>()
                .AddTransient<IClassRepository, ClassRepository>()
                .AddSingleton<IGameRepository, GameRepository>()
                .AddSingleton<IPlayerRepository, PlayerRepository>()
                .AddSingleton<ILocationRepository, LocationRepository>()
                .AddSingleton<IBuildingRepository, BuildingRepository>() 
                .AddSingleton<IRoomRepository, RoomRepository>()
                .AddSingleton<INpcRepository, NpcRepository>()
                .AddSingleton<IDeityRepository, DeityRepository>()
                .AddSingleton<IOpenAiSettingRepository, OpenAiSettingRepository>()
                .AddSingleton<IOpenAiEvaluationRepository, OpenAiEvaluationRepository>()
                .AddSingleton<IDoorRepository, DoorRepository>()
                .AddSingleton<IItemRepository, ItemRepository>()
                .AddSingleton<IPromptRepository, PromptRepository>();
        }

        private static void ConfigureProviders(IServiceCollection services)
        {
            //add providers to service collection
            services.AddSingleton<IAppSettingsProvider, AppSettingsProvider>()
                .AddSingleton<IGlobalItemsProvider, GlobalItemsProvider>()
                .AddSingleton(_ => new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(45) })
                .AddSingleton<IOpenAiProvider, OpenAiProvider>()
                .AddSingleton<IGraphClient>(context =>
                {
                    var settings = context.GetRequiredService<IAppSettingsProvider>();
                    if (!Uri.TryCreate(settings.Neo4jUrl, UriKind.Absolute, out var url) ||
                        string.IsNullOrWhiteSpace(settings.Neo4jUser) || string.IsNullOrWhiteSpace(settings.Neo4jPassword))
                        throw new InvalidOperationException("Configure SystemSettings for Neo4j.");
                    return new GraphClient(url, settings.Neo4jUser, settings.Neo4jPassword);
                });


        }
    }
}
