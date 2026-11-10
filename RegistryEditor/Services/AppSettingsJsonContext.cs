// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json.Serialization;

namespace RegistryEditor.Services;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppSettings))]
internal partial class AppSettingsJsonContext : JsonSerializerContext
{
}
