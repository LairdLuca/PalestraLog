namespace PalestraLog.Models.Backup;

// Nel file si scrive il nome, non il valore numerico: il nome è il contratto.
public enum SetType
{
    Warmup = 1,
    Working = 2,
    BackOff = 3
}
