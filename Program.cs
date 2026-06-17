namespace ProgEsameUniVPM;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Punto di ingresso dell'applicazione. Gestisce il game loop principale, il caricamento/salvataggio
/// e la state machine del gioco tramite l'interfaccia <see cref="IStato"/>.
/// </summary>
class Program
{
    /// <summary>
    /// Entry point: inizializza il sistema di logging, carica un eventuale salvataggio
    /// e avvia il game loop principale.
    /// </summary>
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
                    GameManager.Giocatore.RicostruisciAbilitaArma();
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
            GameManager.StanzaCorrente = Mappa.Stanze[Mappa.CoordinateIniziali];
            log.LogInformation("Nuova partita iniziata. Stanza iniziale: {Stanza}", GameManager.StanzaCorrente.Nome);
            GameManager.CambiaStato(new EsplorazioneStanza(GameManager.StanzaCorrente));
        }

        // Game loop
        while (true)
        {
            if (GameManager.StatoGioco is null) break;
            string input = UI.Input(GameManager.Giocatore);
            ControllaComando(input, out var ris);
            if(ris == RisultatoAzione.ComandoSpeciale) break;
            GameManager.StatoGioco.agisci(input);
        }
    }

    /// <summary>
    /// Verifica se l'input dell'utente corrisponde a un comando globale (esci, salva, carica).
    /// Se riconosciuto, esegue il comando e restituisce <see cref="RisultatoAzione.ComandoSpeciale"/>
    /// per segnalare al game loop di non elaborare ulteriormente l'input.
    /// </summary>
    /// <param name="input">Input testuale dell'utente.</param>
    /// <param name="risultato">Tipo di risultato: <see cref="RisultatoAzione.ComandoSpeciale"/> se è stato eseguito un comando globale, <c>null</c> altrimenti.</param>
    private static void ControllaComando(string input, out RisultatoAzione? risultato)
    {
        if(input is "exit" or "quit")
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
                GameManager.Giocatore.RicostruisciAbilitaArma();
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

/// <summary>
/// Tipi di risultato restituiti da <see cref="IStato.agisci"/> per comunicare al GameManager
/// se l'input è stato riconosciuto, se è avvenuto un errore o se lo stato deve cambiare.
/// </summary>
public enum RisultatoAzione
{
    /// <summary>L'azione è stata eseguita con successo.</summary>
    Continua,
    /// <summary>L'input non è stato riconosciuto o non è valido.</summary>
    Errore,
    /// <summary>È richiesto un cambio di stato.</summary>
    Cambia,
    /// <summary>È stato eseguito un comando speciale globale (esci, salva, carica).</summary>
    ComandoSpeciale
}

/// <summary>
/// Interfaccia che definisce un contratto per gli stati della state machine di gioco.
/// Ogni stato implementa tre fasi: ingresso (<see cref="entra"/>), azione (<see cref="agisci"/>)
/// e uscita (<see cref="esci"/>). La logica di transizione è gestita da <see cref="GameManager.CambiaStato"/>.
/// </summary>
public interface IStato
{
    /// <summary>Eseguito quando lo stato diventa attivo. Tipicamente mostra l'interfaccia o inizializza dati.</summary>
    void entra();
    /// <summary>Elabora l'input dell'utente per lo stato corrente.</summary>
    /// <param name="input">Input testuale dell'utente.</param>
    /// <returns>Risultato dell'azione.</returns>
    RisultatoAzione agisci(string input);
    /// <summary>Eseguito quando lo stato viene rimosso. Usato per eventuali operazioni di pulizia.</summary>
    void esci();
}

/// <summary>
/// Stato iniziale del gioco in cui il giocatore sceglie il nome e l'arma di partenza.
/// </summary>
public class CreazionePersonaggio : IStato
{
    /// <summary>
    /// Richiede il nome del personaggio e la scelta dell'arma iniziale,
    /// quindi inizializza il <see cref="GameManager.Giocatore"/>.
    /// </summary>
    public void entra()
    {
        string nome = UI.ChiediNome();
        GameManager.Giocatore =  new Giocatore(nome);
        Logger.Get<CreazionePersonaggio>().LogInformation("Personaggio creato: {Nome}", nome);

        Armi armaIniziale = UI.ScegliArma();
        GameManager.Giocatore.EquipaggiaArma(armaIniziale);
        Logger.Get<CreazionePersonaggio>().LogInformation("Arma iniziale scelta: {Arma}", armaIniziale.Nome);
    }
    /// <summary>
    /// In questo stato l'input non viene elaborato: la creazione è gestita interamente in <see cref="entra"/>.
    /// </summary>
    public RisultatoAzione agisci(string input)
    {
        return RisultatoAzione.Continua;
    }
    /// <summary>
    /// Nessuna operazione di pulizia necessaria.
    /// </summary>
    public void esci()
    {
        return;
    }
}

/// <summary>
/// Stato di esplorazione di una stanza. Mostra la descrizione della stanza e le azioni disponibili,
/// e smista l'input dell'utente alle azioni configurate.
/// </summary>
public class EsplorazioneStanza : IStato
{
    /// <summary>La stanza attualmente esplorata.</summary>
    public readonly Stanza _stanza;
    /// <summary>
    /// Crea lo stato di esplorazione per la stanza specificata.
    /// </summary>
    /// <param name="stanza">La stanza da esplorare.</param>
    public EsplorazioneStanza(Stanza stanza)
    {
        _stanza = stanza;
    }
    /// <summary>
    /// Mostra la descrizione della stanza e le azioni disponibili.
    /// </summary>
    public void entra()
    {
        UI.MostraStanza(_stanza);
        Logger.Get<EsplorazioneStanza>().LogInformation("Entrato in {Stanza} (Livello {Livello})", _stanza.Nome, _stanza.Livello);
    }
    /// <summary>
    /// Elabora l'input dell'utente cercando un'azione corrispondente tra quelle disponibili nella stanza.
    /// Gestisce anche i comandi "getta"/"lascia" per rimuovere oggetti dall'inventario.
    /// </summary>
    /// <param name="input">Input testuale dell'utente.</param>
    /// <returns><see cref="RisultatoAzione.Continua"/> se l'azione è stata eseguita, <see cref="RisultatoAzione.Errore"/> altrimenti.</returns>
    public RisultatoAzione agisci(string input)
    {
        string id = input.ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(id))
            return RisultatoAzione.Errore;
        
        if (id == "getta" || id == "lascia" || id == "drop")
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
        if (id == "inventario" || id == "guarda inventario" || id == "guarda")
        {
            if (GameManager.Giocatore.Inventario.Count == 0)
            {
                UI.MostraMessaggio("Il tuo inventario è vuoto.");
            }
            else
            {
                var ultimo = GameManager.Giocatore.Inventario.Peek();
                UI.MostraMessaggio($"Ultimo oggetto: {ultimo.Nome} - {ultimo.Descrizione}");
            }
            return RisultatoAzione.Continua;
        }
        if (!_stanza.Azioni.TryGetValue(id, out var azione))
        {
            Logger.Get<EsplorazioneStanza>().LogWarning("Azione non riconosciuta: {Input}", id);
            UI.MostraErrore($"Azione \"{id}\" non riconosciuta.");
            return RisultatoAzione.Errore;
        }
        Logger.Get<EsplorazioneStanza>().LogDebug("Eseguita azione: {Azione}", id);
        azione.Esegui.Invoke();
        return RisultatoAzione.Continua;
    }
    /// <summary>
    /// Nessuna operazione di pulizia necessaria.
    /// </summary>
    public void esci()
    {
        return;
    }
}

