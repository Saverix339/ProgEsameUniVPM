using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
namespace ProgEsameUniVPM;

/// <summary>
/// Classe base per tutti gli oggetti del gioco (armi, consumabili, chiavi).
/// Utilizza la serializzazione polimorfa JSON tramite <see cref="JsonDerivedType"/>.
/// </summary>
[JsonDerivedType(typeof(Armi), "arma")]
[JsonDerivedType(typeof(Consumabili), "consumabile")]
[JsonDerivedType(typeof(OggettoChiave), "chiave")]
public class Oggetto
{
    /// <summary>Identificatore univoco dell'oggetto (cambia a ogni inizializzazione).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Nome dell'oggetto.</summary>
    public string Nome = "Oggetto";

    /// <summary>Descrizione testuale dell'oggetto.</summary>
    public string Descrizione = "Oggetto base";

    /// <summary>Se non vuoto, l'oggetto è una chiave che apre porte con questo ID.</summary>
    public string ChiaveId { get; set; } = "";

    /// <summary>
    /// ID stabile per il salvataggio: assegnato a oggetti statici della mappa.
    /// Usato al posto di <see cref="Id"/> perché quest'ultimo cambia a ogni <see cref="Mappa.Inizializza"/>.
    /// </summary>
    public string IdSalvataggio { get; set; } = "";

    /// <summary>Indica se l'oggetto funge da chiave.</summary>
    public bool isChiave => ChiaveId.Length > 0;
    /// <summary>Valore in oro dell'oggetto. Virtuale: su <see cref="Consumabili"/> riflette <see cref="Consumabili.prezzo"/>.</summary>
    public virtual int Valore { get; set; } = 0;
}

/// <summary>
/// Oggetto chiave utilizzabile per aprire porte bloccate.
/// </summary>
public class OggettoChiave : Oggetto
{
    /// <summary>Identificatore della serratura che questa chiave può aprire.</summary>
    public string Serratura { get; set; } = "";

    /// <summary>
    /// Crea una nuova chiave con la serratura e il nome specificati.
    /// </summary>
    /// <param name="serratura">ID della serratura.</param>
    /// <param name="nome">Nome descrittivo della chiave.</param>
    /// <returns>Una nuova istanza di <see cref="OggettoChiave"/>.</returns>
    public static OggettoChiave Crea(string serratura, string nome)
    {
        return new OggettoChiave
        {
            Nome = nome,
            Descrizione = $"Una chiave per la serratura ({serratura}).",
            Serratura = serratura,
            ChiaveId = serratura
        };
    }
}


/// <summary>
/// Rappresenta un'arma equipaggiabile dal giocatore. Ha potenza, costo stamina
/// e un sistema di rarità (Normale -> Raro -> Epico -> Leggendario) che ne potenzia le statistiche.
/// </summary>
public class Armi : Oggetto
{
    /// <summary>Potenza base dell'arma (danno per attacco).</summary>
    public int potenza=2;
    /// <summary>Costo in stamina per usare l'abilità dell'arma.</summary>
    public int stamina=2;

    /// <summary>
    /// Livello di rarità: 0 = Normale, 1 = Raro, 2 = Epico, 3 = Leggendario.
    /// </summary>
    public int LivelloRarità = 0;

    /// <summary>
    /// Restituisce il nome dell'arma con il suffisso di rarità.
    /// </summary>
    /// <returns>Nome formattato con rarità.</returns>
    public string PrendiNome()
    {
        switch (LivelloRarità)
        {
            case 1: return Nome + " Raro";
            case 2: return Nome + " Epico";
            case 3: return Nome + " Leggendario";
            default: return Nome;
        }
    }

    /// <summary>Abilità speciale associata all'arma, o <c>null</c>. Non serializzabile: contiene delegati.</summary>
    [JsonIgnore]
    public AbilitaArma? AbilitaArma;

    /// <summary>
    /// Restituisce la descrizione dell'arma con l'indicazione della rarità.
    /// </summary>
    /// <returns>Descrizione formattata con rarità.</returns>
    public string PrendiDesc()
    {
        switch (LivelloRarità)
        {
            case 1: return Descrizione + "\nRarita': Raro";
            case 2: return Descrizione + "\nRarita': Epico";
            case 3: return Descrizione + "\nRarita': Leggendario";
            default: return Descrizione;
        }
    }

