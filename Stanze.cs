using System;
using System.Numerics;
using System.Runtime.InteropServices.Marshalling;
using ProgEsameUniVPM;

public readonly record struct Coord(int X, int Y)
{
    public static Coord operator +(Coord a, Coord b) => new(a.X + b.X, a.Y + b.Y);
}

public enum Direzione { Nord, Sud, Est, Ovest }


public static class ManagerDirezioni
{
    private static readonly Dictionary<Direzione, Coord> DeltaPosizione = new()
    {
        [Direzione.Nord]  = new Coord(0, 1),
        [Direzione.Sud]   = new Coord(0, -1),
        [Direzione.Est]   = new Coord(1, 0),
        [Direzione.Ovest] = new Coord(-1, 0),
    };

    public static Coord ToDelta(this Direzione d) => DeltaPosizione[d];
    public static Direzione Opposta(this Direzione d) => d switch
    {
        Direzione.Nord  => Direzione.Sud,
        Direzione.Sud   => Direzione.Nord,
        Direzione.Est   => Direzione.Ovest,
        Direzione.Ovest => Direzione.Est,
        _ => throw new ArgumentOutOfRangeException(nameof(d))
    };
    public static string Testo(this Direzione d) => d.ToString().ToLowerInvariant();
}

public enum StatoPorta { Aperta, Chiusa, Bloccata }

public class Porta
{
    public StatoPorta Stato { get; set; } = StatoPorta.Aperta;
    public string? ChiaveRichiesta { get; set; }

    public bool RichiedeChiave => ChiaveRichiesta is not null;
}

public class Azione
{
    public required string Id { get; init; }
    public required string Nome { get; init; }
    public required string Descrizione { get; init; }
    public string Categoria { get; init; } = "Altro";
    public Action Esegui { get; init; } = () => { };

    //public static Azione Crea(string id, string nome, string descrizione, string categoria, Action callback)
    public static Azione Crea(string id, string nome, string descrizione, Action callback)
    {
        return new Azione
        {
            Id = id,
            Nome = nome,
            Descrizione = descrizione,
            //Categoria = categoria,
            Categoria = "Altro",
            Esegui = callback
        };
    }
}

public class Stanza
{
    public required string Id { get; init; }
    public required string Nome { get; init; }
    public required string Descrizione { get; init; }

    public required Coord Coordinate { get; init; }

    public List<OggettoTrovabile> OggettiStanza { get; } = new();

    public required int Livello { get; init; }

    public Dictionary<string, Azione> Azioni { get; } = new();

    public Dictionary<Direzione, Porta> Porte { get; } = new();

    public bool PrimaVolta { get; set; } = true;

    public Nemico? NemicoStanza;

    public bool NemicoSconfitto = false;

    public void AggiungiAzione(string id, string nome, string descrizione, Action callback)
    {
        Azioni[id] = Azione.Crea(id, nome, descrizione, callback);
    }

    public bool RimuoviAzione(string id)
    {
        return Azioni.Remove(id);
    }

    public bool RaccogliOggetto(Guid id, out Oggetto? oggetto)
    {
        var trovato = OggettiStanza.FirstOrDefault(o => o.oggetto.Id == id);
        if (trovato is null) { oggetto = null; return false; }
        oggetto = trovato.oggetto;
        OggettiStanza.Remove(trovato);
        return true;
    }

    /*
    private List<Oggetto> Tesori;

    public Stanza()
    {
        Tesori.Add(Consumabili.Pane());
        Tesori.Add(Consumabili.Mela());
        Tesori.Add(Armi.coltello());
    }
    */

    public static Stanza Ingresso(Coord posizione)
    {
        Stanza s = new Stanza()
        {
            Id = "ingresso",
            Nome = "Ingresso",
            Descrizione = "L'ingresso del dungeon. Un freddo vento soffia da nord.",
            Livello = 0,
            Coordinate = posizione
        };
        s.AggiungiAzione(
            "raccogli torcia",
            "Raccogli Torcia",
            "Raccogli una torcia appoggiata vicino all'ingresso.",
            () => { UI.MostraMessaggio("Non c'è nulla da raccogliere qui."); }
        );
        return s;
    }

    public static Stanza StanzaDelTesoro(Coord posizione)
    {
        Stanza s = new Stanza()
        {
            Id = "tesoro",
            Nome = "Stanza del Tesoro",
            Descrizione = "Tesoro ovunque. Monete d'oro, gemme...",
            Livello = 0,
            Coordinate = posizione
        };
        s.AggiungiAzione(
            "raccogli",
            "Raccogli Tesoro",
            "Raccogli tutto l'oro e le gemme presenti nella stanza.",
            () => { GameManager.Giocatore.AggiungiOro(20); UI.MostraMessaggio("Hai raccolto 20 oro!"); }
        );
        return s;
    }

