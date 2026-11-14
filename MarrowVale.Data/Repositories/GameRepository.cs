using MarrowVale.Business.Entities.Entities;
using MarrowVale.Common.Contracts;
using MarrowVale.Data.Contracts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace MarrowVale.Data.Repositories
{
    public class GameRepository : IGameRepository
    {
        private readonly ILogger _logger;
        private readonly IAppSettingsProvider _appSettingsProvider;

        private JsonSerializerSettings Settings { get; set; }

        public GameRepository(ILoggerFactory logger, IAppSettingsProvider appSettingsProvider)
        {
            _logger = logger.CreateLogger<GameRepository>();
            _appSettingsProvider = appSettingsProvider;
        }


        public Game LoadGame(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            var path = generateFilePath(fileName);
            return File.Exists(path) ? JsonConvert.DeserializeObject<Game>(File.ReadAllText(path), Settings) : null;
        }

        public void SaveGame(Game game, string oldFileName, string newFileName)
        {
            var path = generateFilePath(newFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + "." + Guid.NewGuid() + ".tmp";
            try
            {
                File.WriteAllText(temporary, JsonConvert.SerializeObject(game, Formatting.Indented, Settings));
                File.Move(temporary, path, overwrite: true);
            }
            finally
            {
                File.Delete(temporary);
            }
        }

        private string generateFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Invalid game save name.", nameof(fileName));
            return Path.GetFullPath(Path.Combine(_appSettingsProvider.DataFilesLocation, fileName + ".json"));
        }
    }
}
