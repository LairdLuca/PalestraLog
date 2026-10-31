using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PalestraLog.Rules;

/// <summary>
/// Identità di riga del backup (AD-7), derivata in modo deterministico: UUID versione 5
/// (RFC 9562 §5.5) sul nome "{entità}/{tabella d'origine}/{chiave locale}[/{posizione}]".
/// Due export dello stesso database producono le stesse identità, e la chiave locale non
/// esce dal file come numero. La regola è il contratto scritto in
/// bmad-output/planning-artifacts/formato-backup.md: cambiarla cambia ogni identità già
/// esportata, quindi richiede una nuova versione di formato.
/// </summary>
public static class BackupUid
{
    /// <summary>Spazio dei nomi fisso del backup della app precedente.</summary>
    public static readonly Guid Namespace = new("ddf767e4-6a5f-4381-a879-a9c294d76163");

    public static Guid Derive(string entity, string sourceTable, int localKey) =>
        FromName(Namespace, string.Create(CultureInfo.InvariantCulture, $"{entity}/{sourceTable}/{localKey}"));

    /// <summary>Per le righe che nascono in più copie dalla stessa riga d'origine (le righe di sequenza).</summary>
    public static Guid Derive(string entity, string sourceTable, int localKey, int position) =>
        FromName(Namespace, string.Create(CultureInfo.InvariantCulture, $"{entity}/{sourceTable}/{localKey}/{position}"));

    public static Guid FromName(Guid nameSpace, string name)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var input = new byte[16 + nameBytes.Length];
        nameSpace.TryWriteBytes(input.AsSpan(0, 16), bigEndian: true, out _);
        nameBytes.CopyTo(input, 16);

        Span<byte> hash = stackalloc byte[20];
        SHA1.HashData(input, hash);

        hash[6] = (byte)((hash[6] & 0x0F) | 0x50); // versione 5
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // variante RFC
        return new Guid(hash[..16], bigEndian: true);
    }
}
