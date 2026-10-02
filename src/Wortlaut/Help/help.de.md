Help texts of Wortlaut (German). Format: see src/Wortlaut/Core/Help/HelpDocument.cs.
The Russian file help.ru.md must contain the same topics in the same order.

# about | Was macht Wortlaut?

Wortlaut schreibt auf, was in Video- und Audioaufnahmen gesprochen wird – zum Beispiel bei Vorträgen, Seminaren, Interviews oder Gesprächen. Aus einer Aufnahme wird eine Textdatei, die Sie lesen, durchsuchen, ausdrucken oder weiterbearbeiten können.

## Ihre Aufnahmen bleiben bei Ihnen

Die Spracherkennung läuft vollständig auf Ihrem Computer. **Keine Aufnahme und kein Text wird ins Internet hochgeladen.** Sie brauchen kein Benutzerkonto und kein Abo.

Das Internet wird nur einmalig gebraucht: zum Herunterladen des Spracherkennungsprogramms und der Sprachmodelle. Danach funktioniert Wortlaut auch ohne Internet.

## Ihre Dateien bleiben unverändert

Wortlaut liest Ihre Aufnahmen nur. Sie werden nie umbenannt, verschoben oder gelöscht. Der Text wird als neue Datei neben die Aufnahme gelegt.

## Woher kommt die Spracherkennung?

Die eigentliche Erkennung übernimmt das kostenlose Programm Faster-Whisper-XXL. Es beruht auf „Whisper“, einer frei verfügbaren Spracherkennung. Wortlaut ist die einfache Oberfläche dafür.

# start | Erste Schritte

1. **Datei wählen:** Ziehen Sie eine Video- oder Audiodatei mit der Maus in das Wortlaut-Fenster. Oder klicken Sie auf „Datei wählen…“.
2. **Einstellungen prüfen:** Oben stehen Modell, Gerät, Sprache und Format. Die vorgeschlagenen Werte passen meistens. Für Russisch: Sprache „Russisch (ru)“.
3. **Starten:** Klicken Sie auf „Transkribieren“. Der Balken zeigt, wie weit die Erkennung ist. Mit „Abbrechen“ können Sie jederzeit aufhören.
4. **Ergebnis öffnen:** Der Text liegt im selben Ordner wie die Aufnahme und heißt genauso – nur mit einer anderen Endung. Aus `Vortrag.mp4` wird zum Beispiel `Vortrag.txt`. Unter „Ergebnis:“ sehen Sie den genauen Pfad schon vorher.

## Wie lange dauert das?

Das hängt vor allem davon ab, ob Ihr Computer eine passende Grafikkarte hat (siehe „Gerät“). Beim ersten Start dauert es etwas länger, weil das Modell geladen wird.

## Gibt es schon einen Text?

Liegt neben der Aufnahme schon ein Text, überspringt Wortlaut die Datei. Soll der alte Text ersetzt werden, setzen Sie das Häkchen bei „Vorhandenes Transkript überschreiben“.

## Unterstützte Dateien

Video und Audio in diesen Formaten: `.mp4 .mp3 .ogg .m4a .mov .avi .wmv .webm .mpeg .m2p .mpg`.

# models | Modelle

Das Modell ist das „Gehirn“ der Spracherkennung. Größere Modelle erkennen genauer, brauchen aber mehr Zeit und mehr Speicherplatz. Jedes Modell wird nur einmal heruntergeladen und danach immer wieder verwendet. Unter „Modelle…“ (neben der Modellauswahl) können Sie Modelle herunterladen und löschen.

- **large-v2** (2,9 GB) – sehr genau. **Unsere Empfehlung für russische Vorträge.**
- **large-v3** (2,9 GB) – ebenfalls sehr genau, erfindet aber bei Pausen, Stille oder Musik manchmal Text, der nicht gesprochen wurde.
- **large-v3-turbo** (1,5 GB) – fast so genau wie die großen Modelle, aber deutlich schneller. Gut ohne passende Grafikkarte oder mit wenig Grafikspeicher.
- **medium** (1,4 GB) – schneller, aber etwas ungenauer.
- **small** (0,5 GB) – sehr schnell, für einfache und deutliche Aufnahmen.

## Welches soll ich nehmen?

Lassen Sie Wortlaut entscheiden: Klicken Sie neben „Gerät“ auf „Prüfen…“ und dann auf „Vorschlag übernehmen“. Wenn Ihnen das Ergebnis nicht genau genug ist, probieren Sie ein größeres Modell.

