Natürlich — hier ist eine kompakte, gut dokumentierte Markdown-Beschreibung des `PolyrhythmGenerator`-Algorithmus:

---

# 🌀 PolyrhythmGenerator

Der **PolyrhythmGenerator** erzeugt rhythmische Pattern durch die **Überlagerung zweier gleichmäßig verteilter Pulsreihen** mit unterschiedlichen Periodenlängen (z. B. 3 : 4, 5 : 7, 7 : 11).
Jede Reihe repräsentiert ein rhythmisches Subpattern, das unabhängig vom anderen auf demselben Raster abläuft.
Durch ihre Kombination entstehen charakteristische polyrhythmische Strukturen.

---

## ⚙️ Algorithmusbeschreibung

1. **Parameter:**

   * `a`: Anzahl der Pulse im ersten Rhythmus (z. B. 3)
   * `b`: Anzahl der Pulse im zweiten Rhythmus (z. B. 4)
   * `velA`, `velB`: optionale Anschlagsstärken (Velocity) für die jeweiligen Rhythmen
   * `lengthA`, `lengthB`: optionale Anschlag-Längen der einzelnen Steps für die jeweiligen Rhythmen

2. **Bestimmung der Zykluslänge:**

   * Der gemeinsame Zyklus ergibt sich aus dem **kleinsten gemeinsamen Vielfachen (LCM)** von `a` und `b`.
   * Beispiel: Für 3 : 4 ist der Zyklus `LCM(3,4) = 12` Schritte lang.

3. **Berechnung der Pulse:**

   * Die Pulse jedes Rhythmus werden **gleichmäßig** über den Zyklus verteilt:

     * Rhythmus A trifft alle `LCM / a` Schritte
     * Rhythmus B trifft alle `LCM / b` Schritte

4. **Überlagerung:**

   * Beide Rhythmen werden in einem gemeinsamen `RhythmPattern` zusammengeführt.
   * Wenn zwei Pulse gleichzeitig auftreten, werden ihre **Velocity-Werte addiert** (bis maximal 127).

---

## 🧮 Beispiel: 3 : 4-Polyrhythmus

| Schritt |    Rhythmus A    | Rhythmus B |     Ergebnis     |
| :-----: | :--------------: | :--------: | :--------------: |
|    0    |         ●        |      ●     | ● (Überlagerung) |
|    3    |                  |      ●     |         ●        |
|    4    |         ●        |            |         ●        |
|    6    |                  |      ●     |         ●        |
|    8    |         ●        |            |         ●        |
|    9    |                  |      ●     |         ●        |
|    12   | Cycle wiederholt |            |                  |

Das resultierende Pattern zeigt das bekannte **3-gegen-4**-Gefühl:
zwei gleichlange Rhythmen, deren Betonungen periodisch gegeneinander verschoben sind.

---

## 💡 Musikalische Anwendung

* Polyrhythmen sind in afrikanischen, indischen und minimalistischen Musiktraditionen (z. B. Steve Reich) weit verbreitet.
* Sie erzeugen Spannung und Bewegung, ohne das Grundtempo zu verändern.
* Ideal zur Generierung komplexer **mehrschichtiger Grooves** in generativer Musik.

---

## 🧩 Pseudocode

```text
function GeneratePolyrhythm(a, b):
    length = LCM(a, b)
    pattern = new RhythmPattern(length)
    for i in 0..length-1:
        hitA = (i % (length / a)) == 0
        hitB = (i % (length / b)) == 0
        if hitA or hitB:
            pattern.Hit[i] = true
            pattern.Velocity[i] = clamp(velA*hitA + velB*hitB)
    return pattern
```

---

## 🧠 Kurzfassung

> **PolyrhythmGenerator** = Überlagerung zweier gleichmäßig verteilter Pulse mit unterschiedlicher Periodenlänge →
> erzeugt rhythmische Interferenzmuster mit komplexem, aber regelmäßigem Groove.

---

# 🌿 LSystemGenerator

Der LSystemGenerator erzeugt rhythmische Pattern mithilfe von Lindenmayer-Systemen (L-Systemen) –
einem regelbasierten Verfahren aus der formalen Grammatik, das ursprünglich zur Modellierung pflanzlichen Wachstums entwickelt wurde.
In der Musik wird es genutzt, um selbstähnliche, rekursive und evolutive Rhythmen zu erzeugen.

⚙️ Algorithmusbeschreibung

Parameter:

axiom: Ausgangszeichenkette (Startsymbol, z. B. "A")

