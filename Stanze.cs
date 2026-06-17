using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.Marshalling;
using ProgEsameUniVPM;
using Microsoft.Extensions.Logging;

/// <summary>
/// Rappresenta una coordinata 2D sulla griglia della mappa (X, Y).
/// Supporta l'addizione tramite operatore +.
/// </summary>
public readonly record struct Coord(int X, int Y)
{
    /// <summary>Somma componente per componente di due coordinate.</summary>
    public static Coord operator +(Coord a, Coord b) => new(a.X + b.X, a.Y + b.Y);
}

/// <summary>Direzioni cardinali per la navigazione tra le stanze.</summary>
public enum Direzione { Nord, Sud, Est, Ovest }


/// <summary>
/// Metodi di estensione per la gestione delle direzioni: conversione in delta di coordinate,
/// direzione opposta e rappresentazione testuale.
/// </summary>
public static class ManagerDirezioni
{
    /// <summary>Mappa ogni direzione al corrispondente spostamento (delta) sulla griglia.</summary>
    private static readonly Dictionary<Direzione, Coord> DeltaPosizione = new()
    {
        [Direzione.Nord]  = new Coord(0, 1),
        [Direzione.Sud]   = new Coord(0, -1),
        [Direzione.Est]   = new Coord(1, 0),
        [Direzione.Ovest] = new Coord(-1, 0),
    };

    /// <summary>Converte una direzione nel delta di coordinate corrispondente.</summary>
    /// <param name="d">La direzione.</param>
    /// <returns>Il delta (es. Nord = (0,1)).</returns>
    public static Coord ToDelta(this Direzione d) => DeltaPosizione[d];

    /// <summary>Restituisce la direzione opposta.</summary>
    /// <param name="d">La direzione di partenza.</param>
    /// <returns>La direzione opposta (es. Nord -> Sud).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Se la direzione non è valida.</exception>
    public static Direzione Opposta(this Direzione d) => d switch
    {
        Direzione.Nord  => Direzione.Sud,
        Direzione.Sud   => Direzione.Nord,
        Direzione.Est   => Direzione.Ovest,
        Direzione.Ovest => Direzione.Est,
        _ => throw new ArgumentOutOfRangeException(nameof(d))
    };

    /// <summary>Restituisce la direzione in minuscolo come stringa (es. "nord").</summary>
    public static string Testo(this Direzione d) => d.ToString().ToLowerInvariant();
}

/// <summary>
/// Stato delle porte che viene salvato nel file di salvataggio.
/// </summary>
public enum StatoPorta { Aperta, Chiusa, Bloccata }

/// <summary>
/// Classe per istanziare porte tra le stanze,
/// possono richiedere una chiave per essere aperte, usano l'ID della chiave.
/// </summary>
public class Porta
{
    /// <summary>Stato attuale della porta.</summary>
    public StatoPorta Stato { get; set; } = StatoPorta.Aperta;
    /// <summary>ID della chiave richiesta per aprire, o <c>null</c> se non serve.</summary>
    public string? ChiaveRichiesta { get; set; }

    /// <summary>Indica se la porta richiede una chiave per essere aperta.</summary>
    public bool RichiedeChiave => ChiaveRichiesta is not null;
}

/// <summary>
/// Rappresenta un'azione eseguibile dal giocatore all'interno di una stanza.
/// </summary>
public class Azione
{
    /// <summary>Identificatore univoco dell'azione (usato come comando testuale).</summary>
    public required string Id { get; init; }
    /// <summary>Nome descrittivo dell'azione.</summary>
    public required string Nome { get; init; }
    /// <summary>Descrizione dettagliata dell'azione. Modificabile per aggiornamenti a runtime (es. stanza curativa esaurita).</summary>
    public required string Descrizione { get; set; }

    /// <summary>Callback eseguito quando l'azione viene attivata.</summary>
    public Action Esegui { get; init; } = () => { };

    /// <summary>
    /// Crea una nuova azione con i parametri specificati.
    /// </summary>
    /// <param name="id">Identificatore/comando dell'azione.</param>
    /// <param name="nome">Nome descrittivo.</param>
    /// <param name="descrizione">Descrizione testuale.</param>
    /// <param name="callback">Callback da eseguire.</param>
    /// <returns>Una nuova istanza di <see cref="Azione"/>.</returns>
    public static Azione Crea(string id, string nome, string descrizione, Action callback)
    {
        return new Azione
        {
            Id = id,
            Nome = nome,
            Descrizione = descrizione,
            Esegui = callback
        };
    }
}

/// <summary>
/// Rappresenta una stanza del dungeon. Contiene coordinate, oggetti, azioni disponibili,
/// porte verso altre stanze e opzionalmente un nemico o un incontro col mercante.
/// </summary>
public class Stanza
{
    /// <summary>Identificatore univoco della stanza.</summary>
    public required string Id { get; init; }
    /// <summary>Nome della stanza.</summary>
    public required string Nome { get; init; }
    /// <summary>Descrizione testuale mostrata al giocatore.</summary>
    public required string Descrizione { get; init; }