/// <summary>
/// Stato di combattimento a turni contro un <see cref="Nemico"/>.
/// Alterna i turni tra giocatore e avversario, gestendo attacchi, abilità, consumabili e fuga.
/// </summary>
public class Combattimento : IStato
{
    /// <summary>Riferimento allo stato di esplorazione da ripristinare al termine del combattimento.</summary>
    public EsplorazioneStanza contestoCombattimento;
    /// <summary>Il nemico che il giocatore sta affrontando.</summary>
    public Nemico Avversario {get; private set;}
    /// <summary>Indica se il nemico è stato sconfitto nel combattimento corrente.</summary>
    public bool FlagNemicoSconfitto = false;

    /// <summary>Turni del combattimento.</summary>
    public enum Turno
    {
        /// <summary>Turno del giocatore.</summary>
        Giocatore,
        /// <summary>Turno dell'avversario.</summary>
        Avversario
    }
    /// <summary>Turno attualmente in corso.</summary>
    Turno TurnoCorrente;

    /// <summary>
    /// Inizializza il combattimento contro il nemico specificato.
    /// </summary>
    /// <param name="Contesto">Stato di esplorazione da ripristinare al termine.</param>
    /// <param name="nemico">Il nemico da affrontare.</param>
    /// <param name="TurnoNemico">Se <c>true</c>, il nemico inizia per primo (es. attacco a sorpresa).</param>
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
    /// <summary>
    /// Avvia il combattimento mostrando l'entrata del nemico e processando il primo turno.
    /// </summary>
    public void entra()
    {
        Logger.Get<Combattimento>().LogInformation("Combattimento iniziato contro {Nemico} (HP: {HP})", Avversario.Nome, Avversario.Salute);
        UI.EntrataNemico(Avversario);
        UI.MostraTurnoGiocatore(GameManager.Giocatore, Avversario);
        agisci("");
    }
    /// <summary>
    /// Elabora un turno di combattimento. Processa gli status effect attivi,
    /// poi esegue l'azione appropriata in base al turno corrente (giocatore o nemico).
    /// </summary>
    /// <param name="input">Azione scelta dal giocatore (attacca, abilita, usa, scappa).</param>
    /// <returns>Risultato dell'azione.</returns>
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
                var scelta = Avversario.ScegliAbilita(GameManager.Giocatore);
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
                    if (!Giocatore.UsaAbilitaArma(contestoCombattimento, Avversario))
                        return RisultatoAzione.Errore;
                    break;
                case "usa":
                    Logger.Get<Combattimento>().LogDebug("Giocatore usa consumabile");
                    if (!Giocatore.UsaConsumabile(contestoCombattimento, Avversario))
                        return RisultatoAzione.Errore;
                    break;
                case "scappa":
                    Logger.Get<Combattimento>().LogDebug("Giocatore tenta la fuga");
                    if (UI.ChiediFuga())
                    {
                        Logger.Get<Combattimento>().LogInformation("Fuga riuscita da {Nemico}", Avversario.Nome);
                        if (GameManager.UltimaDirezione.HasValue)
                            GameManager.Sposta(GameManager.UltimaDirezione.Value.Opposta());
                        else
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

    /// <summary>
    /// All'uscita dal combattimento, se il nemico è stato sconfitto imposta il flag
    /// <see cref="Stanza.NemicoSconfitto"/> sulla stanza del contesto.
    /// </summary>
    public void esci()
    {
        if(FlagNemicoSconfitto == true) {
            contestoCombattimento._stanza.NemicoSconfitto = true;
            if (Avversario.Nome.Equals("Signore del Dungeon"))
            {
                UI.MostraMessaggio("Hai vinto!");
                Logger.Get<Giocatore>().LogInformation("Il giocatore {Nome} ha vinto.", GameManager.Giocatore.Nome);
            }
        }
    }
}

/// <summary>
/// Stato di interazione con il mercante. Permette al giocatore di acquistare gli oggetti
/// esposti sul tavolo del mercante digitandone il nome.
/// </summary>
public class IncontroMercante : IStato
{
    /// <summary>Contesto di esplorazione da ripristinare all'uscita dal mercante.</summary>
    public required EsplorazioneStanza Contesto;
    /// <summary>Lista degli oggetti attualmente in vendita (alias di <see cref="Contesto._stanza.OggettiStanza"/>).</summary>
    public List<OggettoTrovabile> Vendita => Contesto._stanza.OggettiStanza;
    /// <summary>
    /// Mostra la lista degli oggetti in vendita. Se il tavolo è vuoto, torna immediatamente all'esplorazione.
    /// </summary>
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
    /// <summary>
    /// Elabora l'input per acquistare un oggetto (digitandone il nome) o uscire ("esci"/"lascia").
    /// </summary>
    /// <param name="input">Nome dell'oggetto da acquistare o "esci"/"lascia" per andarsene.</param>
    /// <returns>Risultato dell'azione.</returns>
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
            if (carrello is null) throw new ArgumentNullException("Oggetto");
            if (carrello.oggetto is OggettoChiave chiave)
            {
                GameManager.Giocatore.DaiChiave(chiave.Serratura);
                UI.MostraMessaggio($"Hai acquistato: {chiave.Nome}. Aggiunta alle tue chiavi.");
                Logger.Get<IncontroMercante>().LogInformation("Acquistata chiave: {Chiave} (serratura: {Serratura})", chiave.Nome, chiave.Serratura);
            }
            else
            {
                if (!GameManager.Giocatore.AggiungiOggettoInventario(carrello.oggetto))
                    return RisultatoAzione.Errore;
                UI.MostraMessaggio($"Hai acquistato: {carrello.oggetto.Nome}.");
                Logger.Get<IncontroMercante>().LogInformation("Acquistato: {Oggetto}", carrello.oggetto.Nome);
            }
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
    /// <summary>
    /// Nessuna operazione di pulizia necessaria.
    /// </summary>
    public void esci()
    {
        return;
    }
}
