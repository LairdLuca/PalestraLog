using System.Text.Json;
using PalestraLog.Models;
using PalestraLog.Models.Backup;
using PalestraLog.Models.Enums;
using PalestraLog.Rules;
using PalestraLog.Services.Interfaces;

namespace PalestraLog.Services;

/// <summary>
/// Produce il file dei dati del backup nel formato del modello di destinazione (AD-8).
/// È la app precedente a tradurre in uscita: le regole di traduzione sono scritte in
/// bmad-output/planning-artifacts/formato-backup.md, che è la fonte dell'import.
/// Legge soltanto: nessuna riga del database viene scritta.
/// </summary>
public class BackupService(IDatabaseService db) : IBackupService
{
    // Nomi delle tabelle d'origine: entrano nella derivazione delle identità e non vanno cambiati.
    private const string TabSchede = "Schede";
    private const string TabGiorni = "Giorni";
    private const string TabGruppi = "GruppiEsercizi";
    private const string TabEserciziGiorno = "EserciziGiorno";
    private const string TabEsercizi = "Esercizi";
    private const string TabSessioni = "Sessioni";
    private const string TabEserciziSessione = "EserciziSessione";
    private const string TabSetRecords = "SetRecords";

    public async Task<BackupFile> ComposeAsync(CancellationToken cancellationToken = default)
    {
        await db.InitializeAsync();
        cancellationToken.ThrowIfCancellationRequested();

        // Una sola transazione: le otto letture vedono lo stesso stato del database anche se
        // una sessione in corso registra un set nel frattempo.
        Origine origine = null!;
        await db.Database.RunInTransactionAsync(conn =>
        {
            origine = new Origine(
                conn.Table<Scheda>().OrderBy(x => x.Id).ToList(),
                conn.Table<Giorno>().OrderBy(x => x.Id).ToList(),
                conn.Table<GruppoEsercizi>().OrderBy(x => x.Id).ToList(),
                conn.Table<EsercizioGiorno>().OrderBy(x => x.Id).ToList(),
                conn.Table<Esercizio>().OrderBy(x => x.Id).ToList(),
                conn.Table<Sessione>().OrderBy(x => x.Id).ToList(),
                conn.Table<EsercizioSessione>().OrderBy(x => x.Id).ToList(),
                conn.Table<SetRecord>().OrderBy(x => x.Id).ToList());
        });

        cancellationToken.ThrowIfCancellationRequested();
        return Traduci(origine, DateTime.Now);
    }

