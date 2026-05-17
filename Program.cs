namespace ProgEsameUniVPM;
using System.Linq;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("====INIZIO====");

        // Chiedi il nome e crea il giocatore
        string nome = UI.ChiediNome();
        GameManager.Giocatore = new Giocatore(nome);
        
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
        try
        {
            _stanza.Azioni[input.ToLowerInvariant()].Invoke();
        }
        catch (KeyNotFoundException) //Se non trova l'input
        {
            UI.MostraErrore($"Azione \"{input}\" non riconosciuta.\n");
            return RisultatoAzione.Errore;
        }
        return RisultatoAzione.Continua;
    
    }
    public void esci()
    {
        return;
    }
}