rules: Ersetzungsregeln (z. B. A → AB, B → A)

iterations: Anzahl der Iterationen, die die Regeln auf das Axiom anwenden

hitSymbol: Zeichen, das im resultierenden String als aktiver Schlag (Hit) interpretiert wird (z. B. 'X')

hitVelocity: Anschlagsstärke (Velocity) für Hits

Regeliteration:

Das Axiom wird wiederholt gemäß der angegebenen Regeln umgeschrieben:

Axiom:      A
Iteration 1: AB
Iteration 2: ABA
Iteration 3: ABAAB
...


Jede Iteration erweitert die Zeichenkette nach den Produktionsregeln.
Dadurch entstehen selbstähnliche, fraktale Strukturen.

Pattern-Erzeugung:

Nach Abschluss der Iterationen wird die finale Zeichenkette in ein rhythmisches Raster übertragen.

Jeder Index im String entspricht einem Schritt im Pattern:

Wenn das Zeichen dem hitSymbol entspricht → Hit

Sonst → Pause

Anpassung an Rasterlänge:

Wenn das L-System länger als das Raster ist, wird es modulo wiederholt.

Wenn es kürzer ist, wird es mehrfach aneinandergereiht, bis die Länge des Patterns erreicht ist.

🧮 Beispiel

Parameter:

Axiom: "A"
Rules: A → AB, B → A
Iterations: 3
HitSymbol: 'A'


Ergebnis der Iterationen:

1: A      → AB
2: AB     → ABA
3: ABA    → ABAAB


Pattern (HitSymbol = 'A'):

A B A A B
●   ● ●


→ Aktive Schritte an Position 0, 2 und 3 (bei 5 Gesamtschritten)

🧠 Musikalische Anwendung

L-Systeme erzeugen hierarchische rhythmische Strukturen: Wiederholung, Wachstum und Variation im selben Algorithmus.

Ideal für generative Kompositionen, algorithmische Patterns, „wachsendes“ Groove-Material.

Durch Variation der Regeln und Iterationszahl kann man von simplen Claves bis zu komplexen Polymetern alles erzeugen.

🧩 Pseudocode
function GenerateLSystem(axiom, rules, iterations, hitSymbol):
    current = axiom
    for i in 1..iterations:
        next = ""
        for c in current:
            if c in rules:
                next += rules[c]
            else:
                next += c
        current = next

    pattern = new RhythmPattern(length = current.Length)
    for i in 0..length-1:
        if current[i] == hitSymbol:
            pattern.Hit[i] = true
    return pattern

💡 Kurzfassung

LSystemGenerator = wiederholte Anwendung formaler Ersetzungsregeln auf eine Zeichenkette →
selbstähnliche, strukturell wachsende Rhythmen mit kontrollierter Komplexität.

🧩 Typische L-System-Regeln für rhythmische Pattern
Kategorie	Axiom	Regeln	Beschreibung	Rhythmisches Verhalten
1. Periodisch	A	A → AB, B → A	Klassisches Fibonacci-System	Entsteht ein sich wiederholendes, aber langsam wachsendes Pattern (ABAABABA...)
2. Alternierend	X	X → XY, Y → X	Simples Wechselmuster	Wechselt zwischen zwei Zuständen; gut für Clave- oder Pulsstrukturen
3. Verdichtend	A	A → AA, B → B	Jedes „A“ wird verdoppelt	Steigende Dichte über Iterationen, gleichbleibender Rhythmuscharakter
4. Expandierend (kanonisch)	A	A → AB, B → BB	Erweiterung um neue Pulse	Kanonartiges „Hinzugewinnen“ von Beats
5. Puls-Cluster	A	A → ABB, B → A	Clusterbildung mit unregelmäßigen Gruppen	Rhythmisch organisch, leicht chaotisch, für generative Percussion interessant
6. Symmetrisch	A	A → ABBA, B → BAAB	Spiegelnde Rekursion	Produziert palindromische Strukturen – gut für call/response-artige Patterns
7. Chaotisch / organisch	A	A → AB, B → BA	Gegenseitige Rekursion	Chaotisch-wachsende, aber symmetrisch balancierte Patterns
8. Fraktal reduziert	X	X → XYX, Y → Y	Rekursive Einbettung	Erzeugt fraktal anmutende rhythmische Dichten
9. Binär rhythmisch (Hit/Pause)	X	X → X0X1, 0 → 0, 1 → 1	Bit-ähnliches Wachstum	Nützlich für algorithmische Drum- oder Step-Sequencer-Strukturen
10. Polymetrisch	A	A → AB, B → AC, C → A	Drei sich überlagernde Zyklen	Überlagerung verschiedener Rhythmuslängen; ergibt Polymeter-Effekt
💡 Hinweise zur Anwendung

