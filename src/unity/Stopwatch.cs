using System;
using UnityEngine;

namespace MassiveAI
{
	public class Stopwatch
	{
		private float elapsedRunningTime = 0f;
		private float runningStartTime = 0f;
		private float pauseStartTime = 0f;
		private float elapsedPausedTime = 0f;
		private float totalElapsedPausedTime = 0f;
		private bool  running = false;
		private bool  paused = false;
		
		public bool Running { get { return running; } }
		public bool Paused { get { return paused; } }
		
		public event Action OnStart;
		public event Action OnPause;
		public event Action OnUnpause;
		public event Action OnReset;

		public void Begin()
		{
			Reset();
			
			if (!running && !paused)
			{
				runningStartTime = Time.time;
				running = true;
				OnStart?.Invoke();
			}
		}

		public void Pause()
		{
			if (!running || paused)
			{
				Debug.LogWarning("Cannot pause: Stopwatch is not running or already paused.");
				return;
			}

			running = false;
			pauseStartTime = Time.time;
			paused = true;
			OnPause?.Invoke();
		}

		public void Unpause()
		{
			if (running || !paused)
			{
				Debug.LogWarning("Cannot unpause: Stopwatch is already running or not paused.");
				return;
			}

			totalElapsedPausedTime += elapsedPausedTime;
			running = true;
			paused = false;
			OnUnpause?.Invoke();
		}

		public void Reset()
		{
			elapsedRunningTime = 0f;
			runningStartTime = 0f;
			pauseStartTime = 0f;
			elapsedPausedTime = 0f;
			totalElapsedPausedTime = 0f;
			running = false;
			paused = false;
			OnReset?.Invoke();
		}

		public float GetRawElapsedTime()
		{
			if (running)
				elapsedRunningTime = Time.time - runningStartTime - totalElapsedPausedTime;

			return elapsedRunningTime;
		}

		public int GetMinutes() => (int)(GetRawElapsedTime() / 60f);
		public int GetSeconds() => (int)(GetRawElapsedTime() % 60f);
		public float GetMilliseconds() => (GetRawElapsedTime() - Mathf.Floor(GetRawElapsedTime())) * 1000f;
	}
}


/*
EXAMPLE USAGE

using UnityEngine;
public class StopwatchUsageExample : MonoBehaviour
{
	private Stopwatch stopwatch;

	void Start()
	{
		// Attach the Stopwatch component to the GameObject
		stopwatch = new();

		// Subscribe to events
		stopwatch.OnStart += HandleStart;
		stopwatch.OnPause += HandlePause;
		stopwatch.OnUnpause += HandleUnpause;
		stopwatch.OnReset += HandleReset;

		// Begin the stopwatch
		stopwatch.Begin();
	}

	void Update()
	{
		// Display elapsed time
		Debug.Log($"Elapsed Time: {stopwatch.GetMinutes()}:{stopwatch.GetSeconds()}.{stopwatch.GetMilliseconds():F0}");

		if (Input.GetKeyDown(KeyCode.P)) // Pause
			stopwatch.Pause();

		if (Input.GetKeyDown(KeyCode.U)) // Unpause
			stopwatch.Unpause();

		if (Input.GetKeyDown(KeyCode.R)) // Reset
			stopwatch.Reset();
	}

	private void HandleStart()
	{
		Debug.Log("Stopwatch started!");
	}

	private void HandlePause()
	{
		Debug.Log("Stopwatch paused!");
	}

	private void HandleUnpause()
	{
		Debug.Log("Stopwatch resumed!");
	}

	private void HandleReset()
	{
		Debug.Log("Stopwatch reset!");
	}
}
*/