    public async Task WriteDataFileAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        var file = await ComposeAsync(cancellationToken);
        await JsonSerializer.SerializeAsync(destination, file, BackupJsonContext.Default.BackupFile, cancellationToken);
        await destination.FlushAsync(cancellationToken);
    }

    public async Task WriteSnapshotAsync(string destinationPath)
    {
        await db.InitializeAsync();

        // Il motore, non una copia dei byte: copiare il file a metà di una scrittura darebbe un
        // database incoerente, qualunque sia il journal. VACUUM INTO legge in una transazione e
        // scrive un database nuovo e autosufficiente; la connessione dell'app è serializzata,
        // quindi non si sovrappone a un set che si sta registrando. Il database non viene scritto.
        await db.Database.ExecuteAsync("VACUUM INTO ?", destinationPath);
    }

    private sealed record Origine(
        List<Scheda> Schede,
        List<Giorno> Giorni,
        List<GruppoEsercizi> Gruppi,
        List<EsercizioGiorno> EserciziGiorno,
        List<Esercizio> Esercizi,
        List<Sessione> Sessioni,
        List<EsercizioSessione> EserciziSessione,
        List<SetRecord> SetRecords);

    private static BackupFile Traduci(Origine o, DateTime adesso)
    {
        var gruppi = o.Gruppi.ToDictionary(g => g.Id);

        // Gli esercizi temporanei non entrano nella routine (FR-33).
        var programmati = o.EserciziGiorno.Where(eg => !eg.IsTemporaneo).ToList();

        return new BackupFile
        {
            ExportedAt = OraLocale(adesso),

            Programs = o.Schede.Select(s => new ProgramRow
            {
                Uid = BackupUid.Derive("Program", TabSchede, s.Id),
                Name = s.Nome,
                Description = s.Descrizione,
                IsActive = s.IsAttiva,
                Position = s.Ordine,
                CreatedAt = OraLocale(s.DataCreazione)
            }).ToList(),

            Routines = o.Giorni.Select(g => new RoutineRow
            {
                Uid = BackupUid.Derive("Routine", TabGiorni, g.Id),
                ProgramUid = BackupUid.Derive("Program", TabSchede, g.SchedaId),
                Name = g.Nome,
                Position = g.Ordine
            }).ToList(),

            Supersets = o.Gruppi.Select(g => new SupersetRow
            {
                Uid = BackupUid.Derive("Superset", TabGruppi, g.Id),
                RoutineUid = BackupUid.Derive("Routine", TabGiorni, g.GiornoId),
                Name = g.Nome,
                Position = g.Ordine,
                IsOffPlan = g.IsTemporaneo
            }).ToList(),

            RoutineExercises = programmati.Select(eg => new RoutineExerciseRow
            {
                Uid = BackupUid.Derive("RoutineExercise", TabEserciziGiorno, eg.Id),
                RoutineUid = BackupUid.Derive("Routine", TabGiorni, eg.GiornoId),
                ExerciseUid = BackupUid.Derive("Exercise", TabEsercizi, eg.EsercizioId),
                // Solo i gruppi del programma: un gruppo fuori piano vive nello storico, non nella routine.
                SupersetUid = eg.GruppoId is int gid && gruppi.TryGetValue(gid, out var gruppo) && !gruppo.IsTemporaneo
                    ? BackupUid.Derive("Superset", TabGruppi, gid)
                    : null,
                Position = eg.Ordine,
                Notes = eg.Note
            }).ToList(),

            ExerciseTargets = programmati.Select(eg => new ExerciseTargetRow
            {
                Uid = BackupUid.Derive("ExerciseTarget", TabEserciziGiorno, eg.Id),
                RoutineExerciseUid = BackupUid.Derive("RoutineExercise", TabEserciziGiorno, eg.Id),
                FromExecution = 1
            }).ToList(),

            SetTargets = programmati.SelectMany(TraduciSequenza).ToList(),

            ProgressionRules = [],

            Exercises = o.Esercizi.Select(e => new ExerciseRow
            {
                Uid = BackupUid.Derive("Exercise", TabEsercizi, e.Id),
                Name = e.Nome,
                Type = TraduciTipo(e),
                Description = e.Descrizione,
                WeightStep = null,
                RepsStep = null,
                DurationStepSeconds = null,
                NotRecorded =
                [
                    Campo(nameof(ExerciseRow.WeightStep)),
                    Campo(nameof(ExerciseRow.RepsStep)),
                    Campo(nameof(ExerciseRow.DurationStepSeconds))
                ]
            }).ToList(),

            Sessions = o.Sessioni.Select(s => new SessionRow
            {
                Uid = BackupUid.Derive("Session", TabSessioni, s.Id),
                RoutineUid = BackupUid.Derive("Routine", TabGiorni, s.GiornoId),
                RoutineName = null,
                VenueUid = null,
                StartedAt = OraLocale(s.DataInizio),
                EndedAt = s.DataFine is DateTime fine ? OraLocale(fine) : null,
                // La durata si scrive solo insieme alla data di fine: prima vale zero, che non è un dato.
                DurationSeconds = s.DataFine is null ? null : s.DurataTotaleSecondi,
                IsIncomplete = s.DataFine is null,
                Notes = s.Note,
                NotRecorded =
                [
                    Campo(nameof(SessionRow.RoutineName)),
                    Campo(nameof(SessionRow.VenueUid)),
                    FattoPiano
                ]
            }).ToList(),

            SessionExercises = o.EserciziSessione.Select(es => new SessionExerciseRow
            {
                Uid = BackupUid.Derive("SessionExercise", TabEserciziSessione, es.Id),
                SessionUid = BackupUid.Derive("Session", TabSessioni, es.SessioneId),
                ExerciseUid = BackupUid.Derive("Exercise", TabEsercizi, es.EsercizioId),
                // Il legame registrato, anche verso un esercizio temporaneo che non entra nel file.
                RoutineExerciseUid = es.EserciziGiornoId > 0
                    ? BackupUid.Derive("RoutineExercise", TabEserciziGiorno, es.EserciziGiornoId)
                    : null,
                SupersetUid = es.GruppoEserciziId is int gid && gid > 0
                    ? BackupUid.Derive("Superset", TabGruppi, gid)
                    : null,
                Position = es.Ordine,
                // Le righe nascono alla prima serie: una riga senza data di fine è un esercizio iniziato.
                Status = es.DataFine is null ? SessionExerciseStatus.InProgress : SessionExerciseStatus.Done,
                // Il contrassegno temporaneo sta sull'esercizio del giorno e si modifica dal form:
                // il suo valore di oggi non dice se l'esecuzione di allora era fuori piano.
                IsOffPlan = null,
                StartedAt = OraLocale(es.DataInizio),
                EndedAt = es.DataFine is DateTime fine ? OraLocale(fine) : null,
                DurationSeconds = es.DataFine is null ? null : es.TempoTotaleSecondi,
                // Zero è l'assenza di una valutazione, non una fatica.
                Effort = es.Fatica > 0 ? es.Fatica : null,
                EffortNotes = es.NoteFatica,
                ExerciseName = null,
                ExecutionNumber = null,
                NotRecorded =
                [
                    Campo(nameof(SessionExerciseRow.IsOffPlan)),
                    Campo(nameof(SessionExerciseRow.ExerciseName)),
                    Campo(nameof(SessionExerciseRow.ExecutionNumber)),
                    FattoObiettivi
                ]
            }).ToList(),

            SetEntries = o.SetRecords.Select(sr => new SetEntryRow
            {
                Uid = BackupUid.Derive("SetEntry", TabSetRecords, sr.Id),
                SessionExerciseUid = BackupUid.Derive("SessionExercise", TabEserciziSessione, sr.EsercizioSessioneId),
                SetTargetUid = null,
                // La app precedente conosceva solo il riscaldamento: ogni altro set contava come set di lavoro.
                SetType = sr.IsRiscaldamento ? SetType.Warmup : SetType.Working,
                Weight = sr.Peso,
                Reps = sr.Ripetizioni,
                // DurataCardioSecondi porta sia la durata cardio sia la tenuta del corpo libero:
                // nel modello di destinazione sono la stessa grandezza.
                DurationSeconds = sr.DurataCardioSecondi,
                Speed = sr.Velocita,
                Incline = sr.Inclinazione,
                Distance = sr.Distanza,
                SincePreviousSetSeconds = sr.DurataSecondi,
                RecordedAt = OraLocale(sr.Timestamp),
                Notes = sr.Note,
                NotRecorded = [Campo(nameof(SetEntryRow.SetTargetUid))]
            }).ToList(),

            Venues = [],
            Memberships = []
        };
    }

    /// <summary>
    /// Una riga di lavoro per ogni serie prevista. Senza numero di serie ma con valori, una
    /// riga sola: è l'unico modo di non perdere i valori senza inventare un conteggio.
    /// </summary>
    private static IEnumerable<SetTargetRow> TraduciSequenza(EsercizioGiorno eg)
    {
        var haValori = eg.RipetizioniTarget is not null || eg.PesoTarget is not null
                    || eg.DurataTargetSecondi is not null || eg.VelocitaTarget is not null
                    || eg.InclinazioneTarget is not null || eg.DistanzaTarget is not null;
        var righe = eg.SerieTarget is > 0 ? eg.SerieTarget.Value : haValori ? 1 : 0;

        for (var posizione = 1; posizione <= righe; posizione++)
        {
            yield return new SetTargetRow
            {
                Uid = BackupUid.Derive("SetTarget", TabEserciziGiorno, eg.Id, posizione),
                ExerciseTargetUid = BackupUid.Derive("ExerciseTarget", TabEserciziGiorno, eg.Id),
                Position = posizione,
                SetType = SetType.Working,
                Reps = eg.RipetizioniTarget,
                Weight = eg.PesoTarget,
                DurationSeconds = eg.DurataTargetSecondi,
                RestSeconds = null,
                Speed = eg.VelocitaTarget,
                Incline = eg.InclinazioneTarget,
                Distance = eg.DistanzaTarget,
                NotRecorded = [Campo(nameof(SetTargetRow.RestSeconds))]
            };
        }
    }

    private static ExerciseType TraduciTipo(Esercizio e) => e.TipoEsercizio switch
    {
        TipoEsercizio.Pesi => ExerciseType.Weights,
        TipoEsercizio.CorpoLibero => ExerciseType.Bodyweight,
        TipoEsercizio.Cardio => ExerciseType.Cardio,
        // Meglio nessun backup che un tipo inventato: l'export fallisce dichiarando la riga.
        _ => throw new InvalidOperationException($"Esercizio {e.Id}: tipo {e.Tipo} sconosciuto, impossibile tradurlo.")
    };

    // Fatti della app precedente che non corrispondono a un singolo campo della riga.
    private const string FattoPiano = "plan";
    private const string FattoObiettivi = "targets";

    /// <summary>Il nome di un campo così come compare nel file.</summary>
    private static string Campo(string proprieta) => JsonNamingPolicy.CamelCase.ConvertName(proprieta);

    /// <summary>
    /// Le date della app precedente sono l'ora locale del dispositivo, senza fuso: si
    /// scrivono come tali, senza attribuire un fuso che nessuno ha registrato.
    /// </summary>
    private static DateTime OraLocale(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Unspecified);
}
