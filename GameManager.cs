namespace ProgEsameUniVPM;

public static class GameManager
{
    public static Giocatore Giocatore { get; set; } = null!;
    public static Stanza StanzaCorrente {get; set;} = Stanza.StanzaIniziale();
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
}
