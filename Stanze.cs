using System;
using System.Runtime.InteropServices.Marshalling;
using ProgEsameUniVPM;

public class Stanza
{
    public required string Nome, Descrizione;

    public List<OggettoTrovabile> OggettiStanza = new();

    public required int Livello;

    private Dictionary<string, string> Frasi = new();

    public Dictionary<string, Action> Azioni = new();   

    /*
    private List<Oggetto> Tesori;

    public Stanza()
    {
        Tesori.Add(Consumabili.Pane());
        Tesori.Add(Consumabili.Mela());
        Tesori.Add(Armi.coltello());
    }
    */
    public static Stanza StanzaDelTesoro()
    {
        Stanza s = new Stanza()
        {
            Nome = "Stanza del Tesoro.",
            Descrizione = "Tesorooooo",
            Livello = 0,
        };
        /*
        var r = new Random();
        int rint = r.Next(ListeStanze.Tesori.Count());
        
        OggettoTrovabile ogg = new(){oggetto = ListeStanze.Tesori[rint], IsTrovabile = true};
        s.OggettiStanza.Add(ogg);*/

        s.Frasi.Add("entrata", "Entrando nella stanza, vedi vari oggetti di valore attorno a te.");
        s.Azioni.Add("raccogli", () =>
        {
            GameManager.Giocatore.AggiungiOro(20);
        }
        );

        return s;
    }

    public static Stanza Armeria()
    {
        Stanza s = new Stanza()
        {
            Nome = "Armeria",
            Descrizione = "Armeria dove puoi migliorare la tua arma.",
            Livello = 0,
        };
        s.Azioni.Add("migliora", () => {
                if(GameManager.Giocatore.Arma == null)
                {
                    throw new NullReferenceException();
                }
                Armi.RendiRara(GameManager.Giocatore.Arma);
            });
        return s;
    }

    public static Stanza Cantina()
    {
        Stanza s = new Stanza()
        {
            Nome = "Cantina",
            Descrizione = "rifornimento cibo e pozioni",
            Livello = 0
        };
        s.Azioni.Add("raccogli", () =>
        {
            GameManager.Giocatore.AggiungiOggettoInventario(Consumabili.Pozione_curativa_base());
            //todo: logica di oggetto random.
        });
        return s;
    }

    public static Stanza Cura()
    {
        Stanza s = new Stanza()
        {
            Nome = "Stanza Curativa",
            Descrizione = "cura giocatore",
            Livello = 0
        };
        s.Azioni.Add("cura", () =>
        {
            GameManager.Giocatore.Cura(10);
        });
        return s;
    }

    public static Stanza Inceneritore()
    {
        Stanza s = new Stanza()
        {
            Nome = "Inceneritore",
            Descrizione = "fuoco",
            Livello = 0
        };
        s.Azioni.Add("entra", () =>
        {
            GameManager.Giocatore.Danneggia(2);
            GameManager.Giocatore.RimuoviOggettoInventario();
        });
        return s;
    }

    public static Stanza BucoTrappola()
    {
        Stanza s = new Stanza()
        {
            Nome = "Stanza Teletrasporto",
            Descrizione = "",
            Livello = 0
        };
        s.Azioni.Add("entra", () =>
        {
            if(StanzeVisitate.ListaVisitate.Count() == 0)
            {
                //genera nuova stanza, vai avanti
            }
            else
            {
                var r = new Random();
                if(r.Next(2) == 1)
                {
                    //vai avanti
                }
                else
                {
                    //vai indietro in una stanza StanzeVisitate.ListaVisitate
                }
            }

        });
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
        if(s.Nome == "Stanza Teletrasporto")
        {
            //todo
        }
        ListaVisitate.Add(s);
    }
}