using System.Collections.Generic;

public struct HeritageSite 
{
	public string Name;
	public string Description;
	public double Latitude;
	public double Longitude;
	public double DetectionRadius;
}

public static class HeritageLocations
{
	public static readonly List<HeritageSite> Sites = new List<HeritageSite>
	{
		new HeritageSite 
		{ 
			Name = "Spanish Gate", 
			Description = "Historic West Gate of Subic Bay built in 1885.",
			Latitude = 14.825169, 
			Longitude = 120.2762,
			DetectionRadius = 150 
		},
		new HeritageSite 
		{ 
			Name = "Bintana Point", 
			Description = "Your testing area marker.",
			Latitude = 14.849288, 
			Longitude = 120.326777,
			DetectionRadius = 10 
		}
	};
}