Wähle hitSymbol (z. B. 'A' oder 'X') als aktiven Puls, andere Zeichen werden als Pause oder Modulatoren interpretiert.

Du kannst Buchstaben als Instrumente verwenden – z. B.:

A = Kick

B = Snare

C = Hi-Hat
→ Dadurch entstehen komplexe, sich entwickelnde Groove-Patterns.

Iterationszahl steuert die Komplexität:

1–2: klare, periodische Strukturen

3–5: organisches Wachstum

6+: komplex, fraktal, kaum noch periodisch

🧠 Beispiel: „Symmetrisch wachsendes Pattern“

Axiom: A
Regeln: A → ABBA, B → BAAB
HitSymbol: A

Iteration 0: A
Iteration 1: ABBA
Iteration 2: ABBABAABBAABBA

→ ergibt ein palindromisches, in sich spiegelndes Rhythmusmuster –
ideal für strukturierte, aber „natürlich“ wirkende Grooves.

🔳 CellularAutomatonGenerator

Der CellularAutomatonGenerator erzeugt rhythmische Pattern auf Basis von
1D-Elementar-Zellulären Automaten nach Stephen Wolfram.
Dabei entwickelt sich ein binäres Gitter (Zellen = 0 oder 1) über mehrere Generationen
nach einer einfachen Regel, die bestimmt, wie sich eine Zelle aus ihren Nachbarn entwickelt.

Diese Methode eignet sich hervorragend für generative Rhythmen mit organischem,
wachsendem oder fraktalem Charakter.

⚙️ Algorithmusbeschreibung
1️⃣ Grundprinzip

Ein Zellulärer Automat (CA) besteht aus einer Zeile von Zellen (width),
wobei jede Zelle den Zustand 0 (aus) oder 1 (aktiv) hat.
In jeder neuen Generation berechnet sich der Zustand jeder Zelle aus drei Nachbarn:

Left  Center  Right  →  Next


Das Ergebnis wird durch eine Wolfram-Regelnummer (0–255) bestimmt.
Jede Regel definiert, welche Kombinationen aktiv werden.

Beispiel für Rule 90 (bekannt für fraktales Muster):

L	C	R	Next
1	1	1	0
1	1	0	1
1	0	1	0
1	0	0	1
0	1	1	1
0	1	0	0
0	0	1	1
0	0	0	0

Das Bitmuster dieser Tabelle entspricht der Binärdarstellung von 90
(01011010₂).

2️⃣ Ablauf

Initialisierung (Seed):

Startzeile wird anhand von CaSeed gesetzt:

SingleCenter → eine aktive Zelle in der Mitte

Random → Zufällige Belegung, optional reproduzierbar per Seed

Custom → vom Benutzer übergeben

Evolution (Generationen):

Für jede Generation wird jede Zelle gemäß der gewählten Regel aktualisiert.

Dabei werden Nachbarn über eine Randbedingung (CaBoundary) behandelt:

Wrap → zirkulär (linker Rand verbindet sich mit rechter Randzelle)

FixedZero → Randzellen haben immer Zustand 0

FixedOne → Randzellen haben immer Zustand 1

Mapping (Übertragung ins Rhythmus-Pattern):

Nach Abschluss der Generationen wird das Ergebnis auf das rhythmische Raster (RhythmPattern) abgebildet.
Dies erfolgt gemäß CaMapMode:

Modus	Beschreibung
LastRow	Nur die letzte Generation wird als Pattern verwendet
AnyHit	Jede Position, die jemals aktiv war, wird als Hit markiert
SumClip	Anzahl der Aktivierungen bestimmt Velocity (1–127)
EveryN	Jede n-te Generation wird sequentiell als Pattern-Schritt eingefügt
🧮 Beispiel
var ctx = new RhythmContext
{
    StepsTotal = 16,
    Meter = new(4,4),
    Timebase = new(4),
    Seed = 42
};

var gen = new CellularAutomatonGenerator(
    width: 16,
    generations: 10,
    rule: CaRule.Rule90,
    boundary: CaBoundary.Wrap,
    seedMode: CaSeed.SingleCenter,
    mapMode: CaMapMode.SumClip,
    onVelocity: 100
);

var pattern = gen.Generate(ctx);


