using Mirage.Serialization;

namespace NOComponentWIP;

public static class DeployableUnitReaderWriter
{
	public static void WriteDeployableUnit(this NetworkWriter writer, DeployableUnit unit)
	{
		if (unit == null)
		{
			writer.WriteString(string.Empty);
			return;
		}
		writer.WriteString(unit.JsonKey);
	}

	public static DeployableUnit ReadDeployableUnit(this NetworkReader reader)
	{
		string jsonKey = reader.ReadString();

		if (string.IsNullOrEmpty(jsonKey))
			return null;

		if (ModAssets.i?.AllDeployableUnits?.TryGetValue(jsonKey, out var unit) ?? false)
		{
			return unit;
		}
		
		Plugin.Logger.LogError($"Could not find unit for key: {jsonKey}");
		return null;
	}
}