namespace ProgEsameUniVPM;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("====INIZIO====");

        bool caricato = false;
        if (File.Exists("salvataggio.json"))
        {
            if (UI.ChiediCaricamento())
            {
                var salv = JsonSalvataggio.caricaSalvataggio();
                if (salv is not null)
                {
                    GameManager.Giocatore = salv.Giocatore;
                    JsonSalvataggio.ApplicaMondo(salv.Mondo);
                    caricato = true;
                    Console.WriteLine($"Benvenuto/a di nuovo, {GameManager.Giocatore.Nome}!");
                }
                else
                {
                    Console.WriteLine("Impossibile caricare il salvataggio. Verrà avviata una nuova partita.");
                }
            }
        }

        if (!caricato)
        {
            // Avvia la creazione del personaggio tramite lo stato CreazionePersonaggio
            GameManager.CambiaStato(new CreazionePersonaggio());

            // Inizializza la mappa e parti dall'ingresso
            Mappa.Inizializza();
            GameManager.StanzaCorrente = Mappa.Verso(new Coord(0, 0))!;
            GameManager.CambiaStato(new EsplorazioneStanza(GameManager.StanzaCorrente));
        }

        // Game loop
        while (true)
        {
            if (GameManager.StatoGioco is null) break;
            string input = UI.Input(GameManager.Giocatore);
            /*if(string.IsNullOrWhiteSpace(input)) continue;
            if(input is "esci" or "exit" or "quit") break;
            if(input == "salva") { JsonSalvataggio.salva(GameManager.Giocatore); continue; }
            if(input == "carica")
            {
                var salv = JsonSalvataggio.caricaSalvataggio();
                if (salv is not null)
                {
                    GameManager.Giocatore = salv.Giocatore;
                    JsonSalvataggio.ApplicaMondo(salv.Mondo);
                }
                continue;
            }*/
            ControllaComando(input, out var ris);
            if(ris == RisultatoAzione.ComandoSpeciale) break;
            GameManager.StatoGioco.agisci(input);
        }
    }

    //Funzione statica che controlla se l'input è un comando speciale (esci, salva, carica, ecc)
    //Mette in uscita un Risultato azione. Se l'azione è un comando, segnala al game loop di non elaborare l'input-
    //e saltare direttamente a un nuovo input.
    private static void ControllaComando(string input, out RisultatoAzione? risultato)
    {
        if(input is "esci" or "exit" or "quit") Environment.Exit(1);
        if(input == "salva"){JsonSalvataggio.salva(GameManager.Giocatore); risultato = RisultatoAzione.ComandoSpeciale; return;}
        if(input == "carica")
        {
            var salv = JsonSalvataggio.caricaSalvataggio();
            if (salv is not null)
            {
                GameManager.Giocatore = salv.Giocatore;
                JsonSalvataggio.ApplicaMondo(salv.Mondo);
            }
            risultato = RisultatoAzione.ComandoSpeciale;
            return;
        }
        risultato = null;
    }
}

//Enum usato per fare in modo che i vari elementi comunichino tra di loro se l'input è corretto o no.
public enum RisultatoAzione //elemento che viene comunicato al Game manager per fare capire se l'input viene riconosciuto o no.
{
    Continua,
    Errore,
    Cambia,
    ComandoSpeciale //Caso speciale per comandi globali al di fuori della logica di gioco normale.
}

//Interfaccia per gli stati di gioco che compongono la state machine in GameManager. 
//Importante: 'void esci()' viene usato solo per eventuali pulizie necessarie quando lo stato di appartenenza viene tolto,
// ma la logica di cambio stato rimane sempre su GameManager.
public interface IStato
{
    void entra();
    RisultatoAzione agisci(string input);
    void esci();
}

public class CreazionePersonaggio : IStato
{
    public void entra()
    {
        string nome = UI.ChiediNome();
        GameManager.Giocatore =  new Giocatore(nome);

        Armi armaIniziale = UI.ScegliArma();
        GameManager.Giocatore.EquipaggiaArma(armaIniziale);
    }
    public RisultatoAzione agisci(string input)
    {
        return RisultatoAzione.Continua;
    }
    public void esci()
    {
        return;
    }
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

    public void esci()
    {
        GameManager.StatoGioco = contestoCombattimento;
    }
}