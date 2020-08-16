using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace KeySeller
{
    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }
    }
}
