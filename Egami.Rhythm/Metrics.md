Natürlich — hier ist eine **kompakte, aber präzise Markdown-Dokumentation** zu den fünf Metriken, inklusive ihrer **Bedeutung, Interpretation und typischer Einsatzbereiche** im Kontext rhythmischer Patternanalyse:

---

# 🎛️ Quantitative Metriken zur Bewertung euklidischer Rhythmusähnlichkeit

Dieses Dokument beschreibt fünf Metriken, die es ermöglichen, ein gegebenes rhythmisches Pattern quantitativ darauf zu prüfen, **wie stark es einem ideal euklidischen Pattern entspricht**.
Alle Metriken basieren auf binären Pattern-Arrays (`bool[]`), wobei `true` einen **Onset (Hit)** und `false` eine **Pause** darstellt.

---

## 1. 🧮 Hamming Similarity (rotationsinvariant)

### **Definition**

Vergleicht zwei binäre Pattern und zählt die Anzahl unterschiedlicher Werte (1/0) pro Position.
Um zeitliche Verschiebungen zu kompensieren, wird die minimale Distanz über alle Rotationen des Patterns berechnet.

[
D_H^{rot}(E, R) = \min_s \sum_i |E_i - R_{(i+s)\bmod n}|
]

### **Wertebereich**

* **0:** völlig unterschiedlich
* **1:** identisch (bis auf Rotation)

### **Aussage**

Misst **exakte strukturelle Übereinstimmung** der Onsets.
Ideal, wenn es um *formale Gleichheit* der Pattern geht.

### **Einsatzbereich**

* Vergleich algorithmisch erzeugter vs. manuell programmierter Pattern
* Mustererkennung (z. B. Quantisierung rhythmischer Sequenzen)
* Evaluierung generativer Rhythmen gegen ideale Verteilungen

---

## 2. ⚖️ Jaccard Similarity

### **Definition**

Misst den Anteil gemeinsamer Onset-Positionen bezogen auf die Vereinigungsmenge aller Onsets.

[
J(E, R) = \frac{|E \cap R|}{|E \cup R|}
]

### **Wertebereich**

* **0:** keine gemeinsamen Onsets
* **1:** exakt gleiche Onset-Positionen

### **Aussage**

Misst den **Überlappungsgrad** zwischen zwei Onset-Sets, unabhängig von deren zeitlicher Anordnung.

### **Einsatzbereich**

* Robust gegenüber kleinen rhythmischen Abweichungen
* Vergleich unterschiedlicher Pattern mit gleichem Onset-Zählwert
* Evaluation von Pattern-Clustern oder rhythmischen Variationen

---

## 3. 🕒 IOI-MAE (Mean Absolute Error der Inter-Onset-Intervalle)

### **Definition**

Berechnet den mittleren absoluten Unterschied der Abstände zwischen aufeinanderfolgenden Onsets („Inter-Onset-Interval“, IOI).
Ziel ist die Messung, **wie gleichmäßig die Abstände verteilt sind**.

[
D_{IOI} = \frac{1}{k} \sum_i |IOI_{E,i} - IOI_{R,i}|
]

### **Wertebereich**

* **0:** perfekte Gleichverteilung (ideal euklidisch)
* **1:** maximale Unregelmäßigkeit

### **Aussage**

Bewertet die **gleichmäßige Verteilung der Onsets** unabhängig von ihrer Startposition.
Perfekte euklidische Pattern haben konstante IOI-Werte.

### **Einsatzbereich**

* Analyse rhythmischer Gleichmäßigkeit
* Quantitative Bewertung von *Swing*, *Groove* oder *Humanization*
* Kategorisierung rhythmischer Dichteverteilungen

---

## 4. 🔁 Wasserstein-1-Distanz (Circular Earth Mover’s Distance)

### **Definition**

Misst die minimale „Transportarbeit“, um die Onsets des einen Patterns in die Positionen des anderen zu verschieben — auf einem Kreis betrachtet.

