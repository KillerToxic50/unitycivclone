using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Add map wrapping 						Done (Needs drastic optimisation but all the math is there)
// TODO: Add camera boundaries
// TODO: Implement combat
// TODO: Add more UI stuff
// TODO: Allow founding cities					Doing it, have to impelement different unit types first
// TODO: Allow cities to build units
// TODO: Allow cities to build buildings
// TODO: Add resources
// TODO: Add unit promotions
// TODO: Add a menu
// TODO: Add saving and loading
// TODO: Improve map generation and add presets
// TODO: Add content
// TODO: Cleanup code

public class HexMap : MonoBehaviour {

	public int width, height;
	public GameObject hexPrefab;
	public GameObject unitPrefab;
	public GameObject cityPrefab;
	public bool enableWrapping = false;


	Dictionary<Vector2, Hex> hexCoords = new Dictionary<Vector2, Hex>();
	List<Hex> hexes = new List<Hex>();
	List<Unit> units = new List<Unit>();
	List<City> cities = new List<City>();

	void Start () {
		Camera.main.transform.position = new Vector3(((float)width/1.5f)*((Mathf.Sqrt(3f)/2f) * 2f), 20f, height/1.5f);

		for (int row = 0; row < height; row++)
		{
			for (int col = 0; col < width; col++)
			{
				// Create a new tile
				Hex hex = new Hex(col, row);
				GameObject tile = Instantiate (hexPrefab, hex.GetWorldPosition(), Quaternion.identity, transform);
				HexComponent hexComponent = tile.GetComponent<HexComponent>();

				hex.gameObject = tile;
				hex.hexComponent = hexComponent;
				hex.hexMap = this;
				hexComponent.hex = hex;
				hex.gameObject.name = string.Format ("Hex - ({0}, {1})", hex.Q, hex.R);

				hexCoords.Add(new Vector2 (col, row), hex);
				hexes.Add(hex);

				// Set to ocean
				hex.elevation = 0f;
				hex.temperature = 0.5f;
			}
		}
		//Random.InitState(0);
		GenerateContinents(count:1, size:15);
		GenerateBiomes(poleSize:3);
		GenerateResources(resourceDensity:2);

		foreach (Hex h in hexes) {
			if (h.elevationLevel == 1 || h.elevationLevel == 2 && Random.Range(0, (width*height)/10) == 50) {
				SpawnCity (h);
				break;
			}
		}
	}

	private Vector3 lastCamPos;
	void Update () {
		if (enableWrapping) {
			if (Camera.main.transform.position != lastCamPos) {
				foreach (Hex h in hexes) {
					h.DoWrapping (Camera.main.transform.position, width);
				}
				foreach (Unit u in units) {
					u.gameObject.transform.localPosition = Vector3.zero;
				}
			}
			lastCamPos = Camera.main.transform.position;
		}
	}

	public Unit GetUnitThatNeedOrders () {
		foreach (Unit u in units) {
			if (u.CheckIfNeedOrders ())
				return u;
		}
		return null;
	}

	public void DoUnitTurns () {
		foreach (Unit u in units) {
			u.DoTurn ();
		}
	}

	public void DoCityTurns () {
		foreach (City c in cities) {
			c.DoTurn ();
		}
	}

	public void ResetUnitMoves () {
		foreach (Unit u in units) {
			u.movesRemaining = u.moveRange;
		}
	}

	/// <summary>
	/// Generates the landmasses.
	/// </summary>
	/// <param name="count">Number of continents to generate.</param>
	/// <param name="size">Max radius of continents.</param>
	void GenerateContinents(int count = 1, int size = 10) {

		// Repeat to generate more than one continent
		for (int i = 1; i < count+1; i++) {

			int centerRow = height / 2;
			int centerCol = (width / (count+1)) * i;
			int passes = size * 2/3;

			// Splat down continents
			for (int pass = 1; pass < passes+1; pass++) {

				// Decrease radius and increase splats each pass
				int landSplatRadius = (int)Mathf.Round(size / (passes+1 - pass));
				int noLandSplats = size - landSplatRadius;

				for (int splatNo = 0; splatNo < noLandSplats; splatNo++) {
					int col = Random.Range(centerCol - size, centerCol + size);
					int row = Random.Range(centerRow - size, centerRow + size);

					// Elevate hexes
					foreach (Hex h in GetHexAt(col, row).GetHexesWithinRadius(landSplatRadius)) {
						h.elevation += 0.1f;
						//if (h.elevation > 0.5f)
							//h.elevation = 0.5f;
					}
				}
			}
		}
			
		// Randomise the noise offset
		float xOffset = Random.Range(-10,10);
		float yOffset = Random.Range(-10,10);

		// Add some noise to the elevation
		foreach (Hex h in hexes) {
			int col = h.Q;
			int row = h.R;

			// Get noise and pow it to make it exponential, then divide so that it only adds a bit
			// (0-1) to the power of 10, so (0-100), then divide by 400, to get a value of (0-0.5)
			float noise = (Mathf.Pow(Mathf.PerlinNoise(((float)col/width*10f)+xOffset, ((float)row/height*10f)+yOffset), 10f)/200f);

			// Randomly add (1/2) or subtract (1/2) the noise
			if (Random.Range (0, 2) > 0)
				h.elevation += noise;
			else
				h.elevation -= noise;

			h.SetType();
		}
	}

