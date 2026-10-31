namespace PalestraLog.Models.Backup;

/// <summary>
/// Il file dei dati del backup, espresso nelle entità del modello di destinazione (AD-8).
/// La forma è il contratto di bmad-output/planning-artifacts/formato-backup.md: ogni
/// cambiamento passa prima da lì. Tutte le collezioni sono sempre presenti, anche vuote.
/// </summary>
public sealed class BackupFile
{
    /// <summary>Versione del formato del file, indipendente dallo schema del database.</summary>
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; init; } = CurrentFormatVersion;

    /// <summary>Ora locale del dispositivo, senza fuso, come ogni altra data del file.</summary>
    public DateTime ExportedAt { get; init; }

    public List<ProgramRow> Programs { get; init; } = [];
    public List<RoutineRow> Routines { get; init; } = [];
    public List<SupersetRow> Supersets { get; init; } = [];
    public List<RoutineExerciseRow> RoutineExercises { get; init; } = [];
    public List<ExerciseTargetRow> ExerciseTargets { get; init; } = [];
    public List<SetTargetRow> SetTargets { get; init; } = [];
    public List<ProgressionRuleRow> ProgressionRules { get; init; } = [];
    public List<ExerciseRow> Exercises { get; init; } = [];
    public List<SessionRow> Sessions { get; init; } = [];
    public List<SessionExerciseRow> SessionExercises { get; init; } = [];
    public List<SetEntryRow> SetEntries { get; init; } = [];
    public List<VenueRow> Venues { get; init; } = [];
    public List<MembershipRow> Memberships { get; init; } = [];
}