    /// <summary>Crea un'istanza preconfigurata della Spada.</summary>
    /// <returns>Una nuova Spada.</returns>
    public static Armi Spada()
    {
        Armi s = new()
        {
            Nome="Spada",
            Descrizione="attacco base fa meno danno ma non spreca stamina e si"+
            "vuole danno critico si spreca stamina",
            potenza=2,
            stamina=2,
            AbilitaArma = new ColpoPotente()
        };
        return s;
    }

    /// <summary>Crea un'istanza preconfigurata dello Scudo.</summary>
    /// <returns>Un nuovo Scudo.</returns>
    public static Armi Scudo()
    {
        Armi s = new()
        {
            Nome="Scudo",
            Descrizione="riflette i colpi diretti a costo di stamina"+
            "e recupera stamina difendendosi con lo scudo ma subisce meno danni riflette danno ",
            potenza=1,
            stamina=3,
            AbilitaArma = new RiflettiScudo()
        };
        return s;
    }

    /// <summary>Crea un'istanza preconfigurata del Coltello.</summary>
    /// <returns>Un nuovo Coltello.</returns>
    public static Armi coltello()
    {
        Armi s = new()
        {
            Nome="Coltello",
            Descrizione="attacchi base con possibilità bassa di attacare più di 1 volta per turno no stamina"+
             "e se si vuole fare bleed usi stamina ma con il bleed attacchi 1 volta",
            potenza=1,
            stamina=1,
            AbilitaArma = new Sanguinamento()
        };
        return s;
    }

    /// <summary>
    /// Potenzia l'arma al livello Raro (1): raddoppia la potenza e aumenta la stamina di 1.
    /// Fallisce se l'arma è già Rara o superiore.
    /// </summary>
    /// <param name="a">Arma da potenziare.</param>
    /// <exception cref="Exception">Se il livello di rarità non è valido.</exception>
    public static void RendiRara(Armi a)
    {
        if(a.LivelloRarità < 0)
        {
            throw new Exception("Livello rarità non valido.");
        }

        if(a.LivelloRarità >= 1)
        {
            UI.MostraErrore("L'arma " + a.Nome + " è già potenziata!");
            Logger.Get<Armi>().LogDebug("Upgrade raro fallito: {Arma} già potenziata", a.Nome);
            return;
        }
        a.LivelloRarità = 1;
        a.potenza *= 2;
        a.stamina +=1;
        Logger.Get<Armi>().LogInformation("Arma potenziata a Rara: {Arma} (potenza: {Potenza})", a.Nome, a.potenza);
    }

    /// <summary>
    /// Potenzia l'arma al livello Epico (2): aumenta la potenza del 50% e la stamina di 1.
    /// Richiede che l'arma sia già Rara.
    /// </summary>
    /// <param name="a">Arma da potenziare.</param>
    /// <exception cref="Exception">Se il livello di rarità non è valido.</exception>
    public static void RendiEpico(Armi a)
    {
        if(a.LivelloRarità < 0)
        {
            throw new Exception("Livello rarità non valido.");
        }
        if(a.LivelloRarità >= 2)
        {
            Console.WriteLine("L'arma " + a.Nome + " è già potenziata!");
            Logger.Get<Armi>().LogDebug("Upgrade epico fallito: {Arma} già potenziata", a.Nome);
            return;
        }else if(a.LivelloRarità == 1)
        {
        }
        a.LivelloRarità = 2;
        a.potenza = (int)Math.Floor(a.potenza * 1.5f);
        a.stamina += 1;
        Logger.Get<Armi>().LogInformation("Arma potenziata a Epica: {Arma} (potenza: {Potenza})", a.Nome, a.potenza);
    }

    /// <summary>
    /// Potenzia l'arma al livello Leggendario (3): aumenta la potenza del 50%.
    /// </summary>
    /// <param name="a">Arma da potenziare.</param>
    /// <exception cref="Exception">Se il livello di rarità non è valido.</exception>
    public static void RendiLeggendario(Armi a)
    {
        if(a.LivelloRarità < 0)
        {
            throw new Exception("Livello rarità non valido.");
        }
        a.LivelloRarità = 3;
        a.potenza = (int)Math.Floor(a.potenza * 1.5f);
        Logger.Get<Armi>().LogInformation("Arma potenziata a Leggendaria: {Arma} (potenza: {Potenza})", a.Nome, a.potenza);
    }
}

/// <summary>
/// Rappresenta un oggetto consumabile (pozioni, cibo) che ripristina punti vita e/o stamina
/// in percentuale rispetto ai valori massimi del giocatore.
/// </summary>
public class Consumabili : Oggetto
{
    /// <summary>Peso del consumabile nell'inventario.</summary>
    public int peso=1;

