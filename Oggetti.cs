using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace ProgEsameUniVPM;

//Classe genitore di tutte le sottoclassi inerenti agli ogetti
[JsonDerivedType(typeof(Armi), "arma")]
[JsonDerivedType(typeof(Consumabili), "consumabile")]
[JsonDerivedType(typeof(OggettoChiave), "chiave")]
public class Oggetto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome /*{get; private set;}*/ = "Oggetto";

    public string Descrizione = "Oggetto base";

    // Se non vuoto, l'oggetto è una chiave che apre Porte con questo id
    public string ChiaveId { get; set; } = "";

    // ID stabile per il salvataggio: assegnato a oggetti statici della mappa.
    // Viene usato al posto di Guid nei salvataggi perché Id cambia ad ogni Inizializza().
    public string IdSalvataggio { get; set; } = "";

    public bool isChiave => ChiaveId.Length > 0;
    public int Valore = 0;
}

public class OggettoChiave : Oggetto
{
    public string Serratura { get; set; } = "";

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

// per fare arma personaggio mettere prima plubic class arma: oggetto,{} 
//poi public int potenza/stamina=x(è il valore) int per valore string per scritta 
public class Armi : Oggetto
{
    public int potenza=2;
    public int stamina=2;

    public int LivelloRarità = 0;

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

    public Abiita? AbiitaArma;
    
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

    public static Armi Spada()
    {
        Armi s = new()
        {
            Nome="Spada",
            Descrizione="attacco base fa meno danno ma non spreca stamina e si"+
            "vuole danno critico si spreca stamina",
            potenza=2,
            stamina=2
        };
        return s;
    }
    public static Armi Scudo()
    {
        Armi s = new()
        {
            Nome="Scudo",
            Descrizione="riflette i colpi diretti a costo di stamina"+
            "e recupera stamina difendendosi con lo scudo ma subisce meno danni riflette danno ",
            potenza=1,
            stamina=3
        };
        return s;
    }
    public static Armi coltello()
    {
        Armi s = new()
        {
            Nome="Coltello",
            Descrizione="attacchi base con possibilità bassa di attacare più di 1 volta per turno no stamina"+
             "e se si vuole fare bleed usi stamina ma con il bleed attacchi 1 volta",
            potenza=1,
            stamina=1
        };
        return s;
    }

    public static void RendiRara(Armi a)
    {
        if(a.LivelloRarità < 0)
        {
            throw new Exception("Livello rarità non valido.");
        }

        if(a.LivelloRarità >= 1)
        {
            Console.WriteLine("L'arma " + a.Nome + " è già potenziata!");
            return;
        }
        a.LivelloRarità = 1; // 1 = raro
        a.potenza *= 2;
        a.stamina +=1;
        /*a.Nome += " Rara";
        a.Descrizione += " Questa arma è potenziata e di rarità 'Rara'.";*/
    }
    public static void RendiEpico(Armi a)
    {
        if(a.LivelloRarità < 0)
        {
            throw new Exception("Livello rarità non valido.");
        }
        if(a.LivelloRarità >= 2)
        {
            Console.WriteLine("L'arma " + a.Nome + " è già potenziata!");
            return;
        }else if(a.LivelloRarità == 1)
        {
            // r0 -> r1
        }
        a.LivelloRarità = 2; // 2 = epico
        a.potenza = (int)Math.Floor(a.potenza * 1.5f);
        a.stamina += 1;
        /*a.Nome += " Epico";
        a.Descrizione += " Questa arma è molto potenziata e di rarità 'Epica'.";*/
    }
    public static void RendiLeggendario(Armi a)
    {
        if(a.LivelloRarità < 0)
        {
            throw new Exception("Livello rarità non valido.");
        }
        // 2 > 4 > 6 > 9
        // 1 > 2 > 3 > 4
        // 3 > 6 > 9 > 13
        a.LivelloRarità = 3; // 2 = epico
        a.potenza = (int)Math.Floor(a.potenza * 1.5f);
        // a.stamina += 1;
        /*a.Nome += " Leggendario";
        a.Descrizione += " Questa arma è suprema, di rarità 'Leggendaria'.";*/
    }
}

public class Consumabili : Oggetto
{
    public int peso=1;

    public int? prezzo;

    public float recuperoPV = 0f;
    public float recuperoStam = 0f;
    public void Usa()
    {
        if(recuperoPV != 0)
        {
            GameManager.Giocatore.Cura((int)Math.Round(GameManager.Giocatore.PuntiVitaMax * recuperoPV));
        }
        if(recuperoStam != 0)
        {
            GameManager.Giocatore.CambiaStamina((int)Math.Round(GameManager.Giocatore.StaminaMax * recuperoStam));
        }
    }

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

public class OggettoTrovabile
{
    public required Oggetto oggetto;
    public bool IsTrovabile = true;
}

public abstract class Abiita
{
    public string Nome { get; set; } = "Abilità";
    public string Descrizione { get; set; } = "";
    public int CostoStamina { get; set; } = 0;
    public string? Target { get; set; }
    public abstract void Esegui(object? owner, object? target);
}

