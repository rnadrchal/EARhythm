using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Egami.Chemistry.Model;

namespace ChemSequencer.ViewModels;

public sealed class MoleculePlaybackViewModel : INotifyPropertyChanged
{
    public ObservableCollection<AtomVisual> Atoms { get; } = new();
    public ObservableCollection<BondVisual> Bonds { get; } = new();

    // Basis-Pixel-Länge für 1 Tic (kann zur Laufzeit angepasst werden)
    public double BaseTicPixel { get; set; } = 28.0;

    // Maximale Iterationen für Layout-Relaxation (klein halten für schnelle Reaktion)
    public int LayoutIterations { get; set; } = 250;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    public MoleculePlaybackViewModel()
    {
    }

    /// <summary>
    /// Update the visual graph from a StructureGraph and current activation state.
    /// - graph: the structure graph (Atoms,Bonds)
    /// - activeAtomIndices: indices of atoms currently active (highlighted)
    /// - activeBondIndices: indices of bonds currently active
    /// - bondTicProvider: function that returns number of grid tics for a bond; default = 1
    /// </summary>
    public void UpdateFromSequence(
        StructureGraph graph,
        IEnumerable<int>? activeAtomIndices = null,
        IEnumerable<int>? activeBondIndices = null,
        Func<BondEdge, int>? bondTicProvider = null)
    {
        if (graph is null) throw new ArgumentNullException(nameof(graph));

        var atomCount = graph.Atoms?.Count ?? 0;
        var bondCount = graph.Bonds?.Count ?? 0;

        // Prepare sets
        var activeAtoms = activeAtomIndices != null ? new HashSet<int>(activeAtomIndices) : new HashSet<int>();
        var activeBonds = activeBondIndices != null ? new HashSet<int>(activeBondIndices) : new HashSet<int>();
        bondTicProvider ??= (_ => 1);

        // Ensure visuals collections sizes match
        EnsureAtomVisuals(atomCount, graph);
        EnsureBondVisuals(bondCount, graph, bondTicProvider);

        // Compute desired edge lengths (pixels)
        var desiredLengths = new double[bondCount];
        for (int i = 0; i < bondCount; i++)
        {
            var tic = Math.Max(1, bondTicProvider(graph.Bonds[i]));
            desiredLengths[i] = BaseTicPixel * tic;
        }

        // Build adjacency for layout
        var adjacency = new List<int>[atomCount];
        for (int i = 0; i < atomCount; i++) adjacency[i] = new List<int>();
        for (int i = 0; i < bondCount; i++)
        {
            var e = graph.Bonds[i];
            adjacency[e.From].Add(e.To);
            adjacency[e.To].Add(e.From);
        }

        // Initial placement: circle
        var positions = new (double x, double y)[atomCount];
        if (atomCount == 1)
        {
            positions[0] = (0, 0);
        }
        else
        {
            double radius = Math.Max(80, BaseTicPixel * Math.Sqrt(atomCount) * 1.3);
            for (int i = 0; i < atomCount; i++)
            {
                double a = 2 * Math.PI * i / atomCount;
                positions[i] = (Math.Cos(a) * radius, Math.Sin(a) * radius);
            }
        }

        // --- Ring-Erkennung & polygonale Initialpositionierung ---
        var cycles = FindSimpleCycles(graph, maxCycleLength: 12);
        var isCycleAtom = new bool[atomCount];
        foreach (var cycle in cycles)
        {
            // mark atoms in cycle
            foreach (var idx in cycle) isCycleAtom[idx] = true;
            // place ring and radial substituents
            PlaceCycleAndSubstituents(graph, positions, cycle, desiredLengths);
        }

        // Relaxation: spring forces to match desiredLengths + small repulsion
        var rand = new Random(1234);
        for (int iter = 0; iter < LayoutIterations; iter++)
        {
            var disp = new (double dx, double dy)[atomCount];
            // Edge springs
            for (int ei = 0; ei < bondCount; ei++)
            {
                var e = graph.Bonds[ei];
                int u = e.From, v = e.To;
                var pu = positions[u];
                var pv = positions[v];
                double vx = pv.x - pu.x;
                double vy = pv.y - pu.y;
                double dist = Math.Sqrt(vx * vx + vy * vy);
                if (dist < 1e-6)
                {
                    // jitter to avoid overlap
                    vx = (rand.NextDouble() - 0.5) * 1e-3;
                    vy = (rand.NextDouble() - 0.5) * 1e-3;
                    dist = Math.Sqrt(vx * vx + vy * vy);
                }
                double target = desiredLengths[ei];
                double diff = dist - target;
                // spring constant scaled; increase for cycle edges to better preserve polygon shape
                double k = (isCycleAtom[u] && isCycleAtom[v]) ? 0.45 : 0.20;
                double fx = (diff / dist) * k * vx;
                double fy = (diff / dist) * k * vy;
                disp[u].dx += fx;
                disp[u].dy += fy;
                disp[v].dx -= fx;
                disp[v].dy -= fy;
            }

            // Repulsion (all pairs) - only for small graphs to keep complexity acceptable
            if (atomCount <= 120)
            {
                for (int i = 0; i < atomCount; i++)
                {
                    for (int j = i + 1; j < atomCount; j++)
                    {
                        var pi = positions[i];
                        var pj = positions[j];
                        double rx = pj.x - pi.x;
                        double ry = pj.y - pi.y;
                        double d2 = rx * rx + ry * ry;
                        double d = Math.Sqrt(Math.Max(d2, 1e-6));
                        double rep = 2000.0 / (d2 + 1.0); // tunable
                        double fx = rep * (rx / d);
                        double fy = rep * (ry / d);
                        disp[i].dx -= fx;
                        disp[i].dy -= fy;
                        disp[j].dx += fx;
                        disp[j].dy += fy;
                    }
                }
            }

            // Apply displacements with damping
            double damping = 0.65 * (1.0 - (double)iter / LayoutIterations);
            for (int i = 0; i < atomCount; i++)
            {
                // reduce movement for atoms that belong to detected rings to preserve polygonal shape
                double preserveFactor = isCycleAtom[i] ? 0.12 : 1.0;
                positions[i].x += disp[i].dx * damping * preserveFactor;
                positions[i].y += disp[i].dy * damping * preserveFactor;
            }
        }

        // Center positions around positive quadrant for Canvas usage
        double minX = positions.Min(p => p.x);
        double minY = positions.Min(p => p.y);
        // small margin
        const double margin = 16.0;
        for (int i = 0; i < atomCount; i++)
        {
            positions[i].x = positions[i].x - minX + margin;
            positions[i].y = positions[i].y - minY + margin;
        }

        // Update AtomVisuals
        for (int i = 0; i < atomCount; i++)
        {
            var aNode = graph.Atoms[i];
            var av = Atoms[i];
            av.X = positions[i].x;
            av.Y = positions[i].y;
            av.Element = aNode.Element ?? aNode.Element ?? "X";
            av.Label = av.Element;
            av.Fill = CpkColor(av.Element);
            av.IsActive = activeAtoms.Contains(i);
            av.Radius = Math.Clamp(12.0 + (aNode.ElementProps?.AtomicNumber ?? 6) * 0.35, 8, 20);
        }

        // Update BondVisuals (endpoints + stroke)
        for (int bi = 0; bi < bondCount; bi++)
        {
            var e = graph.Bonds[bi];
            var bv = Bonds[bi];
            bv.FromIndex = e.From;
            bv.ToIndex = e.To;
            bv.StartX = Atoms[e.From].X;
            bv.StartY = Atoms[e.From].Y;
            bv.EndX = Atoms[e.To].X;
            bv.EndY = Atoms[e.To].Y;
            bv.IsActive = activeBonds.Contains(bi);
            bv.Thickness = bv.IsActive ? 3.2 : 1.6;
            bv.Stroke = bv.IsActive ? Brushes.OrangeRed : Brushes.LightGray;
            // optionally show desired length by setting StrokeDashArray or other visuals; here length influences layout only
        }

        // Notify collections changed by replacing items (we updated properties in-place, so just raise)
        Raise(nameof(Atoms));
        Raise(nameof(Bonds));
    }
    private void EnsureAtomVisuals(int count, StructureGraph graph)
    {
        while (Atoms.Count < count)
            Atoms.Add(new AtomVisual());
        while (Atoms.Count > count)
            Atoms.RemoveAt(Atoms.Count - 1);

        // Initialize some metadata from graph atoms
        for (int i = 0; i < Math.Min(count, graph.Atoms.Count); i++)
        {
            var node = graph.Atoms[i];
            var av = Atoms[i];
            av.Index = node.Index;
            av.Element = node.Element ?? node.Element ?? av.Element;
        }
    }