    /// <summary>Coordinate della stanza sulla griglia della mappa.</summary>
    public required Coord Coordinate { get; init; }

    /// <summary>Lista degli oggetti presenti a terra nella stanza.</summary>
    public List<OggettoTrovabile> OggettiStanza { get; } = new();

    /// <summary>
    /// Livello del dungeon (per sviluppi futuri). Al momento solo livello 0.
    /// </summary>
    public required int Livello { get; init; }

    /// <summary>Dizionario delle azioni disponibili nella stanza (chiave = ID azione).</summary>
    public Dictionary<string, Azione> Azioni { get; } = new();

    /// <summary>Dizionario delle porte che collegano la stanza ad altre stanze.</summary>
    public Dictionary<Direzione, Porta> Porte { get; } = new();

    /// <summary>Indica se è la prima volta che il giocatore visita questa stanza.</summary>
    public bool PrimaVolta { get; set; } = true;

    /// <summary>Nemico presente nella stanza, o <c>null</c> se la stanza è sicura.</summary>
    public Nemico? NemicoStanza;
    /// <summary>Se <c>true</c>, la stanza ospita un incontro col mercante.</summary>
    public bool IncontroMercante = false;

    /// <summary>Indica se il nemico della stanza è già stato sconfitto.</summary>
    public bool NemicoSconfitto = false;

    /// <summary>Indica se l'oro della stanza è già stato raccolto dal giocatore (persiste nei salvataggi).</summary>
    public bool OroRaccolto = false;

    /// <summary>Indica se la stanza curativa è già stata usata (persiste nei salvataggi).</summary>
    public bool CurativaUsata = false;

    /// <summary>
    /// Aggiunge un'azione alla stanza.
    /// </summary>
    /// <param name="id">Identificatore/comando dell'azione.</param>
    /// <param name="nome">Nome descrittivo.</param>
    /// <param name="descrizione">Descrizione testuale.</param>
    /// <param name="callback">Callback da eseguire all'attivazione.</param>
    public void AggiungiAzione(string id, string nome, string descrizione, Action callback)
    {
        Azioni[id] = Azione.Crea(id, nome, descrizione, callback);
    }

    /// <summary>
    /// Rimuove un'azione dalla stanza.
    /// </summary>
    /// <param name="id">Identificatore dell'azione da rimuovere.</param>
    /// <returns><c>true</c> se l'azione è stata trovata e rimossa.</returns>
    public bool RimuoviAzione(string id)
    {
        return Azioni.Remove(id);
    }

    /// <summary>
    /// Raccoglie un oggetto dalla stanza dato il suo GUID.
    /// </summary>
    /// <param name="id">GUID dell'oggetto da raccogliere.</param>
    /// <param name="oggetto">L'oggetto raccolto, o <c>null</c> se non trovato.</param>
    /// <returns><c>true</c> se l'oggetto è stato trovato e rimosso.</returns>
    public bool RaccogliOggetto(Guid id, out Oggetto? oggetto)
    {
        var trovato = OggettiStanza.FirstOrDefault(o => o.oggetto.Id == id);
        if (trovato is null) { oggetto = null; return false; }
        oggetto = trovato.oggetto;
        OggettiStanza.Remove(trovato);
        return true;
    }

    /// <summary>
    /// Aggiunge un oggetto a <see cref="OggettiStanza"/> e crea automaticamente un'azione "raccogli"
    /// identificata dall'<see cref="Oggetto.IdSalvataggio"/>. Se l'oggetto viene raccolto,
    /// l'azione si auto-rimuove.
    /// </summary>
    /// <param name="o">Oggetto da registrare. Deve avere <see cref="Oggetto.IdSalvataggio"/> non vuoto.</param>
    public void AggiungiOggettoRaccoglibile(Oggetto o)
    {
        if (string.IsNullOrEmpty(o.IdSalvataggio))
            throw new ArgumentException("L'oggetto deve avere un IdSalvataggio per essere registrato.", nameof(o));
        OggettiStanza.Add(new OggettoTrovabile { oggetto = o });
        CreaAzioneRaccogli(o);
    }

    /// <summary>
    /// Crea l'azione "raccogli" per un oggetto già presente in <see cref="OggettiStanza"/>.
    /// </summary>
    private void CreaAzioneRaccogli(Oggetto o)
    {
        string idAzione = $"raccogli {o.IdSalvataggio}";
        if (Azioni.ContainsKey(idAzione)) return;
        string idSalvataggio = o.IdSalvataggio;
        AggiungiAzione(
            idAzione,
            $"Raccogli {o.Nome}",
            $"Raccogli {o.Nome} da terra.",
            () =>
            {
                var trovato = OggettiStanza.FirstOrDefault(ot => ot.oggetto.IdSalvataggio == idSalvataggio);
                if (trovato is null) { UI.MostraMessaggio("Non c'è nulla da raccogliere qui."); return; }
                var oggetto = trovato.oggetto;
                if (oggetto is Armi arma)
                {
                    GameManager.Giocatore.EquipaggiaArma(arma);
                    UI.MostraMessaggio($"Hai equipaggiato: {arma.Nome}.");
                }
                else if (oggetto is OggettoChiave chiave)
                {
                    GameManager.Giocatore.DaiChiave(chiave.Serratura);
                    UI.MostraMessaggio($"Hai raccolto: {chiave.Nome} (apre serrature {chiave.Serratura}).");
                }
                else
                {
                    if (!GameManager.Giocatore.AggiungiOggettoInventario(oggetto)) return;
                    UI.MostraMessaggio($"Hai raccolto: {oggetto.Nome}.");
                }
                OggettiStanza.Remove(trovato);
                RimuoviAzione(idAzione);
            }
        );
    }

