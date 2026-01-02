namespace Egami.Chemistry.Graph;

/// <summary>
/// Liefert Segmente gruppiert in "Layer". Für die reine Sequenz-Bauphase
/// ist hier wichtiger: wir geben die Layer-Nodes aus.
/// </summary>
internal sealed class BfsLayerChordTraversal : ITraversalStrategy
{
    public IEnumerable<TraversalSegment> Traverse(IMoleculeGraph graph, TraversalOptions options)
    {
        // Für LayerChord benutzen wir Segmente nur als "u->v Vorschläge".
        // Die eigentliche Layerbildung passiert im SequenceBuilder (siehe unten),
        // weil dort auch die StepIndex-Planung + LayerDuration entschieden wird.
        //
        // Daher: wir geben hier eine BFS-seq ähnliche Ausgabe zurück, aber der Builder
        // wird sie nicht 1:1 verwenden. (Sauberer: eigenes Layer-API – kommt als nächstes.)
        return new BfsSequentialTraversal().Traverse(graph, options);
    }
}