using System;
using ProgEsameUniVPM;

public class Stanza
{
    public required string Nome, Descrizione;

    public List<OggettoTrovabile> OggettiStanza = new();

    public required int Livello;

    private List<Oggetto> Tesori;

    public Stanza()
    {
        Tesori.Add(Consumabili.Pane());
        Tesori.Add(Consumabili.Mela());
        Tesori.Add(Armi.coltello());
    }

    public Stanza StanzaDelTesoro()
    {
        Stanza s = new Stanza()
        {
            Nome = "Stanza del Tesoro.",
            Descrizione = "Tesorooooo",
            Livello = 0,
        };
        var r = new Random();
        int rint = r.Next(Tesori.Count());
        OggettoTrovabile ogg = new(){oggetto = Tesori[rint], IsTrovabile = true};
        s.OggettiStanza.Add(ogg);
        return s;
    }
} 