    /// <summary>Prezzo in oro se acquistato dal mercante, o <c>null</c> se non acquistabile.</summary>
    public int? prezzo;

    /// <summary>Valore in oro del consumabile. Derivato da <see cref="prezzo"/>, non serializzato.</summary>
    [JsonIgnore]
    public override int Valore => prezzo ?? 0;

    /// <summary>Percentuale di punti vita massimi da recuperare (0.0 - 1.0).</summary>
    public float recuperoPV = 0f;
    /// <summary>Percentuale di stamina massima da recuperare (0.0 - 1.0).</summary>
    public float recuperoStam = 0f;

    /// <summary>
    /// Utilizza il consumabile: cura PV e/o stamina in base alle percentuali configurate.
    /// </summary>
    public void Usa()
    {
        Logger.Get<Consumabili>().LogDebug("Usato consumabile: {Oggetto}", Nome);
        if(recuperoPV != 0)
        {
            GameManager.Giocatore.Cura((int)Math.Round(GameManager.Giocatore.PuntiVitaMax * recuperoPV));
        }
        if(recuperoStam != 0)
        {
            GameManager.Giocatore.CambiaStamina((int)Math.Round(GameManager.Giocatore.StaminaMax * recuperoStam));
        }
    }

    /// <summary>Crea una Mela (recupera 25% stamina, peso 1, prezzo 5).</summary>
    public static Consumabili Mela()
    {
        Consumabili c = new()
        {
            Nome="Mela",
            Descrizione="frutto che ti da un quarto di stamina",
            peso=1,
            prezzo=5,
            recuperoStam = 0.25f
        };
    return c;
    }

    /// <summary>Crea una Pozione Curativa Base (recupera 25% PV, peso 1, prezzo 5).</summary>
    public static Consumabili Pozione_curativa_base()
    {
        Consumabili c = new()
        {
            Nome="Pozione_curativa_base",
            Descrizione="pozione che ti un quarto di vita",
            peso=1,
            prezzo=5,
            recuperoPV = 0.25f
        };
    return c;
    }

    /// <summary>Crea un Pane (recupera 50% stamina, peso 2, prezzo 12).</summary>
    public static Consumabili Pane()
    {
        Consumabili c = new()
        {
            Nome="Pane",
            Descrizione="Pane molto utile per recuperare meta' stamina",
            peso=2,
            prezzo=12,
            recuperoStam = 0.5f
        };
    return c;
    }

    /// <summary>Crea una Pozione Curativa Media (recupera 50% PV, peso 2, prezzo 12).</summary>
    public static Consumabili Pozione_curativa_media()
    {
        Consumabili c = new()
        {
            Nome="Pozione_curativa_media",
            Descrizione="pozione per recuperare meta' vita",
            peso=2,
            prezzo=12,
            recuperoPV = 0.5f
        };
    return c;
    }

    /// <summary>Crea una Torta (recupera 100% stamina, peso 3, prezzo 30).</summary>
    public static Consumabili Torta()
    {
        Consumabili c = new()
        {
            Nome="Torta",
            Descrizione="Torta molto buona che ti fa recupera tutta la stamina",
            peso=3,
            prezzo=30,
            recuperoStam = 1.0f
        };
    return c;
    }

    /// <summary>Crea una Pozione Recupero Totale (recupera 100% PV, peso 3, prezzo 30).</summary>
    public static Consumabili Pozione_recupero_totale()
    {
        Consumabili c = new()
        {
            Nome="Pozione_recupero_totale",
            Descrizione="Pozione salva vita che recupera tutta la vita",
            peso=3,
            prezzo=30,
            recuperoPV =1.0f
        };
    return c;
    }
}

/// <summary>
/// Wrapper per un oggetto trovabile in una stanza, con flag che indica se è raccoglibile.
/// </summary>
public class OggettoTrovabile
{
    /// <summary>L'oggetto trovabile.</summary>
    public required Oggetto oggetto;
    /// <summary>Indica se l'oggetto è attualmente raccoglibile.</summary>
    public bool IsTrovabile = true;
}