    /// <summary>
    /// Rigenera tutte le azioni "raccogli" in base agli oggetti attualmente presenti in <see cref="OggettiStanza"/>.
    /// Chiamato dopo il caricamento di un salvataggio per sincronizzare le azioni con lo stato della lista.
    /// </summary>
    public void RipristinaAzioniRaccogli()
    {
        if (IncontroMercante) return;
        foreach (var kvp in Azioni.Where(a => a.Key.StartsWith("raccogli ")).ToList())
            Azioni.Remove(kvp.Key);
        foreach (var ot in OggettiStanza)
        {
            if (!string.IsNullOrEmpty(ot.oggetto.IdSalvataggio))
                CreaAzioneRaccogli(ot.oggetto);
        }
    }

    /// <summary>
    /// Crea la stanza d'ingresso del dungeon.
    /// </summary>
    /// <param name="posizione">Coordinate della stanza.</param>
    /// <param name="id">Identificatore della stanza (default: "ingresso").</param>
    /// <returns>Una nuova stanza d'ingresso.</returns>
    public static Stanza Ingresso(Coord posizione, string id = "ingresso")
    {
        Stanza s = new Stanza()
        {
            Id = id,
            Nome = "Ingresso",
            Descrizione = "L'ingresso del dungeon. Un freddo vento soffia da nord.",
            Livello = 0,
            Coordinate = posizione
        };
        var pozioneIngresso = Consumabili.Pozione_curativa_base();
        pozioneIngresso.IdSalvataggio = "pozione_ingresso";
        s.AggiungiOggettoRaccoglibile(pozioneIngresso);
        return s;
    }

    /// <summary>
    /// Crea una stanza del tesoro che permette di raccogliere 20 oro.
    /// </summary>
    /// <param name="posizione">Coordinate della stanza.</param>
    /// <param name="id">Identificatore della stanza (default: "tesoro").</param>
    /// <returns>Una nuova stanza del tesoro.</returns>
    public static Stanza StanzaDelTesoro(Coord posizione, string id = "tesoro")
    {
        Stanza s = new Stanza()
        {
            Id = id,
            Nome = "Stanza del Tesoro",
            Descrizione = "Tesoro ovunque. Monete d'oro, gemme...",
            Livello = 0,
            Coordinate = posizione
        };
        s.AggiungiAzione(
            "raccogli",
            "Raccogli Tesoro",
            "Raccogli tutto l'oro e le gemme presenti nella stanza.",
            () =>
            {
                if (s.OroRaccolto)
                {
                    UI.MostraMessaggio("Hai già raccolto tutto l'oro qui.");
                    return;
                }
                s.OroRaccolto = true;
                s.RimuoviAzione("raccogli");
                GameManager.Giocatore.AggiungiOro(20);
                UI.MostraMessaggio("Hai raccolto 20 oro!");
            }
        );
        return s;
    }

    /// <summary>
    /// Crea l'armeria dove il giocatore può migliorare la propria arma.
    /// </summary>
    /// <param name="posizione">Coordinate della stanza.</param>
    /// <param name="id">Identificatore della stanza (default: "armeria").</param>
    /// <returns>Una nuova armeria.</returns>
    public static Stanza Armeria(Coord posizione, string id = "armeria")
    {
        Stanza s = new Stanza()
        {
            Id = id,
            Nome = "Armeria",
            Descrizione = "Armeria dove puoi migliorare la tua arma.",
            Livello = 0,
            Coordinate = posizione
        };
        s.AggiungiAzione(
            "migliora",
            "Migliora Arma",
            "Potenzia l'arma equipaggiata a un livello superiore.",
            () =>
            {
                if (GameManager.Giocatore.Arma == null)
                {
                    UI.MostraErrore("Non hai un'arma equipaggiata.");
                    return;
                }
                Armi.RendiRara(GameManager.Giocatore.Arma);
                UI.MostraMessaggio($"La tua arma è ora più potente!");
            }
        );
        return s;
    }