    private void EnsureBondVisuals(int count, StructureGraph graph, Func<BondEdge,int> bondTicProvider)
    {
        while (Bonds.Count < count)
            Bonds.Add(new BondVisual());
        while (Bonds.Count > count)
            Bonds.RemoveAt(Bonds.Count - 1);

        for (int i = 0; i < Math.Min(count, graph.Bonds.Count); i++)
        {
            var e = graph.Bonds[i];
            var bv = Bonds[i];
            bv.Index = i;
            bv.Order = e.Order;
            bv.LengthTics = Math.Max(1, bondTicProvider(e));
        }
    }

    // Hilfsmethode: Platziert einen erkannten Ring als regelmäßiges Polygon und richtet
    // direkt an den Ring gebundene Substituenten radial aus (bessere Darstellung z.B. für Phenol).
    private void PlaceCycleAndSubstituents(StructureGraph graph, (double x, double y)[] positions, List<int> cycle, double[] desiredLengths)
    {
        int n = cycle.Count;
        if (n < 3) return;

        // mittlere Kantenlänge im Ring
        double avgS = 0; int edgesOnCycle = 0;
        for (int i = 0; i < n; i++)
        {
            int a = cycle[i], b = cycle[(i + 1) % n];
            var bond = graph.Bonds.FirstOrDefault(be => (be.From == a && be.To == b) || (be.From == b && be.To == a));
            if (bond is not null)
            {
                int bi = FindBondIndex(graph, bond);
                if (bi >= 0) { avgS += desiredLengths[bi]; edgesOnCycle++; }
            }
        }
        if (edgesOnCycle == 0) return;
        avgS /= edgesOnCycle;

        // regulärer Polygon-Radius
        double R = avgS / (2.0 * Math.Sin(Math.PI / n));

        // center (mittlerer Ort der aktuellen Ring-Positionen)
        double cx = 0, cy = 0;
        foreach (var idx in cycle) { cx += positions[idx].x; cy += positions[idx].y; }
        cx /= n; cy /= n;

        // optional: bestimme Rotation so dass Substituent (falls vorhanden) zeigt
        // Suche zuerst einen Ring-Vertex mit Substituent (Nachbar, der nicht im Ring ist)
        int rotIndex = 0;
        double rotAngle = 0.0;
        for (int i = 0; i < n; i++)
        {
            int v = cycle[i];
            var neighbors = graph.Bonds
                .Where(b => b.From == v || b.To == v)
                .Select(b => b.From == v ? b.To : b.From)
                .Distinct()
                .Where(nb => !cycle.Contains(nb))
                .ToArray();
            if (neighbors.Length > 0)
            {
                // orientiere Polygon so, dass die erste Substituentenrichtung nach "oben" zeigt
                rotIndex = i;
                // berechne Zielwinkel (up = -PI/2)
                rotAngle = -Math.PI / 2.0;
                break;
            }
        }

        // setze Eckpunkte des Polygons; rotiere so rotIndex auf rotAngle steht
        for (int i = 0; i < n; i++)
        {
            double baseAngle = 2 * Math.PI * i / n;
            // rotate so that rotIndex aligns to rotAngle
            double angle = baseAngle - (2 * Math.PI * rotIndex / n) + rotAngle;
            positions[cycle[i]].x = cx + Math.Cos(angle) * R;
            positions[cycle[i]].y = cy + Math.Sin(angle) * R;
        }

        // Platziere direkte Substituenten radial außerhalb des Ring-Vertex
        for (int i = 0; i < n; i++)
        {
            int v = cycle[i];
            // outward unit vector from center to vertex
            double ux = positions[v].x - cx;
            double uy = positions[v].y - cy;
            var norm = Math.Sqrt(ux * ux + uy * uy);
            if (norm < 1e-6) continue;
            ux /= norm; uy /= norm;

            // finde Nachbarn, die nicht im Ring sind
            var externNeighbors = graph.Bonds
                .Where(b => b.From == v || b.To == v)
                .Select(b => b.From == v ? b.To : b.From)
                .Distinct()
                .Where(nb => !cycle.Contains(nb))
                .ToArray();

            foreach (var nb in externNeighbors)
            {
                var bond = graph.Bonds.FirstOrDefault(be => (be.From == v && be.To == nb) || (be.From == nb && be.To == v));
                double bondLen = avgS;
                if (bond is not null)
                {
                    int bi = FindBondIndex(graph, bond);
                    if (bi >= 0) bondLen = desiredLengths[bi];
                }
                double offset = bondLen + 6.0;
                positions[nb].x = positions[v].x + ux * offset;
                positions[nb].y = positions[v].y + uy * offset;
            }
        }
    }

