namespace ProgEsameUniVPM;
using Microsoft.Extensions.Logging;

/// <summary>
/// Gestore globale dello stato di gioco (pattern Singleton statico).
/// Mantiene il riferimento al giocatore, alla stanza corrente e allo stato attivo della state machine.
/// </summary>
public static class GameManager
{
    /// <summary>Il giocatore controllato dall'utente.</summary>
    public static Giocatore Giocatore { get; set; } = null!;
    /// <summary>La stanza in cui si trova attualmente il giocatore.</summary>
    public static Stanza StanzaCorrente {get; set;} = Mappa.Verso(new Coord(0, 0)) ?? Stanza.Ingresso(new Coord(0, 0));
    /// <summary>Lo stato attivo della state machine di gioco (es. esplorazione, combattimento, mercante).</summary>
    public static IStato? StatoGioco = new CreazionePersonaggio();

    /// <summary>
    /// Metodo placeholder per avanzamento (non implementato).
    /// </summary>
    public static void Avanza()
    {

    }
    /// <summary>
    /// Effettua una transizione di stato: esce dallo stato corrente, imposta il nuovo stato
    /// e chiama il suo metodo <see cref="IStato.entra"/>.
    /// </summary>
    /// <param name="Cambio">Nuovo stato da attivare.</param>
    public static void CambiaStato(IStato Cambio)
    {
        var vecchio = StatoGioco?.GetType().Name ?? "null";
        var nuovo = Cambio.GetType().Name;
        Logger.For("GameManager").LogDebug("Transizione stato: {Vecchio} -> {Nuovo}", vecchio, nuovo);
        StatoGioco?.esci();
        StatoGioco = Cambio;
        StatoGioco.entra();
    }

    /// <summary>
    /// Sposta il giocatore nella direzione specificata, gestendo porte (bloccate/chiuse),
    /// consumo di stamina, e attivazione di combattimenti se la stanza di destinazione ha un nemico.
    /// </summary>
    /// <param name="direzione">Direzione verso cui spostarsi (Nord, Sud, Est, Ovest).</param>
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
        Giocatore.CambiaStamina(2);
        var esplorazione = new EsplorazioneStanza(StanzaCorrente);
        if(StanzaCorrente.NemicoStanza is not null && !StanzaCorrente.NemicoSconfitto)
        {
            log.LogInformation("Nemico incontrato nella stanza: {Nemico}", StanzaCorrente.NemicoStanza.Nome);
            CambiaStato(new Combattimento(esplorazione, StanzaCorrente.NemicoStanza));
            return;
        }
        CambiaStato(esplorazione);
    }
}
