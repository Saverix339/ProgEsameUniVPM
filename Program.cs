namespace ProgEsameUniVPM;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddProvider(new FileLoggerProvider("game.log", LogLevel.Debug));
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        var sp = services.BuildServiceProvider();
        Logger.Init(sp.GetRequiredService<ILoggerFactory>());

        var log = Logger.Get<Program>();
        log.LogInformation("====INIZIO GIOCO====");
        Console.WriteLine("====INIZIO====");

        bool caricato = false;
        if (File.Exists("salvataggio.json"))
        {
            log.LogInformation("Trovato file di salvataggio");
            if (UI.ChiediCaricamento())
            {
                var salv = JsonSalvataggio.caricaSalvataggio();
                if (salv is not null)
                {
                    GameManager.Giocatore = salv.Giocatore;
                    JsonSalvataggio.ApplicaMondo(salv.Mondo);
                    caricato = true;
                    log.LogInformation("Salvataggio caricato con successo per {Nome}", GameManager.Giocatore.Nome);
                    Console.WriteLine($"Benvenuto/a di nuovo, {GameManager.Giocatore.Nome}!");
                }
                else
                {
                    log.LogError("Impossibile caricare il salvataggio");
                    Console.WriteLine("Impossibile caricare il salvataggio. Verrà avviata una nuova partita.");
                }
            }
            else
            {
                log.LogInformation("Caricamento rifiutato, avvio nuova partita");
            }
        }

        if (!caricato)
        {
            GameManager.CambiaStato(new CreazionePersonaggio());

            Mappa.Inizializza();
            GameManager.StanzaCorrente = Mappa.Verso(new Coord(0, 0))!;
            log.LogInformation("Nuova partita iniziata. Stanza iniziale: {Stanza}", GameManager.StanzaCorrente.Nome);
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
        if(input is "esci" or "exit" or "quit")
        {
            Logger.Get<Program>().LogInformation("Uscita dal gioco");
            Environment.Exit(1);
        }
        if(input == "salva")
        {
            JsonSalvataggio.salva(GameManager.Giocatore);
            Logger.Get<Program>().LogInformation("Partita salvata");
            risultato = RisultatoAzione.ComandoSpeciale;
            return;
        }
        if(input == "carica")
        {
            var salv = JsonSalvataggio.caricaSalvataggio();
            if (salv is not null)
            {
                GameManager.Giocatore = salv.Giocatore;
                JsonSalvataggio.ApplicaMondo(salv.Mondo);
                Logger.Get<Program>().LogInformation("Partita caricata per {Nome}", GameManager.Giocatore.Nome);
            }
            else
            {
                Logger.Get<Program>().LogWarning("Caricamento fallito");
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
        Logger.Get<CreazionePersonaggio>().LogInformation("Personaggio creato: {Nome}", nome);

        Armi armaIniziale = UI.ScegliArma();
        GameManager.Giocatore.EquipaggiaArma(armaIniziale);
        Logger.Get<CreazionePersonaggio>().LogInformation("Arma iniziale scelta: {Arma}", armaIniziale.Nome);
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
        Logger.Get<EsplorazioneStanza>().LogInformation("Entrato in {Stanza} (Livello {Livello})", _stanza.Nome, _stanza.Livello);
    }
    public RisultatoAzione agisci(string input)
    {
        string id = input.ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(id))
            return RisultatoAzione.Errore;
        if (!_stanza.Azioni.TryGetValue(id, out var azione))
        {
            Logger.Get<EsplorazioneStanza>().LogWarning("Azione non riconosciuta: {Input}", id);
            UI.MostraErrore($"Azione \"{id}\" non riconosciuta.");
            return RisultatoAzione.Errore;
        }
        if (id == "getta" || id == "lascia")
        {
            if (GameManager.Giocatore.Inventario.Count == 0)
            {
                Logger.Get<EsplorazioneStanza>().LogDebug("Tentativo 'getta' con inventario vuoto");
                UI.MostraMessaggio("Il tuo inventario è vuoto.");
            }
            else
            {
                Logger.Get<EsplorazioneStanza>().LogDebug("Oggetto lasciato a terra");
                GameManager.Giocatore.RimuoviOggettoInventario();
            }
            return RisultatoAzione.Continua;
        }
        Logger.Get<EsplorazioneStanza>().LogDebug("Eseguita azione: {Azione}", id);
        azione.Esegui.Invoke();
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
    public bool FlagNemicoSconfitto = false;
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
        Logger.Get<Combattimento>().LogInformation("Combattimento iniziato contro {Nemico} (HP: {HP})", Avversario.Nome, Avversario.Salute);
        UI.EntrataNemico(Avversario);
        agisci("");
    }
    public RisultatoAzione agisci(string input)
    {
        StatusEffect.ProcessaTurno(GameManager.Giocatore.StatusEffects, GameManager.Giocatore);
        StatusEffect.ProcessaTurno(Avversario.statusEffects, Avversario);

        if (FlagNemicoSconfitto)
        {
            GameManager.CambiaStato(contestoCombattimento);
            return RisultatoAzione.Continua;
        }

        if (TurnoCorrente == Turno.Avversario)
        {
            try
            {
                var scelta = Avversario.ScegliAbilita();
                Logger.Get<Combattimento>().LogDebug("Nemico usa: {Abilita}", scelta.Nome);
                scelta.Esegui(Avversario, GameManager.Giocatore);
            }
            catch (Exception ex)
            {
                Logger.Get<Combattimento>().LogError(ex, "Errore durante il turno del nemico");
                UI.MostraErrore("Qualcosa è andato storto!");
            }
            TurnoCorrente = Turno.Giocatore;
            if (GameManager.Giocatore.PuntiVita <= 0)
            {
                Logger.Get<Combattimento>().LogInformation("Giocatore sconfitto da {Nemico}", Avversario.Nome);
                return RisultatoAzione.Continua;
            }
            UI.MostraTurnoGiocatore(GameManager.Giocatore, Avversario);
            string azione = UI.Input(GameManager.Giocatore);
            return agisci(azione);
        }
        else
        {
            string azione = input.ToLowerInvariant().Trim();
            if (string.IsNullOrEmpty(azione))
                return RisultatoAzione.Errore;

            switch (azione)
            {
                case "attacca":
                    Logger.Get<Combattimento>().LogDebug("Giocatore attacca");
                    Giocatore.Attacca(contestoCombattimento, Avversario);
                    break;
                case "abilita":
                    Logger.Get<Combattimento>().LogDebug("Giocatore usa abilità arma");
                    Giocatore.UsaAbilitaArma(contestoCombattimento, Avversario);
                    break;
                case "usa":
                    Logger.Get<Combattimento>().LogDebug("Giocatore usa consumabile");
                    Giocatore.UsaConsumabile(contestoCombattimento, Avversario);
                    break;
                case "scappa":
                    Logger.Get<Combattimento>().LogDebug("Giocatore tenta la fuga");
                    if (UI.ChiediFuga())
                    {
                        Logger.Get<Combattimento>().LogInformation("Fuga riuscita da {Nemico}", Avversario.Nome);
                        GameManager.CambiaStato(contestoCombattimento);
                        return RisultatoAzione.Continua;
                    }
                    Logger.Get<Combattimento>().LogDebug("Fuga fallita");
                    break;
                default:
                    Logger.Get<Combattimento>().LogWarning("Azione combattimento non riconosciuta: {Azione}", azione);
                    UI.MostraErrore($"Azione \"{azione}\" non riconosciuta.");
                    return RisultatoAzione.Errore;
            }

            if (Avversario.Salute <= 0)
            {
                FlagNemicoSconfitto = true;
                int oro = new Random().Next(5, 15);
                GameManager.Giocatore.AggiungiOro(oro);
                Logger.Get<Combattimento>().LogInformation("{Nemico} sconfitto! Oro ottenuto: {Oro}", Avversario.Nome, oro);
                UI.MostraVittoria(Avversario, oro);
                GameManager.CambiaStato(contestoCombattimento);
                return RisultatoAzione.Continua;
            }

            Logger.Get<Combattimento>().LogDebug("Turno passato al nemico (HP giocatore: {HP}, HP nemico: {HPNemico})", GameManager.Giocatore.PuntiVita, Avversario.Salute);
            TurnoCorrente = Turno.Avversario;
            agisci("");
            return RisultatoAzione.Continua;
        }
    }

    public void esci()
    {
        if(FlagNemicoSconfitto == true) contestoCombattimento._stanza.NemicoSconfitto = true;
    }
}

