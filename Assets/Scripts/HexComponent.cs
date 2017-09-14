using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexComponent : MonoBehaviour {
	
	public Material ocean, tundra, plains, desert, tundraHill, plainsHill, desertHill, mountain;
	public Mesh hillMesh, mountainMesh;
	public Hex hex;

	//static string[,] tempElev = new string[4,3] {//cold			warm			hot
	//											{"ocean",		"ocean",		"ocean"},		// ocean
	//											{"tundra",		"plains",		"desert"},		// flat
	//											{"tundraHill",	"plainsHill",	"desertHill"},	// hill
	//											{"mountain",	"mountain",		"mountain"}};	// mountain
	
	static Material[,] tempElevMaterial;

	public void SetVisuals (int elevationLevel, int temperatureLevel)
	{

		tempElevMaterial = new Material[4,3]	{{ocean,		ocean,			ocean},
												{tundra,		plains,			desert},
												{tundraHill,	plainsHill,		desertHill},
												{mountain,		mountain, 		mountain}};

		GetComponentInChildren<MeshRenderer>().material = tempElevMaterial[elevationLevel,temperatureLevel];

		if (elevationLevel == 3)
			GetComponentInChildren<MeshFilter>().mesh = mountainMesh;
		if (elevationLevel == 2)
			GetComponentInChildren<MeshFilter>().mesh = hillMesh;
	}
}