    /// <summary>
    /// Crea una cantina con rifornimenti: una pozione curativa e una chiave d'oro.
    /// </summary>
    /// <param name="posizione">Coordinate della stanza.</param>
    /// <param name="id">Identificatore della stanza (default: "cantina").</param>
    /// <returns>Una nuova cantina.</returns>
    public static Stanza Cantina(Coord posizione, string id = "cantina")
    {
        Stanza s = new Stanza()
        {
            Id = id,
            Nome = "Cantina",
            Descrizione = "Rifornimento cibo e pozioni. Vedi qualcosa luccicare in un angolo.",
            Livello = 0,
            Coordinate = posizione
        };
        var pozioneCantina = Consumabili.Pozione_curativa_base();
        pozioneCantina.IdSalvataggio = "pozione_cantina";
        s.AggiungiOggettoRaccoglibile(pozioneCantina);
        var chiaveOro = new OggettoChiave
        {
            Nome = "Chiave d'Oro",
            Descrizione = "Una chiave per la serratura (chiave_oro).",
            Serratura = "chiave_oro",
            ChiaveId = "chiave_oro",
            IdSalvataggio = "chiave_oro_cantina"
        };
        s.AggiungiOggettoRaccoglibile(chiaveOro);
        return s;
    }

    /// <summary>
    /// Crea un corridoio di passaggio senza oggetti speciali.
    /// </summary>
    /// <param name="posizione">Coordinate della stanza.</param>
    /// <param name="id">Identificatore della stanza (default: "corridoio").</param>
    /// <returns>Un nuovo corridoio.</returns>
    public static Stanza Corridoio(Coord posizione, string id = "corridoio")
    {
        Stanza s = new Stanza()
        {
            Id = id,
            Nome = "Corridoio",
            Descrizione = "Un corridoio umido. A nord sembra esserci una porta pesante.",
            Livello = 0,
            Coordinate = posizione
        };
        return s;
    }

    /// <summary>
    /// Crea una stanza di combattimento con un nemico specifico.
    /// </summary>
    /// <param name="posizione">Coordinate della stanza.</param>
    /// <param name="nemico">Il nemico da posizionare nella stanza.</param>
    /// <param name="duplicato">Numero duplicato per generare un ID univoco in caso di stanze multiple con lo stesso nemico.</param>
    /// <returns>Una nuova stanza di combattimento.</returns>
    public static Stanza StanzaCombattimento(Coord posizione, Nemico nemico, int duplicato = 0)
    {
        Stanza s = new Stanza()
        {
            Id = $"combattimento_{nemico}{((duplicato==0)?"":duplicato)}",
            Nome = $"Stanza con {nemico}",
            Descrizione = "",
            Livello = 0,
            Coordinate = posizione,
            NemicoStanza = nemico
        };
        return s;
    }

    /// <summary>
    /// Crea una stanza del teletrasporto che sposta il giocatore in una stanza casuale del dungeon.
    /// </summary>
    /// <param name="posizione">Coordinate della stanza.</param>
    /// <param name="id">Identificatore della stanza (default: "teletrasporto").</param>
    /// <returns>Una nuova stanza del teletrasporto.</returns>
    public static Stanza StanzaTeletrasporto(Coord posizione, string id = "teletrasporto")
    {
        Stanza s = new Stanza()
        {
            Id = id,
            Nome = "Stanza Teletrasporto",
            Descrizione = "Una stanza avvolta da un alone di magia. Al centro, un cerchio runico pulsa di energia arcana.",
            Livello = 0,
            Coordinate = posizione
        };
        s.AggiungiAzione(
            "teletrasportati",
            "Teletrasportati",
            "Entra nel cerchio runico e lasciati teletrasportare in una stanza casuale del dungeon.",
            () =>
            {
                Random rng = new Random();
                var altreStanze = Mappa.Stanze.Values.Where(st => st.Id != "teletrasporto").ToList();
                if (altreStanze.Count == 0)
                {
                    Logger.Get<Stanza>().LogWarning("Teletrasporto: nessuna destinazione disponibile");
                    UI.MostraErrore("Il cerchio runico non reagisce... nessuna destinazione disponibile.");
                    return;
                }
                var destinazione = altreStanze[rng.Next(altreStanze.Count)];
                Logger.Get<Stanza>().LogInformation("Teletrasporto: {Origine} -> {Destinazione}", s.Nome, destinazione.Nome);
                UI.MostraTeletrasporto(destinazione.Nome);
                GameManager.StanzaCorrente = destinazione;
                GameManager.CambiaStato(new EsplorazioneStanza(destinazione));
            }
        );
        return s;
    }

    /// <summary>
    /// Crea una stanza curativa che ripristina completamente PV e stamina del giocatore.
    /// </summary>
    /// <param name="posizione">Coordinate della stanza.</param>
    /// <param name="id">Identificatore della stanza.</param>
    /// <returns>Una nuova stanza curativa.</returns>
    public static Stanza StanzaCurativa(Coord posizione, string id)
    {
        Stanza s = new Stanza()
        {
            Id = id,
            Nome = "Stanza Curativa",
            Descrizione = "Una stanza avvolta da una luce calda e ristoratrice. Senti le tue energie rinnovarsi.",
            Livello = 0,
            Coordinate = posizione
        };
        s.AggiungiAzione(
            "riposati",
            "Riposati",
            s.CurativaUsata
                ? "La luce si è affievolita. Non puoi più riposarti qui."
                : "Riposati nella luce curativa e recupera le energie.",
            () =>
            {
                if (s.CurativaUsata)
                {
                    UI.MostraMessaggio("La luce curativa si è ormai esaurita.");
                    return;
                }
                s.CurativaUsata = true;
                GameManager.Giocatore.Cura(GameManager.Giocatore.PuntiVitaMax);
                GameManager.Giocatore.CambiaStamina(GameManager.Giocatore.StaminaMax);
                UI.MostraMessaggio("Ti sei riposato e hai recuperato tutte le energie!");
            }
        );
        return s;
    }