# device | Gerät: Grafikkarte oder Prozessor

Die Spracherkennung braucht viel Rechenleistung. Wortlaut kann sie auf zwei Arten erledigen:

- **cuda – mit der Grafikkarte.** Das ist um ein Vielfaches schneller. Es funktioniert nur mit einer NVIDIA-Grafikkarte. „CUDA“ ist der Name der NVIDIA-Technik dafür.
- **cpu – mit dem Prozessor.** Das funktioniert auf jedem Computer, dauert aber deutlich länger.

## Was heißt „langsamer“ konkret?

Als grobe Orientierung für eine Stunde Aufnahme: Mit einer Mittelklasse-Grafikkarte dauert die Erkennung etwa 10 bis 20 Minuten. Mit dem Prozessor eher eine bis mehrere Stunden – je nach Computer und Modell. Mit dem Prozessor empfehlen wir das schnellere Modell „large-v3-turbo“.

## Woher weiß ich, was mein Computer hat?

Klicken Sie neben „Gerät“ auf „Prüfen…“. Wortlaut prüft die Grafikkarte und schlägt Gerät und Modell vor. Mit „Vorschlag übernehmen“ wird beides eingestellt.

# language | Sprache

Hier stellen Sie ein, in welcher Sprache in der Aufnahme gesprochen wird.

- **Eine feste Sprache** (z. B. „Russisch (ru)“) ist die beste Wahl, wenn Sie die Sprache kennen. Die Erkennung ist dann zuverlässiger.
- **„Automatisch erkennen“** lässt Wortlaut die Sprache anhand der ersten Sekunden selbst bestimmen. Das ist praktisch bei gemischten Ordnern, kann aber danebenliegen – zum Beispiel, wenn die Aufnahme mit Musik oder einer anderen Sprache beginnt.

Andere Sprachen können Sie mit ihrem Kürzel eintippen, zum Beispiel `fr` für Französisch oder `uk` für Ukrainisch.

Hinweis: Wortlaut schreibt auf, was gesprochen wird. Es übersetzt nicht.

# formats | Formate und „Ganze Sätze“

Das Format bestimmt, wie die Ergebnisdatei aussieht:

- **Text (.txt)** – reiner Text ohne Zeitangaben. Das Richtige zum Lesen, Ausdrucken und Weiterbearbeiten.
- **Untertitel (.srt)** – Text mit Zeitangaben, passend zum Video. Videoplayer wie VLC zeigen ihn als Untertitel an, wenn er so heißt wie das Video.
- **WebVTT (.vtt)** – Untertitel für Webseiten und Online-Videoplayer.
- **JSON (.json)** – für Programme und Weiterverarbeitung, mit allen Details. Zum Lesen nicht geeignet.

## „Ganze Sätze“

Ist das Häkchen gesetzt, beginnt jeder Abschnitt mit einem neuen Satz, und Sätze werden nicht mitten im Satz geteilt. Beim Format Text steht dann ein Satz pro Zeile. Das gilt für Text, Untertitel und WebVTT – **nicht für JSON**, dort bleiben die ursprünglichen Abschnitte erhalten.

# folder | Ganzer Ordner

Im Reiter „Ganzer Ordner“ bearbeiten Sie viele Aufnahmen auf einmal.

1. Wählen Sie den Ordner mit „Ordner wählen…“ oder ziehen Sie ihn in das Fenster.
2. Die Liste zeigt alle Aufnahmen mit Länge und Status. Entfernen Sie das Häkchen bei Dateien, die nicht bearbeitet werden sollen.
3. Klicken Sie auf „Alle transkribieren“. Die Dateien werden nacheinander bearbeitet.

## Vorhandene Texte überspringen

Mit „Dateien mit vorhandenem Transkript überspringen“ (Standard) bearbeitet Wortlaut nur Aufnahmen, die noch keinen Text haben. Ohne das Häkchen werden vorhandene Texte ersetzt.

## Abbrechen und später weitermachen

„Abbrechen“ stoppt die laufende Datei und alle weiteren. Fertige Texte bleiben erhalten. Starten Sie den Ordner später einfach noch einmal: Dank „überspringen“ macht Wortlaut dort weiter, wo es aufgehört hat.

Schlägt eine Datei fehl, macht Wortlaut mit der nächsten weiter. Fahren Sie mit der Maus über „Fehler“, um den Grund zu sehen.

# problems | Probleme und Lösungen

## Windows warnt beim ersten Start

