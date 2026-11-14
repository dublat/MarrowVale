# MarrowVale

MarrowVale is a Windows console RPG. The console owns one input loop. Commands resolve against Neo4j, and JSON examples supply the prompts for classification, descriptions, combat, and NPC dialogue.

## Run the game

Install the .NET SDK selected by `global.json`. The nine solution projects target .NET 10 and use the .NET 10 shared framework for configuration, logging, and dependency injection.

From the repository root, configure the executable with these PowerShell commands. Replace the sample values with your own settings.

```powershell
dotnet user-secrets set --project MarrowVale SystemSettings:Neo4jUrl http://localhost:7474
dotnet user-secrets set --project MarrowVale SystemSettings:Neo4jUser YOUR_DATABASE_USER
dotnet user-secrets set --project MarrowVale SystemSettings:Neo4jPassword YOUR_DATABASE_PASSWORD
dotnet user-secrets set --project MarrowVale OpenAiKey YOUR_API_KEY
dotnet restore MarrowVale.sln
dotnet run --project MarrowVale
```

The graph client uses Neo4j's HTTP endpoint. Gameplay uses the default game database. Prompt settings and evaluation records use the existing `devToolBox` database. Inventory and category queries use the neosemantics procedures and expect the project's existing graph schema and ontology. For an existing world use **Continue**. **New Game** writes buildings and characters and can make paid AI calls. The project does not include a complete export of the original world.

Configuration precedence is base JSON, build-configuration JSON, user secrets, then environment variables. Debug and Development load their respective lowercase files from the executable directory. Release uses the base file unless a Release override is supplied there. The shared secrets ID preserves the store historically loaded by Common. Copy any settings kept solely under the old executable ID using the commands above. Environment keys for nested properties use two underscores, for example `SystemSettings__Neo4jUrl`. `ChatModel` and `EmbeddingModel` override the default models. Keep the working directory consistent for relative save paths or configure `MySettings__DataFilesLocation` as an absolute path to existing saves.

## Runtime decisions

- Startup, dispatch, dialogue, NPC generation, trading, combat updates, navigation, and door writes await their dependencies. The next console prompt follows completion of the previous command. Ctrl+C requests cancellation. In-flight graph writes finish at their transaction boundary.
- The AI provider uses the OpenAI Chat Completions and Embeddings HTTP APIs via a reusable HTTP client with a 45-second timeout. Model defaults are `gpt-4.1-mini` and `text-embedding-3-small`. Stored legacy completion model names fall back to the configured chat model. Other stored chat model names take precedence. Choose models compatible with the stored sampling and stop parameters.
- NPC requests carry a fresh system prompt and up to seven previous user/assistant pairs plus the current user message. The full transcript is used for the end-of-conversation summary. Embedding search batches distinct candidates with the query and ranks by cosine similarity. Duplicate descriptions resolve to the first candidate.
- Successful raw prompts, standard prompts, chat turns, and embedding searches record the returned model, elapsed request duration, and total tokens when supplied. Cost is nullable. Historical evaluation records are retained. Transport failures end the request and do not replay a paid call.
- New memories use `(:Character)-[:REMEMBERS]->(:Conversation)-[:MENTIONS]->(:Player)`. One Cypher statement matches both endpoints by ID before creating the conversation and its links. Reads also recognize `MEMORY` and `MENTIONED`. The newest timestamped memory takes precedence. An old record linked to several players lacks enough evidence to recover its original owner. Those links are retained.
- Save files keep the existing JSON format. Replacing a slot uses a same-directory temporary file. Writing a new slot preserves previous slots. Player selection continues to load graph entities.

## Verification

```powershell
dotnet build MarrowVale.sln -c Debug
dotnet build MarrowVale.sln -c Release
dotnet build MarrowVale.sln -c Development
dotnet test MarrowVale.sln
```

Routine tests use fake services, an in-memory HTTP handler, and temporary JSON files. CI builds and tests each configuration on Windows. A separate Linux job starts a disposable Neo4j 4.4 service on port 17474 and exercises the real memory repository. Its fixture checks legacy reads, player isolation, a canonical round trip, and failed creation with a missing endpoint. The standalone legacy .NET 5 project in `OpenAiEvaluation/` is archival. Active evaluation lives in the business service and data repositories. Sandbox and Asset Generation remain placeholders.

## Gameplay backlog

Combat balancing and additional combat actions, richer trading, unmapped commands, NPC autonomy, inference rules, ontology import, authoring tools, procedural expansion, and fine-tuning are separate gameplay work. The current schema, prompt assets, and historical PDF describe different stages of development. Existing live worlds, full interactive playthroughs, and paid model responses require separate validation.
