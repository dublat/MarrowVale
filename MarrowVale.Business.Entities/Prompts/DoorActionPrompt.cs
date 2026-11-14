using MarrowVale.Business.Entities.Entities.PathObstacles;
using MarrowVale.Business.Entities.Enums;
using MarrowVale.Common.Prompts;
using MarrowVale.Common.Prompts.Contracts;
using MarrowVale.Common.Prompts.Examples;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace MarrowVale.Business.Entities.Prompts;

public class DoorActionPrompt : BasePrompt, INonStandardPrompt
{
    public class Example : BaseExample
    {
        public JObject Door { get; set; }
        public JObject Key { get; set; }
        public string Output { get; set; }
    }

    public List<Example> Examples { get; set; }

    public override List<StandardExample> StandardizeExamples() =>
        Examples.Select(e => new StandardExample
        {
            Id = e.Id, Rating = e.Rating, Input = Format(e.Door, e.Key), Output = e.Output
        }).ToList();

    public static string InputFor(CommandEnum action, Door door, DoorKey key)
    {
        var opening = action is CommandEnum.Open or CommandEnum.Close;
        var details = new { door.Name, door.Description, Size = door.Size(), Material = door.Material?.Name,
            door.Color, OpeningMechanism = opening ? door.OpeningMechanism : null,
            LockingMechanism = opening ? null : door.LockingMechanism };
        return Format(details, key == null ? null : new { key.Name, key.Description, key.Color, key.Material });
    }

    private static string Format(object door, object key) =>
        JsonConvert.SerializeObject(new { Door = door, Key = key },
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
}