Beschreibung:

Rule90 → XOR-Regel, erzeugt fraktal-artige Muster (ähnlich Sierpinski-Dreieck)

Wrap → zirkuläre Nachbarschaft, kein harter Rand

SumClip → je öfter eine Position im Verlauf aktiv war, desto stärker der Schlag (Velocity)

🧠 Intuitive Bedeutung
Komponente	Bedeutung in Musik-Metapher
Zelle	Rhythmische Position (z. B. 16tel-Note)
Zustand = 1	Aktiver Schlag (z. B. Kick, Snare)
Regel	Verhaltenslogik, wie Beats sich „entwickeln“
Generation	Zeitliche Entwicklung oder Variationszyklus
Randbedingung	Offenes Ende (Stop) oder Loop (Wraparound)

Damit kann man evolutionäre Rhythmen generieren, die zwischen Ordnung und Chaos changieren –
ähnlich minimaler Musik oder polyrhythmischer Elektronik (à la Autechre oder Reich).

🔢 Wichtige Parameter
Parameter	Typ	Beschreibung
width	int	Anzahl der Zellen (Patternbreite)
generations	int	Wie viele Generationen berechnet werden
rule	CaRule	Steuerungsregel (z. B. Rule30, Rule90, Rule110, Rule184)
boundary	CaBoundary	Verhalten am Rand (Wrap/FixedZero/FixedOne)
seedMode	CaSeed	Initialisierungsmodus (SingleCenter, Random, Custom)
mapMode	CaMapMode	Mapping-Strategie in Rhythmus-Raster
onVelocity	byte	Grundanschlagsstärke für aktive Zellen
everyN	int	Bei EveryN-Mapping: Generationsabstand
🎵 Typische Regelcharakteristika
Regel	Verhalten	Rhythmischer Eindruck
Rule30	chaotisch, pseudorandom	glitchig, komplex
Rule45	asymmetrisch, wachsend	organisch, progressiv
Rule54	stabil-zyklisch	technoid, metrisch stabil
Rule60	spiegelnd, symmetrisch	call/response-Struktur
Rule90	fraktal, geometrisch	Sierpinski-artig, „arithmetischer Groove“
Rule102	invertiertes Muster	invers rhythmisch
Rule110	komplex, fast-chaotisch	„Turing-voll“, evolutionär
Rule126	dichter, clusterartig	cluster beats, noiseartig
Rule150	XOR-basiert, linear	high-frequency, granular
Rule184	„Traffic rule“ (Bewegung nach rechts)	gleichmäßiger Puls mit Drift
💡 Anwendungsideen

Fraktale Beats: Rule90 oder Rule110 mit SumClip → selbstähnliche, „atmende“ Pattern

Polyrhythmische Texturen: Mehrere CA-Generatoren überlagern (Merge) → rhythmische Interferenzen

Evolving Sequences: mapMode = EveryN → jedes Durchlaufen des CA ergibt eine neue Variation

Humanize-Effekt: CA-Ausgabe als Maskierung über deterministische Patterns (z. B. Euclid × CA)

🧩 Pseudocode
function GenerateCA(width, generations, rule):
    cells = initialSeed(width)
    history = [cells]

    for g in 1..generations:
        next = []
        for i in 0..width-1:
            L = cells[(i-1) mod width]
            C = cells[i]
            R = cells[(i+1) mod width]
            code = (L<<2) | (C<<1) | R
            next[i] = ((rule >> code) & 1)
        history.append(next)
        cells = next

    pattern = mapHistoryToRhythm(history, mapMode)
    return pattern

✅ Kurzfassung

CellularAutomatonGenerator
= ein 1D-Wachstumsmodell, das einfache lokale Regeln nutzt, um
komplexe rhythmische Strukturen zu erzeugen –
von geordnet bis chaotisch, perfekt für algorithmische Komposition und Sounddesign.

🎲 RhythmForge – PoissonGenerator

Der PoissonGenerator erzeugt rhythmische Ereignisse auf Basis eines Poisson-Prozesses.
Dabei wird die Zeit als kontinuierlicher Fluss betrachtet, in dem Ereignisse mit einer festen
mittleren Rate λ (lambda) auftreten, deren Abstände aber zufällig variieren.

Das Ergebnis sind natürliche, organisch fluktuierende Rhythmen,
wie man sie z. B. bei biologischen oder physikalischen Prozessen findet (Tropfen, Klicks,
Klangpartikel, granularer Sound).

⚙️ Algorithmusbeschreibung

