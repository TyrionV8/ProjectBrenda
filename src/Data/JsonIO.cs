using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Brenda.Data;

/// <summary>JSON settings shared by data files, saves and the network protocol (snake_case keys).</summary>
public static class JsonIO
{
	public static readonly JsonSerializerOptions Options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
	};

	public static readonly JsonSerializerOptions Indented = new(Options) { WriteIndented = true };

	public static T Load<T>(string path)
	{
		var text = FileAccess.GetFileAsString(path);
		if (string.IsNullOrEmpty(text))
		{
			GD.PushError($"Missing or empty data file: {path}");
			return default;
		}
		return JsonSerializer.Deserialize<T>(text, Options);
	}

	public static string Write<T>(T value, bool indented = false) => JsonSerializer.Serialize(value, indented ? Indented : Options);

	public static T Read<T>(string text) => JsonSerializer.Deserialize<T>(text, Options);

	public static bool Save<T>(string path, T value)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			GD.PushError($"Can't write {path}: {FileAccess.GetOpenError()}");
			return false;
		}
		file.StoreString(Write(value, true));
		return true;
	}
}