    /// <summary>
    /// Crea la stanza dell'inceneritore dove il giocatore può incantare l'arma con il fuoco,
    /// riducendone il costo in stamina di 1.
    /// </summary>
    /// <param name="posizione">Coordinate della stanza.</param>
    /// <param name="id">Identificatore della stanza.</param>
    /// <returns>Una nuova stanza inceneritore.</returns>
    public static Stanza Inceneritore(Coord posizione, string id)
    {
        Stanza s = new Stanza()
        {
            Id = id,
            Nome = "Inceneritore",
            Descrizione = "Un grande inceneritore al centro della stanza emana un calore intenso. Fiamme guizzano dalle grate.",
            Livello = 0,
            Coordinate = posizione
        };
        s.AggiungiAzione(
            "Incanta",
            "Incanta la tua arma col fuoco",
            "Getta la tua arma per incantarla col fuoco.",
            () =>
            {
                if (GameManager.Giocatore.Arma == null)
                {
                    UI.MostraMessaggio("Non hai una arma.");
                    return;
                }
                var arma = GameManager.Giocatore.Arma;
                if(arma.Nome.Contains("fuoco", StringComparison.InvariantCultureIgnoreCase))
                {
                    UI.MostraMessaggio("Arma già infuocata.");
                    return;
                }
                else
                {
                    arma.stamina-=1;
                    arma.Nome += "fuoco";
                    UI.MostraMessaggio("La tua arma viene infuocata ed è più veloce da utilizzare. (-1 Stamina richiesta.)");
                    return;
                }
            }
        );
        return s;
    }

    /// <summary>
    /// Crea la stanza del mercante con oggetti in vendita e la chiave del boss.
    /// </summary>
    /// <param name="posizione">Coordinate della stanza.</param>
    /// <param name="id">Identificatore della stanza.</param>
    /// <returns>Una nuova stanza del mercante.</returns>
    public static Stanza StanzaMercante(Coord posizione, string id)
    {
        Stanza s = new Stanza()
        {
            Id = id,
            Nome = "Mercante",
            Descrizione = "Un uomo dall'aria sospetta è seduto dietro un tavolo colmo di mercanzie.",
            Livello = 0,
            Coordinate = posizione
        };
        s.IncontroMercante = true;
        s.AggiungiAzione(
            "parla al mercante",
            "Parla al Mercante",
            "Parla con il misterioso mercante e guarda la sua merce.",
            () =>
            {
                var esplorazione = GameManager.StatoGioco as EsplorazioneStanza;
                if (esplorazione is not null)
                    GameManager.CambiaStato(new IncontroMercante { Contesto = esplorazione });
            }
        );
        var pozioneMedia = Consumabili.Pozione_curativa_media();
        pozioneMedia.IdSalvataggio = "pozione_media_mercante";
        s.OggettiStanza.Add(new OggettoTrovabile { oggetto = pozioneMedia });
        var torta = Consumabili.Torta();
        torta.IdSalvataggio = "torta_mercante";
        s.OggettiStanza.Add(new OggettoTrovabile { oggetto = torta });
        var pozioneTotale = Consumabili.Pozione_recupero_totale();
        pozioneTotale.IdSalvataggio = "pozione_totale_mercante";
        s.OggettiStanza.Add(new OggettoTrovabile { oggetto = pozioneTotale });
        s.OggettiStanza.Add(new OggettoTrovabile
        {
            oggetto = new OggettoChiave
            {
                Nome = "Chiave del Boss",
                Descrizione = "Una chiave pesante con incisioni oscure. Apre la porta verso la sala del boss (chiave_boss).",
                Serratura = "chiave_boss",
                ChiaveId = "chiave_boss",
                IdSalvataggio = "chiave_boss_mercante"
            }
        });
        return s;
    }

    /// <summary>
    /// Crea una stanza con un miniboss.
    /// </summary>
    /// <param name="posizione">Coordinate della stanza.</param>
    /// <param name="id">Identificatore della stanza.</param>
    /// <param name="miniboss">Il miniboss da posizionare nella stanza.</param>
    /// <returns>Una nuova stanza miniboss.</returns>
    public static Stanza StanzaMiniboss(Coord posizione, string id, Nemico miniboss)
    {
        Stanza s = new Stanza()
        {
            Id = id,
            Nome = "Stanza Miniboss",
            Descrizione = "Una stanza sorvegliata da un potente guardiano. L'aria è densa di tensione.",
            Livello = 0,
            Coordinate = posizione,
            NemicoStanza = miniboss
        };
        return s;
    }