/// <summary>
/// Classe base astratta per le abilità speciali delle armi.
/// Utilizza la serializzazione polimorfa JSON tramite <see cref="JsonDerivedType"/>.
/// </summary>
[JsonDerivedType(typeof(RiflettiScudo), "riflettiScudo")]
[JsonDerivedType(typeof(ColpoPotente), "colpoPotente")]
[JsonDerivedType(typeof(Sanguinamento), "sanguinamento")]
public abstract class AbilitaArma
{
    /// <summary>Nome dell'abilità.</summary>
    public virtual string Nome { get; set; } = "Abilità";
    /// <summary>Descrizione dell'abilità.</summary>
    public virtual string Descrizione { get; set; } = "";
    /// <summary>Costo in stamina per usare l'abilità.</summary>
    public virtual int CostoStamina { get; set; } = 0;
    /// <summary>Bersaglio dell'abilità (opzionale).</summary>
    public virtual Target TargetAbilita { get; set; }
    /// <summary>
    /// Esegue l'abilità. Da implementare nelle classi derivate.
    /// </summary>
    /// <param name="owner">Il proprietario dell'abilità (tipicamente il giocatore).</param>
    /// <param name="target">Il bersaglio dell'abilità (tipicamente un nemico).</param>
    public abstract void Esegui(object? owner, object? target);
}
public class RiflettiScudo : AbilitaArma
{
    public override string Nome {get; set;} = "Rifletti";
    public override string Descrizione { get; set;} = "Rifletti parte del prossimo attacco all'avversario";
    public override int CostoStamina { get; set; } = 4;
    public override Target TargetAbilita { get; set; } = Target.Nemico;

    public override void Esegui(object? owner, object? targetnem)
    {
        var giocatore = (Giocatore)owner!;
        var nemico = (Nemico)targetnem!;

        UI.MostraMessaggio($"Alzi lo scudo, pronto a riflettere il prossimo colpo!");

        var effetto = new StatusEffect
        {
            Name = "Rifletti Scudo",
            target = Target.Giocatore,
            turniRimanenti = 2
        };

        effetto.onDamaged = (sender, danno, attaccante) =>
        {
            int riflesso = danno / 2;
            nemico.Danneggia(riflesso);
            UI.MostraMessaggio($"Rifletti {riflesso} danni!");
            giocatore.StatusEffects.Remove(effetto); 
            return danno - riflesso;
        };

        giocatore.StatusEffects.Add(effetto);
    }
}

/// <summary>
/// Abilità della Spada: carica un colpo potente che raddoppia il danno del prossimo attacco.
/// Applica un bonus al <see cref="Giocatore.ModificatoreDanno"/> pari alla potenza dell'arma,
/// della durata di 2 turni (l'attacco corrente + il successivo).
/// </summary>
public class ColpoPotente : AbilitaArma
{
    public override string Nome {get; set;} = "Colpo Potente";
    public override string Descrizione { get; set;} = "Raddoppia il danno del prossimo attacco";
    public override int CostoStamina { get; set; } = 3;
    public override Target TargetAbilita { get; set; } = Target.Nemico;

    public override void Esegui(object? owner, object? targetnem)
    {
        var giocatore = (Giocatore)owner!;
        var nemico = (Nemico)targetnem!;

        Armi arma = giocatore.Arma!;
        int potenzaArma = arma.potenza;

        UI.MostraMessaggio($"Concentri la forza nel prossimo fendente!");

        var effetto = new StatusEffect
        {
            Name = "Colpo Potente",
            target = Target.Giocatore,
            turniRimanenti = 2
        };

        giocatore.ModificatoreDanno += potenzaArma;
        effetto.onRemove = (sender) => giocatore.ModificatoreDanno -= potenzaArma;

        giocatore.StatusEffects.Add(effetto);
    }
}

/// <summary>
/// Abilità del Coltello: infligge una ferita che causa 2 danni da sanguinamento
/// all'inizio di ogni turno del nemico, per 3 turni.
/// </summary>
public class Sanguinamento : AbilitaArma
{
    public override string Nome {get; set;} = "Sanguinamento";
    public override string Descrizione { get; set;} = "Infligge 2 danni da sanguinamento per 3 turni";
    public override int CostoStamina { get; set; } = 2;
    public override Target TargetAbilita { get; set; } = Target.Nemico;

    public override void Esegui(object? owner, object? targetnem)
    {
        var giocatore = (Giocatore)owner!;
        var nemico = (Nemico)targetnem!;

        UI.MostraMessaggio($"Il coltello affonda nella carne di {nemico.Nome}!");

        var effetto = new StatusEffect
        {
            Name = "sanguinamento",
            target = Target.Nemico,
            turniRimanenti = 3
        };

        effetto.onTurnStart = (sender) =>
        {
            nemico.Danneggia(2);
            UI.MostraMessaggio($"{nemico.Nome} sanguina!");
        };

        nemico.statusEffects.Add(effetto);
    }
}


