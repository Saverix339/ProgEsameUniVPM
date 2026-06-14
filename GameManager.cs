namespace ProgEsameUniVPM;
using Microsoft.Extensions.Logging;

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
        var vecchio = StatoGioco?.GetType().Name ?? "null";
        var nuovo = Cambio.GetType().Name;
        Logger.For("GameManager").LogDebug("Transizione stato: {Vecchio} -> {Nuovo}", vecchio, nuovo);
        StatoGioco?.esci();
        StatoGioco = Cambio;
        StatoGioco.entra();
    }

    public static void Sposta(Direzione direzione)
    {
        var log = Logger.For("GameManager");
        var corrente = StanzaCorrente;
        var target = Mappa.Verso(corrente.Coordinate + direzione.ToDelta());
        if (target is null)
        {
            log.LogDebug("Movimento fallito: nessuna stanza in direzione {Direzione}", direzione);
            UI.MostraErrore("Non c'è nulla in quella direzione.");
            return;
        }

        if (corrente.Porte.TryGetValue(direzione, out var porta))
        {
            if (porta.Stato == StatoPorta.Bloccata)
            {
                if (porta.ChiaveRichiesta is null || !Giocatore.HaChiave(porta.ChiaveRichiesta))
                {
                    log.LogWarning("Porta bloccata verso {Direzione} (serve chiave: {Chiave})", direzione, porta.ChiaveRichiesta ?? "nessuna");
                    UI.MostraErrore("La porta è bloccata.");
                    return;
                }
                porta.Stato = StatoPorta.Aperta;
                if (target.Porte.TryGetValue(direzione.Opposta(), out var portaAlLatoOpposto))
                {
                    portaAlLatoOpposto.Stato = StatoPorta.Aperta;
                }
                log.LogInformation("Porta sbloccata con chiave {Chiave} verso {Direzione}", porta.ChiaveRichiesta, direzione);
                UI.MostraMessaggio($"Sblocchi la porta con la chiave ({porta.ChiaveRichiesta}).");
            }
        }

        log.LogInformation("Spostamento: {Da} -> {A} ({Direzione})", corrente.Nome, target.Nome, direzione);
        StanzaCorrente = target;
        var esplorazione = new EsplorazioneStanza(StanzaCorrente);
        if(StanzaCorrente.NemicoStanza is not null && !StanzaCorrente.NemicoSconfitto)
        {
            log.LogInformation("Nemico incontrato nella stanza: {Nemico}", StanzaCorrente.NemicoStanza.Nome);
            CambiaStato(new Combattimento(esplorazione, StanzaCorrente.NemicoStanza));
        }
        CambiaStato(esplorazione);
    }
}