    /// <summary>
    /// Crea la sala del boss finale con il Signore del Dungeon.
    /// </summary>
    /// <param name="posizione">Coordinate della stanza.</param>
    /// <param name="id">Identificatore della stanza.</param>
    /// <returns>Una nuova sala del boss.</returns>
    public static Stanza StanzaBoss(Coord posizione, string id)
    {
        Stanza s = new Stanza()
        {
            Id = id,
            Nome = "Sala del Boss",
            Descrizione = "La sala del trono del signore del dungeon. Un'aura maligna pervade l'ambiente.",
            Livello = 0,
            Coordinate = posizione,
            NemicoStanza = Nemico.Boss()
        };
        return s;
    }
}

/// <summary>
/// Contiene le posizioni predefinite e i collegamenti tra le stanze del dungeon.
/// </summary>
public static class DizionarioMappa
{
    /// <summary>Mappa gli ID delle stanze alle loro coordinate sulla griglia.</summary>
    public static readonly Dictionary<string, Coord> Posizioni = new()
    {
        ["ingresso"]        = new Coord(0, -2),
        ["cantina_S"]       = new Coord(0, -1),
        ["armeria"]         = new Coord(0, 0),
        ["cantina_N"]       = new Coord(0, 1),
        ["mercante"]        = new Coord(0, 2),
        ["boss"]            = new Coord(0, 3),
        ["inceneritore_W"]  = new Coord(-1, 0),
        ["inceneritore_E"]  = new Coord(1, 0),
        ["mimic_SW"]        = new Coord(-1, -1),
        ["tesoro_SE"]       = new Coord(1, -1),
        ["tesoro_NW"]       = new Coord(-1, 1),
        ["mimic_NE"]        = new Coord(1, 1),
        ["curativa_SW"]     = new Coord(-2, -1),
        ["curativa_NW"]     = new Coord(-2, 1),
        ["curativa_NE"]     = new Coord(2, 1),
        ["miniboss_SW"]     = new Coord(-2, -2),
        ["miniboss_NW"]     = new Coord(-2, 2),
        ["miniboss_NE"]     = new Coord(2, 2),
        ["cantina_SE"]      = new Coord(2, -1),
        ["teletrasporto"]   = new Coord(2, -2),
    };

    /// <summary>Mappa gli ID delle stanze agli ID delle stanze adiacenti (collegamenti).</summary>
    public static readonly Dictionary<string, string[]> Collegamenti = new()
    {
        ["ingresso"]        = new[] { "cantina_S" },
        ["cantina_S"]       = new[] { "ingresso", "armeria", "mimic_SW", "tesoro_SE" },
        ["mimic_SW"]        = new[] { "cantina_S", "curativa_SW", "inceneritore_W" },
        ["curativa_SW"]     = new[] { "mimic_SW", "miniboss_SW" },
        ["miniboss_SW"]     = new[] { "curativa_SW" },
        ["tesoro_SE"]       = new[] { "cantina_S", "inceneritore_E", "cantina_SE" },
        ["cantina_SE"]      = new[] { "tesoro_SE", "teletrasporto" },
        ["teletrasporto"]   = new[] { "cantina_SE" },
        ["armeria"]         = new[] { "cantina_S", "cantina_N", "inceneritore_W", "inceneritore_E" },
        ["inceneritore_W"]  = new[] { "armeria", "mimic_SW", "tesoro_NW" },
        ["inceneritore_E"]  = new[] { "armeria", "tesoro_SE", "mimic_NE" },
        ["cantina_N"]       = new[] { "armeria", "tesoro_NW", "mimic_NE", "mercante" },
        ["tesoro_NW"]       = new[] { "cantina_N", "inceneritore_W", "curativa_NW" },
        ["curativa_NW"]     = new[] { "tesoro_NW", "miniboss_NW" },
        ["miniboss_NW"]     = new[] { "curativa_NW" },
        ["mimic_NE"]        = new[] { "cantina_N", "inceneritore_E", "curativa_NE" },
        ["curativa_NE"]     = new[] { "mimic_NE", "miniboss_NE" },
        ["miniboss_NE"]     = new[] { "curativa_NE" },
        ["mercante"]        = new[] { "cantina_N", "boss" },
        ["boss"]            = new[] { "mercante" },
    };
}

/// <summary>
/// Lista di oggetti tesoro disponibili nel gioco (usata come riferimento).
/// </summary>
public static class ListeStanze
{
    /// <summary>Oggetti tesoro base.</summary>
    public static List<Oggetto> Tesori = [
        Armi.coltello(),
        Consumabili.Pane(),
        Consumabili.Mela()
    ];
}

/// <summary>
/// Tiene traccia delle stanze visitate dal giocatore.
/// </summary>
public static class StanzeVisitate
{
    /// <summary>Lista delle stanze visitate.</summary>
    public static List<Stanza> ListaVisitate = new();