    // Neue Hilfsmethode: findet einfache Zyklen (Ringe) im StructureGraph
    private static List<List<int>> FindSimpleCycles(StructureGraph graph, int maxCycleLength = 12)
    {
        var cycles = new List<List<int>>();
        if (graph == null) return cycles;

        int atomCount = graph.Atoms?.Count ?? 0;
        var bonds = graph.Bonds;
        int bondCount = bonds?.Count ?? 0;

        // adjacency für Suche
        var adjacency = new List<int>[atomCount];
        for (int i = 0; i < atomCount; i++) adjacency[i] = new List<int>();
        for (int i = 0; i < bondCount; i++)
        {
            var e = bonds[i];
            adjacency[e.From].Add(e.To);
            adjacency[e.To].Add(e.From);
        }

        var seenKeys = new HashSet<string>();

        // Für jede Kante: entferne virtuell die Kante und suche kürzesten Pfad zwischen Endpunkten
        for (int bi = 0; bi < bondCount; bi++)
        {
            var e = bonds[bi];
            int u = e.From, v = e.To;

            // BFS von u nach v, Kante (u,v) überspringen
            var prev = new int[atomCount];
            for (int i = 0; i < atomCount; i++) prev[i] = -1;
            var q = new Queue<int>();
            q.Enqueue(u);
            prev[u] = u;
            bool found = false;

            while (q.Count > 0 && !found)
            {
                var x = q.Dequeue();
                foreach (var nb in adjacency[x])
                {
                    if ((x == u && nb == v) || (x == v && nb == u)) continue; // skip test-edge
                    if (prev[nb] != -1) continue;
                    prev[nb] = x;
                    if (nb == v) { found = true; break; }
                    q.Enqueue(nb);
                }
            }

            if (!found) continue;

            // rekonstruiere Pfad u -> v
            var path = new List<int>();
            int cur = v;
            while (cur != prev[cur])
            {
                path.Add(cur);
                cur = prev[cur];
            }
            path.Add(u);
            path.Reverse();

            // Ring = Pfad + Kante (u,v). Mindestens 3 Knoten
            if (path.Count < 3 || path.Count > maxCycleLength) continue;

            // Normalisiere Drehung/Richtung um Duplikate zu vermeiden
            int n = path.Count;
            int minPos = 0;
            for (int i = 1; i < n; i++) if (path[i] < path[minPos]) minPos = i;

            var norm = new List<int>(n);
            for (int i = 0; i < n; i++) norm.Add(path[(minPos + i) % n]);

            var rev = new List<int>(norm);
            rev.Reverse();

            string k1 = string.Join(",", norm);
            string k2 = string.Join(",", rev);
            if (seenKeys.Contains(k1) || seenKeys.Contains(k2)) continue;

            seenKeys.Add(k1);
            cycles.Add(norm);
        }

        return cycles;
    }

