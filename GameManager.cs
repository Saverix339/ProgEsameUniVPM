namespace ProgEsameUniVPM;

public static class GameManager
{
    public static Giocatore Giocatore { get; set; } = null!;
    public static Stanza StanzaCorrente {get; set;} = Mappa.Verso(new Coord(0, 0)) ?? Stanza.Ingresso(new Coord(0, 0));
    public static IStato? StatoGioco = new CreazionePersonaggio();

    public static void Avanza()
    {

    }
    public static void CambiaStato(IStato Cambio)
    {
        StatoGioco?.esci();
        StatoGioco = Cambio;
        StatoGioco.entra();
    }

    public static void Sposta(Direzione direzione)
    {
        var corrente = StanzaCorrente;
        var target = Mappa.Verso(corrente.Coordinate + direzione.ToDelta());
        if (target is null)
        {
            UI.MostraErrore("Non c'è nulla in quella direzione.");
            return;
        }

        // Controllo porta sul lato "da" (e per simmetria, anche sul lato "a")
        if (corrente.Porte.TryGetValue(direzione, out var porta))
        {
            if (porta.Stato == StatoPorta.Bloccata)
            {
                if (porta.ChiaveRichiesta is null || !Giocatore.HaChiave(porta.ChiaveRichiesta))
                {
                    UI.MostraErrore("La porta è bloccata.");
                    return;
                }
                // Sblocca la porta da entrambi i lati
                porta.Stato = StatoPorta.Aperta;
                if (target.Porte.TryGetValue(direzione.Opposta(), out var portaAlLatoOpposto))
                {
                    portaAlLatoOpposto.Stato = StatoPorta.Aperta;
                }
                UI.MostraMessaggio($"Sblocchi la porta con la chiave ({porta.ChiaveRichiesta}).");
            }
        }

        StanzaCorrente = target;
        var esplorazione = new EsplorazioneStanza(StanzaCorrente);
        if(StanzaCorrente.NemicoStanza is not null && !StanzaCorrente.NemicoSconfitto)
        {
            CambiaStato(new Combattimento(esplorazione, StanzaCorrente.NemicoStanza));
        }
        CambiaStato(esplorazione);
    }
}