    /// <summary>Aggiunge una stanza alla lista delle visitate.</summary>
    /// <param name="s">Stanza da aggiungere.</param>
    public static void AggiungiStanza(Stanza s)
    {
        ListaVisitate.Add(s);
    }
}

/// <summary>
/// Gestisce la mappa del dungeon: inizializzazione delle stanze, collegamenti,
/// porte, nemici e azioni di movimento.
/// </summary>
public static class Mappa
{
    /// <summary>Dizionario di tutte le stanze, indicizzate per coordinate.</summary>
    public static Dictionary<Coord, Stanza> Stanze { get; } = new();

    /// <summary>Restituisce la stanza alle coordinate specificate, o <c>null</c> se non esiste.</summary>
    /// <param name="c">Coordinate da cercare.</param>
    /// <returns>La stanza trovata, o <c>null</c>.</returns>
    public static Stanza? Verso(Coord c) => Stanze.TryGetValue(c, out var s) ? s : null;

    /// <summary>Pool di miniboss disponibili per l'assegnazione casuale.</summary>
    private static readonly Nemico[] PoolMiniboss = new[] { Nemico.MaestroArmi(), Nemico.Guardiano() };

    /// <summary>Coordinate della stanza iniziale (ingresso).</summary>
    public static Coord CoordinateIniziali;

    /// <summary>Dizionario che associa ID stanza al nome del miniboss assegnato (per salvataggio).</summary>
    public static readonly Dictionary<string, string> AssegnazioniMiniboss = new();

    /// <summary>
    /// Inizializza (o reinizializza) l'intera mappa: crea tutte le stanze,
    /// collega le porte, assegna miniboss casuali e aggiunge le azioni di movimento.
    /// </summary>
    public static void Inizializza()
    {
        Stanze.Clear();
        AssegnazioniMiniboss.Clear();
        Logger.For("Mappa").LogDebug("Inizializzazione mappa");

        var rng = new Random();

        foreach (var (id, pos) in DizionarioMappa.Posizioni)
        {
            Stanza stanza = CreaStanza(id, pos, rng);
            Stanze[pos] = stanza;
        }

        foreach (var (id, vicini) in DizionarioMappa.Collegamenti)
        {
            var da = Stanze[DizionarioMappa.Posizioni[id]];
            foreach (var vicinoId in vicini)
            {
                var a = Stanze[DizionarioMappa.Posizioni[vicinoId]];
                var dir = DirezioneVerso(da.Coordinate, a.Coordinate);
                if (dir.HasValue && !da.Porte.ContainsKey(dir.Value))
                {
                    string? chiave = (id, vicinoId) switch
                    {
                        ("cantina_S", "mimic_SW") => "chiave_oro",
                        ("mimic_SW", "cantina_S") => "chiave_oro",
                        ("cantina_N", "mimic_NE") => "chiave_oro",
                        ("mimic_NE", "cantina_N") => "chiave_oro",
                        ("cantina_S", "tesoro_SE") => "chiave_oro",
                        ("tesoro_SE", "cantina_S") => "chiave_oro",
                        ("mercante", "boss") => "chiave_boss",
                        ("boss", "mercante") => "chiave_boss",
                        _ => null
                    };
                    Collega(da, a, dir.Value, chiave);
                }
            }
        }

        foreach (var s in Stanze.Values)
            AggiungiAzioniMovimento(s);

        Logger.For("Mappa").LogInformation("Mappa inizializzata: {N} stanze", Stanze.Count);
    }

    /// <summary>
    /// Crea una stanza in base al suo ID, determinandone il tipo.
    /// </summary>
    /// <param name="id">Identificatore della stanza.</param>
    /// <param name="pos">Coordinate della stanza.</param>
    /// <param name="rng">Generatore di numeri casuali per assegnazioni random.</param>
    /// <returns>La stanza creata.</returns>
    /// <exception cref="ArgumentException">Se l'ID della stanza non è riconosciuto.</exception>
    private static Stanza CreaStanza(string id, Coord pos, Random rng)
    {
        if(id == "ingresso") CoordinateIniziali=pos;
        return id switch
        {
            "ingresso" => Stanza.Ingresso(pos, id),
            "armeria" => Stanza.Armeria(pos, id),
            string s when s.StartsWith("cantina_") => Stanza.Cantina(pos, id),
            string s when s.StartsWith("tesoro_") => Stanza.StanzaDelTesoro(pos, id),
            string s when s.StartsWith("mimic_") => Stanza.StanzaCombattimento(pos, Nemico.Mimic()),
            "teletrasporto" => Stanza.StanzaTeletrasporto(pos, id),
            string s when s.StartsWith("curativa_") => Stanza.StanzaCurativa(pos, id),
            string s when s.StartsWith("miniboss_") => CreaMiniboss(id, pos, rng),
            string s when s.StartsWith("inceneritore_") => Stanza.Inceneritore(pos, id),
            "mercante" => Stanza.StanzaMercante(pos, id),
            "boss" => Stanza.StanzaBoss(pos, id),
            _ => throw new ArgumentException($"Tipo stanza sconosciuto: {id}")
        };
    }

