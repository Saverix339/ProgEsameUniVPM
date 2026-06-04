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

public class Combattimento : IStato
{
    public EsplorazioneStanza contestoCombattimento;
    public Nemico Avversario {get; private set;} 
    public enum Turno
    {
        Giocatore,
        Avversario
    }
    Turno TurnoCorrente; 
    public Combattimento(EsplorazioneStanza Contesto, Nemico nemico, bool TurnoNemico = false)
    {
        contestoCombattimento = Contesto;
        Avversario = nemico;
        if (TurnoNemico)
        {
            TurnoCorrente = Turno.Avversario;
        }
        else
        {
            TurnoCorrente = Turno.Giocatore;
        }
    }
    public void entra() 
    {
        UI.EntrataNemico(Avversario);
        agisci("");
    }
    public RisultatoAzione agisci(string input)
    {
        if(TurnoCorrente == Turno.Avversario)
        {
            try{
                var scelta = Avversario.ScegliAbilita();
                scelta.Esegui(Avversario, GameManager.Giocatore);
                TurnoCorrente = Turno.Giocatore;
                agisci(UI.Input(GameManager.Giocatore));
                return RisultatoAzione.Continua;
            }
            catch (Exception)
            {
                return RisultatoAzione.Errore;
            }
        }
        else
        {
            //TODO Codice per input giocatore
            return RisultatoAzione.Errore;
        }
    }
    public void AzioneNemico()
    {
        
    }
}