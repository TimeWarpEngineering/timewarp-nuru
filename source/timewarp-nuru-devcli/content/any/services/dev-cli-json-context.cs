#region Purpose
// AOT-compatible JSON serialization context for dev-cli config types.
// Required because the dev CLI is published with PublishAot=true,
// which disables reflection-based serialization.
#endregion

namespace DevCli;

using System.Text.Json;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(RepoConfig))]
[JsonSerializable(typeof(CheckVersionConfig))]
[JsonSerializable(typeof(CheckVersionStrategy))]
[JsonSerializable(typeof(NuGetVersionIndex))]
internal sealed partial class DevCliJsonContext : JsonSerializerContext;