Erscheint „Der Computer wurde durch Windows geschützt“, klicken Sie auf **„Weitere Informationen“** und dann auf **„Trotzdem ausführen“**. Windows zeigt diese Warnung bei Programmen, die noch wenig verbreitet sind. Laden Sie Wortlaut nur von der offiziellen Projektseite herunter.

## Der Virenscanner meldet faster-whisper

Manche Virenscanner melden Faster-Whisper-XXL fälschlich, weil es ein großes, selbst entpackendes Programm ist. Wortlaut lädt es ausschließlich von der offiziellen Seite des Entwicklers auf GitHub. Fügen Sie im Virenscanner eine Ausnahme für diesen Ordner hinzu: `%LOCALAPPDATA%\Wortlaut`.

## Kein Internet bei der Einrichtung

Das Internet wird nur für die Downloads gebraucht. Bricht die Verbindung ab, klicken Sie auf „Erneut versuchen“: Der Download macht dort weiter, wo er aufgehört hat. Haben Sie Faster-Whisper-XXL schon auf einem anderen Weg bekommen (z. B. per USB-Stick), wählen Sie es mit „Ich habe Faster-Whisper-XXL schon…“ aus.

## Zu wenig Speicherplatz

Das Spracherkennungsprogramm braucht etwa 6 GB, jedes Modell zusätzlich 0,5 bis 3 GB. Schaffen Sie Platz auf dem Laufwerk `C:` oder löschen Sie nicht benötigte Modelle unter „Modelle…“.

## „faster-whisper ist nach dem Schreiben abgestürzt“

Dieser Hinweis ist **harmlos**. Faster-Whisper-XXL stürzt manchmal beim Beenden ab, nachdem der Text schon fertig geschrieben ist. Der Text ist vollständig und wurde übernommen.

## Fehler mit der Grafikkarte

Kann die Grafikkarte nicht verwendet werden, erklärt Wortlaut den Grund und bietet an, auf den Prozessor („cpu“) oder ein kleineres Modell umzustellen. Oft hilft es, den NVIDIA-Grafiktreiber zu aktualisieren (über die NVIDIA App oder `www.nvidia.com/drivers`) und Windows neu zu starten. Danach mit „Prüfen…“ neben „Gerät“ erneut prüfen.

## Die Erkennung erfindet Text oder wiederholt sich

Das passiert vor allem bei langen Pausen, Stille oder Musik. Nehmen Sie das Modell „large-v2“ und stellen Sie die Sprache fest ein, statt „Automatisch erkennen“ zu verwenden.

# uninstall | Deinstallieren

Wortlaut installiert nichts in Windows und ändert keine Systemeinstellungen. Zum Entfernen löschen Sie:

1. den Ordner `%LOCALAPPDATA%\Wortlaut` – darin liegen das Spracherkennungsprogramm, die Modelle und gegebenenfalls die Kopie von Wortlaut. Tippen Sie die Adresse einfach in die Adresszeile des Explorers.
2. die Datei `Wortlaut.exe` und daneben `Wortlaut.settings.json`, falls Sie Wortlaut woanders aufbewahren,
3. die Verknüpfungen „Wortlaut“ auf dem Desktop und im Startmenü, falls vorhanden,
4. falls vorhanden, den Ordner `%APPDATA%\Wortlaut` (dort liegen die Einstellungen, wenn der Ordner von Wortlaut.exe schreibgeschützt war).

Ihre Aufnahmen und die erstellten Texte bleiben dabei unberührt.

# licenses | Lizenzen und Danksagung

Wortlaut ist freie Software unter der **GNU General Public License, Version 3**. Der Quelltext ist öffentlich: `github.com/Schelawski/Wortlaut`.

Wortlaut baut auf der Arbeit anderer auf. Herzlichen Dank an:

- **Faster-Whisper-XXL** von Purfview – das Spracherkennungsprogramm, das Wortlaut steuert (MIT-Lizenz).
- **faster-whisper** von SYSTRAN – die schnelle Umsetzung von Whisper, auf der Faster-Whisper-XXL beruht (MIT-Lizenz).
- **Whisper** von OpenAI – die Spracherkennung und ihre Modelle (MIT-Lizenz).
- **FFmpeg** – liest Video- und Audiodateien, wird mit Faster-Whisper-XXL mitgeliefert (GPL, Version 3).
- **SharpCompress** – entpackt Faster-Whisper-XXL bei der Einrichtung (MIT-Lizenz).

Die Modelle werden von Hugging Face heruntergeladen, das Spracherkennungsprogramm von GitHub.
