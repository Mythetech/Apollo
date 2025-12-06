using System.Text.Json.Serialization;

namespace Apollo.Infrastructure.Resources;

public class BlazorResources
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; }
    
    [JsonPropertyName("coreAssembly")]
    public AssemblyResource[] CoreAssembly { get; set; }
    
    [JsonPropertyName("assembly")]
    public AssemblyResource[] Assembly { get; set; }
    
    [JsonPropertyName("fingerprinting")]
    public Dictionary<string, string> Fingerprinting { get; set; }
    public Dictionary<string, string> WasmNative { get; set; }
    public Dictionary<string, string> Pdb { get; set; }
    public Dictionary<string, Dictionary<string, string>> SatelliteResources { get; set; }
    public Dictionary<string, string> JsModuleNative { get; set; }
    public Dictionary<string, string> JsModuleRuntime { get; set; }
    public Dictionary<string, string> LibraryInitializers { get; set; }
    public Dictionary<string, string> ModulesAfterConfigLoaded { get; set; }
}

public class AssemblyResource
{
    public string name { get; set; }
    public string virtualPath { get; set; }
    public string integrity { get; set; }
}