    // Neue Hilfsmethode: findet Bond-Index in IReadOnlyList<BondEdge>
    private static int FindBondIndex(StructureGraph graph, BondEdge? bond)
    {
        if (bond is null) return -1;
        var bonds = graph.Bonds;
        for (int i = 0; i < bonds.Count; i++)
        {
            var b = bonds[i];
            if (ReferenceEquals(b, bond)) return i;
            // match endpoints (beide Richtungen) und Order als Fallback
            if ((b.From == bond.From && b.To == bond.To && b.Order == bond.Order) ||
                (b.From == bond.To && b.To == bond.From && b.Order == bond.Order))
                return i;
        }
        return -1;
    }

    private static Brush CpkColor(string element)
    {
        // common CPK coloring (subset). returns frozen SolidColorBrush.
        // Add more elements as needed.
        element = element?.Trim() ?? "";
        if (element.Equals("H", StringComparison.OrdinalIgnoreCase)) return Brushes.White;
        if (element.Equals("C", StringComparison.OrdinalIgnoreCase)) return new SolidColorBrush(Color.FromRgb(50, 50, 50)) { Opacity = 1.0 };
        if (element.Equals("N", StringComparison.OrdinalIgnoreCase)) return new SolidColorBrush(Color.FromRgb(48, 80, 248));
        if (element.Equals("O", StringComparison.OrdinalIgnoreCase)) return Brushes.Red;
        if (element.Equals("S", StringComparison.OrdinalIgnoreCase)) return new SolidColorBrush(Color.FromRgb(255, 200, 50));
        if (element.Equals("P", StringComparison.OrdinalIgnoreCase)) return new SolidColorBrush(Color.FromRgb(255, 128, 0));
        if (element.Equals("F", StringComparison.OrdinalIgnoreCase) || element.Equals("Cl", StringComparison.OrdinalIgnoreCase)) return Brushes.Green;
        if (element.Equals("Cl", StringComparison.OrdinalIgnoreCase)) return Brushes.Green;
        if (element.Equals("Br", StringComparison.OrdinalIgnoreCase)) return new SolidColorBrush(Color.FromRgb(165, 42, 42)); // brown
        if (element.Equals("I", StringComparison.OrdinalIgnoreCase)) return Brushes.Purple;
        if (element.Equals("Fe", StringComparison.OrdinalIgnoreCase)) return new SolidColorBrush(Color.FromRgb(224, 102, 51));
        if (element.Equals("Ca", StringComparison.OrdinalIgnoreCase)) return new SolidColorBrush(Color.FromRgb(128, 128, 128));
        // fallback
        return new SolidColorBrush(Color.FromRgb(180, 180, 180));
    }
}

