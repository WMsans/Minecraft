using UnityEngine;

/// <summary>
/// A static class to hold the global seed for procedural generation.
/// Burst-compiled jobs can access this static value.
/// </summary>
public static class SeedController
{
    public static int Seed = 1337;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        // Set a default or random seed when the game starts
        Seed = (int)System.DateTime.Now.Ticks;
        // Or uncomment the line below for a consistent seed for testing
        // Seed = 12345; 
    }

    /// <summary>
    /// Sets a new seed for the noise generator.
    /// </summary>
    /// <param name="newSeed">The integer to use as the new seed.</param>
    public static void SetSeed(int newSeed)
    {
        Seed = newSeed;
    }
}