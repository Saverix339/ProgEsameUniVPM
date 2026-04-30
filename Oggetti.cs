using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.Arm;

namespace ProgEsameUniVPM;

//Classe genitore di tutte le sottoclassi inerenti agli ogetti
public class Oggetto
{
    public int Id /*{get; private set;}*/ = 0;
    public string Nome /*{get; private set;}*/ = "Oggetto";

    public string Descrizione = "Oggetto base";

    static int LastId = 0;

}

// per fare arma personaggio mettere prima plubic class arma: oggetto,{} 
//poi public int potenza/stamina=x(è il valore) int per valore string per scritta 
public class Armi : Oggetto
{
    public int potenza=2;
    public int stamina=2;
    
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
}

public class Consumabili : Oggetto
{
    public int peso=1;

    public int? prezzo;

public static Consumabili Mela()
    {
        Consumabili c = new()
        {
            Nome="Mela",
            Descrizione="frutto che ti da un quarto di stamina",
            peso=1,
            prezzo=5
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
            prezzo=5
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
            prezzo=12
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
            prezzo=12
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
            prezzo=30
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
            prezzo=30
        };
    return c;
    }
}