    /// <summary>
    /// Crea una stanza miniboss assegnando casualmente un nemico dal pool.
    /// </summary>
    /// <param name="id">Identificatore della stanza.</param>
    /// <param name="pos">Coordinate della stanza.</param>
    /// <param name="rng">Generatore di numeri casuali.</param>
    /// <returns>Una nuova stanza miniboss.</returns>
    private static Stanza CreaMiniboss(string id, Coord pos, Random rng)
    {
        var miniboss = PoolMiniboss[rng.Next(PoolMiniboss.Length)];
        AssegnazioniMiniboss[id] = new NemicoCopiabile
        {
            NomeCompleto = miniboss.Nome
        }.Serialize();
        return Stanza.StanzaMiniboss(pos, id, miniboss);
    }

    /// <summary>
    /// Record per serializzare/deserializzare l'assegnazione di un miniboss a una stanza.
    /// </summary>
    public record struct NemicoCopiabile
    {
        /// <summary>Nome completo del nemico (es. "Maestro", "Guardiano Della Cripta").</summary>
        public string NomeCompleto { get; init; }
        /// <summary>Serializza il nome in stringa.</summary>
        public string Serialize() => NomeCompleto;
        /// <summary>Ricostruisce un nemico a partire dal nome.</summary>
        /// <param name="nome">Nome del nemico.</param>
        /// <returns>Il nemico ricostruito, o <c>null</c> se il nome non è riconosciuto.</returns>
        public static Nemico? DaString(string nome) => nome switch
        {
            "Maestro" => Nemico.MaestroArmi(),
            "Guardiano Della Cripta" => Nemico.Guardiano(),
            _ => null
        };
    }

    /// <summary>
    /// Calcola la direzione tra due coordinate adiacenti.
    /// </summary>
    /// <param name="da">Coordinata di partenza.</param>
    /// <param name="a">Coordinata di arrivo.</param>
    /// <returns>La direzione, o <c>null</c> se le coordinate non sono adiacenti ortogonalmente.</returns>
    private static Direzione? DirezioneVerso(Coord da, Coord a)
    {
        var delta = new Coord(a.X - da.X, a.Y - da.Y);
        if (delta.X == 0 && delta.Y > 0) return Direzione.Nord;
        if (delta.X == 0 && delta.Y < 0) return Direzione.Sud;
        if (delta.X > 0 && delta.Y == 0) return Direzione.Est;
        if (delta.X < 0 && delta.Y == 0) return Direzione.Ovest;
        return null;
    }

    /// <summary>
    /// Collega due stanze con una porta. Se è specificata una chiave, la porta sarà bloccata.
    /// </summary>
    /// <param name="da">Stanza di partenza.</param>
    /// <param name="a">Stanza di arrivo.</param>
    /// <param name="dir">Direzione dalla prima alla seconda stanza.</param>
    /// <param name="chiaveRichiesta">ID della chiave richiesta, o <c>null</c> per porta normale.</param>
    private static void Collega(Stanza da, Stanza a, Direzione dir, string? chiaveRichiesta)
    {
        if (chiaveRichiesta is not null)
        {
            da.Porte[dir] = new Porta { Stato = StatoPorta.Bloccata, ChiaveRichiesta = chiaveRichiesta };
            a.Porte[dir.Opposta()] = new Porta { Stato = StatoPorta.Bloccata, ChiaveRichiesta = chiaveRichiesta };
        }
        else
        {
            da.Porte[dir] = new Porta { Stato = StatoPorta.Chiusa };
            a.Porte[dir.Opposta()] = new Porta { Stato = StatoPorta.Chiusa };
        }
    }

    /// <summary>
    /// Verifica se una stanza è collegata a un'altra nella direzione specificata.
    /// </summary>
    /// <param name="s">Stanza di partenza.</param>
    /// <param name="d">Direzione da controllare.</param>
    /// <returns><c>true</c> se esiste una stanza nella direzione indicata.</returns>
    public static bool SiCollega(Stanza s, Direzione d)
    {
        return Verso(s.Coordinate + d.ToDelta()) != null;
    }

    /// <summary>
    /// Aggiunge le azioni di movimento ("vai nord", "vai sud", ecc.) a una stanza
    /// per tutte le direzioni in cui esiste un collegamento.
    /// </summary>
    /// <param name="s">Stanza a cui aggiungere le azioni.</param>
    private static void AggiungiAzioniMovimento(Stanza s)
    {
        foreach (Direzione dir in Enum.GetValues<Direzione>())
        {
            if (SiCollega(s, dir))
            {
                string idAzione = $"vai {dir.Testo()}";
                string nomeDir = dir.ToString();
                s.AggiungiAzione(
                    idAzione,
                    $"Vai {nomeDir}",
                    $"Sposta verso {nomeDir.ToLower()}.",
                    () => GameManager.Sposta(dir)
                );
            }
        }
    }
}
