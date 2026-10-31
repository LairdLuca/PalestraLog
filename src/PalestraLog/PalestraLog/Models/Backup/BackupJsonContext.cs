using System.Text.Json.Serialization;

namespace PalestraLog.Models.Backup;

/// <summary>
/// Contesto di serializzazione generato dal compilatore (AD-14). In Release su Android il
/// taglio del codice è attivo e la serializzazione per reflection fallisce: ogni tipo che
/// entra nel file deve essere raggiungibile da qui. Il progetto disattiva la reflection
/// anche in Debug, così che un tipo dimenticato fallisca durante lo sviluppo.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    WriteIndented = true)]
[JsonSerializable(typeof(BackupFile))]
public partial class BackupJsonContext : JsonSerializerContext;