public class IncontroMercante : IStato
{
    public required EsplorazioneStanza Contesto;
    public List<OggettoTrovabile> Vendita => Contesto._stanza.OggettiStanza;
    public void entra()
    {
        if (Vendita.Count == 0)
        {
            Logger.Get<IncontroMercante>().LogDebug("Tavolo mercante vuoto");
            UI.MostraMessaggio("Il tavolo del mercante è vuoto. Non c'è più nulla da comprare.");
            GameManager.CambiaStato(Contesto);
            return;
        }
        Logger.Get<IncontroMercante>().LogInformation("Incontro mercante: {N} oggetti disponibili", Vendita.Count);
        UI.ListaOggettiMercante([.. Vendita.Select(og => og.oggetto)]);
    }
    public RisultatoAzione agisci(string input)
    {
        string azione = input.ToLowerInvariant().Trim();
        if (azione is "esci" or "lascia")
        {
            Logger.Get<IncontroMercante>().LogDebug("Uscita dal mercante");
            GameManager.CambiaStato(Contesto);
            return RisultatoAzione.Continua;
        }
        try
        {
            var carrello = Vendita.Find(x => x.oggetto.Nome.Equals(input, StringComparison.CurrentCultureIgnoreCase));
            GameManager.Giocatore.AggiungiOggettoInventario(carrello?.oggetto ?? throw new ArgumentNullException("Oggetto"));
            Logger.Get<IncontroMercante>().LogInformation("Acquistato: {Oggetto}", carrello.oggetto.Nome);
            Vendita.Remove(carrello);
            if (Vendita.Count == 0)
            {
                Logger.Get<IncontroMercante>().LogDebug("Mercante: merce esaurita, se ne va");
                UI.MostraMessaggio("Il mercante raccoglie le sue cose e se ne va.");
                GameManager.CambiaStato(Contesto);
                return RisultatoAzione.Continua;
            }
            return RisultatoAzione.Continua;
        }
        catch
        {
            Logger.Get<IncontroMercante>().LogWarning("Tentativo acquisto oggetto non valido: {Input}", input);
            UI.MostraErrore("Il mercante non vende quello. Digita 'esci' per lasciare il mercante.");
            return RisultatoAzione.Errore;
        }
    }
    public void esci()
    {
        return;
    }
}