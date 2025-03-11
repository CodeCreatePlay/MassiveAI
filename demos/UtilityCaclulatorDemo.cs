using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MassiveAI.UtilityCaclulatorDemo {
	public class UtilityCaclulatorDemo : MonoBehaviour
	{
		// Start is called before the first frame update
		void Start()
		{
			float[] factors = { 0.8f, 0.6f, 0.9f };
			float[] weights = { 1.0f, 0.5f, 0.3f };

			float utility = UtilityCalculator.CalculateUtility(factors, weights, factors.Length);
			Debug.Log($"Calculated Utility: {utility}");
		}
	}
}
