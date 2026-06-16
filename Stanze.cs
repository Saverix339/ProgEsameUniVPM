using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.Marshalling;
using ProgEsameUniVPM;
using Microsoft.Extensions.Logging;

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
    public StatoPorta Stato { get; set; } = StatoPorta.Aperta;
    public string? ChiaveRichiesta { get; set; }

    public bool RichiedeChiave => ChiaveRichiesta is not null;
}

public class Azione
{
    public required string Id { get; init; }
    public required string Nome { get; init; }
    public required string Descrizione { get; init; }
    //public string Categoria { get; init; } = "Altro";

    
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
            //Categoria = "Altro",
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

    /// <summary>
    /// Livello Per Sviluppi successivi. Al momento solo livello 0.
    /// </summary>
    public required int Livello { get; init; }

    public Dictionary<string, Azione> Azioni { get; } = new();

    public Dictionary<Direzione, Porta> Porte { get; } = new();

    public bool PrimaVolta { get; set; } = true;

    public Nemico? NemicoStanza;
    public bool IncontroMercante = false; //NOTA: se true, oggettistanza diventa la lista del mercante di default.

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
        s.AggiungiAzione(
            "raccogli torcia",
            "Raccogli Torcia",
            "Raccogli una torcia appoggiata vicino all'ingresso.",
            () => { UI.MostraMessaggio("Non c'è nulla da raccogliere qui."); }
        );
        // s.AggiungiAzione(
        //     "parla al mercante",
        //     "Parla al Mercante",
        //     "Parla con il misterioso mercante e guarda la sua merce.",
        //     () =>
        //     {
        //         var esplorazione = GameManager.StatoGioco as EsplorazioneStanza;
        //         if (esplorazione is not null)
        //             GameManager.CambiaStato(new IncontroMercante { Contesto = esplorazione });
        //     }
        // );
        s.OggettiStanza.Add(new OggettoTrovabile { oggetto = Consumabili.Pozione_curativa_base() });
        //s.OggettiStanza.Add(new OggettoTrovabile { oggetto = Consumabili.Mela() });
        //s.OggettiStanza.Add(new OggettoTrovabile { oggetto = Consumabili.Pane() });
        return s;
    }

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
            () => { GameManager.Giocatore.AggiungiOro(20); UI.MostraMessaggio("Hai raccolto 20 oro!"); }
        );
        return s;
    }

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
            "Riposati nella luce curativa e recupera le energie.",
            () =>
            {
                GameManager.Giocatore.Cura(GameManager.Giocatore.PuntiVitaMax);
                GameManager.Giocatore.CambiaStamina(GameManager.Giocatore.StaminaMax);
                UI.MostraMessaggio("Ti sei riposato e hai recuperato tutte le energie!");
            }
        );
        return s;
    }

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
        s.OggettiStanza.Add(new OggettoTrovabile { oggetto = Consumabili.Pozione_curativa_media() });
        s.OggettiStanza.Add(new OggettoTrovabile { oggetto = Consumabili.Torta() });
        s.OggettiStanza.Add(new OggettoTrovabile { oggetto = Consumabili.Pozione_recupero_totale() });
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

public static class DizionarioMappa
{
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
        ListaVisitate.Add(s);
    }
}

public static class Mappa
{
    public static Dictionary<Coord, Stanza> Stanze { get; } = new();

    public static Stanza? Verso(Coord c) => Stanze.TryGetValue(c, out var s) ? s : null;

    private static readonly Nemico[] PoolMiniboss = new[] { Nemico.MaestroArmi(), Nemico.Guardiano() };

    //private static Stanza? PrendiStanza(string id) => 
    public static Coord CoordinateIniziali;

    public static readonly Dictionary<string, string> AssegnazioniMiniboss = new();

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

    private static Stanza CreaMiniboss(string id, Coord pos, Random rng)
    {
        var miniboss = PoolMiniboss[rng.Next(PoolMiniboss.Length)];
        AssegnazioniMiniboss[id] = new NemicoCopiabile
        {
            NomeCompleto = miniboss.Nome
        }.Serialize();
        return Stanza.StanzaMiniboss(pos, id, miniboss);
    }

    public record struct NemicoCopiabile
    {
        public string NomeCompleto { get; init; }
        public string Serialize() => NomeCompleto;
        public static Nemico? DaString(string nome) => nome switch
        {
            "Maestro" => Nemico.MaestroArmi(),
            "Guardiano Della Cripta" => Nemico.Guardiano(),
            _ => null
        };
    }

    private static Direzione? DirezioneVerso(Coord da, Coord a)
    {
        var delta = new Coord(a.X - da.X, a.Y - da.Y);
        if (delta.X == 0 && delta.Y > 0) return Direzione.Nord;
        if (delta.X == 0 && delta.Y < 0) return Direzione.Sud;
        if (delta.X > 0 && delta.Y == 0) return Direzione.Est;
        if (delta.X < 0 && delta.Y == 0) return Direzione.Ovest;
        return null;
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
            da.Porte[dir] = new Porta { Stato = StatoPorta.Chiusa };
            a.Porte[dir.Opposta()] = new Porta { Stato = StatoPorta.Chiusa };
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