Ein Poisson-Prozess modelliert das zufällige Auftreten von Ereignissen über eine kontinuierliche Zeitachse,
wobei die durchschnittliche Ereignisrate pro Zeiteinheit konstant bleibt.

λ (Lambda) – die Ereignisrate pro Zeiteinheit (z. B. „Treffer pro Takt“).

Die Abstände zwischen Ereignissen folgen einer Exponentialverteilung mit Mittelwert 1 / λ.

Solange die kumulative Zeit t kleiner als 1 (eine Taktlänge) ist,
werden neue Ereignisse erzeugt, bis der Takt voll ist.

Die erzeugten Zeitpunkte werden anschließend auf ein diskretes Raster (RhythmPattern)
quantisiert, um MIDI-kompatible oder grid-basierte Beats zu bilden.

🧩 Mathematische Grundlage

Formel für die Exponentialverteilung der Intervalllängen:

Δ
𝑡
=
−
ln
⁡
(
𝑈
)
𝜆
Δt=−
λ
ln(U)
	​


wobei 
𝑈
U eine gleichverteilte Zufallszahl zwischen 0 und 1 ist.
Damit ist die Wahrscheinlichkeit eines langen Abstands gering,
kurze Abstände treten häufiger auf – aber nicht regelmäßig.

🧮 Parameter
Parameter	Typ	Beschreibung
lambdaPerBar	double	Durchschnittliche Ereignisrate (z. B. 2.5 Treffer pro Takt)
onVelocity	byte	Anschlagsstärke für erzeugte Ereignisse
(aus RhythmContext)		
StepsTotal	int	Anzahl der Raster-Schritte (z. B. 16 für ein 4/4-Takt-Raster)
TempoBpm	double	Nur für Timing/MIDI-Export relevant
Seed	int?	Optionaler Seed für deterministische Wiederholbarkeit
💡 Beispielverwendung
using RhythmForge;
using RhythmForge.Generators;

var ctx = new RhythmContext
{
    StepsTotal = 16,
    Meter = new(4,4),
    Timebase = new(4),
    Seed = 123  // optional → deterministisch
};

var poisson = new PoissonGenerator(
    lambdaPerBar: 2.5, // durchschnittlich 2–3 Hits pro Takt
    onVelocity: 100
);

var pattern = poisson.Generate(ctx);

foreach (var e in pattern.ToEvents())
    Console.WriteLine($"Step {e.Step:D2} → Velocity {e.Velocity}");


Ergebnis:
Das Pattern enthält eine zufällige, aber statistisch kontrollierte Verteilung von Hits –
z. B. bei 16 Steps und λ = 2.5 etwa 2–3 zufällige Schläge pro Durchlauf.

🧠 Musikalische Bedeutung
Konzept	Beschreibung
λ (lambda)	Steuert die durchschnittliche Dichte der Ereignisse
Hoher λ-Wert	dichter, fast gleichmäßig verteilter Rhythmus
Niedriger λ-Wert	spärlich, unregelmäßig, „atemend“
Seed	ermöglicht reproduzierbare oder variierende Ausgaben
Rasterisierung	macht kontinuierliche Zufallszeiten zu MIDI-kompatiblen Steps

Diese Art von Rhythmus eignet sich hervorragend für:

Generative Ambient- oder Glitch-Texturen

Granularsynthese-Trigger

Algorithmische Percussion / Click-Patterns

Ereignisdichte-Steuerung in Live-Performances

🧩 Pseudocode
function GeneratePoisson(lambda, steps):
    pattern = new RhythmPattern(steps)
    t = 0
    while t < 1:
        U = random(0, 1)
        Δt = -ln(U) / λ
        t += Δt
        if t >= 1: break
        step = round(t * (steps - 1))
        pattern.Hit[step] = true
    return pattern

🎛 Parameter-Tuning (Praxisbeispiele)
λ-Wert	Rhythmischer Charakter	Beschreibung
0.5	spärlich	nur 1 Ereignis pro 2 Takte im Schnitt
1.0	locker	ca. 1 Ereignis pro Takt
2.0–3.0	groovig	typische rhythmische Dichte
5.0+	dicht / textural	viele Ereignisse, fast gleichmäßig verteilt
✅ Kurzfassung

PoissonGenerator
= erzeugt rhythmische Ereignisse mit zufälligen, exponentiell verteilten Abständen.
Ideal für organische, stochastische Rhythmen, die sich realistisch und lebendig anfühlen,
besonders in generativer Musik und Sounddesign-Umgebungen.