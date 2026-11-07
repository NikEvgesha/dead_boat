using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum LobbyIslandService { GeneralStore, Armory, Bandages, Healing, Bank, AnimalMerge }

[Serializable]
public sealed class LobbyIslandPlot
{
    [SerializeField] private int id;
    [SerializeField] private Transform root;
    [SerializeField] private Transform arrival;
    [SerializeField] private Transform animalSpawn;
    [SerializeField] private Transform boatSpawn;
    [SerializeField] private Transform departure;
    [SerializeField] private Transform income;
    public int Id => id;
    public Transform Root => root;
    public Transform Arrival => arrival;
    public Transform AnimalSpawn => animalSpawn;
    public Transform BoatSpawn => boatSpawn;
    public Transform Departure => departure;
    public Transform Income => income;
}

[Serializable]
public sealed class LobbyIslandServiceBinding
{
    [SerializeField] private LobbyIslandService service;
    [SerializeField] private Transform interaction;
    public LobbyIslandService Service => service;
    public Transform Interaction => interaction;
}

// Explicit references only: this component does not enable services, connect peers or assign ownership.
[DisallowMultipleComponent]
public sealed class LobbyIslandBindings : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform[] centralArrivals = Array.Empty<Transform>();
    [SerializeField] private LobbyIslandPlot[] plots = Array.Empty<LobbyIslandPlot>();
    [SerializeField] private LobbyIslandServiceBinding[] services = Array.Empty<LobbyIslandServiceBinding>();
    public int PlotCount => plots == null ? 0 : plots.Length;

    public static LobbyIslandBindings FindForScene(Scene scene)
    {
        LobbyIslandBindings result = null;
        foreach (var candidate in FindObjectsByType<LobbyIslandBindings>(FindObjectsSortMode.None))
        {
            if (candidate.gameObject.scene != scene) continue;
            // Do not silently select an arbitrary registry in an incorrectly configured scene.
            if (result != null) return null;
            result = candidate;
        }
        return result;
    }

    public bool TryGetArrival(int index, out Transform point)
    {
        point = centralArrivals != null && index >= 0 && index < centralArrivals.Length ? centralArrivals[index] : null;
        return IsBound(point, visualRoot);
    }

    public bool TryGetPlot(int id, out LobbyIslandPlot plot)
    {
        plot = null;
        if (plots == null) return false;
        foreach (var candidate in plots)
        {
            if (candidate == null || candidate.Id != id) continue;
            if (plot != null) { plot = null; return false; }
            plot = candidate;
        }
        return plot != null && IsPlotBound(plot);
    }

    public bool TryGetService(LobbyIslandService kind, out Transform point)
    {
        point = null;
        bool found = false;
        if (services == null) return false;
        foreach (var binding in services)
        {
            if (binding == null || binding.Service != kind) continue;
            if (found) { point = null; return false; }
            found = true;
            point = binding.Interaction;
        }
        return found && IsBound(point, visualRoot);
    }

    // Diagnostics run on demand, not every frame. A valid registry does not prove walkable colliders.
    public void CollectIssues(int requiredCapacity, List<string> issues)
    {
        if (issues == null) throw new ArgumentNullException(nameof(issues));
        if (requiredCapacity < 1) issues.Add("Room capacity must be positive.");
        if (!IsBound(visualRoot, transform)) issues.Add("Visual root is missing, inactive or belongs to another hierarchy.");
        if (centralArrivals == null || centralArrivals.Length == 0) issues.Add("No central arrivals are bound.");
        else
        {
            var seenArrivals = new HashSet<Transform>();
            for (int i = 0; i < centralArrivals.Length; i++)
                if (!TryGetArrival(i, out var point) || !seenArrivals.Add(point)) issues.Add("Invalid or duplicate central arrival " + i + ".");
        }
        int usable = 0;
        var ids = new HashSet<int>();
        var roots = new HashSet<Transform>();
        if (plots != null) foreach (var plot in plots)
        {
            if (plot == null || plot.Id < 1 || !ids.Add(plot.Id) || !IsPlotBound(plot) || !roots.Add(plot.Root))
                issues.Add("Missing, duplicate or invalid personal plot.");
            else usable++;
        }
        if (usable < requiredCapacity) issues.Add("Personal plots: " + usable + "; required: " + requiredCapacity + ".");
        foreach (LobbyIslandService kind in Enum.GetValues(typeof(LobbyIslandService)))
            if (!TryGetService(kind, out _)) issues.Add("Missing, inactive or duplicate service anchor: " + kind + ".");
    }

    private bool IsPlotBound(LobbyIslandPlot plot) => IsBound(plot.Root, visualRoot) &&
        IsBound(plot.Arrival, plot.Root) && IsBound(plot.AnimalSpawn, plot.Root) &&
        IsBound(plot.BoatSpawn, plot.Root) && IsBound(plot.Departure, plot.Root) && IsBound(plot.Income, plot.Root);

    private bool IsBound(Transform point, Transform root) => point != null && root != null &&
        point.gameObject.scene == gameObject.scene && root.gameObject.scene == gameObject.scene &&
        point.gameObject.activeInHierarchy && root.gameObject.activeInHierarchy &&
        (point == root || point.IsChildOf(root));
}
