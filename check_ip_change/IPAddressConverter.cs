using Newtonsoft.Json;
using System;
using System.Net;

namespace check_ip_change {
    /// <summary>
    /// This is just to serialise an IP address because JsonConvert.Serialize doesn't handle IPAddress natively. 
    /// </summary>
    public class IPAddressConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return (objectType == typeof(IPAddress));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteValue(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            return IPAddress.Parse((string)reader.Value);
        }
    }
}