    public static Stanza Armeria(Coord posizione)
    {
        Stanza s = new Stanza()
        {
            Id = "armeria",
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

    public static Stanza Cantina(Coord posizione)
    {
        Stanza s = new Stanza()
        {
            Id = "cantina",
            Nome = "Cantina",
            Descrizione = "Rifornimento cibo e pozioni. Vedi qualcosa luccicare in un angolo.",
            Livello = 0,
            Coordinate = posizione
        };
        s.AggiungiAzione(
            "raccogli pozione",
            "Raccogli Pozione",
            "Raccogli una pozione curativa dalla mensola.",
            () => { GameManager.Giocatore.AggiungiOggettoInventario(Consumabili.Pozione_curativa_base()); UI.MostraMessaggio("Hai raccolto una pozione curativa!"); }
        );
        s.AggiungiAzione(
            "raccogli chiave",
            "Raccogli Chiave",
            "Raccogli la chiave dorata nell'angolo.",
            () =>
            {
                var trovata = s.OggettiStanza.FirstOrDefault(o => o.oggetto.ChiaveId == "chiave_oro");
                if (trovata is null) { UI.MostraErrore("Non c'è nessuna chiave qui."); return; }
                GameManager.Giocatore.Raccogli(trovata.oggetto);
                s.OggettiStanza.Remove(trovata);
                s.RimuoviAzione("raccogli chiave");
            }
        );
        s.OggettiStanza.Add(new OggettoTrovabile
        {
            oggetto = new OggettoChiave
            {
                Nome = "Chiave d'Oro",
                Descrizione = "Una chiave per la serratura (chiave_oro).",
                Serratura = "chiave_oro",
                ChiaveId = "chiave_oro",
                IdSalvataggio = "chiave_oro_cantina"
            }
        });
        return s;
    }

    public static Stanza Corridoio(Coord posizione)
    {
        Stanza s = new Stanza()
        {
            Id = "corridoio",
            Nome = "Corridoio",
            Descrizione = "Un corridoio umido. A nord sembra esserci una porta pesante.",
            Livello = 0,
            Coordinate = posizione
        };
        return s;
    }
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
}

public static class ListeStanze
{
    public static List<Oggetto> Tesori = [
        Armi.coltello(),
        Consumabili.Pane(),
        Consumabili.Mela()
    ];
}

public static class StanzeVisitate
{
    public static List<Stanza> ListaVisitate = new();
    public static void AggiungiStanza(Stanza s)
    {
        if (s.Nome == "Stanza Teletrasporto")
        {
            //todo
        }
        ListaVisitate.Add(s);
    }
}

public static class Mappa
{
    public static Dictionary<Coord, Stanza> Stanze { get; } = new();

    public static Stanza? Verso(Coord c) => Stanze.TryGetValue(c, out var s) ? s : null;

    public static void Inizializza()
    {
        Stanze.Clear();

        var ingresso = Stanza.Ingresso(new Coord(0, 0));
        var corridoio = Stanza.Corridoio(new Coord(0, 1));
        var armeria = Stanza.Armeria(new Coord(1, 1));
        var cantina = Stanza.Cantina(new Coord(-1, 0));
        var tesoro = Stanza.StanzaDelTesoro(new Coord(0, 2));

        // Registra stanze
        foreach (var s in new[] { ingresso, corridoio, armeria, cantina, tesoro })
            Stanze[s.Coordinate] = s;

        // Configura adiacenze + porte
        // Ingresso (0,0) <-> Cantina (-1,0)  : libera
        // Ingresso (0,0) <-> Corridoio (0,1)  : libera
        // Corridoio (0,1) <-> Armeria (1,1)   : libera
        // Corridoio (0,1) <-> Tesoro (0,2)    : bloccata, richiede "chiave_oro"
        //(mappa da esempio ovviamente)

        Collega(ingresso, corridoio, Direzione.Nord, null);
        Collega(corridoio, ingresso, Direzione.Sud, null);

        Collega(ingresso, cantina, Direzione.Ovest, null);
        Collega(cantina, ingresso, Direzione.Est, null);

        Collega(corridoio, armeria, Direzione.Est, null);
        Collega(armeria, corridoio, Direzione.Ovest, null);

        Collega(corridoio, tesoro, Direzione.Nord, "chiave_oro");
        Collega(tesoro, corridoio, Direzione.Sud, "chiave_oro");

        // Aggiunge le azioni di movimento in ogni stanza
        foreach (var s in Stanze.Values)
            AggiungiAzioniMovimento(s);
    }

    private static void Collega(Stanza da, Stanza a, Direzione dir, string? chiaveRichiesta)
    {
        if (chiaveRichiesta is not null)
        {
            da.Porte[dir] = new Porta { Stato = StatoPorta.Bloccata, ChiaveRichiesta = chiaveRichiesta };
            a.Porte[dir.Opposta()] = new Porta { Stato = StatoPorta.Bloccata, ChiaveRichiesta = chiaveRichiesta };
        }
        else
        {
            da.Porte[dir] = new Porta { Stato = StatoPorta.Chiusa};
            a.Porte[dir.Opposta()] = new Porta { Stato = StatoPorta.Chiusa};
        }
    }

    public static bool SiCollega(Stanza s, Direzione d)
    {
        return Verso(s.Coordinate + d.ToDelta()) != null;
    }

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