	/// <summary>
	/// Generates different biomes.
	/// </summary>
	/// <param name="poleSize">Height of the tundra strips at top and bottom of map.</param>
	void GenerateBiomes(int poleSize = 3) {
		// Randomise the noise offset
		float xOffset = Random.Range(-1,1);
		float yOffset = Random.Range(-1,1);

		// Generate temperatures
		foreach (Hex h in hexes) {
			int col = h.Q;
			int row = h.R;
			// Add a random amount of temperature (0-0.4) to create deserts
			h.temperature += Mathf.PerlinNoise(((float)col/width*10f)+xOffset, ((float)row/height*10f)+yOffset)/2.5f;

			// Set poles to tundra
			if (row > height - poleSize || row < poleSize)
				h.temperature = 0;

			h.SetType ();
		}
	}

	/// <summary>
	/// Generates resources.
	/// </summary>
	/// <param name="resourceDensity">Resources per every 10 tiles.</param>
	void GenerateResources(int resourceDensity = 1) {
		// Times total tiles by 1/10th of the resource density so we get 1 resource every 10 tiles
		int noResources = (int)((float)(width*height) * (float)(resourceDensity/10f));

		// Splat down resources
		for (int splatNo = 0; splatNo < noResources; splatNo++) {

			int col = Random.Range(0, width);
			int row = Random.Range(0, width);

			// Get hex and add resource
			Hex h = GetHexAt(col, row);
			if (h != null && h.elevationLevel == 1) {
				// TODO: Add flat resources
			}
			else if (h != null && h.elevationLevel == 2) {
				// TODO: Add hill resources
			}
		}
	}

	/// <summary>
	/// Spawns a unit in a given city.
	/// </summary>
	/// <returns>The spawned <see cref="City"/>.</returns>
	/// <param name="spawnCity">City to spawn unit in.</param>
	/// <param name="type">Unit type to spawn.</param>#
	public Unit SpawnUnit (City spawnCity, int type) {
		Unit unitClass = new Unit();
		unitClass.gameObject = Instantiate(unitPrefab, spawnCity.hexes[0].gameObject.transform.position, Quaternion.identity, spawnCity.hexes[0].gameObject.transform);
		UnitComponent unitComponent = unitClass.gameObject.GetComponent<UnitComponent>();
		unitComponent.SetVisuals (type);
		unitClass.unitComponent = unitComponent;
		unitClass.currentHex = spawnCity.hexes[0];
		unitClass.belongsTo = spawnCity;
		unitComponent.unit = unitClass;
		units.Add(unitClass);
		return unitClass;
	}
		
	/// <summary>
	/// Spawns a city on a given hex
	/// </summary>
	/// <returns>The spawned<see cref="City"/>.</returns>
	/// <param name="col">Spawn hex</param>
	public City SpawnCity (Hex spawnHex) {
		City cityClass = new City (spawnHex);
		cities.Add (cityClass);
		cityClass.gameObject = Instantiate(cityPrefab, spawnHex.gameObject.transform.position, Quaternion.identity, spawnHex.gameObject.transform);
		CityComponent cityComponent = cityClass.gameObject.GetComponent<CityComponent>();
		cityClass.cityComponent = cityComponent;
		cityClass.hexMap = this;
		cityComponent.city = cityClass;
		spawnHex.belongsTo = cityClass;
		spawnHex.gameObject.GetComponentInChildren<MeshRenderer> ().material = null;
		return cityClass;
	}

	/// <summary>
	/// Returns the hex at a map location.
	/// </summary>
	/// <returns>The <see cref="Hex"/>.</returns>
	/// <param name="col">Map column.</param>
	/// <param name="row">Map row.</param>
	public Hex GetHexAt (int col, int row) {
		//Debug.Log(string.Format("Getting Hex at {0}, {1}", col, row));
		if (hexCoords.ContainsKey(new Vector2 (col, row)))
			return hexCoords [new Vector2 (col, row)];
		else
			return null;
	}
}