[
D_W(E, R) = \min_{\text{Rotation}} \frac{1}{k} \sum_i d_{circ}(E_i, R_i)
]
mit
[
d_{circ}(a,b) = \min(|a-b|, n - |a-b|)
]

### **Wertebereich**

* **0:** identisch oder rotationsäquivalent
* **1:** maximal unterschiedlich

### **Aussage**

Erfasst **zeitliche Nähe** und **strukturelle Ähnlichkeit** der Onsets.
Sensibel für Verschiebungen und Mikroabweichungen, aber robust gegen Rotationen.

### **Einsatzbereich**

* Wahrnehmungsnahe Rhythmusvergleiche
* Clustering rhythmischer Pattern nach *Formähnlichkeit*
* Beurteilung von Patternvarianten im Zeitkontinuum (z. B. bei Polyrhythmen)

---

## 5. 🔊 Autokorrelations-Cosine-Similarity

### **Definition**

Berechnet die zirkulare Autokorrelation jedes Patterns (Selbstähnlichkeit über alle Lags) und vergleicht deren Struktur per Cosine Similarity.

[
\rho_E(\tau) = \sum_i E_i E_{(i+\tau)\bmod n}
]
[
sim = \frac{\rho_E \cdot \rho_R}{||\rho_E|| , ||\rho_R||}
]

### **Wertebereich**

* **0:** völlig unterschiedliche Pulsstruktur
* **1:** gleiche rhythmische Periodizität

### **Aussage**

Bewertet die **interne Pulsstruktur** und damit die wahrgenommene rhythmische Ähnlichkeit — auch wenn einzelne Onsets verschieden sind.
Ein Pattern mit gleicher Periodizität, aber leicht verschobenen Onsets, erzielt dennoch hohe Ähnlichkeit.

### **Einsatzbereich**

* Wahrnehmungsorientierte Rhythmusanalyse
* Erkennung metrisch ähnlicher Pattern
* Klassifikation rhythmischer Archetypen (z. B. Clave, Tresillo, Techno-Pattern)

---

## 🧩 Vergleichstabelle

| Metrik                 | Typ              | Rotation-invariant | Wahrnehmungsrelevant | Wertebereich | Aussage                   |
| :--------------------- | :--------------- | :----------------: | :------------------: | :----------- | :------------------------ |
| **Hamming Similarity** | binär            |          ✅         |           ❌          | 0–1          | Strukturelle Identität    |
| **Jaccard Similarity** | mengenbasiert    |          ✅         |          🔸          | 0–1          | Onset-Überlappung         |
| **IOI-MAE**            | intervallbasiert |          ✅         |           ✅          | 0–1          | Gleichmäßigkeit           |
| **Wasserstein-1**      | geometrisch      |          ✅         |           ✅          | 0–1          | Zeitliche Formähnlichkeit |
| **Autokorrelation**    | spektral         |          ✅         |           ✅          | 0–1          | Pulsstruktur-Ähnlichkeit  |

---

## 🧠 Interpretationsempfehlung

| Szenario                                                    | Empfohlene Metrik                                      |
| ----------------------------------------------------------- | ------------------------------------------------------ |
| **Exakte Musteridentifikation**                             | Hamming Similarity                                     |
| **Variationen mit ähnlicher Struktur**                      | Jaccard oder Wasserstein                               |
| **Analyse der Gleichverteilung**                            | IOI-MAE                                                |
| **Wahrnehmungsähnliche Vergleichbarkeit**                   | Autokorrelations-Similarity                            |
| **Generative Rhythmusevaluation (z. B. Euclidicity Score)** | Kombination mehrerer Metriken (gewichteter Mittelwert) |

---

## 💡 Beispielhafte Anwendung

```csharp
bool[] pattern = { true, false, false, true, false, false, true, false };
bool[] euclid  = RhythmExtensions.EuclideanPattern(8, 3);

double s1 = pattern.HammingSimilarityRot(euclid);
double s2 = pattern.JaccardSimilarity(euclid);
double d3 = pattern.IoiMae(euclid);
double d4 = pattern.WassersteinCircular(euclid);
double s5 = pattern.AutocorrCosineSimilarity(euclid);
```

---