public sealed class AtomVisual : INotifyPropertyChanged
{
    public int Index { get; set; }
    private double _x, _y, _radius;
    private string? _element;
    private string? _label;
    private Brush? _fill;
    private bool _isActive;

    public double X { get => _x; set { _x = value; Raise(); } }
    public double Y { get => _y; set { _y = value; Raise(); } }
    public double Radius { get => _radius; set { _radius = value; Raise(); } }
    public string? Element { get => _element; set { _element = value; Raise(); } }
    public string? Label { get => _label; set { _label = value; Raise(); } }
    public Brush? Fill { get => _fill; set { _fill = value; Raise(); } }
    public bool IsActive { get => _isActive; set { _isActive = value; Raise(); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public sealed class BondVisual : INotifyPropertyChanged
{
    public int Index { get; set; }
    public int FromIndex { get; set; }
    public int ToIndex { get; set; }
    private double _startX, _startY, _endX, _endY;
    private Brush? _stroke;
    private double _thickness;
    private bool _isActive;
    public int Order { get; set; }
    public int LengthTics { get; set; } = 1;

    public double StartX { get => _startX; set { _startX = value; Raise(); } }
    public double StartY { get => _startY; set { _startY = value; Raise(); } }
    public double EndX { get => _endX; set { _endX = value; Raise(); } }
    public double EndY { get => _endY; set { _endY = value; Raise(); } }

    public Brush? Stroke { get => _stroke; set { _stroke = value; Raise(); } }
    public double Thickness { get => _thickness; set { _thickness = value; Raise(); } }
    public bool IsActive { get => _isActive; set { _isActive = value; Raise(); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}