using System.Text.RegularExpressions;

namespace Log4Mongo
{
	public class UnitResolver
	{
		public long Resolve(string valueWithUnit)
		{
			if(valueWithUnit == null)
			{
				return 0;
			}

			int result;
			
			if (!int.TryParse(valueWithUnit, out result))
			{
				var regex = new Regex(@"^(\d+)(k|MB){0,1}$");
				var match = regex.Match(valueWithUnit);

				if (match.Success)
				{
					var value = int.Parse(match.Groups[1].Value);
					var multiplier = GetMultiplier(match.Groups[2].Value);
					result = value * multiplier;
				}
			}

			return result;
		}

		private int GetMultiplier(string unit)
		{
			switch (unit)
			{
				case "k":
					return 1000;
				case "MB":
					return 1024 * 1024;
				default:
					return 0;
			}
		}
	}
}