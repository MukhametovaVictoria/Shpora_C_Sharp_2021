using Newtonsoft.Json;
using System;
using System.Linq;

namespace ArchiveTask
{
    public class Archive
    {
        private Operation[] operations { get; set; }
        public Archive(string[] serializedOperations)
        {
            operations = serializedOperations
                .Select(x => JsonConvert.DeserializeObject<Operation>(x))
                .ToArray();
        }

        public Guid[] GetOperationIds(string time)
        {
            var date = DateTime.Parse(time, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime();
            return operations
                .Where(op => DateTime.Parse(op.Time, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime() == date)
                .Select(op => new Guid(op.OperationId))
                .ToArray();
        }
    }

    public class Operation
    {
        [JsonProperty("OperationId")]
        public string OperationId { get; set; }
        [JsonProperty("Time")]
        public string Time { get; set; }
    }
}