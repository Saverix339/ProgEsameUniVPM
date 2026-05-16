namespace ProgEsameUniVPM;
using System.Linq;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("====INIZIO====");
        
    }
}

public static class GameManager
{
    public static IStato StatoGioco;
    public static void Avanza()
    {
        
    }
    public static void CambiaStato(IStato Iniziale, IStato Cambio)
    {
        Iniziale.esci();
        StatoGioco = Cambio;
        StatoGioco.entra();
    }
}

public enum RisultatoAzione //elemento che viene comunicato al Game manager per fare capire se l'input viene riconosciuto o no.
{
    Continua,
    Errore,
    Cambia,
    ComandoSpeciale //Caso speciale se il player inserisce /help o simili
}
public interface IStato
{
    void entra();
    RisultatoAzione agisci(string input);
    void esci();
}

public class EsplorazioneStanza : IStato
{
    public readonly Stanza _stanza;
    public EsplorazioneStanza(Stanza stanza)
    {
        _stanza = stanza;
    }
    public void entra()
    {
        UI.MostraStanza(_stanza);
    }
    public RisultatoAzione agisci(string input)
    {
        if(_stanza.Azioni.Keys.Contains(input.ToLower())){ //Se trova una azione che corrisponde all'input...
            try
            {
                _stanza.Azioni[input.ToLower()].Invoke();
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine($"Azione \"{input}\" non riconosciuta.\n");
                return RisultatoAzione.Errore;
            }
            return RisultatoAzione.Continua;
        }
    }
    public void esci()
    {
        return;
    }
}